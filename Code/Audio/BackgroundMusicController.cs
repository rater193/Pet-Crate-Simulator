using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class BackgroundMusicController : Component
{
	public static BackgroundMusicController Local { get; private set; }

	[Property] public SoundEvent DefaultSound { get; set; }
	[Property] public SoundEvent TargetSound { get; set; }
	[Property] public float FadeDuration { get; set; } = 2f;
	[Property] public float MasterVolume { get; set; } = 1f;
	[Property] public bool PlayDefaultOnStart { get; set; } = true;
	[Property] public bool LoopMusic { get; set; } = true;
	[Property] public bool Force2d { get; set; } = true;

	private readonly Dictionary<Component, MusicRequest> requests = new();
	private MusicLayer currentLayer;
	private MusicLayer fadingLayer;
	private string currentSoundKey = string.Empty;
	private string targetSoundKey = string.Empty;
	private float targetVolume = 1f;
	private float fadeElapsed;
	private int requestSequence;

	protected override void OnEnabled()
	{
		Local = this;
	}

	protected override void OnDisabled()
	{
		if ( Local == this )
			Local = null;

		StopAllMusic( 0.25f );
	}

	protected override void OnDestroy()
	{
		if ( Local == this )
			Local = null;

		StopAllMusic( 0.25f );
	}

	protected override void OnStart()
	{
		Local = this;

		if ( PlayDefaultOnStart && DefaultSound.IsValid() && !TargetSound.IsValid() )
		{
			TargetSound = DefaultSound;
		}

		ApplyTargetSound();
	}

	protected override void OnUpdate()
	{
		ApplyTargetSound();
		UpdateLooping();
		UpdateFade();
	}

	public static BackgroundMusicController GetOrCreate( Scene scene )
	{
		if ( Local.IsValid() && Local.Scene == scene )
			return Local;

		var existing = scene?.GetAllComponents<BackgroundMusicController>().FirstOrDefault();
		if ( existing.IsValid() )
		{
			Local = existing;
			return existing;
		}

		if ( scene == null )
			return null;

		var controllerObject = new GameObject( scene, true, "Background Music Controller" );
		Local = controllerObject.Components.Create<BackgroundMusicController>();
		return Local;
	}

	public void RequestMusic( Component source, SoundEvent sound, int priority = 0, float volume = 1f )
	{
		if ( source == null || !sound.IsValid() )
			return;

		requests[source] = new MusicRequest( sound, priority, MathF.Max( 0f, volume ), ++requestSequence );
		RefreshRequestedTarget();
	}

	public void ClearRequest( Component source )
	{
		if ( source == null )
			return;

		if ( requests.Remove( source ) )
		{
			RefreshRequestedTarget();
		}
	}

	public void SetTarget( SoundEvent sound, float volume = 1f )
	{
		TargetSound = sound;
		targetVolume = MathF.Max( 0f, volume );
		ApplyTargetSound( forceVolumeRefresh: true );
	}

	private void RefreshRequestedTarget()
	{
		var bestRequest = requests
			.Where( entry => entry.Key.IsValid() && entry.Value.Sound.IsValid() )
			.Select( entry => entry.Value )
			.OrderByDescending( request => request.Priority )
			.ThenByDescending( request => request.Sequence )
			.FirstOrDefault();

		if ( bestRequest.Sound.IsValid() )
		{
			SetTarget( bestRequest.Sound, bestRequest.Volume );
			return;
		}

		SetTarget( DefaultSound, 1f );
	}

	private void ApplyTargetSound( bool forceVolumeRefresh = false )
	{
		var newTargetKey = GetSoundKey( TargetSound );
		if ( newTargetKey == targetSoundKey && !forceVolumeRefresh )
		{
			return;
		}

		targetSoundKey = newTargetKey;

		if ( string.IsNullOrWhiteSpace( targetSoundKey ) || !TargetSound.IsValid() )
		{
			FadeOutCurrent();
			return;
		}

		if ( currentSoundKey == targetSoundKey && currentLayer.Handle.IsValid() )
		{
			currentLayer.TargetVolume = GetTargetVolume();
			return;
		}

		StartTransition( TargetSound, targetSoundKey, GetTargetVolume() );
	}

	private void StartTransition( SoundEvent sound, string soundKey, float volume )
	{
		if ( fadingLayer.Handle.IsValid() )
		{
			fadingLayer.Handle.Stop( FadeDuration );
		}

		fadingLayer = currentLayer;
		fadingLayer.StartVolume = fadingLayer.Handle.IsValid() ? fadingLayer.Handle.Volume : 0f;
		fadingLayer.TargetVolume = 0f;

		currentLayer = StartLayer( sound, volume );
		currentSoundKey = soundKey;
		fadeElapsed = 0f;
	}

	private MusicLayer StartLayer( SoundEvent sound, float volume, float startVolume = 0f )
	{
		var handle = Sound.Play( sound );
		if ( handle.IsValid() )
		{
			handle.Volume = MathF.Max( 0f, startVolume );
			handle.Name = $"Background Music - {sound.ResourceName}";

			if ( Force2d )
			{
				handle.ListenLocal = true;
				handle.SpacialBlend = 0f;
				handle.DistanceAttenuation = false;
				handle.Occlusion = false;
				handle.AirAbsorption = false;
				handle.Transmission = false;
			}
		}

		return new MusicLayer
		{
			Sound = sound,
			Handle = handle,
			StartVolume = MathF.Max( 0f, startVolume ),
			TargetVolume = volume
		};
	}

	private void FadeOutCurrent()
	{
		if ( currentLayer.Handle.IsValid() )
		{
			currentLayer.Handle.Stop( FadeDuration );
		}

		currentLayer = default;
		currentSoundKey = string.Empty;
	}

	private void StopAllMusic( float fadeTime )
	{
		if ( currentLayer.Handle.IsValid() )
			currentLayer.Handle.Stop( fadeTime );

		if ( fadingLayer.Handle.IsValid() )
			fadingLayer.Handle.Stop( fadeTime );

		currentLayer = default;
		fadingLayer = default;
		currentSoundKey = string.Empty;
		targetSoundKey = string.Empty;
	}

	private void UpdateFade()
	{
		var duration = MathF.Max( 0.01f, FadeDuration );
		fadeElapsed += Time.Delta;
		var progress = Math.Clamp( fadeElapsed / duration, 0f, 1f );
		var easedProgress = progress * progress * (3f - (2f * progress));

		if ( currentLayer.Handle.IsValid() )
		{
			currentLayer.TargetVolume = GetTargetVolume();
			currentLayer.Handle.Volume = MathX.Lerp( currentLayer.StartVolume, currentLayer.TargetVolume, easedProgress );
		}

		if ( fadingLayer.Handle.IsValid() )
		{
			fadingLayer.Handle.Volume = MathX.Lerp( fadingLayer.StartVolume, 0f, easedProgress );
		}

		if ( progress < 1f )
			return;

		if ( currentLayer.Handle.IsValid() )
		{
			currentLayer.Handle.Volume = currentLayer.TargetVolume;
		}

		if ( fadingLayer.Handle.IsValid() )
		{
			fadingLayer.Handle.Stop();
		}

		fadingLayer = default;
	}

	private void UpdateLooping()
	{
		if ( !LoopMusic || !currentLayer.Sound.IsValid() || currentSoundKey != targetSoundKey )
			return;

		if ( currentLayer.Handle.IsValid() && currentLayer.Handle.IsPlaying )
			return;

		var restartVolume = GetTargetVolume();
		currentLayer = StartLayer( currentLayer.Sound, restartVolume, restartVolume );
		fadeElapsed = FadeDuration;
	}

	private float GetTargetVolume()
	{
		if ( AudioSettings.MusicMuted )
			return 0f;

		return MathF.Max( 0f, targetVolume * MasterVolume );
	}

	private static string GetSoundKey( SoundEvent sound )
	{
		if ( !sound.IsValid() )
			return string.Empty;

		if ( !string.IsNullOrWhiteSpace( sound.ResourcePath ) )
			return sound.ResourcePath;

		return sound.ResourceName ?? string.Empty;
	}

	private readonly record struct MusicRequest( SoundEvent Sound, int Priority, float Volume, int Sequence );

	private struct MusicLayer
	{
		public SoundEvent Sound;
		public SoundHandle Handle;
		public float StartVolume;
		public float TargetVolume;
	}
}
