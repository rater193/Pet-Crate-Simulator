using Sandbox;

public sealed class InteractionTestRemovePets : Interactable
{

	protected override void OnStart()
	{
		this.text = "Remove all pets";
	}
	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( !interactingPlayer.IsProxy )
		{

			Inventory inv = interactingPlayer.GetComponent<PlayerData>().inventory;
			inv.InventorySize = 100;

			while( inv.Count > 0)
			{
				inv.RemovePet( 0 );
			}
			


		}
	}

	protected override void OnUpdate()
	{

	}
}
