using Sandbox;
using System.Collections.Generic;

public sealed class MusicZoneTrigger : Component, Component.ITriggerListener
{
	[Property] public SoundEvent Music { get; set; }
	[Property] public BackgroundMusicController Controller { get; set; }
	[Property] public int Priority { get; set; }
	[Property] public float Volume { get; set; } = 1f;
	[Property] public bool ClearMusicWhenExited { get; set; } = true;

	private readonly Dictionary<GameObject, int> touchingLocalPlayers = new();

	protected override void OnStart()
	{
		EnsureTriggerCollider();
	}

	protected override void OnDisabled()
	{
		ClearMusicRequest();
		touchingLocalPlayers.Clear();
	}

	protected override void OnDestroy()
	{
		ClearMusicRequest();
	}

	public void OnTriggerEnter( Collider other )
	{
		var player = GetLocalPlayer( other );
		if ( player == null )
			return;

		var playerObject = player.GameObject;
		touchingLocalPlayers.TryGetValue( playerObject, out var touchCount );
		touchingLocalPlayers[playerObject] = touchCount + 1;

		if ( touchCount == 0 )
		{
			RequestMusic();
		}
	}

	public void OnTriggerExit( Collider other )
	{
		var player = GetLocalPlayer( other );
		if ( player == null )
			return;

		var playerObject = player.GameObject;
		if ( !touchingLocalPlayers.TryGetValue( playerObject, out var touchCount ) )
			return;

		touchCount--;
		if ( touchCount > 0 )
		{
			touchingLocalPlayers[playerObject] = touchCount;
			return;
		}

		touchingLocalPlayers.Remove( playerObject );

		if ( touchingLocalPlayers.Count == 0 && ClearMusicWhenExited )
		{
			ClearMusicRequest();
		}
	}

	public void RequestMusic()
	{
		if ( !Music.IsValid() )
			return;

		ResolveController()?.RequestMusic( this, Music, Priority, Volume );
	}

	public void ClearMusicRequest()
	{
		ResolveController()?.ClearRequest( this );
	}

	private BackgroundMusicController ResolveController()
	{
		if ( Controller.IsValid() )
			return Controller;

		Controller = BackgroundMusicController.GetOrCreate( Scene );
		return Controller;
	}

	private PlayerController GetLocalPlayer( Collider other )
	{
		if ( other == null )
			return null;

		var player = other.Components.Get<PlayerController>( FindMode.InAncestors );
		if ( player == null || player.IsProxy )
			return null;

		return player;
	}

	private void EnsureTriggerCollider()
	{
		var collider = Components.Get<Collider>( FindMode.EverythingInSelf );
		if ( collider == null )
		{
			var box = Components.Create<BoxCollider>();
			box.Scale = new Vector3( 512f, 512f, 256f );
			box.IsTrigger = true;
			return;
		}

		collider.IsTrigger = true;
	}
}
