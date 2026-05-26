using Sandbox;

public sealed class InventoryPetSlot : Component
{
	[Property] public string DisplayName { get; set; } = "Pet";
	[Property] public GameObject PetPrefab { get; set; }
	[Property] public string PetPrefabPath { get; set; }
	[Property] public PetRarity Rarity { get; set; } = PetRarity.Common;

	public bool HasPet => GetPetPrefab().IsValid();
	public float RarityValueMultiplier => Rarity.GetValueMultiplier();

	public GameObject GetPetPrefab()
	{
		if ( PetPrefab.IsValid() )
			return PetPrefab;

		// 1. Direct path resolve.
		if ( !string.IsNullOrWhiteSpace( PetPrefabPath ) )
		{
			var direct = GameObject.GetPrefab( PetPrefabPath );
			if ( direct.IsValid() )
			{
				PetPrefab = direct;
				return direct;
			}
		}

		// 2. Fallback: resolve via the scene's known pet prefabs by filename / display name. This
		//    recovers pets saved with a stale/wrong path (e.g. a missing subfolder), and self-heals
		//    the stored path to a resolvable one so future loads are direct.
		var resolved = PetPrefabResolver.Resolve( Scene, PetPrefabPath, DisplayName );
		if ( resolved.IsValid() )
		{
			PetPrefab = resolved;
			if ( !string.IsNullOrWhiteSpace( resolved.PrefabInstanceSource ) )
				PetPrefabPath = resolved.PrefabInstanceSource;

			return resolved;
		}

		return null;
	}

	public JSONObject ToJsonObject()
	{
		return new JSONObject()
			.Set( "DisplayName", DisplayName )
			.Set( "PetPrefabPath", PetPrefabPath )
			.Set( "Rarity", Rarity.ToString() );
	}

	public string ToJson()
	{
		return JSONObject.ToJson( ToJsonObject() );
	}

	public void LoadJson( string jsonData )
	{
		LoadJson( JSONObject.FromJson( jsonData ) );
	}

	public void LoadJson( JSONObject data )
	{
		if ( data == null )
			return;

		DisplayName = data.Get( "DisplayName", DisplayName );
		PetPrefabPath = data.Get( "PetPrefabPath", PetPrefabPath );
		Rarity = ParseRarity( data.Get( "Rarity", Rarity.ToString() ) );
		PetPrefab = null;
	}

	public bool IsSamePetAndRarity( InventoryPetSlot other )
	{
		if ( !other.IsValid() )
			return false;

		return Rarity == other.Rarity
			&& string.Equals( PetPrefabPath, other.PetPrefabPath, System.StringComparison.OrdinalIgnoreCase );
	}

	public static void ApplyRarityToPetComponent( PetComponent component, PetRarity rarity )
	{
		if ( component == null )
			return;

		var multiplier = rarity.GetValueMultiplier();
		component.CoinMultiplier *= multiplier;
		component.Damage = System.Math.Max( 0, (int)System.MathF.Round( component.Damage * multiplier ) );
		component.AttackInterval = System.MathF.Max( 0.05f, component.AttackInterval / multiplier );
	}

	private static PetRarity ParseRarity( string value )
	{
		return System.Enum.TryParse<PetRarity>( value, true, out var rarity )
			? rarity
			: PetRarity.Common;
	}
}
