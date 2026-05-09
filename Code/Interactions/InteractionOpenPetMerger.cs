using Sandbox;

public sealed class InteractionOpenPetMerger : Interactable
{
	protected override void OnStart()
	{
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			text = "Merge Pets";
		}
	}

	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( interactingPlayer == null || interactingPlayer.IsProxy )
			return;

		PlayerHud.OpenPetMerger();
	}
}
