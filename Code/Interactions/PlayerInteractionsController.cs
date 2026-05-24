using Sandbox;
using System.Numerics;
using static Sandbox.PhysicsContact;

public sealed class PlayerInteractionsController : Component
{
	GameObject interactionObject;

	public static PlayerInteractionsController Local { get; private set; }

	/// <summary>True while the local player is aiming at a valid interactable object.</summary>
	public bool IsHoveringInteractable { get; private set; }

	/// <summary>Prompt text of the interactable currently under the player's aim.</summary>
	public string HoverPromptText { get; private set; } = string.Empty;

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		Local = this;
		IsHoveringInteractable = false;
		HoverPromptText = string.Empty;

		// World-space prompt that shows the interact key icon on the object itself.
		interactionObject ??= GameObject.GetPrefab( "Prefabs/UI/interactionsenginepanel.prefab" ).Clone();

		var interactable = FindAimedInteractable();
		if ( !interactable.IsValid() )
		{
			if ( interactionObject.IsValid() )
				interactionObject.Enabled = false;

			return;
		}

		IsHoveringInteractable = true;
		HoverPromptText = interactable.text;

		if ( interactionObject.IsValid() )
		{
			interactionObject.Enabled = true;
			interactionObject.WorldPosition = interactable.GameObject.WorldPosition + interactable.InteractableShortcutOffset;
		}

		if ( Input.Pressed( "Use" ) )
		{
			GameStatsTracker.RecordInteraction( interactable.GetType().Name, interactable.GameObject.Name );
			interactable.OnInteract( GetComponent<PlayerController>() );
		}
	}

	private Interactable FindAimedInteractable()
	{
		var camera = Scene.Camera;
		if ( camera == null )
			return null;

		var startPos = camera.WorldPosition;
		var direction = camera.WorldRotation.Forward;
		var endPos = startPos + (direction * 400f);

		var tr = Scene.Trace.Ray( startPos, endPos )
			.UseHitboxes( true )
			.IgnoreGameObject( GameObject ) // Ignore the player
			.Run();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
			return null;

		// Use the hit GameObject (not the collider) so this works for both collider hits (crates,
		// doors) and hitbox hits (players, whose body colliders are flagged IgnoreTraces so the
		// camera doesn't clip on them).
		var interactable = tr.GameObject.Components.Get<Interactable>( FindMode.EverythingInSelfAndAncestors );
		if ( !interactable.IsValid() || IsOwnInteractable( interactable ) )
			return null;

		return interactable;
	}

	protected override void OnDestroy()
	{
		if ( Local == this )
			Local = null;
	}

	private bool IsOwnInteractable( Interactable interactable )
	{
		for ( var current = interactable.GameObject; current.IsValid(); current = current.Parent )
		{
			if ( current == GameObject )
				return true;
		}

		return false;
	}
}
