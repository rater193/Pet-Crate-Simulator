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

		if ( string.IsNullOrWhiteSpace( PetPrefabPath ) )
			return null;

		return GameObject.GetPrefab( PetPrefabPath );
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
		component.AttackRangeMultiplier *= multiplier;
		component.AttackLungeMultiplier *= multiplier;
		component.AttackInterval = System.MathF.Max( 0.05f, component.AttackInterval / multiplier );
	}

	private static PetRarity ParseRarity( string value )
	{
		return System.Enum.TryParse<PetRarity>( value, true, out var rarity )
			? rarity
			: PetRarity.Common;
	}
}
