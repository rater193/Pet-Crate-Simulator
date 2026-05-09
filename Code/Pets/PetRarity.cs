using Sandbox;
using System;

public enum PetRarity
{
	Common,
	Uncommon,
	Rare,
	Epic,
	Legendary,
	Mythic,
	Ancestral
}

public static class PetRarityExtensions
{
	public static PetRarity NextRarity( this PetRarity rarity )
	{
		return rarity >= PetRarity.Ancestral ? PetRarity.Ancestral : rarity + 1;
	}

	public static bool CanMergeUp( this PetRarity rarity )
	{
		return rarity < PetRarity.Ancestral;
	}

	public static float GetValueMultiplier( this PetRarity rarity )
	{
		return MathF.Pow( 2f, Math.Clamp( (int)rarity, 0, (int)PetRarity.Ancestral ) );
	}

	public static Color GetColor( this PetRarity rarity )
	{
		return rarity switch
		{
			PetRarity.Common => Color.White,
			PetRarity.Uncommon => new Color( 0.25f, 1f, 0.36f ),
			PetRarity.Rare => new Color( 0.24f, 0.56f, 1f ),
			PetRarity.Epic => new Color( 0.7f, 0.34f, 1f ),
			PetRarity.Legendary => new Color( 1f, 0.56f, 0.16f ),
			PetRarity.Mythic => new Color( 1f, 0.84f, 0.22f ),
			PetRarity.Ancestral => new Color( 1f, 0.18f, 0.18f ),
			_ => Color.White
		};
	}

	public static string GetCssColor( this PetRarity rarity )
	{
		return rarity switch
		{
			PetRarity.Common => "#ffffff",
			PetRarity.Uncommon => "#4bff5c",
			PetRarity.Rare => "#3d8fff",
			PetRarity.Epic => "#b356ff",
			PetRarity.Legendary => "#ff8f28",
			PetRarity.Mythic => "#ffd740",
			PetRarity.Ancestral => "#ff3434",
			_ => "#ffffff"
		};
	}

	public static string GetCssClass( this PetRarity rarity )
	{
		return rarity.ToString().ToLowerInvariant();
	}
}
