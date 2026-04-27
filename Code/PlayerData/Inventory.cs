using Sandbox;

public sealed class Inventory : Component
{
	[Property] int InventorySize = 10;
	private List<InventoryItem> items = new List<InventoryItem>();
}
