using Sandbox;

public class Interactable : Component
{

	//[Property] public string text;

	[Rpc.Broadcast]
	public virtual void OnInteract( PlayerController interactingPlayer)
	{
		GameObject.Destroy();
	}
}
