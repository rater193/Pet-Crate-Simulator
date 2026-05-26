using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Distance-based model render culling. Disables <see cref="ModelRenderer"/> components whose
/// GameObject is further than <see cref="CullDistance"/> from the active camera, and re-enables
/// them when they come back in range (with hysteresis to avoid flicker at the boundary).
///
/// Only renderers THIS controller culled are ever re-enabled, so it never fights other systems
/// that disable a renderer for their own reasons. Because <c>Scene.GetAllComponents&lt;T&gt;()</c>
/// returns only ENABLED components, we keep a persistent registry (a renderer we disabled would
/// otherwise vanish from the scan and never come back).
///
/// Console: <c>perf_cull</c> toggles it, <c>perf_culldist &lt;n&gt;</c> sets the distance.
/// </summary>
public sealed class RenderCullingController : Component
{
	[Property] public bool CullingEnabled { get; set; } = true;

	/// <summary>Renderers further than this from the camera are hidden (world units).</summary>
	[Property, Range( 256f, 20000f )] public float CullDistance { get; set; } = 4000f;

	/// <summary>Re-enable fraction: a culled renderer comes back when it's within CullDistance * this.</summary>
	[Property, Range( 0.5f, 0.99f )] public float HysteresisFraction { get; set; } = 0.92f;

	/// <summary>How often we re-evaluate distances over the cached renderer set (seconds).</summary>
	[Property] public float ScanInterval { get; set; } = 0.2f;

	/// <summary>How often we re-scan the scene for newly spawned renderers (seconds).</summary>
	[Property] public float RegistryRefreshInterval { get; set; } = 2f;

	/// <summary>GameObjects with any of these tags are never culled (e.g. the local player).</summary>
	[Property] public List<string> NeverCullTags { get; set; } = new() { "player" };

	public static RenderCullingController Instance { get; private set; }

	// Stats / state read by the perf UI.
	public static bool IsActive { get; private set; }
	public static float ActiveCullDistance { get; private set; }
	public static int TrackedRenderers { get; private set; }
	public static int CulledRenderers { get; private set; }

	private readonly HashSet<ModelRenderer> tracked = new();
	private readonly HashSet<ModelRenderer> culledByUs = new();
	private float scanTimer;
	private float registryTimer;

	protected override void OnStart()
	{
		Instance = this;
		RefreshRegistry();
	}

	protected override void OnDestroy()
	{
		// Be a good citizen: restore anything we hid before going away.
		RestoreAllCulled();

		if ( Instance == this )
			Instance = null;
	}

	protected override void OnUpdate()
	{
		ActiveCullDistance = CullDistance;

		if ( !CullingEnabled )
		{
			if ( IsActive )
			{
				RestoreAllCulled();
				IsActive = false;
				CulledRenderers = 0;
			}
			return;
		}

		IsActive = true;

		registryTimer -= Time.Delta;
		if ( registryTimer <= 0f )
		{
			registryTimer = MathF.Max( 0.25f, RegistryRefreshInterval );
			RefreshRegistry();
		}

		scanTimer -= Time.Delta;
		if ( scanTimer <= 0f )
		{
			scanTimer = MathF.Max( 0.05f, ScanInterval );
			ScanAndCull();
		}
	}

	/// <summary>Merge newly-found (enabled) renderers into the registry; prune destroyed ones.</summary>
	private void RefreshRegistry()
	{
		if ( Scene == null )
			return;

		foreach ( var renderer in Scene.GetAllComponents<ModelRenderer>() )
		{
			if ( renderer.IsValid() && !IsNeverCull( renderer ) )
				tracked.Add( renderer );
		}

		tracked.RemoveWhere( r => !r.IsValid() );
		culledByUs.RemoveWhere( r => !r.IsValid() );
		TrackedRenderers = tracked.Count;
	}

	private void ScanAndCull()
	{
		var camera = Scene?.Camera;
		if ( !camera.IsValid() )
			return;

		var cameraPosition = camera.WorldPosition;
		var cullSquared = CullDistance * CullDistance;
		var restoreDistance = CullDistance * HysteresisFraction;
		var restoreSquared = restoreDistance * restoreDistance;

		foreach ( var renderer in tracked )
		{
			if ( !renderer.IsValid() )
				continue;

			var distanceSquared = (renderer.GameObject.WorldPosition - cameraPosition).LengthSquared;

			if ( distanceSquared > cullSquared )
			{
				// Too far: hide it (only if it's currently visible and not already ours-disabled).
				if ( renderer.Enabled )
				{
					renderer.Enabled = false;
					culledByUs.Add( renderer );
				}
			}
			else if ( distanceSquared < restoreSquared && culledByUs.Contains( renderer ) )
			{
				// Back in range: restore only renderers WE hid.
				renderer.Enabled = true;
				culledByUs.Remove( renderer );
			}
		}

		culledByUs.RemoveWhere( r => !r.IsValid() );
		CulledRenderers = culledByUs.Count;
	}

	private void RestoreAllCulled()
	{
		foreach ( var renderer in culledByUs )
		{
			if ( renderer.IsValid() )
				renderer.Enabled = true;
		}

		culledByUs.Clear();
		CulledRenderers = 0;
	}

	private bool IsNeverCull( ModelRenderer renderer )
	{
		if ( NeverCullTags == null || NeverCullTags.Count == 0 )
			return false;

		var go = renderer.GameObject;
		if ( !go.IsValid() )
			return false;

		foreach ( var tag in NeverCullTags )
		{
			if ( !string.IsNullOrWhiteSpace( tag ) && go.Tags.Has( tag ) )
				return true;
		}

		return false;
	}

	// --- UI / console hooks ---

	public static void ToggleCulling()
	{
		if ( !Instance.IsValid() )
			return;

		Instance.CullingEnabled = !Instance.CullingEnabled;
		AdminPerfController.RequestRefresh();
	}

	public static void AdjustCullDistance( float delta )
	{
		if ( !Instance.IsValid() )
			return;

		Instance.CullDistance = MathF.Max( 256f, Instance.CullDistance + delta );
		AdminPerfController.RequestRefresh();
	}

	[ConCmd( "perf_cull" )]
	public static void Cmd_ToggleCulling()
	{
		ToggleCulling();
		Log.Info( $"[perf] Render culling {(Instance?.CullingEnabled == true ? "ENABLED" : "disabled")}" );
	}

	[ConCmd( "perf_culldist" )]
	public static void Cmd_SetCullDistance( float distance )
	{
		if ( !Instance.IsValid() )
			return;

		Instance.CullDistance = MathF.Max( 256f, distance );
		Log.Info( $"[perf] Cull distance = {Instance.CullDistance:0}" );
		AdminPerfController.RequestRefresh();
	}
}
