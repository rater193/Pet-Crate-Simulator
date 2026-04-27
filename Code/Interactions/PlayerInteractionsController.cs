using Sandbox;
using System.Numerics;
using static Sandbox.PhysicsContact;

public sealed class PlayerInteractionsController : Component
{
	GameObject interactionObject;
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if( interactionObject == null)
		{
			interactionObject = GameObject.GetPrefab( "Prefabs/UI/interactionsenginepanel.prefab" ).Clone();
		}

		var tar = interactionObject;
		if (tar != null )
		{
			// Example in s&box
			var camera = Scene.Camera;
			var startPos = camera.WorldPosition;
			var direction = camera.WorldRotation.Forward;
			var endPos = startPos + (direction * 1000f); // 1000f is distance

			var tr = Scene.Trace.Ray( startPos, endPos )
				.UseHitboxes( true )
				.IgnoreGameObject( GameObject ) // Ignore the player
				.Run();

			if ( tr.Hit )
			{
				if ( tr.Collider.GetComponent<Interactable>() != null )
				{
					var interactable = tr.Collider.GetComponent<Interactable>();
					tar.Enabled = true;
					tar.WorldPosition = tr.Collider.WorldPosition;// * tr.Collider.GetComponent<Interactable>().LocalInteractionPromptOffset;
					if(Input.Pressed("Use"))
					{
						interactable.OnInteract( GetComponent<PlayerController>() );
					}
				}
				else
				{
					tar.Enabled = false;
				}
			}
			else
			{
				tar.Enabled = false;
			}
		}
	}
}
