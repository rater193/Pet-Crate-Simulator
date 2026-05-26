using Sandbox;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class PetFramework : Component
{
	public static PetFramework LOCAL;

	[Property] public int MaxEquippedPets { get; set; } = 3;
	[Property] public float PetCircleRadius { get; set; } = 48f;
	[Property] public float PetCircleHeight { get; set; } = 0f;
	[Property] public float PetCircleAngleOffset { get; set; } = 0f;
	[Property] public float PetMoveSpeed { get; set; } = 240f;
	[Property] public float PetTurnSpeed { get; set; } = 12f;
	[Property] public float PetBobHeight { get; set; } = 6f;
	[Property] public float PetBobSpeed { get; set; } = 12f;
	[Property] public float PetAttackDuration { get; set; } = 0.28f;
	[Property] public float PetAttackRange { get; set; } = 36f;
	[Property] public float PetAttackLungeDistance { get; set; } = 22f;
	[Property] public float PetDefaultAttackInterval { get; set; } = 1f;
	[Property] public float PetMinimumAttackInterval { get; set; } = 0.25f;
	[Property] public float PetSwarmRadius { get; set; } = 64f;
	[Property] public float PetSwarmAngleOffset { get; set; } = 0f;
	[Property] public float PetAttackStartDelayStep { get; set; } = 0.12f;
	[Property, Group( "Auto Battle" )] public bool AutoBattleEnabled { get; set; } = true;
	[Property, Group( "Auto Battle" )] public float AutoBattleRange { get; set; } = 260f;
	[Property, Group( "Auto Battle" )] public float AutoBattleScanInterval { get; set; } = 0.25f;
	[Property] public float GroundTraceHeight { get; set; } = 96f;
	[Property] public Rotation EquippedPetLocalRotation { get; set; } = Rotation.Identity;
	[Property] public bool NetworkSpawnEquippedPet { get; set; } = true;
	[Property] public float PetRefreshInterval { get; set; } = 0.25f;

	private readonly List<PetSlot> equippedPets = new();
	private GameObject activeAttackTarget;
	private PlayerController activeAttackOwner;
	private float refreshTimer;
	private float autoBattleScanTimer;

	public IReadOnlyList<GameObject> EquippedPets => equippedPets.Select( slot => slot.Pet ).ToList();
	public GameObject EquippedPet => equippedPets.FirstOrDefault()?.Pet;
	public PetComponent EquippedPetComponent => equippedPets.FirstOrDefault( slot => slot.Component.IsValid() )?.Component;

	public float CoinMultiplier
	{
		get
		{
			var multiplier = 1f;
			foreach ( var slot in equippedPets )
			{
				if ( slot.Component.IsValid() )
				{
					multiplier += slot.Component.CoinMultiplier - 1f;
				}
			}

			return MathF.Max( 0f, multiplier );
		}
	}

	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			LOCAL = this;
		}
	}

	protected override void OnUpdate()
	{
		TickRefresh();

		if ( IsProxy )
		{
			// Admin bisect: this proxy-only loop is the per-other-player cost that scales when
			// players join. Toggle it off in-game with the `perf_pets` console command to confirm.
			if ( !AdminPerfController.DisableProxyPetAnimation )
				UpdateEquippedPetVisuals();
			return;
		}

		UpdateEquippedPets();
	}

	private void TickRefresh()
	{
		// Cheap every-frame prune keeps the update loops safe without walking the hierarchy.
		PruneInvalidPets();

		refreshTimer -= Time.Delta;
		if ( refreshTimer > 0f )
			return;

		refreshTimer = MathF.Max( 0f, PetRefreshInterval );
		RefreshEquippedPets();
	}

	private void PruneInvalidPets()
	{
		for ( var i = equippedPets.Count - 1; i >= 0; i-- )
		{
			if ( !equippedPets[i].Pet.IsValid() )
			{
				equippedPets.RemoveAt( i );
			}
		}
	}

	protected override void OnDestroy()
	{
		if ( !IsProxy )
		{
			UnequipAll();
		}

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
		Equip( prefab, PetRarity.Common );
	}

	public void Equip( InventoryPetSlot inventorySlot )
	{
		if ( !inventorySlot.IsValid() )
			return;

		Equip( inventorySlot.GetPetPrefab(), inventorySlot.Rarity );
	}

	public void Equip( GameObject prefab, PetRarity rarity )
	{
		if ( IsProxy || !prefab.IsValid() )
			return;

		if ( equippedPets.Count >= MaxEquippedPets )
		{
			Log.Warning( $"Cannot equip pet '{prefab.Name}'. Max equipped pets is {MaxEquippedPets}." );
			return;
		}

		var pet = prefab.Clone();
		pet.Parent = null;
		pet.WorldPosition = SnapToGround( GameObject.WorldPosition );
		pet.WorldRotation = EquippedPetLocalRotation;

		var petComponent = pet.GetComponent<PetComponent>() ?? pet.GetComponentInChildren<PetComponent>();
		InventoryPetSlot.ApplyRarityToPetComponent( petComponent, rarity );
		ApplyRarityVisuals( pet, rarity );
		var slot = new PetSlot( pet, petComponent );
		equippedPets.Add( slot );

		if ( petComponent == null )
		{
			Log.Warning( $"Equipped pet prefab '{prefab.Name}' does not have a PetComponent." );
		}

		ArrangeEquippedPets();

		if ( NetworkSpawnEquippedPet )
		{
			pet.NetworkSpawn();
		}

		RegisterEquippedPet( pet );
	}

	private void ApplyRarityVisuals( GameObject pet, PetRarity rarity )
	{
		if ( !pet.IsValid() || rarity < PetRarity.Uncommon )
			return;

		pet.GetOrAddComponent<EquippedPetRarityVisuals>().Configure( rarity );
	}

	public void Unequip()
	{
		if ( equippedPets.Count == 0 )
			return;

		var slot = equippedPets[^1];
		if ( slot.Pet.IsValid() )
		{
			slot.Pet.Destroy();
		}

		equippedPets.RemoveAt( equippedPets.Count - 1 );
		ArrangeEquippedPets();
	}

	public void UnequipAll()
	{
		foreach ( var slot in equippedPets )
		{
			if ( slot.Pet.IsValid() )
			{
				slot.Pet.Destroy();
			}
		}

		equippedPets.Clear();
	}

	public void AttackTarget( GameObject target )
	{
		if ( !target.IsValid() )
			return;

		SetAttackTarget( target, GetComponent<PlayerController>() );
	}

	[Rpc.Broadcast]
	private void RegisterEquippedPet( GameObject pet )
	{
		if ( !pet.IsValid() )
			return;

		GetOrCreateSlot( pet );
	}

	[Rpc.Broadcast( NetFlags.Unreliable )]
	private void SetAttackTarget( GameObject target, PlayerController owner )
	{
		if ( !target.IsValid() )
			return;

		activeAttackTarget = target;
		activeAttackOwner = owner;
		RefreshEquippedPets();

		for ( var i = 0; i < equippedPets.Count; i++ )
		{
			var slot = equippedPets[i];
			slot.AttackTargetPosition = target.WorldPosition;
			slot.AttackTimeRemaining = PetAttackDuration;
			slot.AttackCooldown = MathF.Min( slot.AttackCooldown, i * PetAttackStartDelayStep );
		}
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
			var slot = equippedPets[i];
			if ( !slot.Pet.IsValid() )
				continue;

			var angle = angleOffset + (angleStep * i);
			var targetPosition = GetOrbitWorldPosition( angle );
			slot.Pet.WorldPosition = targetPosition;
			slot.Pet.WorldRotation = EquippedPetLocalRotation;
			slot.RestWorldPosition = targetPosition;
			slot.LastWorldPosition = targetPosition;
		}
	}

	private void UpdateEquippedPets()
	{
		var count = equippedPets.Count;
		if ( count == 0 )
			return;

		if ( !IsAttackTargetValid() )
		{
			activeAttackTarget = null;
			activeAttackOwner = null;
		}

		TryAcquireAutoBattleTarget();

		var angleStep = MathF.PI * 2f / count;
		var angleOffset = PetCircleAngleOffset * (MathF.PI / 180f);
		var swarmAngleOffset = PetSwarmAngleOffset * (MathF.PI / 180f);

		for ( var i = 0; i < count; i++ )
		{
			var slot = equippedPets[i];
			if ( !slot.Pet.IsValid() )
				continue;

			AdminPerfController.PetAnimAccumulator++;
			slot.AttackCooldown = MathF.Max( 0f, slot.AttackCooldown - Time.Delta );

			var orbitAngle = angleOffset + (angleStep * i);
			var restPosition = GetOrbitWorldPosition( orbitAngle );
			var targetPosition = restPosition;

			slot.RestWorldPosition = restPosition;
			if ( activeAttackTarget.IsValid() )
			{
				var swarmAngle = swarmAngleOffset + (angleStep * i);
				slot.AttackTargetPosition = activeAttackTarget.WorldPosition;
				targetPosition = GetSwarmWorldPosition( activeAttackTarget.WorldPosition, swarmAngle );
			}

			var previousPosition = slot.Pet.WorldPosition;
			var moveSpeed = PetMoveSpeed * MathF.Max( 0.05f, slot.Component?.MoveSpeedMultiplier ?? 1f );
			slot.Pet.WorldPosition = MoveTowards( previousPosition, targetPosition, moveSpeed * Time.Delta );

			var movedDistance = (slot.Pet.WorldPosition - previousPosition).Length;
			var isMoving = movedDistance > 0.1f;
			var attackProgress = GetAttackProgress( slot );

			slot.BobTime += Time.Delta * PetBobSpeed * MathF.Max( 0.05f, slot.Component?.MoveSpeedMultiplier ?? 1f );
			ApplyVisualMotion( slot, isMoving, attackProgress );
			FacePetDirection( slot, previousPosition, attackProgress );

			if ( slot.AttackTimeRemaining > 0f )
			{
				slot.AttackTimeRemaining -= Time.Delta;
			}

			TryPetAttack( slot );

			slot.LastWorldPosition = slot.Pet.WorldPosition;
		}
	}

	private void UpdateEquippedPetVisuals()
	{
		foreach ( var slot in equippedPets )
		{
			if ( !slot.Pet.IsValid() )
				continue;

			AdminPerfController.PetAnimAccumulator++;
			var movedDistance = (slot.Pet.WorldPosition - slot.LastWorldPosition).Length;
			var isMoving = movedDistance > 0.1f;
			var attackProgress = GetAttackProgress( slot );

			slot.BobTime += Time.Delta * PetBobSpeed * MathF.Max( 0.05f, slot.Component?.MoveSpeedMultiplier ?? 1f );
			ApplyVisualMotion( slot, isMoving, attackProgress );

			if ( slot.AttackTimeRemaining > 0f )
			{
				slot.AttackTimeRemaining -= Time.Delta;
			}

			slot.LastWorldPosition = slot.Pet.WorldPosition;
		}
	}

	private Vector3 GetOrbitWorldPosition( float angle )
	{
		var localOffset = new Vector3( MathF.Cos( angle ) * PetCircleRadius, MathF.Sin( angle ) * PetCircleRadius, 0f );
		var worldOffset = (GameObject.WorldRotation.Forward * localOffset.x) + (GameObject.WorldRotation.Right * localOffset.y);
		var position = GameObject.WorldPosition + worldOffset;
		position.z = GameObject.WorldPosition.z + PetCircleHeight;

		return SnapToGround( position );
	}

	private Vector3 GetSwarmWorldPosition( Vector3 targetPosition, float angle )
	{
		var position = targetPosition + new Vector3( MathF.Cos( angle ) * PetSwarmRadius, MathF.Sin( angle ) * PetSwarmRadius, 0f );
		return SnapToGround( position );
	}

	private bool IsAttackTargetValid()
	{
		if ( !activeAttackTarget.IsValid() )
			return false;

		var destructable = activeAttackTarget.GetComponent<InteractGivePlayerCoin>() ?? activeAttackTarget.GetComponentInChildren<InteractGivePlayerCoin>();
		return destructable.IsValid() && destructable.CanReceivePetDamage;
	}

	private void TryAcquireAutoBattleTarget()
	{
		if ( !AutoBattleEnabled || activeAttackTarget.IsValid() )
			return;

		autoBattleScanTimer -= Time.Delta;
		if ( autoBattleScanTimer > 0f )
			return;

		autoBattleScanTimer = MathF.Max( 0f, AutoBattleScanInterval );

		var target = FindNearestAutoBattleTarget();
		if ( target.IsValid() )
		{
			SetAttackTarget( target, GetComponent<PlayerController>() );
		}
	}

	private GameObject FindNearestAutoBattleTarget()
	{
		if ( Scene == null || AutoBattleRange <= 0f )
			return null;

		var rangeSquared = AutoBattleRange * AutoBattleRange;
		var bestDistanceSquared = float.MaxValue;
		GameObject bestTarget = null;

		foreach ( var destructable in Scene.GetAllComponents<InteractGivePlayerCoin>() )
		{
			if ( !destructable.IsValid() || !destructable.CanReceivePetDamage )
				continue;

			var target = destructable.GameObject;
			if ( !target.IsValid() )
				continue;

			var distanceSquared = (target.WorldPosition - GameObject.WorldPosition).LengthSquared;
			if ( distanceSquared > rangeSquared || distanceSquared >= bestDistanceSquared )
				continue;

			bestDistanceSquared = distanceSquared;
			bestTarget = target;
		}

		return bestTarget;
	}

	private void TryPetAttack( PetSlot slot )
	{
		if ( slot.AttackCooldown > 0f || !IsAttackTargetValid() )
			return;

		var component = slot.Component;
		var damage = Math.Max( 0, component?.Damage ?? 1 );
		if ( damage <= 0 )
			return;

		var toTarget = activeAttackTarget.WorldPosition - slot.Pet.WorldPosition;
		toTarget.z = 0f;

		var attackReach = PetAttackRange * MathF.Max( 0.05f, component?.AttackRangeMultiplier ?? 1f );
		if ( toTarget.LengthSquared > attackReach * attackReach )
			return;

		var destructable = activeAttackTarget.GetComponent<InteractGivePlayerCoin>() ?? activeAttackTarget.GetComponentInChildren<InteractGivePlayerCoin>();
		if ( !destructable.IsValid() )
			return;

		destructable.ApplyPetDamage( activeAttackOwner ?? GetComponent<PlayerController>(), damage );
		PlayPetAttack( slot.Pet, activeAttackTarget.WorldPosition );
		GameStatsTracker.RecordPetAttack( slot.Component?.DisplayName ?? slot.Pet.Name, activeAttackTarget.Name, damage );

		var attackInterval = component?.AttackInterval ?? PetDefaultAttackInterval;
		slot.AttackCooldown = MathF.Max( PetMinimumAttackInterval, attackInterval );
	}

	[Rpc.Broadcast( NetFlags.Unreliable | NetFlags.DiscardOnDelay )]
	private void PlayPetAttack( GameObject pet, Vector3 targetPosition )
	{
		var slot = GetOrCreateSlot( pet );
		if ( slot == null )
			return;

		slot.AttackTargetPosition = targetPosition;
		slot.AttackTimeRemaining = PetAttackDuration;
	}

	private float GetAttackProgress( PetSlot slot )
	{
		if ( slot.AttackTimeRemaining <= 0f || PetAttackDuration <= 0f )
			return 0f;

		return 1f - (slot.AttackTimeRemaining / PetAttackDuration).Clamp( 0f, 1f );
	}

	private void ApplyVisualMotion( PetSlot slot, bool isMoving, float attackProgress )
	{
		var bobHeight = 0f;
		if ( isMoving )
		{
			var bobMultiplier = MathF.Max( 0f, slot.Component?.BobHeightMultiplier ?? 1f );
			bobHeight = MathF.Abs( MathF.Sin( slot.BobTime ) ) * PetBobHeight * bobMultiplier;
		}

		var lungeAmount = 0f;
		if ( attackProgress > 0f )
		{
			var attackWave = MathF.Sin( attackProgress * MathF.PI );
			var lungeMultiplier = MathF.Max( 0f, slot.Component?.AttackLungeMultiplier ?? 1f );
			lungeAmount = attackWave * PetAttackLungeDistance * lungeMultiplier;
			bobHeight += attackWave * PetBobHeight * 0.75f;
		}

		var lungeDirection = slot.AttackTargetPosition - slot.Pet.WorldPosition;
		lungeDirection.z = 0f;
		lungeDirection = lungeDirection.LengthSquared > 0.01f ? lungeDirection.Normal : Vector3.Zero;
		var localLunge = WorldDirectionToLocal( lungeDirection * lungeAmount, slot.Pet.WorldRotation );

		foreach ( var visual in slot.Visuals )
		{
			if ( !visual.GameObject.IsValid() )
				continue;

			visual.GameObject.LocalPosition = visual.LocalPosition + localLunge + new Vector3( 0f, 0f, bobHeight );
		}
	}

	private void FacePetDirection( PetSlot slot, Vector3 previousPosition, float attackProgress )
	{
		if ( attackProgress > 0f )
		{
			var attackDirection = slot.AttackTargetPosition - slot.Pet.WorldPosition;
			attackDirection.z = 0f;

			if ( attackDirection.LengthSquared > 0.01f )
			{
				TurnPetTowards( slot, Rotation.LookAt( attackDirection.Normal, Vector3.Up ) );
				return;
			}
		}

		FaceMovementDirection( slot, previousPosition );
	}

	private void FaceMovementDirection( PetSlot slot, Vector3 previousPosition )
	{
		var delta = slot.Pet.WorldPosition - previousPosition;
		delta.z = 0f;
		if ( delta.LengthSquared < 0.01f )
			return;

		TurnPetTowards( slot, Rotation.LookAt( delta.Normal, Vector3.Up ) );
	}

	private void TurnPetTowards( PetSlot slot, Rotation targetRotation )
	{
		if ( PetTurnSpeed <= 0f )
		{
			slot.Pet.WorldRotation = targetRotation;
			return;
		}

		var turnAmount = 1f - MathF.Exp( -PetTurnSpeed * Time.Delta );
		slot.Pet.WorldRotation = slot.Pet.WorldRotation.SlerpTo( targetRotation, turnAmount.Clamp( 0f, 1f ) );
	}

	private Vector3 SnapToGround( Vector3 position )
	{
		if ( Scene == null || GroundTraceHeight <= 0f )
			return position;

		AdminPerfController.GroundTraceAccumulator++;
		var start = position + (Vector3.Up * GroundTraceHeight);
		var end = position + (Vector3.Down * GroundTraceHeight);
		var trace = Scene.Trace.Ray( start, end )
			.IgnoreGameObject( GameObject )
			.Run();

		if ( trace.Hit )
		{
			position.z = trace.EndPosition.z + PetCircleHeight;
		}

		return position;
	}

	private static Vector3 WorldDirectionToLocal( Vector3 direction, Rotation rotation )
	{
		return new Vector3(
			Vector3.Dot( direction, rotation.Forward ),
			Vector3.Dot( direction, rotation.Right ),
			direction.z
		);
	}

	private static Vector3 MoveTowards( Vector3 current, Vector3 target, float maxDistance )
	{
		var delta = target - current;
		var distance = delta.Length;
		if ( distance <= maxDistance || distance <= 0.001f )
			return target;

		return current + (delta / distance * maxDistance);
	}

	private void RefreshEquippedPets()
	{
		DiscoverEquippedPets();

		for ( var i = equippedPets.Count - 1; i >= 0; i-- )
		{
			var slot = equippedPets[i];
			if ( !slot.Pet.IsValid() )
			{
				equippedPets.RemoveAt( i );
				continue;
			}

			if ( !slot.Component.IsValid() )
			{
				slot.Component = slot.Pet.GetComponent<PetComponent>() ?? slot.Pet.GetComponentInChildren<PetComponent>();
			}

			if ( slot.Visuals.Count == 0 )
			{
				slot.CacheVisuals();
			}
		}
	}

	private void DiscoverEquippedPets()
	{
		foreach ( var petComponent in GameObject.Components.GetAll<PetComponent>( FindMode.EverythingInChildren ) )
		{
			GetOrCreateSlot( petComponent.GameObject );
		}
	}

	private PetSlot GetOrCreateSlot( GameObject pet )
	{
		if ( !pet.IsValid() )
			return null;

		var slot = equippedPets.FirstOrDefault( existingSlot => existingSlot.Pet == pet );
		if ( slot != null )
			return slot;

		var petComponent = pet.GetComponent<PetComponent>() ?? pet.GetComponentInChildren<PetComponent>();
		slot = new PetSlot( pet, petComponent );
		equippedPets.Add( slot );
		return slot;
	}

	private sealed class PetSlot
	{
		public GameObject Pet { get; }
		public PetComponent Component { get; set; }
		public List<PetVisual> Visuals { get; } = new();
		public Vector3 RestWorldPosition { get; set; }
		public Vector3 AttackTargetPosition { get; set; }
		public Vector3 LastWorldPosition { get; set; }
		public float AttackTimeRemaining { get; set; }
		public float AttackCooldown { get; set; }
		public float BobTime { get; set; }

		public PetSlot( GameObject pet, PetComponent component )
		{
			Pet = pet;
			Component = component;
			AttackTargetPosition = pet.WorldPosition;
			LastWorldPosition = pet.WorldPosition;
			CacheVisuals();
		}

		public void CacheVisuals()
		{
			Visuals.Clear();

			foreach ( var renderer in Pet.Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ) )
			{
				if ( renderer.GameObject.IsValid() )
				{
					Visuals.Add( new PetVisual( renderer.GameObject ) );
				}
			}
		}
	}

	private sealed class PetVisual
	{
		public GameObject GameObject { get; }
		public Vector3 LocalPosition { get; }

		public PetVisual( GameObject gameObject )
		{
			GameObject = gameObject;
			LocalPosition = gameObject.LocalPosition;
		}
	}
}
