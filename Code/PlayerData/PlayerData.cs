using Sandbox;
using System;

public sealed class PlayerData : Component
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
			GameStatsTracker.RecordSessionStarted( this );
		}
	}

	protected override void OnUpdate()
	{
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
		}

		if ( !IsProxy && saveQueued )
		{
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

		try
		{
			FileSystem.Data.CreateDirectory( SaveDirectory );
			FileSystem.Data.WriteAllText( SaveFilePath, ToJson() );
			saveQueued = false;
			saveDelayRemaining = 0f;
			GameStatsTracker.RecordSaveWritten();
		}
		catch ( System.Exception exception )
		{
			Log.Warning( exception, $"Failed to save player data to '{SaveFilePath}'." );
		}
	}

	public bool Load()
	{
		if ( IsProxy )
			return false;

		try
		{
			if ( !FileSystem.Data.FileExists( SaveFilePath ) )
			{
				GameStatsTracker.RecordSaveMissing();
				return false;
			}

			var json = FileSystem.Data.ReadAllText( SaveFilePath );
			if ( string.IsNullOrWhiteSpace( json ) )
			{
				GameStatsTracker.RecordSaveMissing();
				return false;
			}

			LoadJson( json );
			GameStatsTracker.RecordSaveLoaded();
			return true;
		}
		catch ( System.Exception exception )
		{
			Log.Warning( exception, $"Failed to load player data from '{SaveFilePath}'. Starting with current defaults." );
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
