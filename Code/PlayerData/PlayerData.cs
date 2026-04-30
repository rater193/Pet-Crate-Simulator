using Sandbox;

public sealed class PlayerData : Component
{
	[Property, Sync] public int PlayerMoney { get; set; } = 0;
}
