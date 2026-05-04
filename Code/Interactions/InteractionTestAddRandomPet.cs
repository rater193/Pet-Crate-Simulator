using Sandbox;

public sealed class InteractionTestAddRandomPet : Interactable
{
	[Property] List<GameObject> petList;

	protected override void OnStart()
	{
		this.text = "Give random pet";
	}
	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( !interactingPlayer.IsProxy )
		{

			Inventory inv = interactingPlayer.GetComponent<PlayerData>().inventory;
			inv.InventorySize = 100;

			/*
			while( inv.Count > 0)
			{
				inv.RemovePet( 0 );
			}
			*/


			if ( inv.AddPetPrefab( petList[Game.Random.Next( petList.Count )] ) )
			{
				inv.EquipPet( inv.Count - 1 );
				Log.Info( inv.ToJson() );
			}

		}
	}

	protected override void OnUpdate()
	{

	}
}
