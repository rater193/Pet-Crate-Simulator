using Sandbox;
using System;

public sealed class EquippedPetRarityVisuals : Component
{
	private const string ParticleTexturePath = "textures/particles/particle_glow_01.vtex";

	[Property] public PetRarity Rarity { get; set; } = PetRarity.Common;
	[Property] public float OutlineWidth { get; set; } = 0.42f;
	[Property] public float AuraRadius { get; set; } = 34f;
	[Property] public float AuraRate { get; set; } = 12f;

	private HighlightOutline outline;
	private ParticleEffect auraEffect;

	protected override void OnStart()
	{
		ApplyVisuals();
	}

	protected override void OnUpdate()
	{
		if ( Rarity < PetRarity.Uncommon )
			return;

		EnsureCameraHighlight();
		UpdateColor();
	}

	public void Configure( PetRarity rarity )
	{
		Rarity = rarity;
		ApplyVisuals();
	}

	private void ApplyVisuals()
	{
		if ( Rarity < PetRarity.Uncommon )
			return;

		EnsureCameraHighlight();
		EnsureOutline();

		if ( Rarity >= PetRarity.Legendary )
		{
			EnsureAura();
		}

		UpdateColor();
	}

	private void EnsureOutline()
	{
		outline ??= GameObject.GetOrAddComponent<HighlightOutline>();
		outline.Width = OutlineWidth;
		outline.InsideColor = Color.Transparent;
		outline.InsideObscuredColor = Color.Transparent;
	}

	private void EnsureAura()
	{
		if ( auraEffect.IsValid() )
			return;

		var auraObject = new GameObject( GameObject, true, "RarityAura" );
		auraObject.LocalPosition = Vector3.Zero;
		auraObject.LocalRotation = Rotation.Identity;
		auraObject.LocalScale = Vector3.One;

		auraEffect = auraObject.AddComponent<ParticleEffect>();
		auraEffect.MaxParticles = 80;
		auraEffect.Lifetime = new ParticleFloat( 0.75f, 1.45f );
		auraEffect.StartVelocity = new ParticleFloat( 4f, 18f );
		auraEffect.Damping = 0.35f;
		auraEffect.LocalSpace = 0.65f;
		auraEffect.ApplyColor = true;
		auraEffect.ApplyAlpha = true;
		auraEffect.Alpha = new ParticleFloat( 0.35f, 0.82f );
		auraEffect.ApplyShape = true;
		auraEffect.Scale = new ParticleFloat( 3.5f, 8f );

		var emitter = auraObject.AddComponent<ParticleSphereEmitter>();
		emitter.Radius = AuraRadius;
		emitter.Velocity = 10f;
		emitter.OnEdge = true;
		emitter.Burst = 10f;
		emitter.Rate = AuraRate;
		emitter.Loop = true;

		var renderer = auraObject.AddComponent<ParticleSpriteRenderer>();
		renderer.Sprite = Sprite.FromTexture( Texture.Load( ParticleTexturePath, false ) ?? Texture.White );
		renderer.Additive = true;
		renderer.Scale = 1f;
		renderer.DepthFeather = 18f;
		renderer.FogStrength = 0.15f;

		var light = auraObject.AddComponent<ParticleLightRenderer>();
		light.Ratio = 0.18f;
		light.MaximumLights = 3;
		light.Scale = 46f;
		light.Brightness = 0.45f;
		light.UseParticleColor = true;
	}

	private void UpdateColor()
	{
		var color = GetCurrentColor();

		if ( outline.IsValid() )
		{
			outline.Color = color;
			outline.ObscuredColor = color.WithAlpha( 0.28f );
		}

		if ( auraEffect.IsValid() )
		{
			auraEffect.Tint = color;
			auraEffect.Gradient = color;
		}
	}

	private Color GetCurrentColor()
	{
		if ( Rarity != PetRarity.Ancestral )
			return Rarity.GetColor();

		return new ColorHsv( (Time.Now * 120f) % 360f, 0.82f, 1f, 1f );
	}

	private void EnsureCameraHighlight()
	{
		if ( Scene?.Camera.IsValid() != true )
			return;

		Scene.Camera.GameObject.GetOrAddComponent<Highlight>();
	}
}
