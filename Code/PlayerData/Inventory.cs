using Sandbox;
using Sandbox.Services;
using System;

public sealed class Inventory : Component
{
	[Property] public int InventorySize { get; set; } = 10;
	[Property] public List<InventoryPetSlot> Slots { get; set; } = new();
	[Property] public List<int> EquippedSlotIndexes { get; set; } = new();

	private bool isLoadingJson;
	private bool isRestoringEquippedPets;

	// Raw JSON for slots whose pet prefab couldn't be resolved on load. Re-emitted on save so a
	// transient resolve failure (missing/renamed prefab path) never permanently deletes a pet.
	private readonly List<JSONObject> unresolvedSlots = new();

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

		petFramework.Equip( slot );
		if ( !EquippedSlotIndexes.Contains( slotNumber ) )
		{
			EquippedSlotIndexes.Add( slotNumber );
		}

		GameStatsTracker.RecordPetEquipped( slot.DisplayName, slot.PetPrefabPath );
		QueueOwnerSave();
		return true;
	}

	public bool UnequipPet( int slotNumber )
	{
		if ( !IsPetEquipped( slotNumber ) )
			return false;

		var slot = GetPet( slotNumber );
		EquippedSlotIndexes.RemoveAll( index => index == slotNumber );
		RestoreEquippedPets();
		GameStatsTracker.RecordPetUnequipped( slot?.DisplayName, slot?.PetPrefabPath );
		QueueOwnerSave();
		return true;
	}

	public bool TogglePetEquipped( int slotNumber )
	{
		return IsPetEquipped( slotNumber )
			? UnequipPet( slotNumber )
			: EquipPet( slotNumber );
	}

	public bool EquipBestPets( InventoryAutoEquipMode mode )
	{
		var petFramework = GetComponent<PetFramework>();
		if ( !petFramework.IsValid() )
		{
			Log.Warning( "Tried to equip best pets, but no PetFramework was found on the player." );
			return false;
		}

		var maxEquippedPets = Math.Max( 0, petFramework.MaxEquippedPets );
		if ( maxEquippedPets == 0 )
			return false;

		var bestSlotIndexes = GetBestPetSlotIndexes( mode, maxEquippedPets );
		if ( bestSlotIndexes.Count == 0 )
			return false;

		var oldEquippedSlotIndexes = GetEquippedSlotIndexes();
		if ( oldEquippedSlotIndexes.SequenceEqual( bestSlotIndexes ) )
			return false;

		foreach ( var oldSlotIndex in oldEquippedSlotIndexes.Except( bestSlotIndexes ) )
		{
			var slot = GetPet( oldSlotIndex );
			GameStatsTracker.RecordPetUnequipped( slot?.DisplayName, slot?.PetPrefabPath );
		}

		foreach ( var newSlotIndex in bestSlotIndexes.Except( oldEquippedSlotIndexes ) )
		{
			var slot = GetPet( newSlotIndex );
			GameStatsTracker.RecordPetEquipped( slot?.DisplayName, slot?.PetPrefabPath );
		}

		LoadEquippedSlotIndexes( bestSlotIndexes );
		QueueOwnerSave();
		return true;
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
		if ( slot.IsValid() && slot.HasPet )
		{
			GameStatsTracker.RecordPetRemoved( slot.DisplayName, slot.PetPrefabPath );
		}

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
		GameStatsTracker.RecordPetAdded( slot.DisplayName, slot.PetPrefabPath, slot.GetPetPrefab() );
		SaveOwnerNow();
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

	public bool AddPetPrefab( GameObject petPrefab, string displayName = null, PetRarity rarity = PetRarity.Common )
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
		slot.Rarity = rarity;

		Slots.Add( slot );
		GameStatsTracker.RecordPetAdded( slot.DisplayName, slot.PetPrefabPath, petPrefab );
		SaveOwnerNow();
		return true;
	}

	public bool CanMergePets( IReadOnlyList<int> slotIndexes )
	{
		return GetMergeCandidate( slotIndexes ) != null;
	}

	public bool TryMergePets( IReadOnlyList<int> slotIndexes, out InventoryPetSlot mergedSlot )
	{
		mergedSlot = null;

		var candidate = GetMergeCandidate( slotIndexes );
		if ( candidate == null )
			return false;

		var sortedIndexes = slotIndexes.Distinct().OrderByDescending( index => index ).ToList();
		var sourceSlot = candidate;
		var sourceDisplayName = sourceSlot.DisplayName;
		var sourcePrefab = sourceSlot.PetPrefab;
		var sourcePrefabPath = sourceSlot.PetPrefabPath;
		var sourceRarity = sourceSlot.Rarity;
		var nextRarity = sourceSlot.Rarity.NextRarity();
		var wasAnyEquipped = sortedIndexes.Any( IsPetEquipped );

		foreach ( var slotIndex in sortedIndexes )
		{
			if ( slotIndex < 0 || slotIndex >= Slots.Count )
				return false;

			var slot = Slots[slotIndex];
			Slots.RemoveAt( slotIndex );
			RemoveEquippedSlotIndex( slotIndex );

			if ( slot.IsValid() && slot.GameObject.IsValid() && slot.GameObject.Parent == GameObject )
			{
				slot.GameObject.Destroy();
			}
		}

		mergedSlot = CreateRuntimeSlot();
		mergedSlot.DisplayName = sourceDisplayName;
		mergedSlot.PetPrefab = sourcePrefab;
		mergedSlot.PetPrefabPath = sourcePrefabPath;
		mergedSlot.Rarity = nextRarity;
		Slots.Add( mergedSlot );

		if ( wasAnyEquipped )
		{
			EquippedSlotIndexes.Add( Slots.Count - 1 );
		}

		RestoreEquippedPets();
		GameStatsTracker.RecordPetMerged( mergedSlot.DisplayName, mergedSlot.PetPrefabPath, sourceRarity, nextRarity );
		QueueOwnerSave();
		return true;
	}

	public bool CanAutoMergePets()
	{
		return GetAutoMergeCandidate() != null;
	}

	public int TryAutoMergePets( int maxMergeCount = 256 )
	{
		var mergeCount = 0;
		while ( mergeCount < maxMergeCount )
		{
			var candidateIndexes = GetAutoMergeCandidate();
			if ( candidateIndexes == null || candidateIndexes.Count != 3 )
				break;

			if ( !TryMergePets( candidateIndexes, out _ ) )
				break;

			mergeCount++;
		}

		return mergeCount;
	}

	private InventoryPetSlot GetMergeCandidate( IReadOnlyList<int> slotIndexes )
	{
		if ( slotIndexes == null )
			return null;

		var distinctIndexes = slotIndexes.Distinct().ToList();
		if ( distinctIndexes.Count != 3 )
			return null;

		var slots = distinctIndexes.Select( GetPet ).ToList();
		if ( slots.Any( slot => !slot.IsValid() || !slot.HasPet || !slot.Rarity.CanMergeUp() ) )
			return null;

		var first = slots[0];
		return slots.All( slot => first.IsSamePetAndRarity( slot ) ) ? first : null;
	}

	private List<int> GetAutoMergeCandidate()
	{
		return Slots
			.Select( ( slot, index ) => new { Slot = slot, Index = index } )
			.Where( entry => entry.Slot.IsValid()
				&& entry.Slot.HasPet
				&& entry.Slot.Rarity.CanMergeUp()
				&& !string.IsNullOrWhiteSpace( entry.Slot.PetPrefabPath ) )
			.GroupBy( entry => new
			{
				entry.Slot.PetPrefabPath,
				entry.Slot.Rarity
			} )
			.Where( group => group.Count() >= 3 )
			.OrderBy( group => (int)group.Key.Rarity )
			.ThenBy( group => group.First().Slot.DisplayName, System.StringComparer.OrdinalIgnoreCase )
			.ThenBy( group => group.Key.PetPrefabPath, System.StringComparer.OrdinalIgnoreCase )
			.Select( group => group.Take( 3 ).Select( entry => entry.Index ).ToList() )
			.FirstOrDefault();
	}

	private List<int> GetBestPetSlotIndexes( InventoryAutoEquipMode mode, int maxEquippedPets )
	{
		return Slots
			.Select( ( slot, index ) => new
			{
				Slot = slot,
				Index = index,
				Score = GetAutoEquipScore( slot, mode ),
				TieBreakerScore = GetAutoEquipScore( slot, mode == InventoryAutoEquipMode.Damage ? InventoryAutoEquipMode.CoinMultiplier : InventoryAutoEquipMode.Damage )
			} )
			.Where( entry => entry.Slot.IsValid() && entry.Slot.HasPet )
			.OrderByDescending( entry => entry.Score )
			.ThenByDescending( entry => entry.TieBreakerScore )
			.ThenByDescending( entry => (int)entry.Slot.Rarity )
			.ThenBy( entry => entry.Slot.DisplayName, System.StringComparer.OrdinalIgnoreCase )
			.ThenBy( entry => entry.Index )
			.Take( maxEquippedPets )
			.Select( entry => entry.Index )
			.ToList();
	}

	private static float GetAutoEquipScore( InventoryPetSlot slot, InventoryAutoEquipMode mode )
	{
		if ( !slot.IsValid() || !slot.HasPet )
			return 0f;

		var petPrefab = slot.GetPetPrefab();
		var petComponent = petPrefab?.GetComponent<PetComponent>() ?? petPrefab?.GetComponentInChildren<PetComponent>();
		var rarityMultiplier = slot.RarityValueMultiplier;

		return mode switch
		{
			InventoryAutoEquipMode.Damage => Math.Max( 0, (int)MathF.Round( (petComponent?.Damage ?? 1) * rarityMultiplier ) ),
			_ => MathF.Max( 0f, petComponent?.CoinMultiplier ?? 1f ) * rarityMultiplier
		};
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

		// Re-emit any slots whose prefab couldn't be resolved this session so they aren't lost.
		foreach ( var raw in unresolvedSlots )
		{
			if ( raw != null )
				slotData.Add( raw );
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
			unresolvedSlots.Clear();

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
				else
				{
					// Don't drop it — preserve the raw data so it survives to the next save and can
					// resolve in a later session (prevents permanent pet loss from a bad prefab path).
					Log.Warning( $"Preserving unresolved pet inventory slot '{slot.DisplayName}' ('{slot.PetPrefabPath}') so it is not lost on save." );
					unresolvedSlots.Add( slotJson );
					if ( slot.GameObject.IsValid() )
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

				petFramework.Equip( slot );
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

	// Immediate write, used when the player RECEIVES a pet so a new pet can never be lost
	// (e.g. a crash or quick disconnect right after opening a crate).
	private void SaveOwnerNow()
	{
		if ( isLoadingJson || isRestoringEquippedPets )
			return;

		GetComponent<PlayerData>()?.SaveNow();
	}

	private static string GetPetPrefabPath( GameObject petPrefab )
	{
		// Prefer the real asset path. The old fallback assumed every pet lived directly in
		// "Prefabs/Pets/", but they're in subfolders (e.g. Prefabs/Pets/Playtest/Z1), so that path
		// never resolved on load. Storing the bare name lets PetPrefabResolver match by filename.
		if ( !string.IsNullOrWhiteSpace( petPrefab.PrefabInstanceSource ) )
			return petPrefab.PrefabInstanceSource;

		return petPrefab.IsValid() && !string.IsNullOrWhiteSpace( petPrefab.Name ) ? petPrefab.Name : string.Empty;
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

public enum InventoryAutoEquipMode
{
	Damage,
	CoinMultiplier
}
