using Sandbox;
using System;
using System.Linq;

/// <summary>
/// Client-local admin performance inspector. Gated by SteamId like <see cref="PlayerNoticeController"/>
/// and <see cref="BanListController"/>.
///
/// Two UIs read this controller:
///   - <c>AdminPerfPanel</c>  : a tiny read-only FPS pill (pointer-events: none) shown to admins
///                              whenever the explorer is closed, so mouse-look is never broken.
///   - <c>PerfExplorerPanel</c>: the full interactive menu (FPS chart, collapsible detail sections,
///                              bisect toggles, and a GameObject browser). Opening it puts the cursor
///                              up (it's a menu), which is fine for a diagnostic tool.
///
/// Toggle the explorer with the unused "Menu" input (default Q) or the <c>perf_explorer</c> console
/// command. Bisect toggles are also console commands: perf_pets, perf_rarityfx, perf_cull, perf_culldist.
/// </summary>
public sealed class AdminPerfController : Component
{
	public const int HistorySize = 96;

	[Property] public bool TrackingEnabled { get; set; } = true;

	/// <summary>Debug escape hatch: when true, every client may open the tools (ignore the SteamId list).</summary>
	[Property] public bool AllowEveryone { get; set; } = false;

	/// <summary>SteamIds allowed to open the tools. Mirrors the PlayerNoticeController/BanList config style.</summary>
	[Property] public List<long> AdminSteamIds { get; set; } = new();

	/// <summary>How often the scene counts + displayed readout + chart refresh (seconds).</summary>
	[Property] public float SampleInterval { get; set; } = 0.25f;

	public static AdminPerfController Instance { get; private set; }

	// --- Admin / UI visibility state ---
	public static bool IsLocalAdmin { get; private set; }
	public static bool IsExplorerOpen { get; private set; }
	public static bool MiniVisible { get; private set; } = true;
	public static int Version { get; private set; }

	// --- Collapsible detail sections in the explorer ---
	public static bool ShowChart { get; private set; } = true;
	public static bool ShowFrame { get; private set; } = true;
	public static bool ShowScene { get; private set; } = true;
	public static bool ShowPetWork { get; private set; } = true;
	public static bool ShowCulling { get; private set; } = true;
	public static bool ShowBrowser { get; private set; } = true;

	// --- Live frame timing ---
	public static float Fps { get; private set; }
	public static float FrameMs { get; private set; }
	public static float LowFps { get; private set; }

	// --- FPS history (oldest..newest) for the chart ---
	private static readonly float[] fpsHistory = new float[HistorySize];
	public static float HistoryMaxFps { get; private set; } = 60f;
	public static float[] CopyFpsHistory()
	{
		var copy = new float[HistorySize];
		Array.Copy( fpsHistory, copy, HistorySize );
		return copy;
	}

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
		IsExplorerOpen = false;
		smoothedDelta = MathF.Max( 0.0001f, Time.Delta );
		Version++;
		RecomputeLocalAdmin();
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
			Instance = null;
	}

	private void RecomputeLocalAdmin()
	{
		IsLocalAdmin = AllowEveryone || (AdminSteamIds?.Any( id => id == Game.SteamId ) ?? false);
	}

	/// <summary>Host-side admin check by SteamId (used to authorize kick/ban RPCs).</summary>
	public static bool IsAdmin( long steamId )
	{
		var instance = Instance;
		if ( instance == null )
			return false;

		return instance.AllowEveryone || (instance.AdminSteamIds?.Any( id => id == steamId ) ?? false);
	}

	protected override void OnUpdate()
	{
		if ( !TrackingEnabled )
			return;

		RecomputeLocalAdmin();

		if ( IsLocalAdmin && Input.Pressed( "Menu" ) )
			ToggleExplorer();

		SampleFrame();
	}

	private void SampleFrame()
	{
		var dt = MathF.Max( 0.0001f, Time.Delta );
		smoothedDelta = MathX.Lerp( smoothedDelta, dt, 0.1f );

		if ( dt > worstDeltaThisWindow )
			worstDeltaThisWindow = dt;

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

		PushHistory( Fps );

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

	private static void PushHistory( float fps )
	{
		// Shift left, append newest at the end. (Small fixed buffer; cheap.)
		Array.Copy( fpsHistory, 1, fpsHistory, 0, HistorySize - 1 );
		fpsHistory[HistorySize - 1] = fps;

		var max = 60f;
		for ( var i = 0; i < HistorySize; i++ )
			max = MathF.Max( max, fpsHistory[i] );

		HistoryMaxFps = max;
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

	/// <summary>Bump the UI version and ask the HUD to rebuild. Safe to call from anywhere.</summary>
	public static void RequestRefresh()
	{
		Version++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}

	private static void ToggleExplorer()
	{
		IsExplorerOpen = !IsExplorerOpen;
		RequestRefresh();
	}

	public static void CloseExplorer()
	{
		IsExplorerOpen = false;
		RequestRefresh();
	}

	// --- Detail section toggles (explorer headers) ---
	public static void ToggleSection( string id )
	{
		switch ( id )
		{
			case "chart": ShowChart = !ShowChart; break;
			case "frame": ShowFrame = !ShowFrame; break;
			case "scene": ShowScene = !ShowScene; break;
			case "petwork": ShowPetWork = !ShowPetWork; break;
			case "culling": ShowCulling = !ShowCulling; break;
			case "browser": ShowBrowser = !ShowBrowser; break;
		}

		RequestRefresh();
	}

	// --- Console commands ---

	[ConCmd( "perf_explorer" )]
	public static void Cmd_ToggleExplorer()
	{
		IsLocalAdmin = true; // deliberate console action
		ToggleExplorer();
	}

	[ConCmd( "perf_mini" )]
	public static void Cmd_ToggleMini()
	{
		MiniVisible = !MiniVisible;
		RequestRefresh();
	}

	[ConCmd( "perf_pets" )]
	public static void Cmd_TogglePetAnim()
	{
		DisableProxyPetAnimation = !DisableProxyPetAnimation;
		Log.Info( $"[perf] Proxy pet animation {(DisableProxyPetAnimation ? "DISABLED" : "enabled")}" );
		RequestRefresh();
	}

	[ConCmd( "perf_rarityfx" )]
	public static void Cmd_ToggleRarityFx()
	{
		DisableRarityVisualFx = !DisableRarityVisualFx;
		Log.Info( $"[perf] Rarity visual FX {(DisableRarityVisualFx ? "DISABLED" : "enabled")}" );
		RequestRefresh();
	}

	// Methods used by the explorer's clickable bisect buttons.
	public static void TogglePetAnim() => Cmd_TogglePetAnim();
	public static void ToggleRarityFx() => Cmd_ToggleRarityFx();
}
