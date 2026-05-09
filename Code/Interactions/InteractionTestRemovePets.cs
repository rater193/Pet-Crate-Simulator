using Sandbox;

public sealed class InteractionTestRemovePets : Interactable
{

	protected override void OnStart()
	{
		this.text = "Player Core Data Reset";
	}
	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( !interactingPlayer.IsProxy )
		{
			PlayerData playerdata = interactingPlayer.GetComponent<PlayerData>();
			/*Inventory inv = playerdata.inventory;
			inv.InventorySize = 100;*/
			playerdata.PlayerMoney += 1000;
			/*
			while ( inv.Count > 0)
			{
				inv.RemovePet( 0 );
			}
			*/


		}
	}

	protected override void OnUpdate()
	{

	}
}
