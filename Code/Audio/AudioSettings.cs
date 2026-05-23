using Sandbox;

/// <summary>
/// Client-local audio preferences (music / sound-effect mute), persisted to disk.
/// Muting is applied where audio is produced: <see cref="BackgroundMusicController"/> reads
/// <see cref="MusicMuted"/> for music volume, and the destructible sound path reads
/// <see cref="SoundMuted"/> before playing gameplay sound effects.
/// </summary>
public static class AudioSettings
{
	private const string SettingsDirectory = "settings";
	private const string SettingsFilePath = "settings/audio.json";

	public static bool MusicMuted { get; private set; }
	public static bool SoundMuted { get; private set; }

	// Bumped whenever a setting changes so UI build hashes can react.
	public static int Version { get; private set; }

	private static bool loaded;

	public static void EnsureLoaded()
	{
		if ( loaded )
			return;

		loaded = true;
		Load();
	}

	public static void ToggleMusicMuted()
	{
		SetMusicMuted( !MusicMuted );
	}

	public static void ToggleSoundMuted()
	{
		SetSoundMuted( !SoundMuted );
	}

	public static void SetMusicMuted( bool muted )
	{
		if ( MusicMuted == muted )
			return;

		MusicMuted = muted;
		OnChanged();
	}

	public static void SetSoundMuted( bool muted )
	{
		if ( SoundMuted == muted )
			return;

		SoundMuted = muted;
		OnChanged();
	}

	private static void OnChanged()
	{
		Version++;
		Save();
	}

	private static void Load()
	{
		try
		{
			if ( !FileSystem.Data.FileExists( SettingsFilePath ) )
				return;

			var json = FileSystem.Data.ReadAllText( SettingsFilePath );
			if ( string.IsNullOrWhiteSpace( json ) )
				return;

			var data = JSONObject.FromJson( json );
			if ( data == null )
				return;

			MusicMuted = data.Get( "MusicMuted", MusicMuted );
			SoundMuted = data.Get( "SoundMuted", SoundMuted );
		}
		catch ( System.Exception exception )
		{
			Log.Warning( exception, "Failed to load audio settings." );
		}
	}

	private static void Save()
	{
		try
		{
			FileSystem.Data.CreateDirectory( SettingsDirectory );

			var data = new JSONObject()
				.Set( "MusicMuted", MusicMuted )
				.Set( "SoundMuted", SoundMuted );

			FileSystem.Data.WriteAllText( SettingsFilePath, data.ToJson( true ) );
		}
		catch ( System.Exception exception )
		{
			Log.Warning( exception, "Failed to save audio settings." );
		}
	}
}
