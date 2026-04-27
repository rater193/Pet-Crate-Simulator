using Sandbox;

public class InteractableDestructable : Interactable
{
	[Sync] public int _hp { get; set; } = 5;
	[Property] public int hp { get; set; } = 5;
	[Property] public int hpto { get; set; } = 5;


	[Rpc.Broadcast]
	public override void OnInteract( PlayerController interactingPlayer )
	{

	}


}
