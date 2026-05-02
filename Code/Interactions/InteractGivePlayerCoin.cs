using Sandbox;
public sealed class InteractGivePlayerCoin : Interactable
{


	[Property, Sync] public int health { get; set; } = 10;
	[Property, Sync] public int maxHealth { get; set; } = 10;

	[Property] public int moneyPerHit = 1;
	[Property] public int moneyWhenDestroyed = 5;

	private float hitTimeScale = 0f;
	private Vector3 startPos;
	private Vector3 dirOffset;


	private WorldSpaceHealthbar healthbar;


	protected override void OnStart()
	{

	}

	protected override void OnUpdate()
	{
		if(!IsProxy)
		{


			if ( hitTimeScale > 0)
			{
				hitTimeScale -= Time.Delta * 5f;
				if( hitTimeScale < 0) { hitTimeScale = 0; }

				GameObject.WorldPosition = startPos + (dirOffset * hitTimeScale * 10f);

			}
			else
			{
				startPos = GameObject.WorldPosition;
			}
		}
	}

	[Rpc.Host]
	void CreateHealthbar()
	{
		GameObject clone = GameObject.GetPrefab( "Prefabs/CoinHealthbar.prefab" ).Clone();
		healthbar = clone.GetComponentInChildren<WorldSpaceHealthbar>();
		healthbar.health = health;
		healthbar.maxHealth = maxHealth;

		clone.Parent = GameObject;
		clone.LocalPosition = Vector3.Zero;
		clone.NetworkSpawn();
	}

	void UpdateHealthbar( PlayerController interactingPlayer=null )
	{
		if ( healthbar == null )
		{
			if ( Network.IsOwner )
			{
				CreateHealthbar();
			}
			else
			{
				healthbar = GetComponentInChildren<WorldSpaceHealthbar>();
			}
		}
		if ( healthbar == null ) return;

		healthbar.health = health;
		healthbar.maxHealth = maxHealth;

		if ( healthbar.health <= 0 )
		{
			if ( interactingPlayer != null )
			{
				interactingPlayer.GetComponent<PlayerData>().PlayerMoney += moneyWhenDestroyed;
			}
			GameObject.Destroy();
		}
		else
		{

			if ( interactingPlayer != null )
			{
				interactingPlayer.GetComponent<PlayerData>().PlayerMoney += moneyPerHit;
			}
		}

	}


	[Rpc.Broadcast]
	public override void OnInteract( PlayerController interactingPlayer )
	{

		if(!IsProxy)
		{
			hitTimeScale = 1f;
			dirOffset = new Vector3( (float)Game.Random.NextDouble() - 0.5f, (float)Game.Random.NextDouble() - 0.5f, (float)Game.Random.NextDouble() - 0.5f ).Normal;
		}
		health -= 1;
		UpdateHealthbar( interactingPlayer );
	}
}
