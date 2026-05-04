using Sandbox;

public sealed class InventoryPetSlot : Component
{
	[Property] public string DisplayName { get; set; } = "Pet";
	[Property] public GameObject PetPrefab { get; set; }
	[Property] public string PetPrefabPath { get; set; }

	public bool HasPet => GetPetPrefab().IsValid();

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
			.Set( "PetPrefabPath", PetPrefabPath );
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
		PetPrefab = null;
	}
}
