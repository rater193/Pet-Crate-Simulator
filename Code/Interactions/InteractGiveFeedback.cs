using Sandbox;

/// <summary>
/// World-space button that opens the feedback menu. The submitted feedback is POSTed to the
/// configured Discord webhook by <see cref="FeedbackPanel"/>.
/// </summary>
public sealed class InteractGiveFeedback : Interactable
{
	/// <summary>Discord webhook URL that feedback is sent to.</summary>
	[Property, TextArea] public string WebhookUrl { get; set; }

	protected override void OnStart()
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			text = "Give Feedback";
	}

	public override void OnInteract( PlayerController interactingPlayer )
	{
		if ( interactingPlayer == null || interactingPlayer.IsProxy )
			return;

		PlayerHud.OpenFeedback( WebhookUrl );
	}
}
