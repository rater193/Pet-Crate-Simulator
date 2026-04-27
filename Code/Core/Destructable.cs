using Sandbox;

public sealed class Destructable : Component
{
	[Sync( SyncFlags.FromHost ), Property] public int hp { get; set; } = 5;
	private int maxhp = 5;
	private DestructableNameplate nameplate;

	protected override void OnStart()
	{
		nameplate = GetComponentInChildren<DestructableNameplate>();
		nameplate.GameObject.Enabled = false;

		maxhp = hp;
	}

}
