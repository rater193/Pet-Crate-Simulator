using Sandbox;

/// <summary>
/// Global, reusable hover-tooltip state for pets. Any pet UI (inventory, forge, trade, ...)
/// calls <see cref="Show"/> on hover and <see cref="Hide"/> on hover-out. A single
/// <c>PetStatsTooltip</c> overlay (hosted by PlayerHud) renders the current info near the cursor.
/// </summary>
public static class PetTooltip
{
	public static PetTooltipInfo Current { get; private set; }
	public static bool IsVisible { get; private set; }

	// Bumped on every change so UI build hashes refresh.
	public static int Version { get; private set; }

	public static void Show( PetTooltipInfo info )
	{
		if ( info == null )
		{
			Hide();
			return;
		}

		Current = info;
		IsVisible = true;
		Version++;
	}

	public static void Hide()
	{
		if ( !IsVisible )
			return;

		IsVisible = false;
		Version++;
	}
}

/// <summary>
/// Detailed, rarity-scaled pet stats shown in the tooltip. <see cref="FromSlot"/> is the single
/// place pet stats are derived, so changing what's shown (or how it's scaled) happens here.
/// </summary>
public sealed class PetTooltipInfo
{
	public string DisplayName { get; set; } = "Pet";
	public PetRarity Rarity { get; set; } = PetRarity.Common;
	public float CoinMultiplier { get; set; } = 1f;
	public int Damage { get; set; } = 1;
	public float AttackInterval { get; set; } = 1f;
	public float AttackRangeMultiplier { get; set; } = 1f;
	public float MoveSpeedMultiplier { get; set; } = 1f;
	public float BobHeightMultiplier { get; set; } = 1f;
	public float AttackLungeMultiplier { get; set; } = 1f;

	public string RarityText => Rarity.ToString();
	public string RarityCssColor => Rarity.GetCssColor();
	public string CoinText => CoinMultiplier.ToString( "0.##" );
	public string DamageText => Damage.ToString();
	public string AttackIntervalText => AttackInterval.ToString( "0.##" );
	public string AttacksPerSecondText => (AttackInterval > 0.01f ? 1f / AttackInterval : 0f).ToString( "0.##" );
	public string AttackRangeText => AttackRangeMultiplier.ToString( "0.##" );
	public string MoveSpeedText => MoveSpeedMultiplier.ToString( "0.##" );
	public string BobHeightText => BobHeightMultiplier.ToString( "0.##" );
	public string LungeText => AttackLungeMultiplier.ToString( "0.##" );

	/// <summary>
	/// Builds detailed stats from an inventory slot, applying the same rarity scaling the game uses
	/// (see <see cref="InventoryPetSlot.ApplyRarityToPetComponent"/>): coins/damage scale up,
	/// attack interval scales down; the rest are per-pet base values.
	/// </summary>
	public static PetTooltipInfo FromSlot( InventoryPetSlot slot )
	{
		if ( !slot.IsValid() )
			return null;

		var prefab = slot.GetPetPrefab();
		var component = prefab?.GetComponent<PetComponent>() ?? prefab?.GetComponentInChildren<PetComponent>();
		var rarityMultiplier = slot.RarityValueMultiplier;
		var displayName = !string.IsNullOrWhiteSpace( component?.DisplayName ) ? component.DisplayName : slot.DisplayName;

		return new PetTooltipInfo
		{
			DisplayName = string.IsNullOrWhiteSpace( displayName ) ? "Pet" : displayName,
			Rarity = slot.Rarity,
			CoinMultiplier = System.MathF.Max( 0f, component?.CoinMultiplier ?? 1f ) * rarityMultiplier,
			Damage = System.Math.Max( 0, (int)System.MathF.Round( (component?.Damage ?? 1) * rarityMultiplier ) ),
			AttackInterval = System.MathF.Max( 0.05f, (component?.AttackInterval ?? 1f) / rarityMultiplier ),
			AttackRangeMultiplier = component?.AttackRangeMultiplier ?? 1f,
			MoveSpeedMultiplier = component?.MoveSpeedMultiplier ?? 1f,
			BobHeightMultiplier = component?.BobHeightMultiplier ?? 1f,
			AttackLungeMultiplier = component?.AttackLungeMultiplier ?? 1f
		};
	}
}
