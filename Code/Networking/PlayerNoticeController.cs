using Sandbox;
using System;

/// <summary>
/// Shows a one-time, click-to-dismiss notice to players whose SteamId is listed.
/// Configured like <see cref="BanListController"/>: a list of SteamId + message entries.
/// Unlike a ban, the player is not kicked; they simply acknowledge the message with "OK".
/// </summary>
public sealed class PlayerNoticeController : Component
{
	[Property] public bool NoticesEnabled { get; set; } = true;
	[Property] public List<PlayerNoticeEntry> NoticePlayers { get; set; } = new();

	public static bool IsNoticeVisible { get; private set; }
	public static string NoticeMessage { get; private set; } = string.Empty;
	public static int NoticeVersion { get; private set; }

	private bool localNoticeProcessed;

	protected override void OnStart()
	{
		ResetSessionState();
		CheckLocalNotice();
	}

	protected override void OnUpdate()
	{
		if ( !NoticesEnabled )
			return;

		CheckLocalNotice();
	}

	private void CheckLocalNotice()
	{
		if ( localNoticeProcessed || !NoticesEnabled )
			return;

		if ( !TryGetNotice( Game.SteamId, out var notice ) )
			return;

		localNoticeProcessed = true;
		ShowNoticeLocal( NormalizeMessage( notice.Message ) );
	}

	private bool TryGetNotice( long steamId, out PlayerNoticeEntry notice )
	{
		notice = null;
		if ( steamId <= 0 || NoticePlayers == null )
			return false;

		notice = NoticePlayers.FirstOrDefault( entry => entry != null && entry.SteamId == steamId );
		return notice != null;
	}

	private static void ShowNoticeLocal( string message )
	{
		IsNoticeVisible = true;
		NoticeMessage = message;
		NoticeVersion++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}

	/// <summary>Called by the notice panel's "OK" button.</summary>
	public static void Dismiss()
	{
		if ( !IsNoticeVisible )
			return;

		IsNoticeVisible = false;
		NoticeVersion++;
		PlayerHud.SINGLETON?.StateHasChanged();
	}

	private void ResetSessionState()
	{
		localNoticeProcessed = false;
		IsNoticeVisible = false;
		NoticeVersion++;
	}

	private static string NormalizeMessage( string message )
	{
		return string.IsNullOrWhiteSpace( message ) ? "The host has left you a message." : message.Trim();
	}
}

public sealed class PlayerNoticeEntry
{
	[Property] public long SteamId { get; set; }
	[Property, TextArea] public string Message { get; set; } = "Please reach out to the host on Discord.";
}
