using Sandbox;
using System;

[Title( "Volumetric Cloud Animator" )]
[Category( "Environment" )]
[Icon( "cloud" )]
public sealed class VolumetricCloudAnimator : Component, Component.ExecuteInEditor
{
	private const string CloudNamePrefix = "Cloud Puff";
	private const string VisualName = "Cloud Visible Layer";

	public enum CloudCoveragePreset
	{
		Custom,
		Clear,
		Fluffy,
		Overcast
	}

	[Property] public CloudCoveragePreset Preset { get; set; } = CloudCoveragePreset.Fluffy;
	[Property] public bool UseProceduralNoiseLayer { get; set; } = true;
	[Property, Range( 1, 128 )] public int CloudCount { get; set; } = 49;
	[Property, Range( 1, 16 )] public int CloudColumns { get; set; } = 7;
	[Property, Range( 1, 16 )] public int CloudRows { get; set; } = 7;
	[Property] public bool AutoGenerateVolumes { get; set; } = true;
	[Property] public bool ArrangeVolumesFromSeed { get; set; } = true;
	[Property] public bool EnableVisibleFallback { get; set; } = true;
	[Property] public bool AnimateInEditor { get; set; } = false;
	[Property] public int Seed { get; set; } = 193;

	[Property] public bool FollowMainCamera { get; set; } = true;
	[Property] public GameObject FollowTarget { get; set; }
	[Property] public Vector3 FollowOffset { get; set; } = Vector3.Zero;
	[Property, Range( 0f, 10000f )] public float LayerHeight { get; set; } = 1850f;
	[Property, Range( 0f, 30000f )] public float FadeStartDistance { get; set; } = 10500f;
	[Property, Range( 1000f, 40000f )] public float FadeEndDistance { get; set; } = 15000f;

	[Property] public Vector3 Center { get; set; } = new( 1000f, 1800f, 1850f );
	[Property] public Vector3 AreaSize { get; set; } = new( 22000f, 16000f, 850f );
	[Property] public Vector3 MinVolumeSize { get; set; } = new( 2000f, 1400f, 650f );
	[Property] public Vector3 MaxVolumeSize { get; set; } = new( 5200f, 3600f, 1450f );

	[Property, Range( 256f, 20000f )] public float NoiseScale { get; set; } = 4300f;
	[Property, Range( 1, 8 )] public int NoiseOctaves { get; set; } = 4;
	[Property, Range( 0f, 1f )] public float Coverage { get; set; } = 0.48f;
	[Property, Range( 0f, 1f )] public float Fluffiness { get; set; } = 0.78f;
	[Property, Range( 0f, 1f )] public float DetailStrength { get; set; } = 0.52f;
	[Property, Range( 0f, 1f )] public float OvercastFloor { get; set; } = 0.28f;

	[Property] public Vector3 WindDirection { get; set; } = new( 1f, 0.28f, 0f );
	[Property, Range( 0f, 500f )] public float WindSpeed { get; set; } = 30f;
	[Property, Range( 0f, 3f )] public float Strength { get; set; } = 1.15f;
	[Property, Range( 0.2f, 8f )] public float EdgeSoftness { get; set; } = 1.15f;
	[Property, Range( 0f, 500f )] public float VerticalBob { get; set; } = 65f;
	[Property, Range( 0f, 2f )] public float BobSpeed { get; set; } = 0.08f;
	[Property, Range( 0f, 0.5f )] public float ScalePulse { get; set; } = 0.055f;

	[Property] public string VisibleFallbackTexture { get; set; } = "textures/particles/smoke/smoke_static_b.vtex";
	[Property] public Color VisibleFallbackTint { get; set; } = new( 1f, 1f, 1f, 0.58f );
	[Property, Range( 0.25f, 3f )] public float VisibleFallbackScale { get; set; } = 1.08f;

	private readonly Dictionary<GameObject, CloudState> cloudStates = new();
	private string loadedFallbackTexturePath;
	private Texture loadedFallbackTexture;
	private Sprite loadedFallbackSprite;

	private int RequiredCloudCount => UseProceduralNoiseLayer
		? Math.Max( 1, CloudColumns ) * Math.Max( 1, CloudRows )
		: Math.Max( 1, CloudCount );

	protected override void OnStart()
	{
		EnsureClouds();
		ArrangeClouds();
		ApplyVolumeSettings();
	}

	protected override void OnValidate()
	{
		CloudColumns = Math.Max( 1, CloudColumns );
		CloudRows = Math.Max( 1, CloudRows );
		CloudCount = Math.Max( 1, UseProceduralNoiseLayer ? RequiredCloudCount : CloudCount );
		FadeEndDistance = MathF.Max( FadeEndDistance, FadeStartDistance + 500f );
		AreaSize = MaxVector( AreaSize, new Vector3( 100f, 100f, 100f ) );
		MinVolumeSize = MaxVector( MinVolumeSize, new Vector3( 64f, 64f, 64f ) );
		MaxVolumeSize = MaxVector( MaxVolumeSize, MinVolumeSize );
		NoiseScale = MathF.Max( 64f, NoiseScale );

		cloudStates.Clear();
		EnsureClouds();
		ArrangeClouds();
		ApplyVolumeSettings();
	}

	protected override void OnUpdate()
	{
		if ( Game.IsEditor && !AnimateInEditor )
			return;

		EnsureClouds();

		if ( UseProceduralNoiseLayer )
		{
			UpdateProceduralClouds();
			return;
		}

		AnimateSeededClouds();
	}

	private void EnsureClouds()
	{
		var required = RequiredCloudCount;
		var clouds = GetClouds().ToList();

		if ( AutoGenerateVolumes )
		{
			for ( var i = clouds.Count; i < required; i++ )
			{
				var cloud = CreateCloud( i );
				clouds.Add( cloud );
			}
		}

		for ( var i = 0; i < clouds.Count; i++ )
		{
			clouds[i].Enabled = i < required;
			GetState( clouds[i], i );
			EnsureVisibleFallback( clouds[i] );
		}
	}

	private void ArrangeClouds()
	{
		if ( UseProceduralNoiseLayer )
		{
			UpdateProceduralClouds();
			return;
		}

		if ( !ArrangeVolumesFromSeed )
			return;

		var clouds = GetClouds().ToList();
		for ( var i = 0; i < clouds.Count; i++ )
		{
			var random = CreateRandom( i );
			var cloud = clouds[i];
			cloud.WorldPosition = CreateCloudPosition( random );
			cloud.WorldRotation = Rotation.FromYaw( RandomRange( random, -8f, 8f ) );

			if ( cloud.GetComponent<VolumetricFogVolume>( true ) is { } volume )
				volume.Bounds = BoundsFromSize( CreateCloudSize( random ) );
		}

		cloudStates.Clear();
	}

	private void UpdateProceduralClouds()
	{
		var clouds = GetClouds().ToList();
		var layerCenter = GetLayerCenter();
		var windOffset = GetWind() * WindSpeed * Time.Now;
		var columns = Math.Max( 1, CloudColumns );
		var rows = Math.Max( 1, CloudRows );
		var required = RequiredCloudCount;

		for ( var i = 0; i < clouds.Count; i++ )
		{
			var cloud = clouds[i];
			if ( i >= required )
			{
				cloud.Enabled = false;
				continue;
			}

			var column = i % columns;
			var row = i / columns;
			var x = columns == 1 ? 0f : column / (float)(columns - 1) - 0.5f;
			var y = rows == 1 ? 0f : row / (float)(rows - 1) - 0.5f;
			var basePosition = layerCenter + new Vector3( x * AreaSize.x, y * AreaSize.y, 0f );
			var noisePosition = basePosition + windOffset;
			var noise = FractalNoise( noisePosition.x / NoiseScale, noisePosition.y / NoiseScale );
			var density = EvaluateDensity( noise );
			var fade = DistanceFade( basePosition, layerCenter );
			var visibleDensity = density * fade;
			var state = GetState( cloud, i );

			var heightNoise = FractalNoise( noisePosition.x / (NoiseScale * 1.7f) + 37.2f, noisePosition.y / (NoiseScale * 1.7f) - 18.9f ) - 0.5f;
			var bob = MathF.Sin( Time.Now * BobSpeed + state.Phase ) * VerticalBob * state.BobScale;
			basePosition.z += heightNoise * AreaSize.z * 0.5f + bob;

			var pulse = 1f + MathF.Sin( Time.Now * BobSpeed * 0.67f + state.Phase * 1.7f ) * ScalePulse;
			var size = Lerp( MinVolumeSize, MaxVolumeSize, SmoothStep( 0f, 1f, density ) ) * pulse;

			cloud.Enabled = true;
			cloud.WorldPosition = basePosition;
			cloud.WorldRotation = Rotation.FromYaw( state.Yaw );

			if ( cloud.GetComponent<VolumetricFogVolume>( true ) is { } volume )
			{
				volume.Enabled = visibleDensity > 0.025f;
				volume.Bounds = BoundsFromSize( size );
				volume.Strength = EffectiveStrength() * visibleDensity * state.StrengthScale;
				volume.FalloffExponent = EffectiveEdgeSoftness();
			}

			ApplyVisibleFallback( cloud, size, visibleDensity );
		}
	}

	private void AnimateSeededClouds()
	{
		var wind = GetWind();
		var delta = wind * WindSpeed * Time.Delta;
		var clouds = GetClouds().ToList();
		var fadeCenter = GetLayerCenter();

		for ( var i = 0; i < clouds.Count; i++ )
		{
			var cloud = clouds[i];
			if ( !cloud.Enabled || i >= CloudCount )
				continue;

			var state = GetState( cloud, i );
			var pos = cloud.WorldPosition + delta;
			pos.x = Wrap( pos.x, Center.x - AreaSize.x * 0.5f, Center.x + AreaSize.x * 0.5f );
			pos.y = Wrap( pos.y, Center.y - AreaSize.y * 0.5f, Center.y + AreaSize.y * 0.5f );
			pos.z = state.BaseHeight + MathF.Sin( Time.Now * BobSpeed + state.Phase ) * VerticalBob * state.BobScale;
			cloud.WorldPosition = pos;

			var distanceFade = DistanceFade( pos, fadeCenter );
			if ( cloud.GetComponent<VolumetricFogVolume>( true ) is not { } volume )
				continue;

			var pulse = 1f + MathF.Sin( Time.Now * BobSpeed * 0.67f + state.Phase * 1.7f ) * ScalePulse;
			var size = state.Size * pulse;
			volume.Enabled = distanceFade > 0.025f;
			volume.Bounds = BoundsFromSize( size );
			volume.Strength = EffectiveStrength() * state.StrengthScale * distanceFade;
			volume.FalloffExponent = EffectiveEdgeSoftness();
			ApplyVisibleFallback( cloud, size, distanceFade );
		}
	}

	private void ApplyVolumeSettings()
	{
		if ( UseProceduralNoiseLayer )
		{
			UpdateProceduralClouds();
			return;
		}

		var clouds = GetClouds().ToList();
		for ( var i = 0; i < clouds.Count; i++ )
		{
			if ( clouds[i].GetComponent<VolumetricFogVolume>( true ) is not { } volume )
				continue;

			var state = GetState( clouds[i], i );
			volume.Bounds = BoundsFromSize( state.Size );
			volume.Strength = EffectiveStrength() * state.StrengthScale;
			volume.FalloffExponent = EffectiveEdgeSoftness();
			ApplyVisibleFallback( clouds[i], state.Size, 1f );
		}
	}

	private GameObject CreateCloud( int index )
	{
		var random = CreateRandom( index );
		var cloud = new GameObject( GameObject, true, $"{CloudNamePrefix} {index + 1:00}" );
		cloud.NetworkMode = NetworkMode.Never;
		cloud.WorldPosition = CreateCloudPosition( random );
		cloud.WorldRotation = Rotation.FromYaw( RandomRange( random, -6f, 6f ) );
		cloud.WorldScale = Vector3.One;

		var volume = cloud.AddComponent<VolumetricFogVolume>();
		var size = CreateCloudSize( random );
		volume.Bounds = BoundsFromSize( size );
		volume.Strength = EffectiveStrength() * RandomRange( random, 0.78f, 1.12f );
		volume.FalloffExponent = EffectiveEdgeSoftness();

		cloudStates[cloud] = CreateState( cloud, index, size );
		EnsureVisibleFallback( cloud );
		ApplyVisibleFallback( cloud, size, 1f );
		return cloud;
	}

	private void EnsureVisibleFallback( GameObject cloud )
	{
		var visual = cloud.Children.FirstOrDefault( child => child.Name == VisualName );
		if ( visual is null )
		{
			visual = new GameObject( cloud, true, VisualName );
			visual.NetworkMode = NetworkMode.Never;
			visual.LocalPosition = Vector3.Zero;
			visual.LocalRotation = Rotation.Identity;
			visual.LocalScale = Vector3.One;
		}

		visual.Enabled = EnableVisibleFallback;
		var renderer = visual.GetOrAddComponent<SpriteRenderer>();
		renderer.Billboard = SpriteRenderer.BillboardMode.Always;
		renderer.Lighting = false;
		renderer.Shadows = false;
		renderer.Opaque = false;
		renderer.DepthFeather = 320f;
		renderer.FogStrength = 0f;
		renderer.IsSorted = true;
		ApplyFallbackTexture( renderer );
	}

	private void ApplyFallbackTexture( SpriteRenderer renderer )
	{
		if ( loadedFallbackTexturePath != VisibleFallbackTexture )
		{
			loadedFallbackTexturePath = VisibleFallbackTexture;
			loadedFallbackTexture = Texture.Load( VisibleFallbackTexture, false );
			loadedFallbackSprite = Sprite.FromTexture( loadedFallbackTexture );
		}

		renderer.Sprite = loadedFallbackSprite;
		renderer.StartingAnimationName = "Default";
	}

	private void ApplyVisibleFallback( GameObject cloud, Vector3 size, float opacity )
	{
		var visual = cloud.Children.FirstOrDefault( child => child.Name == VisualName );
		if ( visual?.GetComponent<SpriteRenderer>( true ) is not { } renderer )
			return;

		var visible = EnableVisibleFallback && opacity > 0.025f;
		visual.Enabled = visible;
		renderer.Size = new Vector2( size.x * VisibleFallbackScale, size.y * VisibleFallbackScale );
		renderer.Color = WithAlpha( VisibleFallbackTint, VisibleFallbackTint.a * Saturate( opacity ) );
	}

	private IEnumerable<GameObject> GetClouds()
	{
		return GameObject.Children
			.Where( child => child.Name.StartsWith( CloudNamePrefix, StringComparison.OrdinalIgnoreCase ) )
			.Where( child => child.GetComponent<VolumetricFogVolume>( true ) is not null )
			.OrderBy( child => child.Name );
	}

	private CloudState GetState( GameObject cloud, int index )
	{
		if ( cloudStates.TryGetValue( cloud, out var state ) )
			return state;

		var size = GetVolumeSize( cloud.GetComponent<VolumetricFogVolume>( true ) );
		if ( size.LengthSquared < 1f )
			size = CreateCloudSize( CreateRandom( index ) );

		state = CreateState( cloud, index, size );
		cloudStates[cloud] = state;
		return state;
	}

	private CloudState CreateState( GameObject cloud, int index, Vector3 size )
	{
		var random = CreateRandom( index );
		return new CloudState
		{
			Size = size,
			BaseHeight = cloud.WorldPosition.z,
			Phase = RandomRange( random, 0f, MathF.PI * 2f ),
			BobScale = RandomRange( random, 0.65f, 1.35f ),
			StrengthScale = RandomRange( random, 0.82f, 1.15f ),
			Yaw = RandomRange( random, -10f, 10f )
		};
	}

	private Vector3 GetLayerCenter()
	{
		if ( !FollowMainCamera && FollowTarget is null )
			return Center;

		var targetPosition = FollowTarget?.WorldPosition ?? Scene?.Camera?.WorldPosition ?? Center;
		return new Vector3(
			targetPosition.x + FollowOffset.x,
			targetPosition.y + FollowOffset.y,
			targetPosition.z + LayerHeight + FollowOffset.z
		);
	}

	private Vector3 CreateCloudPosition( Random random )
	{
		return new Vector3(
			Center.x + RandomRange( random, -AreaSize.x * 0.5f, AreaSize.x * 0.5f ),
			Center.y + RandomRange( random, -AreaSize.y * 0.5f, AreaSize.y * 0.5f ),
			Center.z + RandomRange( random, -AreaSize.z * 0.5f, AreaSize.z * 0.5f )
		);
	}

	private Vector3 CreateCloudSize( Random random )
	{
		return new Vector3(
			RandomRange( random, MinVolumeSize.x, MaxVolumeSize.x ),
			RandomRange( random, MinVolumeSize.y, MaxVolumeSize.y ),
			RandomRange( random, MinVolumeSize.z, MaxVolumeSize.z )
		);
	}

	private float EvaluateDensity( float noise )
	{
		if ( Preset == CloudCoveragePreset.Clear )
			return 0f;

		var coverage = EffectiveCoverage();
		var fluffiness = EffectiveFluffiness();
		var threshold = Lerp( 1.08f, 0.18f, coverage );
		var contrast = Lerp( 2.4f, 8.5f, fluffiness );
		var density = SmoothStep( 0f, 1f, Saturate( (noise - threshold) * contrast ) );

		var overcast = Saturate( (coverage - 0.68f) / 0.32f );
		if ( overcast > 0f )
		{
			var floor = EffectiveOvercastFloor() * overcast;
			density = MathF.Max( density, floor * Lerp( 0.72f, 1f, noise ) );
		}

		return Saturate( density );
	}

	private float FractalNoise( float x, float y )
	{
		var value = 0f;
		var amplitude = 1f;
		var frequency = 1f;
		var amplitudeTotal = 0f;
		var persistence = Lerp( 0.38f, 0.68f, EffectiveDetailStrength() );
		var octaves = Math.Clamp( NoiseOctaves, 1, 8 );

		for ( var i = 0; i < octaves; i++ )
		{
			value += ValueNoise( x * frequency, y * frequency ) * amplitude;
			amplitudeTotal += amplitude;
			amplitude *= persistence;
			frequency *= 2f;
		}

		return amplitudeTotal > 0f ? value / amplitudeTotal : 0f;
	}

	private float ValueNoise( float x, float y )
	{
		var x0 = (int)MathF.Floor( x );
		var y0 = (int)MathF.Floor( y );
		var tx = x - x0;
		var ty = y - y0;
		var sx = tx * tx * tx * (tx * (tx * 6f - 15f) + 10f);
		var sy = ty * ty * ty * (ty * (ty * 6f - 15f) + 10f);

		var a = Hash01( x0, y0 );
		var b = Hash01( x0 + 1, y0 );
		var c = Hash01( x0, y0 + 1 );
		var d = Hash01( x0 + 1, y0 + 1 );
		return Lerp( Lerp( a, b, sx ), Lerp( c, d, sx ), sy );
	}

	private float Hash01( int x, int y )
	{
		unchecked
		{
			var hash = (uint)Seed;
			hash ^= (uint)x * 374761393u;
			hash ^= (uint)y * 668265263u;
			hash = (hash ^ (hash >> 13)) * 1274126177u;
			hash ^= hash >> 16;
			return (hash & 0x00FFFFFF) / 16777215f;
		}
	}

	private float DistanceFade( Vector3 position, Vector3 center )
	{
		var offset = new Vector3( position.x - center.x, position.y - center.y, 0f );
		var fade = 1f - SmoothStep( FadeStartDistance, FadeEndDistance, offset.Length );
		return Saturate( fade );
	}

	private float EffectiveCoverage()
	{
		return Preset switch
		{
			CloudCoveragePreset.Clear => 0f,
			CloudCoveragePreset.Fluffy => 0.48f,
			CloudCoveragePreset.Overcast => 0.92f,
			_ => Saturate( Coverage )
		};
	}

	private float EffectiveFluffiness()
	{
		return Preset switch
		{
			CloudCoveragePreset.Clear => 0f,
			CloudCoveragePreset.Fluffy => 0.78f,
			CloudCoveragePreset.Overcast => 0.22f,
			_ => Saturate( Fluffiness )
		};
	}

	private float EffectiveDetailStrength()
	{
		return Preset switch
		{
			CloudCoveragePreset.Clear => 0f,
			CloudCoveragePreset.Fluffy => 0.58f,
			CloudCoveragePreset.Overcast => 0.28f,
			_ => Saturate( DetailStrength )
		};
	}

	private float EffectiveOvercastFloor()
	{
		return Preset switch
		{
			CloudCoveragePreset.Overcast => 0.42f,
			CloudCoveragePreset.Fluffy => 0.12f,
			CloudCoveragePreset.Clear => 0f,
			_ => Saturate( OvercastFloor )
		};
	}

	private float EffectiveStrength()
	{
		return Preset switch
		{
			CloudCoveragePreset.Clear => 0f,
			CloudCoveragePreset.Overcast => Strength * 1.12f,
			_ => Strength
		};
	}

	private float EffectiveEdgeSoftness()
	{
		return Preset switch
		{
			CloudCoveragePreset.Overcast => MathF.Max( EdgeSoftness, 2.1f ),
			_ => EdgeSoftness
		};
	}

	private Vector3 GetWind()
	{
		var wind = new Vector3( WindDirection.x, WindDirection.y, 0f );
		return wind.LengthSquared > 0.0001f ? wind.Normal : Vector3.Zero;
	}

	private Random CreateRandom( int index )
	{
		return new Random( Seed + index * 7919 );
	}

	private static float RandomRange( Random random, float min, float max )
	{
		return min + (max - min) * (float)random.NextDouble();
	}

	private static BBox BoundsFromSize( Vector3 size )
	{
		var half = size * 0.5f;
		return new BBox( -half, half );
	}

	private static Vector3 GetVolumeSize( VolumetricFogVolume volume )
	{
		if ( volume is null )
			return Vector3.Zero;

		var size = volume.Bounds.Maxs - volume.Bounds.Mins;
		return new Vector3( MathF.Abs( size.x ), MathF.Abs( size.y ), MathF.Abs( size.z ) );
	}

	private static Vector3 MaxVector( Vector3 value, Vector3 min )
	{
		return new Vector3(
			MathF.Max( value.x, min.x ),
			MathF.Max( value.y, min.y ),
			MathF.Max( value.z, min.z )
		);
	}

	private static Vector3 Lerp( Vector3 a, Vector3 b, float t )
	{
		return new Vector3(
			Lerp( a.x, b.x, t ),
			Lerp( a.y, b.y, t ),
			Lerp( a.z, b.z, t )
		);
	}

	private static float Lerp( float a, float b, float t )
	{
		return a + (b - a) * Saturate( t );
	}

	private static float SmoothStep( float min, float max, float value )
	{
		if ( max <= min )
			return value >= max ? 1f : 0f;

		var t = Saturate( (value - min) / (max - min) );
		return t * t * (3f - 2f * t);
	}

	private static float Saturate( float value )
	{
		return Math.Clamp( value, 0f, 1f );
	}

	private static Color WithAlpha( Color color, float alpha )
	{
		return new Color( color.r, color.g, color.b, Saturate( alpha ) );
	}

	private static float Wrap( float value, float min, float max )
	{
		var size = max - min;
		if ( size <= 0f )
			return min;

		while ( value < min )
			value += size;

		while ( value > max )
			value -= size;

		return value;
	}

	private sealed class CloudState
	{
		public Vector3 Size { get; init; }
		public float BaseHeight { get; init; }
		public float Phase { get; init; }
		public float BobScale { get; init; }
		public float StrengthScale { get; init; }
		public float Yaw { get; init; }
	}
}
