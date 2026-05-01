using Sandbox;
public sealed class InteractGivePlayerCoin : Interactable
{


	[Property, Sync] public int health { get; set; } = 10;
	[Property, Sync] public int maxHealth { get; set; } = 10;

	[Property] public int moneyPerHit = 1;
	[Property] public int moneyWhenDestroyed = 5;


	private WorldSpaceHealthbar healthbar;


	protected override void OnStart()
	{

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
		health -= 1;
		UpdateHealthbar( interactingPlayer );
	}
}
