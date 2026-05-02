using Sandbox;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class PetFramework : Component
{
	public static PetFramework LOCAL;

	[Property] public int MaxEquippedPets { get; set; } = 3;
	[Property] public float PetCircleRadius { get; set; } = 48f;
	[Property] public float PetCircleHeight { get; set; } = 24f;
	[Property] public float PetCircleAngleOffset { get; set; } = 0f;
	[Property] public Rotation EquippedPetLocalRotation { get; set; } = Rotation.Identity;
	[Property] public bool NetworkSpawnEquippedPet { get; set; } = true;

	private readonly List<GameObject> equippedPets = new();
	private readonly List<PetComponent> equippedPetComponents = new();

	public IReadOnlyList<GameObject> EquippedPets => equippedPets;
	public GameObject EquippedPet => equippedPets.FirstOrDefault();
	public PetComponent EquippedPetComponent => equippedPetComponents.FirstOrDefault( pet => pet.IsValid() );

	public float CoinMultiplier
	{
		get
		{
			RefreshEquippedPetComponents();

			var multiplier = 1f;
			foreach ( var petComponent in equippedPetComponents )
			{
				if ( petComponent.IsValid() )
				{
					multiplier *= petComponent.CoinMultiplier;
				}
			}

			return multiplier;
		}
	}

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

		if ( equippedPets.Count >= MaxEquippedPets )
		{
			Log.Warning( $"Cannot equip pet '{prefab.Name}'. Max equipped pets is {MaxEquippedPets}." );
			return;
		}

		var pet = prefab.Clone();
		pet.Parent = GameObject;
		pet.LocalRotation = EquippedPetLocalRotation;

		equippedPets.Add( pet );

		var petComponent = pet.GetComponent<PetComponent>() ?? pet.GetComponentInChildren<PetComponent>();
		equippedPetComponents.Add( petComponent );

		if ( petComponent == null )
		{
			Log.Warning( $"Equipped pet prefab '{prefab.Name}' does not have a PetComponent." );
		}

		ArrangeEquippedPets();

		if ( NetworkSpawnEquippedPet )
		{
			pet.NetworkSpawn();
		}
	}

	public void Unequip()
	{
		if ( equippedPets.Count == 0 )
			return;

		var pet = equippedPets[^1];
		if ( pet.IsValid() )
		{
			pet.Destroy();
		}

		equippedPets.RemoveAt( equippedPets.Count - 1 );
		equippedPetComponents.RemoveAt( equippedPetComponents.Count - 1 );
		ArrangeEquippedPets();
	}

	public void UnequipAll()
	{
		foreach ( var pet in equippedPets )
		{
			if ( pet.IsValid() )
			{
				pet.Destroy();
			}
		}

		equippedPets.Clear();
		equippedPetComponents.Clear();
	}

	public int ApplyCoinMultiplier( int baseCoins )
	{
		if ( baseCoins <= 0 )
			return 0;

		return Math.Max( 0, (int)MathF.Round( baseCoins * CoinMultiplier ) );
	}

	private void ArrangeEquippedPets()
	{
		var count = equippedPets.Count;
		if ( count == 0 )
			return;

		var angleStep = MathF.PI * 2f / count;
		var angleOffset = PetCircleAngleOffset * (MathF.PI / 180f);

		for ( var i = 0; i < count; i++ )
		{
			var pet = equippedPets[i];
			if ( !pet.IsValid() )
				continue;

			var angle = angleOffset + (angleStep * i);
			pet.LocalPosition = new Vector3( MathF.Cos( angle ) * PetCircleRadius, MathF.Sin( angle ) * PetCircleRadius, PetCircleHeight );
			pet.LocalRotation = EquippedPetLocalRotation;
		}
	}

	private void RefreshEquippedPetComponents()
	{
		for ( var i = equippedPets.Count - 1; i >= 0; i-- )
		{
			var pet = equippedPets[i];
			if ( !pet.IsValid() )
			{
				equippedPets.RemoveAt( i );
				equippedPetComponents.RemoveAt( i );
				continue;
			}

			if ( !equippedPetComponents[i].IsValid() )
			{
				equippedPetComponents[i] = pet.GetComponent<PetComponent>() ?? pet.GetComponentInChildren<PetComponent>();
			}
		}
	}
}
