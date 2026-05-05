using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class InteractBuyCrate : Interactable
{
	[Property] public int Cost { get; set; } = 25;
	[Property] public float RevealHeight { get; set; } = 96f;
	[Property] public float RevealDuration { get; set; } = 1.25f;
	[Property] public Vector3 RevealStartLocalOffset { get; set; } = Vector3.Zero;
	[Property] public Rotation RevealedPetRotation { get; set; } = Rotation.Identity;
	[Property] public List<PetCrateReward> Pets { get; set; } = new();
	[Property] public List<PetRarityParticle> RarityParticles { get; set; } = new();

	private GameObject revealedPet;
	private PlayerData pendingPlayerData;
	private Inventory pendingInventory;
	private PetCrateReward pendingReward;
	private Vector3 revealStartPosition;
	private Vector3 revealEndPosition;
	private float revealElapsed;
	private bool isRevealing;

	protected override void OnUpdate()
	{
		UpdateReveal();
	}

	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( isRevealing || interactingPlayer == null || interactingPlayer.IsProxy )
			return;

		var playerData = interactingPlayer.GetComponent<PlayerData>() ?? PlayerData.LOCALDATA;
		var inventory = playerData?.inventory ?? playerData?.GetComponent<Inventory>();
		if ( playerData == null || inventory == null )
		{
			Log.Warning( "Cannot buy a pet crate because the local player data or pet inventory is missing." );
			return;
		}

		if ( inventory.Count >= inventory.InventorySize )
		{
			Log.Warning( "Cannot buy a pet crate because the pet inventory is full." );
			return;
		}

		if ( playerData.PlayerMoney < Cost )
		{
			Log.Info( $"Not enough money to buy a pet crate. Need {Cost}, have {playerData.PlayerMoney}." );
			return;
		}

		var reward = RollReward();
		if ( reward == null || !reward.PetPrefab.IsValid() )
		{
			Log.Warning( "Cannot buy a pet crate because no valid weighted pet rewards are configured." );
			return;
		}

		playerData.PlayerMoney -= Cost;
		playerData.QueueSave();

		StartReveal( playerData, inventory, reward );
	}

	private void StartReveal( PlayerData playerData, Inventory inventory, PetCrateReward reward )
	{
		pendingPlayerData = playerData;
		pendingInventory = inventory;
		pendingReward = reward;
		revealElapsed = 0f;
		isRevealing = true;

		revealStartPosition = GameObject.WorldPosition + (GameObject.WorldRotation * RevealStartLocalOffset);
		revealEndPosition = revealStartPosition + (Vector3.Up * MathF.Max( 0f, RevealHeight ));

		revealedPet = reward.PetPrefab.Clone();
		revealedPet.Parent = GameObject;
		revealedPet.WorldPosition = revealStartPosition;
		revealedPet.WorldRotation = RevealedPetRotation;
		revealedPet.Enabled = true;

		SpawnRarityParticle( reward.Rarity, revealStartPosition );
	}

	private void UpdateReveal()
	{
		if ( !isRevealing )
			return;

		revealElapsed += Time.Delta;
		var duration = MathF.Max( 0.01f, RevealDuration );
		var progress = Math.Clamp( revealElapsed / duration, 0f, 1f );
		var easedProgress = progress * progress * (3f - (2f * progress));

		if ( revealedPet.IsValid() )
		{
			revealedPet.WorldPosition = revealStartPosition + ((revealEndPosition - revealStartPosition) * easedProgress);
		}

		if ( progress < 1f )
			return;

		FinishReveal();
	}

	private void FinishReveal()
	{
		if ( pendingInventory.IsValid() && pendingReward?.PetPrefab.IsValid() == true )
		{
			if ( !pendingInventory.AddPetPrefab( pendingReward.PetPrefab ) )
			{
				RefundPendingPurchase();
			}
			else
			{
				pendingPlayerData?.QueueSave();
			}
		}
		else
		{
			RefundPendingPurchase();
		}

		ClearRevealState();
	}

	private void RefundPendingPurchase()
	{
		if ( pendingPlayerData == null )
			return;

		pendingPlayerData.PlayerMoney += Cost;
		pendingPlayerData.QueueSave();
		Log.Warning( "Pet crate reward could not be added to inventory, so the purchase was refunded." );
	}

	private void ClearRevealState()
	{
		if ( revealedPet.IsValid() )
		{
			revealedPet.Destroy();
		}

		revealedPet = null;
		pendingPlayerData = null;
		pendingInventory = null;
		pendingReward = null;
		isRevealing = false;
	}

	private PetCrateReward RollReward()
	{
		var validRewards = Pets
			.Where( reward => reward != null && reward.PetPrefab.IsValid() && reward.SpawnWeight > 0f )
			.ToList();

		if ( validRewards.Count == 0 )
			return null;

		var totalWeight = validRewards.Sum( reward => reward.SpawnWeight );
		var roll = (float)Game.Random.NextDouble() * totalWeight;

		foreach ( var reward in validRewards )
		{
			roll -= reward.SpawnWeight;
			if ( roll <= 0f )
				return reward;
		}

		return validRewards[^1];
	}

	private void SpawnRarityParticle( PetRarity rarity, Vector3 position )
	{
		var particlePrefab = RarityParticles
			.FirstOrDefault( particle => particle != null && particle.Rarity == rarity && particle.ParticlePrefab.IsValid() )
			?.ParticlePrefab;

		if ( !particlePrefab.IsValid() )
			return;

		var particle = particlePrefab.Clone();
		particle.Parent = revealedPet.IsValid() ? revealedPet : GameObject;
		particle.WorldPosition = position;
		particle.Enabled = true;
	}

	public enum PetRarity
	{
		Common,
		Uncommon,
		Rare,
		Epic,
		Legendary
	}

	public sealed class PetCrateReward
	{
		[Property] public PetRarity Rarity { get; set; } = PetRarity.Common;
		[Property] public float SpawnWeight { get; set; } = 1f;
		[Property] public GameObject PetPrefab { get; set; }
	}

	public sealed class PetRarityParticle
	{
		[Property] public PetRarity Rarity { get; set; } = PetRarity.Common;
		[Property] public GameObject ParticlePrefab { get; set; }
	}
}
