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
				Vector3 originalScale = newObj.WorldScale;
				newObj.Parent = GameObject;
				newObj.LocalPosition = new Vector3( -25f + (x*2* GridSize / area.x*25), -25f + (y*2 * GridSize / area.y * 25), 0 );
				newObj.WorldPosition += new Vector3( (float)Game.Random.NextDouble()*25f, (float)Game.Random.NextDouble() * 25f, 0 );
				newObj.WorldRotation = Rotation.FromYaw( (float)Game.Random.NextDouble()*360f );
				float value = (float)Game.Random.NextDouble() * 100f;
				value = value * 0.001f;
				value += 1f;
				newObj.WorldScale = originalScale * value;
				newObj.NetworkSpawn();
			}
		}
	}

	protected override void OnUpdate()
	{

	}
}
