using Sandbox;

public sealed class LockedDoorSaveState : PlayerDataSaveExtension
{
	[Property] public string Key { get; set; } = "";
	[Property] public InteractLockedDoor Door { get; set; }
	[Property] public bool DefaultUnlocked { get; set; }

	public bool IsUnlocked { get; private set; }
	private bool hasLoaded;

	protected override void OnStart()
	{
		Door ??= GetComponent<InteractLockedDoor>();
		if ( !hasLoaded )
		{
			IsUnlocked = DefaultUnlocked;
		}

		ApplyToDoor();
	}

	public override void OnSave( JSONObject jsonObject )
	{
		var resolvedKey = GetResolvedKey();
		if ( jsonObject == null || string.IsNullOrWhiteSpace( resolvedKey ) )
			return;

		jsonObject.Set( resolvedKey, IsUnlocked );
	}

	public override void OnLoad( JSONObject jsonObject )
	{
		var resolvedKey = GetResolvedKey();
		if ( jsonObject == null || string.IsNullOrWhiteSpace( resolvedKey ) )
			return;

		IsUnlocked = jsonObject.Get( resolvedKey, DefaultUnlocked );
		hasLoaded = true;
		ApplyToDoor();
	}

	public void SetUnlocked( bool unlocked, bool queueSave = true )
	{
		if ( IsUnlocked == unlocked )
			return;

		IsUnlocked = unlocked;
		ApplyToDoor();

		if ( queueSave )
		{
			PlayerData.LOCALDATA?.QueueSave();
		}
	}

	private void ApplyToDoor()
	{
		Door ??= GetComponent<InteractLockedDoor>();
		Door?.SetPersistedUnlocked( IsUnlocked );
	}

	private string GetResolvedKey()
	{
		if ( !string.IsNullOrWhiteSpace( Key ) )
			return Key;

		return $"Door:{GameObject.Id}";
	}
}
