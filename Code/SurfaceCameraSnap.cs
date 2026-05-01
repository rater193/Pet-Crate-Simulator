using Sandbox;

public sealed class SurfaceCameraSnap : Component
{
	[Property] public GameObject Visual { get; set; }

	protected override void OnPreRender()
	{
		if ( !Visual.IsValid() )
			return;

		if ( !Scene.Camera.IsValid() )
			return;

		Visual.WorldRotation = Scene.Camera.WorldRotation * Rotation.FromYaw( 180f );
	}
}
