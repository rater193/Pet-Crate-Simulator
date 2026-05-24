using Sandbox;
using System.Numerics;
using static Sandbox.PhysicsContact;

public sealed class PlayerInteractionsController : Component
{
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

		var camera = Scene.Camera;
		if ( camera == null )
			return;

		var startPos = camera.WorldPosition;
		var direction = camera.WorldRotation.Forward;
		var endPos = startPos + (direction * 400f);

		var tr = Scene.Trace.Ray( startPos, endPos )
			.UseHitboxes( true )
			.IgnoreGameObject( GameObject ) // Ignore the player
			.Run();

		if ( !tr.Hit )
			return;

		var interactable = tr.Collider.Components.Get<Interactable>( FindMode.EverythingInSelfAndAncestors );
		if ( !interactable.IsValid() || IsOwnInteractable( interactable ) )
			return;

		IsHoveringInteractable = true;
		HoverPromptText = interactable.text;

		if ( Input.Pressed( "Use" ) )
		{
			GameStatsTracker.RecordInteraction( interactable.GetType().Name, interactable.GameObject.Name );
			interactable.OnInteract( GetComponent<PlayerController>() );
		}
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
