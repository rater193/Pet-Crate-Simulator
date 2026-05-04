using Sandbox;
using System.Numerics;
using static Sandbox.PhysicsContact;

public sealed class PlayerInteractionsController : Component
{
	GameObject interactionObject;
	InteractionFrameworkUI interactionUi;

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if( interactionObject == null)
		{
			interactionObject = GameObject.GetPrefab( "Prefabs/UI/interactionsenginepanel.prefab" ).Clone();
			interactionUi = interactionObject.GetComponent<InteractionFrameworkUI>();
		}

		var tar = interactionObject;
		if (tar != null )
		{
			// Example in s&box
			var camera = Scene.Camera;
			var startPos = camera.WorldPosition;
			var direction = camera.WorldRotation.Forward;
			var endPos = startPos + (direction * 400f); // 1000f is distance

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
					tar.WorldPosition = tr.Collider.WorldPosition + interactable.InteractableShortcutOffset;// * tr.Collider.GetComponent<Interactable>().LocalInteractionPromptOffset;
					interactionUi ??= tar.GetComponent<InteractionFrameworkUI>();
					interactionUi?.SetPromptText( interactable.text );

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
