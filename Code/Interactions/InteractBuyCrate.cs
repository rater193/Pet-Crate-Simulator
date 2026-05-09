using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class InteractBuyCrate : Interactable
{
	[Property] public int Cost { get; set; } = 25;
	[Property] public float RevealHeight { get; set; } = 96f;
	[Property] public float RevealDuration { get; set; } = 1.25f;
	[Property] public float CelebrationDuration { get; set; } = 1.15f;
	[Property] public int CelebrationHopCount { get; set; } = 3;
	[Property] public float CelebrationHopHeight { get; set; } = 34f;
	[Property] public float CelebrationSpinDegrees { get; set; } = 360f;
	[Property] public float CelebrationSquashStretch { get; set; } = 0.18f;
	[Property] public float CelebrationWiggleDegrees { get; set; } = 8f;
	[Property] public float CollectDuration { get; set; } = 0.45f;
	[Property] public float CollectHopHeight { get; set; } = 24f;
	[Property] public string CrateModelName { get; set; } = "Model";
	[Property] public float CrateOpenDuration { get; set; } = 0.45f;
	[Property] public float CrateOpenShakeDistance { get; set; } = 3f;
	[Property] public float CrateOpenLift { get; set; } = 10f;
	[Property] public float CrateOpenWobbleDegrees { get; set; } = 5f;
	[Property] public float CrateOpenSquashStretch { get; set; } = 0.08f;
	[Property] public Vector3 RevealStartLocalOffset { get; set; } = Vector3.Zero;
	[Property] public Rotation RevealedPetRotation { get; set; } = Rotation.Identity;
	[Property] public List<PetCrateReward> Pets { get; set; } = new();
	[Property] public List<PetRarityParticle> RarityParticles { get; set; } = new();

	private GameObject crateModel;
	private GameObject revealedPet;
	private PlayerData pendingPlayerData;
	private Inventory pendingInventory;
	private PetCrateReward pendingReward;
	private Vector3 crateModelBaseLocalPosition;
	private Rotation crateModelBaseLocalRotation;
	private Vector3 crateModelBaseLocalScale;
	private Vector3 revealStartPosition;
	private Vector3 revealEndPosition;
	private Rotation revealBaseRotation;
	private Vector3 revealedPetBaseScale;
	private float revealElapsed;
	private bool isRevealing;
	private RevealPhase revealPhase;

	protected override void OnUpdate()
	{
		UpdateReveal();
	}

	protected override void OnDisabled()
	{
		RestoreCrateModel();
	}

	protected override void OnDestroy()
	{
		RestoreCrateModel();
	}

	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( isRevealing || interactingPlayer == null || interactingPlayer.IsProxy )
			return;

		GameStatsTracker.RecordCratePurchaseAttempt( Cost );

		var playerData = interactingPlayer.GetComponent<PlayerData>() ?? PlayerData.LOCALDATA;
		var inventory = playerData?.inventory ?? playerData?.GetComponent<Inventory>();
		if ( playerData == null || inventory == null )
		{
			Log.Warning( "Cannot buy a pet crate because the local player data or pet inventory is missing." );
			GameStatsTracker.RecordCratePurchaseFailed( "missing_player_data_or_inventory", Cost );
			return;
		}

		if ( inventory.Count >= inventory.InventorySize )
		{
			Log.Warning( "Cannot buy a pet crate because the pet inventory is full." );
			GameStatsTracker.RecordCratePurchaseFailed( "inventory_full", Cost );
			return;
		}

		if ( playerData.PlayerMoney < Cost )
		{
			Log.Info( $"Not enough money to buy a pet crate. Need {Cost}, have {playerData.PlayerMoney}." );
			GameStatsTracker.RecordCratePurchaseFailed( "not_enough_money", Cost );
			return;
		}

		var reward = RollReward();
		if ( reward == null || !reward.PetPrefab.IsValid() )
		{
			Log.Warning( "Cannot buy a pet crate because no valid weighted pet rewards are configured." );
			GameStatsTracker.RecordCratePurchaseFailed( "invalid_rewards", Cost );
			return;
		}

		playerData.PlayerMoney -= Cost;
		GameStatsTracker.RecordCratePurchased( Cost );
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
		revealPhase = RevealPhase.Rising;

		revealStartPosition = GameObject.WorldPosition + (GameObject.WorldRotation * RevealStartLocalOffset);
		revealEndPosition = revealStartPosition + (Vector3.Up * MathF.Max( 0f, RevealHeight ));
		revealBaseRotation = GameObject.WorldRotation * RevealedPetRotation;

		revealedPet = reward.PetPrefab.Clone();
		revealedPet.Parent = GameObject;
		revealedPet.WorldPosition = revealStartPosition;
		revealedPet.WorldRotation = revealBaseRotation;
		revealedPet.Enabled = true;
		revealedPetBaseScale = revealedPet.LocalScale;

		CacheCrateModel();
		ApplyCrateModelAnimation( 0f, 0f );
		SpawnRarityParticle( reward.Rarity, revealStartPosition );
	}

	private void UpdateReveal()
	{
		if ( !isRevealing )
			return;

		if ( revealPhase == RevealPhase.Celebrating )
		{
			UpdateCelebration();
			return;
		}

		if ( revealPhase == RevealPhase.Collecting )
		{
			UpdateCollect();
			return;
		}

		revealElapsed += Time.Delta;
		var duration = MathF.Max( 0.01f, RevealDuration );
		var progress = Math.Clamp( revealElapsed / duration, 0f, 1f );
		var easedProgress = progress * progress * (3f - (2f * progress));
		var openProgress = Math.Clamp( revealElapsed / MathF.Max( 0.01f, CrateOpenDuration ), 0f, 1f );
		var openExcitement = MathF.Sin( openProgress * MathF.PI );

		if ( revealedPet.IsValid() )
		{
			revealedPet.WorldPosition = revealStartPosition + ((revealEndPosition - revealStartPosition) * easedProgress);
		}

		ApplyCrateModelAnimation( EaseOutBack( openProgress ), openExcitement );

		if ( progress < 1f )
			return;

		BeginCelebration();
	}

	private void BeginCelebration()
	{
		revealPhase = RevealPhase.Celebrating;
		revealElapsed = 0f;

		if ( revealedPet.IsValid() )
		{
			revealedPet.WorldPosition = revealEndPosition;
			revealedPet.WorldRotation = revealBaseRotation;
			revealedPet.LocalScale = revealedPetBaseScale;
		}
	}

	private void UpdateCelebration()
	{
		revealElapsed += Time.Delta;
		var duration = MathF.Max( 0.01f, CelebrationDuration );
		var progress = Math.Clamp( revealElapsed / duration, 0f, 1f );
		var easedProgress = EaseOutBack( progress );
		var hopCount = Math.Max( 1, CelebrationHopCount );
		var hopWave = MathF.Abs( MathF.Sin( progress * MathF.PI * hopCount ) );
		var landingWave = MathF.Pow( MathF.Abs( MathF.Cos( progress * MathF.PI * hopCount ) ), 10f );
		var decay = 1f - (progress * 0.25f);
		var squashStretch = MathF.Max( 0f, CelebrationSquashStretch );
		ApplyCrateModelAnimation( 1f, (hopWave + landingWave) * (1f - (progress * 0.35f)) );

		if ( revealedPet.IsValid() )
		{
			revealedPet.WorldPosition = revealEndPosition + (Vector3.Up * MathF.Max( 0f, CelebrationHopHeight ) * hopWave * decay);
			revealedPet.WorldRotation = revealBaseRotation
				* Rotation.FromYaw( CelebrationSpinDegrees * easedProgress )
				* Rotation.FromRoll( MathF.Sin( progress * MathF.PI * hopCount * 2f ) * CelebrationWiggleDegrees * (1f - progress) );

			var stretch = hopWave * squashStretch;
			var squash = landingWave * squashStretch * (1f - progress);
			revealedPet.LocalScale = new Vector3(
				revealedPetBaseScale.x * MathF.Max( 0.1f, 1f - (stretch * 0.35f) + (squash * 0.45f) ),
				revealedPetBaseScale.y * MathF.Max( 0.1f, 1f - (stretch * 0.35f) + (squash * 0.45f) ),
				revealedPetBaseScale.z * MathF.Max( 0.1f, 1f + stretch - squash )
			);
		}

		if ( progress < 1f )
			return;

		BeginCollect();
	}

	private void BeginCollect()
	{
		revealPhase = RevealPhase.Collecting;
		revealElapsed = 0f;

		if ( revealedPet.IsValid() )
		{
			revealedPet.WorldPosition = revealEndPosition;
			revealedPet.WorldRotation = revealBaseRotation;
			revealedPet.LocalScale = revealedPetBaseScale;
		}
	}

	private void UpdateCollect()
	{
		revealElapsed += Time.Delta;
		var duration = MathF.Max( 0.01f, CollectDuration );
		var progress = Math.Clamp( revealElapsed / duration, 0f, 1f );
		var hopWave = MathF.Sin( progress * MathF.PI );
		var scale = MathF.Max( 0f, 1f - EaseInBack( progress ) );
		ApplyCrateModelAnimation( 1f - EaseOutCubic( progress ), hopWave * 0.5f );

		if ( revealedPet.IsValid() )
		{
			revealedPet.WorldPosition = revealEndPosition + (Vector3.Up * MathF.Max( 0f, CollectHopHeight ) * hopWave);
			revealedPet.WorldRotation = revealBaseRotation * Rotation.FromYaw( CelebrationSpinDegrees + (180f * progress) );
			revealedPet.LocalScale = revealedPetBaseScale * scale;
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
				var displayName = GetPetDisplayName( pendingReward.PetPrefab );
				var prefabPath = GetPetPrefabPath( pendingReward.PetPrefab );
				GameStatsTracker.RecordCrateOpened( displayName, prefabPath, pendingReward.Rarity, Cost );
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
		GameStatsTracker.RecordCrateRefunded( Cost );
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
		revealPhase = RevealPhase.Rising;
		RestoreCrateModel();
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

	private void CacheCrateModel()
	{
		crateModel = FindCrateModel();
		if ( !crateModel.IsValid() )
			return;

		crateModelBaseLocalPosition = crateModel.LocalPosition;
		crateModelBaseLocalRotation = crateModel.LocalRotation;
		crateModelBaseLocalScale = crateModel.LocalScale;
	}

	private GameObject FindCrateModel()
	{
		if ( string.IsNullOrWhiteSpace( CrateModelName ) )
			return GameObject.Children.FirstOrDefault();

		return GameObject.GetAllObjects( true )
			.FirstOrDefault( child => child != GameObject && child.Name.Equals( CrateModelName, StringComparison.OrdinalIgnoreCase ) );
	}

	private void ApplyCrateModelAnimation( float openProgress, float excitement )
	{
		if ( !crateModel.IsValid() )
			return;

		openProgress = Math.Clamp( openProgress, 0f, 1f );
		excitement = Math.Clamp( excitement, 0f, 1f );

		var wobble = MathF.Sin( Time.Now * 34f ) * CrateOpenWobbleDegrees * excitement;
		var shakeX = MathF.Sin( Time.Now * 52f ) * CrateOpenShakeDistance * excitement;
		var shakeY = MathF.Cos( Time.Now * 47f ) * CrateOpenShakeDistance * 0.65f * excitement;
		var squash = MathF.Max( 0f, CrateOpenSquashStretch ) * excitement;

		crateModel.LocalPosition = crateModelBaseLocalPosition + new Vector3( shakeX, shakeY, CrateOpenLift * openProgress );
		crateModel.LocalRotation = crateModelBaseLocalRotation
			* Rotation.FromYaw( wobble )
			* Rotation.FromRoll( wobble * 0.35f );
		crateModel.LocalScale = new Vector3(
			crateModelBaseLocalScale.x * (1f + (squash * 0.6f)),
			crateModelBaseLocalScale.y * (1f + (squash * 0.6f)),
			crateModelBaseLocalScale.z * MathF.Max( 0.1f, 1f - squash )
		);
	}

	private void RestoreCrateModel()
	{
		if ( !crateModel.IsValid() )
			return;

		crateModel.LocalPosition = crateModelBaseLocalPosition;
		crateModel.LocalRotation = crateModelBaseLocalRotation;
		crateModel.LocalScale = crateModelBaseLocalScale;
	}

	private static float EaseOutBack( float value )
	{
		value = Math.Clamp( value, 0f, 1f );
		const float overshoot = 1.70158f;
		var shifted = value - 1f;
		return 1f + ((overshoot + 1f) * shifted * shifted * shifted) + (overshoot * shifted * shifted);
	}

	private static float EaseInBack( float value )
	{
		value = Math.Clamp( value, 0f, 1f );
		const float overshoot = 1.70158f;
		return ((overshoot + 1f) * value * value * value) - (overshoot * value * value);
	}

	private static float EaseOutCubic( float value )
	{
		value = Math.Clamp( value, 0f, 1f );
		var shifted = 1f - value;
		return 1f - (shifted * shifted * shifted);
	}

	private static string GetPetDisplayName( GameObject petPrefab )
	{
		var petComponent = petPrefab.GetComponent<PetComponent>() ?? petPrefab.GetComponentInChildren<PetComponent>();
		if ( !string.IsNullOrWhiteSpace( petComponent?.DisplayName ) )
			return petComponent.DisplayName;

		return petPrefab.IsValid() ? petPrefab.Name : "Unknown Pet";
	}

	private static string GetPetPrefabPath( GameObject petPrefab )
	{
		if ( !string.IsNullOrWhiteSpace( petPrefab.PrefabInstanceSource ) )
			return petPrefab.PrefabInstanceSource;

		return petPrefab.IsValid() ? petPrefab.Name : string.Empty;
	}

	private enum RevealPhase
	{
		Rising,
		Celebrating,
		Collecting
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
