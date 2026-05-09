using Sandbox;

public sealed class InteractLockedDoor : Interactable
{
	[Property] public int unlockCost { get; set; } = 25;
	[Property] public LockedDoorSaveState SaveState { get; set; }

	private bool hasUnlocked = false;
	private BoxCollider doorCollider;
	private UnlockableDoor door;

	protected override void OnStart()
	{
		EnsureSaveState();
		ApplyLockState();
	}

	protected override void OnFixedUpdate()
	{
		ApplyLockState();
	}

	[Rpc.Broadcast]
	public override void OnInteract( PlayerController interactingPlayer )
	{
		Log.Info( "interactingPlayer.IsProxy: " + interactingPlayer.IsProxy );
		Log.Info( "hasUnlocked: " + hasUnlocked );
		if ( !interactingPlayer.IsProxy && hasUnlocked == false )
		{
			var saveState = EnsureSaveState();
			GameStatsTracker.RecordDoorUnlockAttempt( saveState.ResolvedKey, unlockCost );

			PlayerData playerData = interactingPlayer.GetComponent<PlayerData>();
			if ( playerData != null && playerData.PlayerMoney >= unlockCost )
			{
				playerData.PlayerMoney -= unlockCost;
				SetUnlocked( true );
				GameStatsTracker.RecordDoorUnlocked( saveState.ResolvedKey, unlockCost );
				playerData.QueueSave();
			}
			else
			{
				GameStatsTracker.RecordDoorUnlockFailed( saveState.ResolvedKey, unlockCost, "not_enough_money" );
			}
		}
	}

	public void SetUnlocked( bool unlocked )
	{
		EnsureSaveState().SetUnlocked( unlocked );
	}

	public void SetPersistedUnlocked( bool unlocked )
	{
		hasUnlocked = unlocked;
		ApplyLockState();
	}

	public LockedDoorSaveState EnsureSaveState()
	{
		SaveState ??= GetComponent<LockedDoorSaveState>() ?? GameObject.GetOrAddComponent<LockedDoorSaveState>();
		SaveState.Door ??= this;
		return SaveState;
	}

	private void ApplyLockState()
	{
		doorCollider ??= GameObject.Components.Get<BoxCollider>( FindMode.EverythingInSelf );
		door ??= GameObject.Components.Get<UnlockableDoor>( FindMode.EverythingInChildren );

		if ( door != null )
		{
			door.unlockValue = unlockCost;
			door.Enabled = !hasUnlocked;
		}

		if ( doorCollider != null )
		{
			doorCollider.Enabled = !hasUnlocked;
		}
	}
}
