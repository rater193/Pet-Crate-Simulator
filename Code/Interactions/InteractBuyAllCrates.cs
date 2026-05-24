using Sandbox;
using System.Collections.Generic;

/// <summary>
/// When used, triggers a purchase on every <see cref="InteractBuyCrate"/> in the same shop.
/// Runs locally (like the individual crate purchases), buying from each crate the player can
/// still afford / fit in their inventory.
/// </summary>
public sealed class InteractBuyAllCrates : Interactable
{
	/// <summary>
	/// Optional explicit shop root to search for crates. When unset, the button's parent
	/// (and, if it contains no crates, its ancestors) are searched instead.
	/// </summary>
	[Property] public GameObject ShopRoot { get; set; }

	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( interactingPlayer == null || interactingPlayer.IsProxy )
			return;

		foreach ( var crate in FindCrates() )
		{
			if ( crate.IsValid() )
				crate.OnInteract( interactingPlayer );
		}
	}

	private List<InteractBuyCrate> FindCrates()
	{
		var results = new List<InteractBuyCrate>();
		var root = ShopRoot.IsValid() ? ShopRoot : GameObject.Parent;
		var current = root.IsValid() ? root : GameObject;

		while ( current.IsValid() )
		{
			foreach ( var obj in current.GetAllObjects( true ) )
			{
				if ( obj == GameObject )
					continue;

				var crate = obj.GetComponent<InteractBuyCrate>();
				if ( crate.IsValid() && !results.Contains( crate ) )
					results.Add( crate );
			}

			// Found this shop's crates, or the user pinned an explicit root — stop here.
			if ( results.Count > 0 || ShopRoot.IsValid() )
				break;

			current = current.Parent;
		}

		return results;
	}
}
