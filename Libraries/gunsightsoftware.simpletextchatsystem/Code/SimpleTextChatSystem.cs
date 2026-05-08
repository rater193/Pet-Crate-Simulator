using Sandbox;
using System.Collections.Generic;

public static class SimpleTextChatSystem
{
	public record ChatEntry( string PlayerName, string Message, float Timestamp );

	public static List<ChatEntry> History { get; } = new();
	public static bool IsOpen { get; set; }

	[Rpc.Broadcast]
	public static void Send( string message )
	{
		if ( string.IsNullOrWhiteSpace( message ) ) return;

		var name = Rpc.Caller?.DisplayName ?? Connection.Local?.DisplayName ?? "Player";
		History.Add( new ChatEntry( name, message, Time.Now ) );
		Log.Info( $"{name}: {message}" );

		if ( History.Count > 100 )
			History.RemoveAt( 0 );
	}

	[ConCmd( "say" )]
	public static void Say( string message )
	{
		if ( !string.IsNullOrWhiteSpace( message ) )
			Send( message );
	}
}
