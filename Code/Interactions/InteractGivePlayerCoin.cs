using Sandbox;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class InteractGivePlayerCoin : Interactable
{
	[Property, Sync] public int health { get; set; } = 10;
	[Property, Sync] public int maxHealth { get; set; } = 10;

	[Property] public int moneyPerHit = 1;
	[Property] public int moneyWhenDestroyed = 5;

	[Property] public float HitMoveDistance { get; set; } = 10f;
	[Property] public float HitFeedbackDuration { get; set; } = 0.22f;
	[Property] public Color HitFlashColor { get; set; } = new( 1f, 0.08f, 0.05f, 1f );
	[Property, Group( "Sounds" )] public List<SoundEvent> HitSounds { get; set; } = new();
	[Property, Group( "Sounds" )] public List<SoundEvent> BreakSounds { get; set; } = new();
	[Property, Group( "Sounds" )] public Vector2 SoundPitchRange { get; set; } = new( 0.94f, 1.08f );
	[Property, Group( "Sounds" )] public Vector2 SoundVolumeRange { get; set; } = new( 0.88f, 1.04f );

	private WorldSpaceHealthbar healthbar;
	private readonly List<ModelVisual> modelVisuals = new();

	private float hitFeedbackRemaining;
	private Vector3 hitFeedbackDirection = Vector3.Up;
	private bool hitFeedbackActive;

	public bool CanReceivePetDamage => health > 0;


	protected override void OnStart()
	{
		CacheModelVisuals();
	}

	protected override void OnUpdate()
	{
		UpdateHitFeedback();
	}

	protected override void OnDisabled()
	{
		RestoreHitFeedback();
	}

	protected override void OnDestroy()
	{
		RestoreHitFeedback();
	}

	[Rpc.Host]
	void CreateHealthbar()
	{
		GameObject clone = GameObject.GetPrefab( "Prefabs/CoinHealthbar.prefab" ).Clone();
		healthbar = clone.GetComponentInChildren<WorldSpaceHealthbar>();
		healthbar.health = health;
		healthbar.maxHealth = maxHealth;

		clone.Parent = GameObject;
		clone.LocalPosition = Vector3.Zero;
		clone.NetworkSpawn();
	}

	void UpdateHealthbar()
	{
		if ( healthbar == null )
		{
			if ( Network.IsOwner )
			{
				CreateHealthbar();
			}
			else
			{
				healthbar = GetComponentInChildren<WorldSpaceHealthbar>();
			}
		}
		if ( healthbar == null ) return;

		healthbar.health = health;
		healthbar.maxHealth = maxHealth;


	}


	[Rpc.Broadcast]
	public override void OnInteract( PlayerController interactingPlayer )
	{
		BeginHitFeedback();

		if ( IsProxy )
			return;

		interactingPlayer?.GetComponent<PetFramework>()?.AttackTarget( GameObject );

		ApplyDamage( 1, interactingPlayer, false );
	}

	[Rpc.Broadcast]
	public void ApplyPetDamage( PlayerController interactingPlayer, int damage )
	{
		if ( damage <= 0 || health <= 0 )
			return;

		BeginHitFeedback();

		if ( IsProxy )
			return;

		ApplyDamage( damage, interactingPlayer, true );
	}

	private void ApplyDamage( int damage, PlayerController interactingPlayer, bool petDamage )
	{
		if ( damage <= 0 || health <= 0 )
			return;

		health -= damage;
		var destroyed = health <= 0;

		var playerData = interactingPlayer?.GetComponent<PlayerData>();
		GameStatsTracker.RecordObjectDamage( GameObject.Name, damage, destroyed, petDamage );
		PlayRandomDestructibleSound( destroyed ? BreakSounds : HitSounds );

		if ( destroyed )
		{
			playerData?.AddMoney( moneyWhenDestroyed );
			GameObject.Destroy();
		}
		else
		{
			playerData?.AddMoney( moneyPerHit );
		}

		UpdateHealthbar();
	}

	private void PlayRandomDestructibleSound( List<SoundEvent> sounds )
	{
		if ( sounds == null || sounds.Count == 0 )
			return;

		var validSounds = sounds.Where( sound => sound.IsValid() && !string.IsNullOrWhiteSpace( sound.ResourcePath ) ).ToList();
		if ( validSounds.Count == 0 )
			return;

		var sound = validSounds[Game.Random.Next( validSounds.Count )];
		var pitch = RandomInRange( SoundPitchRange, 1f );
		var volume = RandomInRange( SoundVolumeRange, 1f );
		PlayDestructibleSound( sound.ResourcePath, WorldPosition, pitch, volume );
	}

	[Rpc.Broadcast]
	private void PlayDestructibleSound( string soundPath, Vector3 position, float pitch, float volume )
	{
		if ( string.IsNullOrWhiteSpace( soundPath ) )
			return;

		var handle = Sound.Play( soundPath, position );
		handle.Pitch = MathF.Max( 0.01f, pitch );
		handle.Volume = MathF.Max( 0f, volume );
	}

	private static float RandomInRange( Vector2 range, float fallback )
	{
		var min = MathF.Min( range.x, range.y );
		var max = MathF.Max( range.x, range.y );
		if ( max <= 0f )
			return fallback;

		return min + ((float)Game.Random.NextDouble() * (max - min));
	}

	private void BeginHitFeedback()
	{
		if ( modelVisuals.Count == 0 )
		{
			CacheModelVisuals();
		}

		hitFeedbackRemaining = MathF.Max( 0.01f, HitFeedbackDuration );
		hitFeedbackDirection = new Vector3(
			(float)Game.Random.NextDouble() - 0.5f,
			(float)Game.Random.NextDouble() - 0.5f,
			(float)Game.Random.NextDouble() * 0.35f + 0.25f
		).Normal;

		hitFeedbackActive = true;
		ApplyHitFeedback( 0f );
	}

	private void UpdateHitFeedback()
	{
		if ( !hitFeedbackActive )
			return;

		var duration = MathF.Max( 0.01f, HitFeedbackDuration );
		var progress = 1f - (hitFeedbackRemaining / duration).Clamp( 0f, 1f );

		ApplyHitFeedback( progress );

		hitFeedbackRemaining -= Time.Delta;
		if ( hitFeedbackRemaining > 0f )
			return;

		RestoreHitFeedback();
	}

	private void ApplyHitFeedback( float progress )
	{
		var remaining = 1f - progress.Clamp( 0f, 1f );
		var spring = remaining * remaining * MathF.Cos( progress * MathF.PI * 3f );
		var flashAmount = remaining * remaining;
		var offset = hitFeedbackDirection * HitMoveDistance * spring;

		foreach ( var visual in modelVisuals )
		{
			if ( !visual.Renderer.IsValid() )
				continue;

			if ( visual.GameObject.IsValid() && visual.GameObject != GameObject )
			{
				visual.GameObject.LocalPosition = visual.LocalPosition + offset;
			}

			visual.Renderer.Tint = Color.Lerp( visual.Tint, HitFlashColor, flashAmount, true );
		}
	}

	private void RestoreHitFeedback()
	{
		hitFeedbackActive = false;
		hitFeedbackRemaining = 0f;

		foreach ( var visual in modelVisuals )
		{
			if ( !visual.Renderer.IsValid() )
				continue;

			if ( visual.GameObject.IsValid() && visual.GameObject != GameObject )
			{
				visual.GameObject.LocalPosition = visual.LocalPosition;
			}

			visual.Renderer.Tint = visual.Tint;
		}
	}

	private void CacheModelVisuals()
	{
		modelVisuals.Clear();

		foreach ( var renderer in GameObject.Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndDescendants ) )
		{
			if ( !renderer.IsValid() )
				continue;

			modelVisuals.Add( new ModelVisual( renderer ) );
		}
	}

	private sealed class ModelVisual
	{
		public ModelRenderer Renderer { get; }
		public GameObject GameObject { get; }
		public Vector3 LocalPosition { get; }
		public Color Tint { get; }

		public ModelVisual( ModelRenderer renderer )
		{
			Renderer = renderer;
			GameObject = renderer.GameObject;
			LocalPosition = renderer.GameObject.LocalPosition;
			Tint = renderer.Tint;
		}
	}
}
