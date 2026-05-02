using Sandbox;

using System;

public sealed class PetFramework : Component
{
	public static PetFramework LOCAL;

	[Property] public Vector3 EquippedPetLocalPosition { get; set; } = new( 32f, -42f, 24f );
	[Property] public Rotation EquippedPetLocalRotation { get; set; } = Rotation.Identity;
	[Property] public bool NetworkSpawnEquippedPet { get; set; } = true;

	public GameObject EquippedPet { get; private set; }
	public PetComponent EquippedPetComponent { get; private set; }

	public float CoinMultiplier => GetEquippedPetComponent()?.CoinMultiplier ?? 1f;

	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			LOCAL = this;
		}
	}

	protected override void OnDestroy()
	{
		if ( LOCAL == this )
		{
			LOCAL = null;
		}
	}

	public static void EquipPet( GameObject prefab )
	{
		if ( LOCAL == null )
		{
			Log.Warning( "Tried to equip a pet before the local PetFramework was available." );
			return;
		}

		LOCAL.Equip( prefab );
	}

	public void Equip( GameObject prefab )
	{
		if ( IsProxy || !prefab.IsValid() )
			return;

		Unequip();

		var pet = prefab.Clone();
		pet.Parent = GameObject;
		pet.LocalPosition = EquippedPetLocalPosition;
		pet.LocalRotation = EquippedPetLocalRotation;

		EquippedPet = pet;
		EquippedPetComponent = pet.GetComponent<PetComponent>() ?? pet.GetComponentInChildren<PetComponent>();

		if ( EquippedPetComponent == null )
		{
			Log.Warning( $"Equipped pet prefab '{prefab.Name}' does not have a PetComponent." );
		}

		if ( NetworkSpawnEquippedPet )
		{
			pet.NetworkSpawn();
		}
	}

	public void Unequip()
	{
		if ( EquippedPet.IsValid() )
		{
			EquippedPet.Destroy();
		}

		EquippedPet = null;
		EquippedPetComponent = null;
	}

	public int ApplyCoinMultiplier( int baseCoins )
	{
		if ( baseCoins <= 0 )
			return 0;

		return Math.Max( 0, (int)MathF.Round( baseCoins * CoinMultiplier ) );
	}

	private PetComponent GetEquippedPetComponent()
	{
		if ( EquippedPetComponent.IsValid() )
			return EquippedPetComponent;

		if ( EquippedPet.IsValid() )
		{
			EquippedPetComponent = EquippedPet.GetComponent<PetComponent>() ?? EquippedPet.GetComponentInChildren<PetComponent>();
		}

		EquippedPetComponent ??= GameObject.GetComponentInChildren<PetComponent>();
		return EquippedPetComponent;
	}
}
