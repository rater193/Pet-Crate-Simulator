using Sandbox;
using Sandbox.Services;
using System;
using System.Collections.Generic;
using System.Text;

public static class GameStatsTracker
{
	private const float SnapshotInterval = 30f;
	private const float PlaytimeMinute = 60f;

	private static bool sessionStarted;
	private static float sessionSeconds;
	private static float pendingPlaytimeSeconds;
	private static float snapshotElapsed;

	public static void RecordSessionStarted( PlayerData playerData )
	{
		if ( sessionStarted || playerData == null || playerData.IsProxy )
			return;

		sessionStarted = true;
		sessionSeconds = 0f;
		pendingPlaytimeSeconds = 0f;
		snapshotElapsed = 0f;

		Increment( "login_count" );
		Increment( "sessions_started" );
		SetValue( "last_session_start_unix", DateTimeOffset.UtcNow.ToUnixTimeSeconds() );
		RecordPlayerSnapshot( playerData );
		Flush();
	}

	public static void UpdateSession( PlayerData playerData, float deltaSeconds )
	{
		if ( !sessionStarted || playerData == null || playerData.IsProxy || deltaSeconds <= 0f )
			return;

		sessionSeconds += deltaSeconds;
		pendingPlaytimeSeconds += deltaSeconds;
		snapshotElapsed += deltaSeconds;

		var minutes = (int)(pendingPlaytimeSeconds / PlaytimeMinute);
		if ( minutes > 0 )
		{
			pendingPlaytimeSeconds -= minutes * PlaytimeMinute;
			Increment( "minutes_played", minutes );
			Increment( "playtime_minutes", minutes );
			Increment( "playtime_seconds", minutes * PlaytimeMinute );
		}

		if ( snapshotElapsed < SnapshotInterval )
			return;

		snapshotElapsed = 0f;
		RecordPlayerSnapshot( playerData );
		Flush();
	}

	public static void RecordSessionEnded( PlayerData playerData )
	{
		if ( !sessionStarted || playerData == null || playerData.IsProxy )
			return;

		Increment( "sessions_completed" );
		if ( pendingPlaytimeSeconds > 0f )
		{
			Increment( "playtime_seconds", MathF.Floor( pendingPlaytimeSeconds ) );
			pendingPlaytimeSeconds = 0f;
		}

		SetValue( "last_session_seconds", MathF.Floor( sessionSeconds ) );
		SetValue( "current_session_seconds", MathF.Floor( sessionSeconds ) );
		RecordPlayerSnapshot( playerData );
		Flush();
	}

	public static void RecordSaveWritten()
	{
		Increment( "saves_written" );
	}

	public static void RecordSaveLoaded()
	{
		Increment( "save_load_success" );
	}

	public static void RecordSaveMissing()
	{
		Increment( "save_file_missing" );
	}

	public static void RecordSaveFailed()
	{
		Increment( "save_load_failed" );
		Flush();
	}

	public static void RecordCoinsEarned( int finalAmount, int baseAmount )
	{
		if ( finalAmount <= 0 )
			return;

		Increment( "coins_earned", finalAmount );
		Increment( "coins_earned_base", Math.Max( 0, baseAmount ) );

		var bonus = finalAmount - baseAmount;
		if ( bonus > 0 )
		{
			Increment( "coins_earned_from_pet_bonus", bonus );
		}
	}

	public static void RecordCoinsSpent( string source, int amount )
	{
		if ( amount <= 0 )
			return;

		Increment( "coins_spent", amount );
		Increment( $"coins_spent_{ToStatKeySuffix( source, "unknown" )}", amount );
	}

	public static void RecordPetAdded( string displayName, string prefabPath, GameObject petPrefab )
	{
		var petName = GetPetName( displayName, petPrefab );
		var petKey = ToStatKeySuffix( petName, "unknown_pet" );
		var data = PetData( petName, prefabPath );

		Increment( "pets_added_total", 1, data );
		Increment( $"pets_added_{petKey}", 1, data );
		Increment( $"pet_added_{petKey}", 1, data );

		if ( !string.IsNullOrWhiteSpace( petName ) )
		{
			Increment( petName, 1, data );
		}
	}

	public static void RecordPetHatched( string displayName, string prefabPath, PetRarity rarity, int crateCost )
	{
		var petKey = ToStatKeySuffix( displayName, "unknown_pet" );
		var rarityKey = ToStatKeySuffix( rarity.ToString(), "unknown" );
		var data = PetData( displayName, prefabPath );
		data["rarity"] = rarity.ToString();
		data["crate_cost"] = crateCost;

		Increment( "petshatched", 1, data );
		Increment( "pets_hatched", 1, data );
		Increment( $"pets_hatched_{petKey}", 1, data );
		Increment( $"pets_hatched_rarity_{rarityKey}", 1, data );
		Increment( $"crate_rewards_{rarityKey}", 1, data );
	}

	public static void RecordPetEquipped( string displayName, string prefabPath )
	{
		var petKey = ToStatKeySuffix( displayName, "unknown_pet" );
		var data = PetData( displayName, prefabPath );

		Increment( "pets_equipped", 1, data );
		Increment( $"pets_equipped_{petKey}", 1, data );
	}

	public static void RecordPetUnequipped( string displayName, string prefabPath )
	{
		var petKey = ToStatKeySuffix( displayName, "unknown_pet" );
		var data = PetData( displayName, prefabPath );

		Increment( "pets_unequipped", 1, data );
		Increment( $"pets_unequipped_{petKey}", 1, data );
	}

	public static void RecordPetRemoved( string displayName, string prefabPath )
	{
		var petKey = ToStatKeySuffix( displayName, "unknown_pet" );
		var data = PetData( displayName, prefabPath );

		Increment( "pets_removed", 1, data );
		Increment( $"pets_removed_{petKey}", 1, data );
	}

	public static void RecordPetMerged( string displayName, string prefabPath, PetRarity fromRarity, PetRarity toRarity )
	{
		var petKey = ToStatKeySuffix( displayName, "unknown_pet" );
		var fromKey = ToStatKeySuffix( fromRarity.ToString(), "unknown" );
		var toKey = ToStatKeySuffix( toRarity.ToString(), "unknown" );
		var data = PetData( displayName, prefabPath );
		data["from_rarity"] = fromRarity.ToString();
		data["to_rarity"] = toRarity.ToString();

		Increment( "pets_merged", 1, data );
		Increment( $"pets_merged_{petKey}", 1, data );
		Increment( $"pets_merged_from_{fromKey}", 1, data );
		Increment( $"pets_merged_to_{toKey}", 1, data );
		Flush();
	}

	public static void RecordPetAttack( string displayName, string targetName, int damage )
	{
		var petKey = ToStatKeySuffix( displayName, "unknown_pet" );
		var targetKey = ToStatKeySuffix( targetName, "object" );
		var data = new Dictionary<string, object>
		{
			["pet"] = displayName ?? "",
			["target"] = targetName ?? "",
			["damage"] = damage
		};

		Increment( "pet_attacks_landed", 1, data );
		Increment( $"pet_attacks_{petKey}", 1, data );
		Increment( $"pet_attacks_target_{targetKey}", 1, data );
	}

	public static void RecordCratePurchaseAttempt( int cost )
	{
		Increment( "crate_purchase_attempts", 1, new Dictionary<string, object> { ["cost"] = cost } );
	}

	public static void RecordCratePurchaseFailed( string reason, int cost )
	{
		var reasonKey = ToStatKeySuffix( reason, "unknown" );
		var data = new Dictionary<string, object>
		{
			["reason"] = reason,
			["cost"] = cost
		};

		Increment( "crate_purchase_failed", 1, data );
		Increment( $"crate_purchase_failed_{reasonKey}", 1, data );
		Flush();
	}

	public static void RecordCratePurchased( int cost )
	{
		Increment( "crate_purchase_success", 1, new Dictionary<string, object> { ["cost"] = cost } );
		Increment( "crates_purchased", 1 );
		RecordCoinsSpent( "crates", cost );
	}

	public static void RecordCrateOpened( string displayName, string prefabPath, PetRarity rarity, int cost )
	{
		var data = PetData( displayName, prefabPath );
		data["rarity"] = rarity.ToString();
		data["cost"] = cost;

		Increment( "crates_opened", 1, data );
		RecordPetHatched( displayName, prefabPath, rarity, cost );
		Flush();
	}

	public static void RecordCrateRefunded( int cost )
	{
		Increment( "crate_refunds", 1, new Dictionary<string, object> { ["cost"] = cost } );
		Increment( "coins_refunded", cost );
		Flush();
	}

	public static void RecordDoorUnlockAttempt( string key, int cost )
	{
		Increment( "door_unlock_attempts", 1, DoorData( key, cost ) );
	}

	public static void RecordDoorUnlockFailed( string key, int cost, string reason )
	{
		var reasonKey = ToStatKeySuffix( reason, "unknown" );
		var data = DoorData( key, cost );
		data["reason"] = reason;

		Increment( "door_unlock_failed", 1, data );
		Increment( $"door_unlock_failed_{reasonKey}", 1, data );
		Flush();
	}

	public static void RecordDoorUnlocked( string key, int cost )
	{
		var doorKey = ToStatKeySuffix( key, "unknown_door" );
		var data = DoorData( key, cost );

		Increment( "doors_unlocked", 1, data );
		Increment( $"door_unlocked_{doorKey}", 1, data );
		RecordCoinsSpent( "doors", cost );
		Flush();
	}

	public static void RecordObjectDamage( string objectName, int damage, bool destroyed, bool petDamage )
	{
		if ( damage <= 0 )
			return;

		var objectKey = ToStatKeySuffix( objectName, "object" );
		var source = petDamage ? "pet" : "manual";
		var data = new Dictionary<string, object>
		{
			["object"] = objectName,
			["damage"] = damage,
			["source"] = source,
			["destroyed"] = destroyed
		};

		Increment( "objects_hit", 1, data );
		Increment( $"{source}_object_hits", 1, data );
		Increment( "object_damage_dealt", damage, data );
		Increment( $"{source}_damage_dealt", damage, data );
		Increment( $"object_hits_{objectKey}", 1, data );

		if ( !destroyed )
			return;

		Increment( "objects_destroyed", 1, data );
		Increment( $"objects_destroyed_by_{source}", 1, data );
		Increment( $"destroyed_object_{objectKey}", 1, data );
		Flush();
	}

	public static void RecordInteraction( string interactableType, string objectName )
	{
		var typeKey = ToStatKeySuffix( interactableType, "unknown" );
		var objectKey = ToStatKeySuffix( objectName, "object" );
		var data = new Dictionary<string, object>
		{
			["interactable_type"] = interactableType ?? "",
			["object"] = objectName ?? ""
		};

		Increment( "interactions_used", 1, data );
		Increment( $"interactions_{typeKey}", 1, data );
		Increment( $"interacted_object_{objectKey}", 1, data );
	}

	public static void RecordPlayerSnapshot( PlayerData playerData )
	{
		if ( playerData == null )
			return;

		var inventory = playerData.inventory ?? playerData.GetComponent<Inventory>();
		SetValue( "current_money", playerData.PlayerMoney );
		SetValue( "current_session_seconds", MathF.Floor( sessionSeconds ) );

		if ( inventory == null )
			return;

		SetValue( "current_pet_count", inventory.Count );
		SetValue( "current_pet_inventory_size", inventory.InventorySize );
		SetValue( "current_equipped_pet_count", inventory.GetEquippedSlotIndexes().Count );
	}

	private static void Increment( string name, double amount = 1, Dictionary<string, object> data = null )
	{
		if ( string.IsNullOrWhiteSpace( name ) || Math.Abs( amount ) <= double.Epsilon )
			return;

		try
		{
			if ( data == null )
			{
				Stats.Increment( name, amount );
			}
			else
			{
				Stats.Increment( name, amount, data );
			}
		}
		catch ( Exception exception )
		{
			Log.Warning( exception, $"Failed to increment stat '{name}'." );
		}
	}

	private static void SetValue( string name, double value, Dictionary<string, object> data = null )
	{
		if ( string.IsNullOrWhiteSpace( name ) )
			return;

		try
		{
			if ( data == null )
			{
				Stats.SetValue( name, value );
			}
			else
			{
				Stats.SetValue( name, value, data );
			}
		}
		catch ( Exception exception )
		{
			Log.Warning( exception, $"Failed to set stat '{name}'." );
		}
	}

	private static void Flush()
	{
		try
		{
			Stats.Flush();
		}
		catch ( Exception exception )
		{
			Log.Warning( exception, "Failed to flush stats." );
		}
	}

	private static Dictionary<string, object> PetData( string displayName, string prefabPath )
	{
		return new Dictionary<string, object>
		{
			["pet"] = displayName ?? "",
			["pet_key"] = ToStatKeySuffix( displayName, "unknown_pet" ),
			["prefab"] = prefabPath ?? ""
		};
	}

	private static Dictionary<string, object> DoorData( string key, int cost )
	{
		return new Dictionary<string, object>
		{
			["door_key"] = key ?? "",
			["cost"] = cost
		};
	}

	private static string GetPetName( string displayName, GameObject petPrefab )
	{
		if ( !string.IsNullOrWhiteSpace( displayName ) )
			return displayName;

		if ( !petPrefab.IsValid() )
			return "Unknown Pet";

		var component = petPrefab.GetComponent<PetComponent>() ?? petPrefab.GetComponentInChildren<PetComponent>();
		if ( !string.IsNullOrWhiteSpace( component?.DisplayName ) )
			return component.DisplayName;

		return petPrefab.IsValid() ? petPrefab.Name : "Unknown Pet";
	}

	public static string ToStatKeySuffix( string value, string fallback )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
			return fallback;

		var builder = new StringBuilder();
		var lastWasSeparator = false;

		foreach ( var character in value.Trim().ToLowerInvariant() )
		{
			if ( char.IsLetterOrDigit( character ) )
			{
				builder.Append( character );
				lastWasSeparator = false;
			}
			else if ( !lastWasSeparator )
			{
				builder.Append( '_' );
				lastWasSeparator = true;
			}
		}

		return builder.ToString().Trim( '_' ) is { Length: > 0 } result ? result : fallback;
	}
}
