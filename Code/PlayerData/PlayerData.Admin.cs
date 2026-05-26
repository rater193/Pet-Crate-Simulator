using Sandbox;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Admin / moderation tooling that lives on the player (already networked + owner-driven).
/// Driven by the Admin tab in the performance menu (PerfExplorerPanel), gated to admins.
///
/// IMPORTANT: this deliberately uses ONLY engine APIs already exercised elsewhere in this project
/// (Scene.Camera.WorldRotation, WorldPosition, Components.GetAll&lt;ModelRenderer&gt;, ModelRenderer.Enabled,
/// Connection.*, Rpc.*). Introducing a never-before-used engine member here previously took the whole
/// gamemode assembly down via the sbox API whitelist (which `dotnet build` does NOT enforce).
/// </summary>
public sealed partial class PlayerData
{
	/// <summary>Synced so other clients hide this player's model when an admin goes invisible.</summary>
	[Sync] public bool IsInvisible { get; set; }

	private const float FlySpeed = 650f;

	private bool flyMode;
	private Vector3 flyPosition;
	private List<ModelRenderer> adminRenderers;
	private bool? lastInvisibleApplied;

	public bool FlyEnabled => flyMode;
	public bool InvisibleEnabled => IsInvisible;

	// Called every frame from OnUpdate (for ALL clients, including proxies).
	private void AdminUpdate()
	{
		UpdateInvisibility();

		if ( !IsProxy && flyMode )
			UpdateFly();
	}

	// --- Fly (local player only) ---

	public void ToggleFly()
	{
		if ( IsProxy )
			return;

		flyMode = !flyMode;
		if ( flyMode )
			flyPosition = WorldPosition;
	}

	private void UpdateFly()
	{
		var camera = Scene?.Camera;
		if ( !camera.IsValid() )
			return;

		// Drive movement from the camera facing; track our own fly position and write WorldPosition
		// every frame so gravity/walk are overridden. Uses only whitelisted APIs (no controller internals).
		var rotation = camera.WorldRotation;
		var move = Input.AnalogMove; // x = forward, y = left
		var wish = (rotation.Forward * move.x) + (rotation.Left * move.y);

		if ( Input.Down( "Jump" ) ) wish += Vector3.Up;
		if ( Input.Down( "Duck" ) ) wish += Vector3.Down;

		if ( wish.LengthSquared > 0.0001f )
			flyPosition += wish.Normal * FlySpeed * Time.Delta;

		WorldPosition = flyPosition;
	}

	// --- Invisibility (synced; hidden from other clients only) ---

	public void ToggleInvisible()
	{
		if ( IsProxy )
			return; // only the owner flips their own synced flag

		IsInvisible = !IsInvisible;
	}

	private void UpdateInvisibility()
	{
		// Hide from OTHER clients (where this player is a proxy) but keep the owner able to see
		// themselves, so an invisible admin can still navigate.
		var shouldHide = IsInvisible && IsProxy;

		var renderersValid = adminRenderers != null && adminRenderers.All( r => r.IsValid() );
		if ( lastInvisibleApplied == shouldHide && renderersValid )
			return;

		if ( !renderersValid )
			adminRenderers = Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ).ToList();

		foreach ( var renderer in adminRenderers )
		{
			if ( renderer.IsValid() )
				renderer.Enabled = !shouldHide;
		}

		lastInvisibleApplied = shouldHide;
	}

	// --- Give money / pet (run on the target player's owner) ---

	[Rpc.Owner]
	public void AdminGiveMoney( int amount )
	{
		if ( IsProxy || amount == 0 )
			return;

		PlayerMoney = (int)System.Math.Clamp( (long)PlayerMoney + amount, 0, int.MaxValue );
		QueueSave();
	}

	[Rpc.Owner]
	public void AdminGivePet( string prefabPath, int rarity )
	{
		if ( IsProxy || string.IsNullOrWhiteSpace( prefabPath ) )
			return;

		var prefab = GameObject.GetPrefab( prefabPath );
		if ( !prefab.IsValid() )
		{
			Log.Warning( $"[Admin] Could not resolve pet prefab '{prefabPath}' to give." );
			return;
		}

		inventory ??= GetComponent<Inventory>() ?? GameObject.GetOrAddComponent<Inventory>();
		inventory?.AddPetPrefab( prefab, null, (PetRarity)rarity );
	}

	// --- Moderation (run on the host, admin-verified) ---

	[Rpc.Host]
	public void AdminRequestKick( long targetSteamId )
	{
		if ( !CallerIsAdmin() )
			return;

		var connection = Connection.All.FirstOrDefault( c => c != null && (long)c.SteamId == targetSteamId && !c.IsHost );
		connection?.Kick( "Kicked by an admin." );
	}

	[Rpc.Host]
	public void AdminRequestBan( long targetSteamId, string reason )
	{
		if ( !CallerIsAdmin() )
			return;

		BanListController.AddRuntimeBan( targetSteamId, reason );
	}

	private static bool CallerIsAdmin()
	{
		var caller = Rpc.Caller;
		var steamId = caller != null ? (long)caller.SteamId : (long)Game.SteamId;
		return AdminPerfController.IsAdmin( steamId );
	}
}
