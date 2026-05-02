using Sandbox;

public sealed class Test_InteractEquipPet : Interactable
{
	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( interactingPlayer.IsProxy ) return;
		PetFramework.EquipPet(GameObject.GetPrefab( "prefabs/pets/pet-bever.prefab" ).Clone());
	}
}
