using Sandbox;

public sealed class InteractLockedDoor : Interactable
{
	[Property] public int unlockCost { get; set; } = 25;

	private bool hasUnlocked = false;

	protected override void OnFixedUpdate()
	{
		var collider = GameObject.Components.Get<BoxCollider>( FindMode.EverythingInSelf );
		var door = GameObject.Components.Get<UnlockableDoor>( FindMode.EverythingInChildren );
		door.unlockValue = unlockCost;
		collider.Enabled = !hasUnlocked;
		door.Enabled = !hasUnlocked;
	}

	[Rpc.Broadcast]
	public override void OnInteract( PlayerController interactingPlayer )
	{
		Log.Info( "interactingPlayer.IsProxy: " + interactingPlayer.IsProxy );
		Log.Info( "hasUnlocked: " + hasUnlocked );
		if ( !interactingPlayer.IsProxy && hasUnlocked == false )
		{
			PlayerData playerData = interactingPlayer.GetComponent<PlayerData>();
			if ( playerData.PlayerMoney > unlockCost)
			{
				playerData.PlayerMoney -= unlockCost;
				GetComponentInChildren<UnlockableDoor>().Enabled = false;
				GetComponent<BoxCollider>().Enabled = false;
				hasUnlocked = true;
			}
		}
	}
}
