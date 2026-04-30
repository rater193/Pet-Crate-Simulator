using Sandbox;
public sealed class InteractGivePlayerCoin : Interactable
{

	[Rpc.Broadcast]
	public override void OnInteract( PlayerController interactingPlayer )
	{
		if(interactingPlayer.Network.IsOwner)
		{
			PlayerData.LOCALDATA.PlayerMoney += 5;
		}
		else
		{

		}
	}
}
