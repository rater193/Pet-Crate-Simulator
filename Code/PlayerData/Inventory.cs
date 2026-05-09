using Sandbox;
using Sandbox.Services;

public sealed class Inventory : Component
{
	[Property] public int InventorySize { get; set; } = 10;
	[Property] public List<InventoryPetSlot> Slots { get; set; } = new();
	[Property] public List<int> EquippedSlotIndexes { get; set; } = new();

	private bool isLoadingJson;
	private bool isRestoringEquippedPets;

	public int Count => Slots.Count( slot => slot.IsValid() && slot.HasPet );

	public bool EquipPet( int slotNumber )
	{
		var slot = GetPet( slotNumber );
		if ( !slot.IsValid() )
			return false;

		if ( IsPetEquipped( slotNumber ) )
			return true;

		var petPrefab = slot.GetPetPrefab();
		if ( !petPrefab.IsValid() )
		{
			Log.Warning( $"Pet inventory slot {slotNumber} does not have a valid pet prefab." );
			return false;
		}

		var petFramework = GetComponent<PetFramework>();
		if ( !petFramework.IsValid() )
		{
			Log.Warning( "Tried to equip a pet, but no PetFramework was found on the player." );
			return false;
		}

		if ( GetEquippedSlotIndexes().Count >= petFramework.MaxEquippedPets )
		{
			Log.Warning( $"Cannot equip pet '{slot.DisplayName}'. Max equipped pets is {petFramework.MaxEquippedPets}." );
			return false;
		}

		petFramework.Equip( petPrefab );
		if ( !EquippedSlotIndexes.Contains( slotNumber ) )
		{
			EquippedSlotIndexes.Add( slotNumber );
		}

		QueueOwnerSave();
		return true;
	}

	public bool UnequipPet( int slotNumber )
	{
		if ( !IsPetEquipped( slotNumber ) )
			return false;

		EquippedSlotIndexes.RemoveAll( index => index == slotNumber );
		RestoreEquippedPets();
		QueueOwnerSave();
		return true;
	}

	public bool TogglePetEquipped( int slotNumber )
	{
		return IsPetEquipped( slotNumber )
			? UnequipPet( slotNumber )
			: EquipPet( slotNumber );
	}

	public bool IsPetEquipped( int slotNumber )
	{
		return GetPet( slotNumber ).IsValid() && EquippedSlotIndexes.Contains( slotNumber );
	}

	public InventoryPetSlot GetPet( int slotNumber )
	{
		if ( slotNumber < 0 || slotNumber >= Slots.Count )
			return null;

		var slot = Slots[slotNumber];
		return slot.IsValid() && slot.HasPet ? slot : null;
	}

	public bool RemovePet( int slotNumber )
	{
		if ( slotNumber < 0 || slotNumber >= Slots.Count )
			return false;

		var slot = Slots[slotNumber];
		Slots.RemoveAt( slotNumber );
		RemoveEquippedSlotIndex( slotNumber );

		if ( slot.IsValid() && slot.GameObject.IsValid() && slot.GameObject.Parent == GameObject )
		{
			slot.GameObject.Destroy();
		}

		RestoreEquippedPets();
		QueueOwnerSave();
		return true;
	}

	public bool AddPet( InventoryPetSlot slot )
	{
		if ( !slot.IsValid() || !slot.HasPet )
			return false;

		if ( Slots.Count >= InventorySize )
		{
			Log.Warning( $"Cannot add pet '{slot.DisplayName}'. Inventory size is {InventorySize}." );
			return false;
		}

		Slots.Add( slot );
		QueueOwnerSave();
		return true;
	}

	public bool AddPetInventoryPrefab( GameObject inventorySlotPrefab )
	{
		if ( !inventorySlotPrefab.IsValid() )
			return false;

		var slotObject = inventorySlotPrefab.Clone();
		slotObject.Parent = GameObject;
		slotObject.Enabled = false;

		var slot = slotObject.GetComponent<InventoryPetSlot>() ?? slotObject.GetComponentInChildren<InventoryPetSlot>();
		if ( !slot.IsValid() )
		{
			slotObject.Destroy();
			Log.Warning( $"Inventory prefab '{inventorySlotPrefab.Name}' does not have an InventoryPetSlot component." );
			return false;
		}

		if ( AddPet( slot ) )
			return true;

		slotObject.Destroy();
		return false;
	}

	public bool AddPetPrefab( GameObject petPrefab, string displayName = null )
	{
		if ( !petPrefab.IsValid() )
			return false;

		if ( Slots.Count >= InventorySize )
		{
			Log.Warning( $"Cannot add pet '{petPrefab.Name}'. Inventory size is {InventorySize}." );
			return false;
		}

		var slot = CreateRuntimeSlot();
		var petComponent = petPrefab.GetComponent<PetComponent>() ?? petPrefab.GetComponentInChildren<PetComponent>();
		slot.DisplayName = !string.IsNullOrWhiteSpace( displayName )
			? displayName
			: !string.IsNullOrWhiteSpace( petComponent?.DisplayName )
				? petComponent.DisplayName
				: petPrefab.Name;
		slot.PetPrefab = petPrefab;
		slot.PetPrefabPath = GetPetPrefabPath( petPrefab );

		Slots.Add( slot );
		QueueOwnerSave();
		Log.Info( "Increased " + petPrefab.GetComponent<PetComponent>().DisplayName + " by 1" );
		Stats.Increment( petPrefab.GetComponent<PetComponent>().DisplayName, 1 );
		Stats.FlushAsync();
		return true;
	}

	public List<int> GetEquippedSlotIndexes()
	{
		return EquippedSlotIndexes
			.Where( index => GetPet( index ).IsValid() )
			.Distinct()
			.ToList();
	}

	public void LoadEquippedSlotIndexes( IEnumerable<int> slotIndexes, bool equipPets = true )
	{
		var wasRestoringEquippedPets = isRestoringEquippedPets;
		isRestoringEquippedPets = true;
		EquippedSlotIndexes.Clear();

		try
		{
			foreach ( var slotIndex in slotIndexes )
			{
				if ( GetPet( slotIndex ).IsValid() && !EquippedSlotIndexes.Contains( slotIndex ) )
				{
					EquippedSlotIndexes.Add( slotIndex );
				}
			}
		}
		finally
		{
			isRestoringEquippedPets = wasRestoringEquippedPets;
		}

		if ( equipPets )
		{
			RestoreEquippedPets();
		}
	}

	public string ToJson()
	{
		return JSONObject.ToJson( ToJsonObject() );
	}

	public JSONObject ToJsonObject()
	{
		var data = new JSONObject();
		var slotData = new List<JSONObject>();

		foreach ( var slot in Slots )
		{
			if ( slot.IsValid() && slot.HasPet )
			{
				slotData.Add( slot.ToJsonObject() );
			}
		}

		data.Set( "InventorySize", InventorySize );
		data.Set( "Count", Count );
		data.Set( "Slots", slotData );
		data.Set( "EquippedSlotIndexes", GetEquippedSlotIndexes() );

		return data;
	}

	public void LoadJson( string jsonData )
	{
		LoadJson( JSONObject.FromJson( jsonData ) );
	}

	public void LoadJson( JSONObject data )
	{
		if ( data == null )
			return;

		isLoadingJson = true;

		try
		{
			ClearRuntimeSlots();
			Slots.Clear();
			EquippedSlotIndexes.Clear();

			InventorySize = data.Get( "InventorySize", InventorySize );

			var slotData = data.Get<List<object>>( "Slots", new() );
			foreach ( var slotObject in slotData )
			{
				if ( Slots.Count >= InventorySize )
					break;

				if ( slotObject is not JSONObject slotJson )
					continue;

				var slot = CreateRuntimeSlot();
				slot.LoadJson( slotJson );

				if ( slot.HasPet )
				{
					Slots.Add( slot );
				}
				else if ( slot.GameObject.IsValid() )
				{
					Log.Warning( $"Skipped pet inventory slot '{slot.DisplayName}' because '{slot.PetPrefabPath}' could not be loaded." );
					slot.GameObject.Destroy();
				}
			}

			LoadEquippedSlotIndexes( ReadIntList( data, "EquippedSlotIndexes" ), false );
		}
		finally
		{
			isLoadingJson = false;
		}
	}

	public void RestoreEquippedPets()
	{
		if ( isRestoringEquippedPets )
			return;

		isRestoringEquippedPets = true;

		try
		{
			var petFramework = GetComponent<PetFramework>();
			if ( !petFramework.IsValid() )
			{
				Log.Warning( "Tried to restore equipped pets, but no PetFramework was found on the player." );
				return;
			}

			petFramework.UnequipAll();

			foreach ( var slotIndex in GetEquippedSlotIndexes() )
			{
				var slot = GetPet( slotIndex );
				var petPrefab = slot?.GetPetPrefab();

				if ( !petPrefab.IsValid() )
				{
					Log.Warning( $"Could not restore equipped pet slot {slotIndex}; the pet prefab is missing." );
					continue;
				}

				petFramework.Equip( petPrefab );
			}
		}
		finally
		{
			isRestoringEquippedPets = false;
		}
	}

	private InventoryPetSlot CreateRuntimeSlot()
	{
		var slotObject = new GameObject( GameObject, false, "InventoryPetSlot" );
		return slotObject.AddComponent<InventoryPetSlot>();
	}

	private void ClearRuntimeSlots()
	{
		foreach ( var slot in Slots )
		{
			if ( slot.IsValid() && slot.GameObject.IsValid() && slot.GameObject.Parent == GameObject )
			{
				slot.GameObject.Destroy();
			}
		}
	}

	private void RemoveEquippedSlotIndex( int removedSlotNumber )
	{
		for ( var i = EquippedSlotIndexes.Count - 1; i >= 0; i-- )
		{
			var equippedIndex = EquippedSlotIndexes[i];
			if ( equippedIndex == removedSlotNumber )
			{
				EquippedSlotIndexes.RemoveAt( i );
			}
			else if ( equippedIndex > removedSlotNumber )
			{
				EquippedSlotIndexes[i] = equippedIndex - 1;
			}
		}
	}

	private void QueueOwnerSave()
	{
		if ( isLoadingJson || isRestoringEquippedPets )
			return;

		GetComponent<PlayerData>()?.QueueSave();
	}

	private static string GetPetPrefabPath( GameObject petPrefab )
	{
		if ( !string.IsNullOrWhiteSpace( petPrefab.PrefabInstanceSource ) )
			return petPrefab.PrefabInstanceSource;

		if ( string.IsNullOrWhiteSpace( petPrefab.Name ) )
			return string.Empty;

		return $"Prefabs/Pets/{petPrefab.Name}.prefab";
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
