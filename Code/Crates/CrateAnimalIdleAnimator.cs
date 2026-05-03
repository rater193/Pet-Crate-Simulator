using Sandbox;

using System;

public sealed class CrateAnimalIdleAnimator : Component
{
	[Property] public bool AnimateOnProxies { get; set; } = false;
	[Property] public float MinPauseTime { get; set; } = 1.2f;
	[Property] public float MaxPauseTime { get; set; } = 3.4f;
	[Property] public float MinBurstDuration { get; set; } = 0.55f;
	[Property] public float MaxBurstDuration { get; set; } = 1.1f;
	[Property] public float HopHeight { get; set; } = 18f;
	[Property] public float HopSpeed { get; set; } = 18f;
	[Property] public float RockAngle { get; set; } = 9f;
	[Property] public float TwistAngle { get; set; } = 5f;
	[Property] public float SettleSpeed { get; set; } = 14f;

	private Vector3 restPosition;
	private Rotation restRotation;
	private float pauseTimeRemaining;
	private float burstTimeRemaining;
	private float burstDuration;
	private float animationTime;
	private float burstPhase;

	protected override void OnStart()
	{
		restPosition = GameObject.WorldPosition;
		restRotation = GameObject.WorldRotation;
		SchedulePause();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy && !AnimateOnProxies )
			return;

		if ( burstTimeRemaining > 0f )
		{
			UpdateBurst();
			return;
		}

		UpdatePause();
	}

	protected override void OnDisabled()
	{
		RestoreRestTransform();
	}

	protected override void OnDestroy()
	{
		RestoreRestTransform();
	}

	private void UpdatePause()
	{
		pauseTimeRemaining -= Time.Delta;
		SettleToRest();

		if ( pauseTimeRemaining > 0f )
			return;

		StartBurst();
	}

	private void UpdateBurst()
	{
		animationTime += Time.Delta;
		burstTimeRemaining -= Time.Delta;

		var progress = 1f - (burstTimeRemaining / MathF.Max( 0.01f, burstDuration )).Clamp( 0f, 1f );
		var intensity = MathF.Sin( progress * MathF.PI );
		var hop = MathF.Abs( MathF.Sin( animationTime * HopSpeed + burstPhase ) ) * HopHeight * intensity;
		var rock = MathF.Sin( animationTime * HopSpeed * 0.55f + burstPhase ) * RockAngle * intensity;
		var twist = MathF.Sin( animationTime * HopSpeed * 0.37f + burstPhase * 1.7f ) * TwistAngle * intensity;

		GameObject.WorldPosition = restPosition + (Vector3.Up * hop);
		GameObject.WorldRotation = restRotation * Rotation.FromPitch( rock ) * Rotation.FromYaw( twist );

		if ( burstTimeRemaining > 0f )
			return;

		SchedulePause();
	}

	private void StartBurst()
	{
		burstDuration = RandomRange( MinBurstDuration, MaxBurstDuration );
		burstTimeRemaining = burstDuration;
		burstPhase = RandomRange( 0f, MathF.PI * 2f );
	}

	private void SchedulePause()
	{
		burstTimeRemaining = 0f;
		burstDuration = 0f;
		pauseTimeRemaining = RandomRange( MinPauseTime, MaxPauseTime );
	}

	private void SettleToRest()
	{
		var settleAmount = 1f - MathF.Exp( -SettleSpeed * Time.Delta );
		GameObject.WorldPosition = Vector3.Lerp( GameObject.WorldPosition, restPosition, settleAmount.Clamp( 0f, 1f ) );
		GameObject.WorldRotation = GameObject.WorldRotation.SlerpTo( restRotation, settleAmount.Clamp( 0f, 1f ) );
	}

	private void RestoreRestTransform()
	{
		if ( !GameObject.IsValid() )
			return;

		GameObject.WorldPosition = restPosition;
		GameObject.WorldRotation = restRotation;
	}

	private static float RandomRange( float min, float max )
	{
		if ( max < min )
		{
			(min, max) = (max, min);
		}

		return min + ((float)Game.Random.NextDouble() * (max - min));
	}
}
