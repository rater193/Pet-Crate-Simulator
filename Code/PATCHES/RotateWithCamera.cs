using Sandbox;
using System.Numerics;

public sealed class RotateWithCamera : Component
{

	protected override void OnPreRender()
	{
		WorldRotation = Scene.Camera.WorldRotation.RotateAroundAxis( Vector3.Up, 180 );
	}
}
