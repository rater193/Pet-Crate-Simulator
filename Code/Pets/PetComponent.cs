using Sandbox;

public sealed class PetComponent : Component
{
	[Property] public string DisplayName { get; set; } = "Pet";
	[Property] public float CoinMultiplier { get; set; } = 1f;
	[Property] public int Damage { get; set; } = 1;
	[Property] public float AttackInterval { get; set; } = 1f;
	[Property] public float AttackRangeMultiplier { get; set; } = 1f;
	[Property] public float MoveSpeedMultiplier { get; set; } = 1f;
	[Property] public float BobHeightMultiplier { get; set; } = 1f;
	[Property] public float AttackLungeMultiplier { get; set; } = 1f;
}
