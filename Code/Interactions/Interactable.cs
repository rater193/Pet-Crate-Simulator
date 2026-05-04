using Sandbox;

public class Interactable : Component
{

	//[Property] public string text;
	[Property] public Vector3 InteractableShortcutOffset = Vector3.Zero;

	[Rpc.Broadcast]
	public virtual void OnInteract( PlayerController interactingPlayer)
	{
		GameObject.Destroy();
	}
}
