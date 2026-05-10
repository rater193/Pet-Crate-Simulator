using Sandbox;

public sealed class InteractPlayerDetails : Interactable
{
	protected override void OnStart()
	{
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			text = "View Player";
		}
	}

	[Rpc.Broadcast]
	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( interactingPlayer.IsProxy )
			return;

		if ( interactingPlayer.GameObject == GameObject )
			return;

		PlayerTradeController.LOCAL?.OpenPlayerDetails( GameObject );
	}
}
