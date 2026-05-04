using Sandbox;

public sealed class Test_VerifyJsonObject : Interactable
{

	[Property] List<GameObject> petList;

	protected override void OnStart()
	{
		this.text = "Verify JSON structure";
	}

	public override void OnInteract( PlayerController interactingPlayer )
	{
		var data = new JSONObject()
			.Set( "name", "Dog" )
			.Set( "coins", 25 )
			.Set( "pet", new JSONObject()
			.Set( "damage", 3 )
			.Set( "multiplier", 1.5f ) );

		string json = JSONObject.ToJson( data );
		Log.Info( json );

		JSONObject loaded = JSONObject.FromJson( json );

		bool hasPet = loaded.Exists( "pet" );
		loaded.Remove( "coins" );
		Log.Info( loaded.ToJson() );
		
	}

	protected override void OnUpdate()
	{

	}
}
