using Sandbox;
using System;

public sealed class BanListController : Component
{
	private const string DefaultBanReason = "No reason was provided.";

	[Property] public bool BanListEnabled { get; set; } = true;
	[Property] public float KickDelay { get; set; } = 1.5f;
	[Property] public List<BanListEntry> BannedPlayers { get; set; } = new();

	public static bool IsBanNoticeVisible { get; private set; }
	public static string BanNoticeReason { get; private set; } = DefaultBanReason;
	public static int NoticeVersion { get; private set; }
	public static BanListController Instance { get; private set; }

	private readonly Dictionary<Guid, PendingKick> pendingKicks = new();
	private float nextConnectionCheckTime;
	private float localDisconnectTime;
	private bool localBanProcessed;

	protected override void OnStart()
	{
		Instance = this;
		ResetSessionBanState();
		CheckLocalBan();
		CheckRemoteConnections();
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
			Instance = null;
	}

	/// <summary>
	/// Host-side runtime ban: adds the SteamId to the ban list so the normal enforcement loop
	/// kicks them (and blocks rejoin for the session). Call from an admin-verified host RPC.
	/// Note: this is session-only unless the SteamId is also added to the scene config.
	/// </summary>
	public static void AddRuntimeBan( long steamId, string reason )
	{
		var instance = Instance;
		if ( instance == null || !Networking.IsHost || steamId <= 0 )
			return;

		instance.BannedPlayers ??= new();
		if ( !instance.BannedPlayers.Any( entry => entry != null && entry.SteamId == steamId ) )
		{
			instance.BannedPlayers.Add( new BanListEntry
			{
				SteamId = steamId,
				Reason = string.IsNullOrWhiteSpace( reason ) ? "Banned by an admin." : reason.Trim()
			} );
		}

		instance.nextConnectionCheckTime = 0f; // enforce on the next update tick
	}

	protected override void OnUpdate()
	{
		if ( !BanListEnabled )
		{
			ClearBanNoticeLocal();
			return;
		}

		CheckLocalBan();
		UpdateLocalDisconnect();

		if ( !Networking.IsHost )
			return;

		if ( Time.Now >= nextConnectionCheckTime )
		{
			nextConnectionCheckTime = Time.Now + 1f;
			CheckRemoteConnections();
		}

		UpdatePendingKicks();
	}

	private void CheckRemoteConnections()
	{
		if ( !BanListEnabled || !Networking.IsHost )
			return;

		foreach ( var connection in Connection.All )
		{
			if ( connection == null || !connection.IsActive || connection.IsHost )
				continue;

			if ( pendingKicks.ContainsKey( connection.Id ) )
				continue;

			if ( !TryGetBan( connection.SteamId, out var ban ) )
				continue;

			var reason = NormalizeReason( ban.Reason );
			using ( Rpc.FilterInclude( connection ) )
			{
				ShowBanNotice( reason );
			}

			pendingKicks[connection.Id] = new PendingKick( connection, Time.Now + MathF.Max( KickDelay, 0.1f ), reason );
		}
	}

	private void CheckLocalBan()
	{
		if ( localBanProcessed || !BanListEnabled )
			return;

		if ( !TryGetBan( Game.SteamId, out var ban ) )
		{
			ClearBanNoticeLocal();
			return;
		}

		localBanProcessed = true;
		var reason = NormalizeReason( ban.Reason );
		ShowBanNoticeLocal( reason );
		localDisconnectTime = Time.Now + MathF.Max( KickDelay, 0.1f );
	}

	private void UpdatePendingKicks()
	{
		if ( pendingKicks.Count == 0 )
			return;

		foreach ( var pair in pendingKicks.ToArray() )
		{
			if ( Time.Now < pair.Value.KickAt )
				continue;

			pair.Value.Connection?.Kick( $"You have been banned: {pair.Value.Reason}" );
			pendingKicks.Remove( pair.Key );
		}
	}

	private void UpdateLocalDisconnect()
	{
		if ( localDisconnectTime <= 0f || Time.Now < localDisconnectTime )
			return;

		localDisconnectTime = 0f;
		Game.Disconnect();
	}

	private bool TryGetBan( long steamId, out BanListEntry ban )
	{
		ban = null;
		if ( steamId <= 0 || BannedPlayers == null )
			return false;

		ban = BannedPlayers.FirstOrDefault( entry => entry != null && entry.SteamId == steamId );
		return ban != null;
	}

	private static string NormalizeReason( string reason )
	{
		return string.IsNullOrWhiteSpace( reason ) ? DefaultBanReason : reason.Trim();
	}

	[Rpc.Broadcast( NetFlags.Reliable | NetFlags.SendImmediate )]
	private void ShowBanNotice( string reason )
	{
		ShowBanNoticeLocal( reason );
	}

	private static void ShowBanNoticeLocal( string reason )
	{
		IsBanNoticeVisible = true;
		BanNoticeReason = NormalizeReason( reason );
		NoticeVersion++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}

	private void ResetSessionBanState()
	{
		pendingKicks.Clear();
		localDisconnectTime = 0f;
		localBanProcessed = false;
		ClearBanNoticeLocal( forceVersionBump: true );
	}

	private static void ClearBanNoticeLocal( bool forceVersionBump = false )
	{
		if ( !IsBanNoticeVisible && !forceVersionBump )
			return;

		IsBanNoticeVisible = false;
		BanNoticeReason = DefaultBanReason;
		NoticeVersion++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}

	private sealed record PendingKick( Connection Connection, float KickAt, string Reason );
}

public sealed class BanListEntry
{
	[Property] public long SteamId { get; set; }
	[Property, TextArea] public string Reason { get; set; } = "Banned from this playtest.";
}
