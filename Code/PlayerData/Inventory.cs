using Sandbox;

public sealed class Inventory : Component
{
	[Property] public int InventorySize { get; set; } = 10;
	[Property] public List<InventoryPetSlot> Slots { get; set; } = new();

	public int Count => Slots.Count( slot => slot.IsValid() && slot.HasPet );

	public bool EquipPet( int slotNumber )
	{
		var slot = GetPet( slotNumber );
		if ( !slot.IsValid() )
			return false;

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

		petFramework.Equip( petPrefab );
		return true;
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

		if ( slot.IsValid() && slot.GameObject.IsValid() && slot.GameObject.Parent == GameObject )
		{
			slot.GameObject.Destroy();
		}

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
		slot.DisplayName = string.IsNullOrWhiteSpace( displayName ) ? petPrefab.Name : displayName;
		slot.PetPrefab = petPrefab;
		slot.PetPrefabPath = petPrefab.PrefabInstanceSource ?? string.Empty;

		Slots.Add( slot );
		return true;
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

		ClearRuntimeSlots();
		Slots.Clear();

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
				slot.GameObject.Destroy();
			}
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
}
