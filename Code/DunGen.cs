using Sandbox;
using System;
using System.Numerics;

public sealed class DunGen : Component
{

	[Property] public List<GameObject> roomPrefabs;

	[Property] public int RoomsToSpawnMin, RoomsToSpawnMax;

	protected override void OnStart()
	{
		var StartPrefab = GetRandomRoom(0);


		OnTick( StartPrefab );
	}

	protected GameObject GetRandomRoom(int index)
	{
		return roomPrefabs[index].Clone();
	}

	protected void OnTick(GameObject target)
	{
		List<GameObject> connectors = target.Children;

		// Logic to connect rooms

		GameObject newPrefab = GetRandomRoom(1);

		GameObject node1 = target.Children[0];
		GameObject node2 = newPrefab.Children[0];

		node1.Name = "1";
		node2.Name = "2";
		//node1.Parent = null;
		//node2.Parent = null;


		//Step1: get rotation difference
		Rotation rDif = Rotation.Difference(node1.WorldRotation,node2.WorldRotation);

		//Step2: rotate by the difference
		//node2.WorldRotation = node2.WorldRotation * rDif;

		node2.Parent.WorldRotation = node2.Parent.WorldRotation * rDif * Rotation.FromAxis(Vector3.Up, 180);

		//Step3: Rotate by the up axis by 180 degrees
		Vector3 posDif = node1.WorldPosition - node2.WorldPosition;
		node2.Parent.WorldPosition += posDif;


	}
}
