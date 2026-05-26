using Sandbox;
using System;
using System.Linq;

/// <summary>
/// Client-local admin performance inspector. Gated by SteamId like <see cref="PlayerNoticeController"/>
/// and <see cref="BanListController"/>. When a listed admin presses the toggle (the "Menu" input,
/// default Q) the read-only <c>AdminPerfPanel</c> overlay appears with live FPS plus the scene
/// counts that scale with player count (the usual cause of host-fine / clients-laggy).
///
/// The overlay itself is read-only (pointer-events: none) so it never grabs the cursor / breaks
/// mouse-look. Live "bisect" toggles are exposed as console commands so an admin can switch
/// suspected per-frame systems off in-game and watch the FPS recover:
///   perf_overlay   - toggle the overlay
///   perf_pets      - toggle proxy pet animation (the per-other-player cost)
///   perf_rarityfx  - toggle the Legendary+ rarity aura/outline per-frame update
/// </summary>
public sealed class AdminPerfController : Component
{
	[Property] public bool TrackingEnabled { get; set; } = true;

	/// <summary>Debug escape hatch: when true, every client may open the overlay (ignore the SteamId list).</summary>
	[Property] public bool AllowEveryone { get; set; } = false;

	/// <summary>SteamIds allowed to open the overlay. Mirrors the PlayerNoticeController/BanList config style.</summary>
	[Property] public List<long> AdminSteamIds { get; set; } = new();

	/// <summary>How often the scene counts + displayed readout refresh (seconds).</summary>
	[Property] public float SampleInterval { get; set; } = 0.25f;

	public static AdminPerfController Instance { get; private set; }

	// --- Overlay / admin state (read by AdminPerfPanel) ---
	public static bool IsLocalAdmin { get; private set; }
	public static bool IsOverlayOpen { get; private set; }
	public static int Version { get; private set; }

	// --- Live frame timing ---
	public static float Fps { get; private set; }
	public static float FrameMs { get; private set; }
	public static float LowFps { get; private set; } // worst (lowest) fps seen this window

	// --- Scene snapshot (refreshed every SampleInterval) ---
	public static int PlayerCount { get; private set; }
	public static int ProxyPlayerCount { get; private set; }
	public static int PetFrameworkCount { get; private set; }
	public static int ProxyPetFrameworkCount { get; private set; }
	public static int PetComponentCount { get; private set; }
	public static int DestructibleCount { get; private set; }
	public static int RarityVisualCount { get; private set; }
	public static int WorldPanelCount { get; private set; }
	public static int TotalObjectCount { get; private set; }

	// --- Per-frame averaged work counters (the cost that scales with players) ---
	public static int PetAnimTicksPerFrame { get; private set; }
	public static int GroundTracesPerFrame { get; private set; }

	// --- Bisect toggles read by the hot paths ---
	public static bool DisableProxyPetAnimation { get; private set; }
	public static bool DisableRarityVisualFx { get; private set; }

	// Hot paths increment these every frame; we average them over the sample window.
	public static int PetAnimAccumulator;
	public static int GroundTraceAccumulator;

	private float smoothedDelta = 1f / 60f;
	private float worstDeltaThisWindow;
	private int petAnimWindowAccum;
	private int groundTraceWindowAccum;
	private int framesThisWindow;
	private float sampleTimer;

	protected override void OnStart()
	{
		Instance = this;
		ResetSessionState();
		RecomputeLocalAdmin();
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
			Instance = null;
	}

	private void ResetSessionState()
	{
		IsOverlayOpen = false;
		smoothedDelta = MathF.Max( 0.0001f, Time.Delta );
		worstDeltaThisWindow = 0f;
		petAnimWindowAccum = 0;
		groundTraceWindowAccum = 0;
		framesThisWindow = 0;
		sampleTimer = 0f;
		Version++;
	}

	private void RecomputeLocalAdmin()
	{
		IsLocalAdmin = AllowEveryone || (AdminSteamIds?.Any( id => id == Game.SteamId ) ?? false);
	}

	protected override void OnUpdate()
	{
		if ( !TrackingEnabled )
			return;

		// Cheap admin re-check is fine; SteamId doesn't change but AllowEveryone might be toggled in editor.
		RecomputeLocalAdmin();

		// Toggle with the unused "Menu" action (default Q). Admin-gated so normal players never trigger it.
		if ( IsLocalAdmin && Input.Pressed( "Menu" ) )
			ToggleOverlay();

		SampleFrame();
	}

	private void SampleFrame()
	{
		var dt = MathF.Max( 0.0001f, Time.Delta );
		smoothedDelta = MathX.Lerp( smoothedDelta, dt, 0.1f );

		if ( dt > worstDeltaThisWindow )
			worstDeltaThisWindow = dt;

		// Drain the per-frame work counters published by the hot paths.
		petAnimWindowAccum += PetAnimAccumulator;
		groundTraceWindowAccum += GroundTraceAccumulator;
		PetAnimAccumulator = 0;
		GroundTraceAccumulator = 0;
		framesThisWindow++;

		sampleTimer -= dt;
		if ( sampleTimer > 0f )
			return;

		sampleTimer = MathF.Max( 0.05f, SampleInterval );
		PublishSample();
	}

	private void PublishSample()
	{
		Fps = 1f / MathF.Max( 0.0001f, smoothedDelta );
		FrameMs = smoothedDelta * 1000f;
		LowFps = worstDeltaThisWindow > 0f ? 1f / worstDeltaThisWindow : Fps;
		worstDeltaThisWindow = 0f;

		var frames = Math.Max( 1, framesThisWindow );
		PetAnimTicksPerFrame = petAnimWindowAccum / frames;
		GroundTracesPerFrame = groundTraceWindowAccum / frames;
		petAnimWindowAccum = 0;
		groundTraceWindowAccum = 0;
		framesThisWindow = 0;

		RefreshSceneCounts();

		Version++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}

	private void RefreshSceneCounts()
	{
		if ( Scene == null )
			return;

		var players = Scene.GetAllComponents<PlayerData>().ToList();
		PlayerCount = players.Count;
		ProxyPlayerCount = players.Count( p => p.IsValid() && p.IsProxy );

		var frameworks = Scene.GetAllComponents<PetFramework>().ToList();
		PetFrameworkCount = frameworks.Count;
		ProxyPetFrameworkCount = frameworks.Count( f => f.IsValid() && f.IsProxy );

		PetComponentCount = Scene.GetAllComponents<PetComponent>().Count();
		DestructibleCount = Scene.GetAllComponents<InteractGivePlayerCoin>().Count();
		RarityVisualCount = Scene.GetAllComponents<EquippedPetRarityVisuals>().Count();
		WorldPanelCount = Scene.GetAllComponents<Sandbox.WorldPanel>().Count();
		TotalObjectCount = Scene.GetAllObjects( true ).Count();
	}

	private static void ToggleOverlay()
	{
		IsOverlayOpen = !IsOverlayOpen;
		Version++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}

	// --- Console commands (work like the chat "say" command) ---

	[ConCmd( "perf_overlay" )]
	public static void Cmd_ToggleOverlay()
	{
		// Console use is a deliberate admin action; allow it to show even if SteamId isn't listed.
		IsLocalAdmin = true;
		ToggleOverlay();
	}

	[ConCmd( "perf_pets" )]
	public static void Cmd_TogglePetAnim()
	{
		DisableProxyPetAnimation = !DisableProxyPetAnimation;
		Log.Info( $"[perf] Proxy pet animation {(DisableProxyPetAnimation ? "DISABLED" : "enabled")}" );
		Version++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}

	[ConCmd( "perf_rarityfx" )]
	public static void Cmd_ToggleRarityFx()
	{
		DisableRarityVisualFx = !DisableRarityVisualFx;
		Log.Info( $"[perf] Rarity visual FX {(DisableRarityVisualFx ? "DISABLED" : "enabled")}" );
		Version++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}
}
