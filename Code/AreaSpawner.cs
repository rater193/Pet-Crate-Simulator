using Sandbox;
using System;

public sealed class AreaSpawner : Component
{
	[Property] public List<GameObject> spawnlist;
	[Property] private float GridSize = 1f;

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		PopulateArea();
	}

	private void PopulateArea()
	{
		Random r = new Random();
		Vector2 area = new Vector2( (float)Math.Floor(WorldScale.x), (float)Math.Floor(WorldScale.y) );
		Log.Info( area );

		for ( int x = 0; x < area.x/ GridSize; x++ )
		{

			for ( int y = 0; y < area.y/ GridSize; y++ )
			{
				GameObject newObj = spawnlist[r.Next( spawnlist.Count)].Clone();
				Rotation newRotation = new Rotation(0, 0, Rotation.Random.z, 0);
				newObj.Parent = GameObject;
				newObj.LocalPosition = new Vector3( -25f + (x*2* GridSize / area.x*25), -25f + (y*2 * GridSize / area.y * 25), 0 );
				//newObj.WorldRotation = newObj.WorldRotation * newRotation;
				newObj.WorldScale = Vector3.One;
				newObj.NetworkSpawn();
			}
		}
	}

	protected override void OnUpdate()
	{

	}
}
