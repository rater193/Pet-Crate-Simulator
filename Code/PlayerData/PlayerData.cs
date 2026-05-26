using Sandbox;
using System;

public sealed partial class PlayerData : Component
{
	private const int SaveVersion = 1;
	private const string SaveDirectory = "playerdata";

	public static PlayerData LOCALDATA;
	[Property, Sync] public int PlayerMoney { get; set; } = 0;
	[Property] public Inventory inventory { get; set; }
	[Property] public string SaveFilePath { get; set; } = "playerdata/save.json";
	[Property] public bool AutoLoadOnStart { get; set; } = true;
	[Property] public bool AutoSaveEnabled { get; set; } = true;
	[Property] public float AutoSaveDelay { get; set; } = 2f;

	private bool saveQueued;
	private float saveDelayRemaining;
	private bool isLoadingJson;
	private bool loadFailed;
	private bool initialized;

	/// <summary>
	/// Per-player save path keyed by the OWNING player's SteamId. Without this, every player saved to
	/// the same file, so a second player (or a transient spawn) with default data clobbered everyone
	/// else's save on a shared FileSystem.Data (host machine / editor). Keyed by Owner (not Game.SteamId)
	/// so a remote player's object on the host writes its OWN file, never the host's.
	/// </summary>
	private string ResolveSavePath()
	{
		var basePath = string.IsNullOrWhiteSpace( SaveFilePath ) ? "playerdata/save.json" : SaveFilePath;
		var ownerSteamId = (long)(GameObject?.Network?.Owner?.SteamId ?? Game.SteamId);

		var dot = basePath.LastIndexOf( '.' );
		var stem = dot < 0 ? basePath : basePath.Substring( 0, dot );
		var ext = dot < 0 ? string.Empty : basePath.Substring( dot );
		return $"{stem}_{ownerSteamId}{ext}";
	}

	[Rpc.Owner]
	public void AddMoney( int amount )
	{
		if ( amount <= 0 )
			return;

		var petFramework = GetComponent<PetFramework>();
		var finalAmount = petFramework?.ApplyCoinMultiplier( amount ) ?? amount;
		PlayerMoney += finalAmount;
		GameStatsTracker.RecordCoinsEarned( finalAmount, amount );
		QueueSave();
	}

	protected override void OnStart()
	{
		inventory ??= GetComponent<Inventory>() ?? GameObject.GetOrAddComponent<Inventory>();

		if ( !IsProxy && AutoLoadOnStart )
		{
			Load();
		}

		if ( !IsProxy )
		{
			// Only allow saving AFTER we've gone through OnStart/load. A freshly-spawned or transient
			// PlayerData that never loaded must not write its default (empty) state over a real save.
			initialized = true;
			GameStatsTracker.RecordSessionStarted( this );
		}
	}

	protected override void OnUpdate()
	{
		// Admin fly + invisibility (invisibility must run on proxies too, so it's outside the guard).
		AdminUpdate();

		if ( !IsProxy )
		{
			inventory ??= GetComponent<Inventory>() ?? GameObject.GetOrAddComponent<Inventory>();
			LOCALDATA = this;
			if ( PlayerHud.SINGLETON != null )
			{
				PlayerHud.SINGLETON.RenderedMoneyValue = PlayerMoney;
			}

			UpdateQueuedSave();
			GameStatsTracker.UpdateSession( this, Time.Delta );
		}
	}

	protected override void OnDestroy()
	{
		if ( !IsProxy )
		{
			GameStatsTracker.RecordSessionEnded( this );

			// Always flush on leave, not only when a save happens to be queued. The autosave is a
			// 2s-delayed queue, so gating on saveQueued meant any change in the last ~2s before
			// disconnect (or any session with AutoSaveEnabled off) wrote nothing on exit.
			Save();
		}
	}

	public JSONObject ToJsonObject()
	{
		inventory ??= GetComponent<Inventory>() ?? GameObject.GetOrAddComponent<Inventory>();

		var data = new JSONObject()
			.Set( "Version", SaveVersion )
			.Set( "PlayerMoney", PlayerMoney )
			.Set( "PetInventory", inventory?.ToJsonObject() )
			.Set( "EquippedPetSlots", inventory?.GetEquippedSlotIndexes() ?? new List<int>() );

		SaveExtensions( data );

		return data;
	}

	public string ToJson()
	{
		return ToJsonObject().ToJson( true );
	}

	public void LoadJson( string jsonData )
	{
		LoadJson( JSONObject.FromJson( jsonData ) );
	}

	public void LoadJson( JSONObject data )
	{
		if ( IsProxy || data == null )
			return;

		isLoadingJson = true;

		try
		{
			inventory ??= GetComponent<Inventory>() ?? GameObject.GetOrAddComponent<Inventory>();

			PlayerMoney = data.Get( "PlayerMoney", PlayerMoney );

			var inventoryData = data.GetObject( "PetInventory" );
			if ( inventoryData != null )
			{
				inventory.LoadJson( inventoryData );
			}

			var equippedPetSlots = ReadIntList( data, "EquippedPetSlots" );
			if ( equippedPetSlots.Count == 0 && inventoryData != null )
			{
				equippedPetSlots = ReadIntList( inventoryData, "EquippedSlotIndexes" );
			}

			inventory.LoadEquippedSlotIndexes( equippedPetSlots, false );
			inventory.RestoreEquippedPets();

			LoadExtensions( data );

			saveQueued = false;
			saveDelayRemaining = 0f;
		}
		finally
		{
			isLoadingJson = false;
		}
	}

	public void Save()
	{
		if ( IsProxy )
			return;

		// Don't let a PlayerData that never completed OnStart/load write its default (empty) state.
		if ( !initialized )
			return;

		// Never overwrite the save file if the last load FAILED — writing the current (empty/default)
		// in-memory state would permanently wipe the player's real save. This is the guard that stops
		// the "load errored -> autosave/exit-save clobbers good data" data-loss bug.
		if ( loadFailed )
		{
			Log.Warning( $"[PlayerData] Skipping save because the last load FAILED (refusing to overwrite existing data)." );
			return;
		}

		var path = ResolveSavePath();

		try
		{
			FileSystem.Data.CreateDirectory( SaveDirectory );
			FileSystem.Data.WriteAllText( path, ToJson() );
			saveQueued = false;
			saveDelayRemaining = 0f;
			GameStatsTracker.RecordSaveWritten();
			Log.Info( $"[PlayerData] Saved '{path}' (money={PlayerMoney}, pets={inventory?.Count ?? 0})." );
		}
		catch ( System.Exception exception )
		{
			Log.Warning( exception, $"Failed to save player data to '{path}'." );
		}
	}

	public bool Load()
	{
		if ( IsProxy )
			return false;

		loadFailed = false;
		var path = ResolveSavePath();

		try
		{
			if ( !FileSystem.Data.FileExists( path ) )
			{
				GameStatsTracker.RecordSaveMissing();
				return false;
			}

			var json = FileSystem.Data.ReadAllText( path );
			if ( string.IsNullOrWhiteSpace( json ) )
			{
				GameStatsTracker.RecordSaveMissing();
				return false;
			}

			LoadJson( json );
			GameStatsTracker.RecordSaveLoaded();
			Log.Info( $"[PlayerData] Loaded '{path}' (money={PlayerMoney}, pets={inventory?.Count ?? 0})." );
			return true;
		}
		catch ( System.Exception exception )
		{
			// Mark the load as failed so Save() refuses to overwrite the (possibly good) file on disk.
			loadFailed = true;
			Log.Warning( exception, $"Failed to load player data from '{path}'. Refusing to overwrite it this session." );
			GameStatsTracker.RecordSaveFailed();
			return false;
		}
	}

	public void QueueSave()
	{
		if ( IsProxy || isLoadingJson || !AutoSaveEnabled )
			return;

		saveQueued = true;
		saveDelayRemaining = MathF.Max( 0f, AutoSaveDelay );
	}

	/// <summary>
	/// Writes the save file right now, skipping the 2s autosave delay. Used for important,
	/// must-not-lose events like receiving a pet. Safe to call from gameplay; ignored on proxies
	/// and while a save is being loaded.
	/// </summary>
	public void SaveNow()
	{
		if ( IsProxy || isLoadingJson )
			return;

		Save();
	}

	private void UpdateQueuedSave()
	{
		if ( !saveQueued )
			return;

		saveDelayRemaining -= Time.Delta;
		if ( saveDelayRemaining > 0f )
			return;

		Save();
	}

	private void SaveExtensions( JSONObject data )
	{
		EnsureDoorSaveStates();

		foreach ( var extension in Scene.GetAll<PlayerDataSaveExtension>() )
		{
			if ( extension == null || !extension.Enabled )
				continue;

			extension.OnSave( data );
		}
	}

	private void LoadExtensions( JSONObject data )
	{
		EnsureDoorSaveStates();

		foreach ( var extension in Scene.GetAll<PlayerDataSaveExtension>() )
		{
			if ( extension == null || !extension.Enabled )
				continue;

			extension.OnLoad( data );
		}
	}

	private void EnsureDoorSaveStates()
	{
		foreach ( var door in Scene.GetAll<InteractLockedDoor>() )
		{
			door?.EnsureSaveState();
		}
	}

	private static List<int> ReadIntList( JSONObject data, string key )
	{
		var result = new List<int>();
		var values = data.Get<List<object>>( key, new() );

		foreach ( var value in values )
		{
			if ( value is int intValue )
			{
				result.Add( intValue );
			}
			else if ( value is long longValue )
			{
				result.Add( (int)longValue );
			}
			else if ( value is double doubleValue )
			{
				result.Add( (int)doubleValue );
			}
		}

		return result;
	}
}
