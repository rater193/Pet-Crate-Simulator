using Sandbox;

public sealed class Test_InteractEquipPet : Interactable
{

	[Property] List<GameObject> prefabsToSpawn = new List<GameObject>();
	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( interactingPlayer.IsProxy ) return;
		PetFramework.EquipPet(prefabsToSpawn[Game.Random.Next( prefabsToSpawn.Count )]);
	}
}
