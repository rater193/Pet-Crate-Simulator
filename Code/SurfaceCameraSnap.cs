using Sandbox;

public sealed class SurfaceCameraSnap : Component
{
	[Property] public GameObject Visual { get; set; }

	protected override void OnEnabled()
	{
		ApplyCameraRotation();
	}

	protected override void OnStart()
	{
		ApplyCameraRotation();
	}

	protected override void OnPreRender()
	{
		ApplyCameraRotation();
	}

	public void ApplyCameraRotation()
	{
		if ( !Visual.IsValid() )
			return;

		if ( Scene?.Camera.IsValid() != true )
			return;

		Visual.WorldRotation = Scene.Camera.WorldRotation * Rotation.FromYaw( 180f );
	}
}
