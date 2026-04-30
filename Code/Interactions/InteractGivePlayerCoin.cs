using Sandbox;

public sealed class InteractGivePlayerCoin : Interactable
{

	[Rpc.Broadcast]
	public override void OnInteract( PlayerController interactingPlayer )
	{
		if( interactingPlayer.IsProxy)
		{
			Log.Info( "IS PROXY" );
		}
		else
		{
			Log.Info( "NOT PROXY" );
		}
	}
}
