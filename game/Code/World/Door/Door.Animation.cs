namespace Dxura.RP.Game;

public partial class Door
{
	private const float MaxHandleRotation = 21f;

	// Animation Properties
	[Property] public Curve AnimationCurve { get; set; } = new( new Curve.Frame( 0f, 0f ), new Curve.Frame( 1f, 1.0f ) );

	[Property]
	public Curve HandleCurve { get; set; } = new(
		new Curve.Frame( 0f, 0f ),
		new Curve.Frame( 0.25f, 1.0f ),
		new Curve.Frame( 0.75f, 1.0f ),
		new Curve.Frame( 1f, 0f )
	);

	// Animation Timing
	[Property] public float OpenDuration { get; set; } = 0.5f;
	[Property] public float CloseDuration { get; set; } = 0.5f;
	[Property] public float HandleRotationDuration { get; set; } = 0.4f;
	[Property] public float AutoCloseDelay { get; set; } = 0f;

	// Door Movement Configuration
	[Property] [Range( 0.0f, 90.0f )] public float TargetAngle { get; set; } = 90.0f;
	[Property] public bool OpenAwayFromPlayer { get; set; } = true;
	[Property] [Group( "Roller" )] public float TargetHeight { get; set; } = 3.0f;

	[Property] public bool ReverseDirection { get; set; }

	// Handle Animation State
	private TimeSince HandleAnimationTime { get; set; }

	private bool IsAnimating { get; set; }
	private DoorState AnimationTargetState { get; set; }
	private TimeSince DoorAnimationTime { get; set; }
	private bool _isAnimatingHandles;


	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastState( DoorState newState, bool reverseDirection )
	{
		if ( State == newState )
		{
			return;
		}

		ReverseDirection = reverseDirection;
		State = newState;
		IsAnimating = true;
		AnimationTargetState = newState;
		DoorAnimationTime = 0;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void AnimateHandles()
	{
		HandleAnimationTime = 0;
		_isAnimatingHandles = true;
	}

	private void UpdateHandleAnimation()
	{
		if ( !_isAnimatingHandles || !HasHandles )
		{
			return;
		}

		var progress = Math.Clamp( HandleAnimationTime / HandleRotationDuration, 0f, 1f );
		var handleRotation = HandleCurve.Evaluate( progress ) * MaxHandleRotation;

		MainDoor.RotateHandles( handleRotation );
		if ( Type == DoorType.Double )
		{
			SecondDoor?.RotateHandles( handleRotation );
		}

		if ( progress >= 1.0f )
		{
			_isAnimatingHandles = false;

			MainDoor.ResetHandles();
			if ( Type == DoorType.Double )
			{
				SecondDoor?.ResetHandles();
			}
		}
	}

	private void UpdateDoorAnimation()
	{
		if ( !IsAnimating )
		{
			return;
		}

		var duration = AnimationTargetState == DoorState.Open ? OpenDuration : CloseDuration;
		if ( duration <= 0 )
		{
			duration = 0.1f;
		}

		var time = DoorAnimationTime.Relative.Remap( 0.0f, duration );
		var curve = AnimationCurve.Evaluate( time );

		if ( AnimationTargetState == DoorState.Closed )
		{
			curve = 1.0f - curve;
		}

		AnimateDoor( curve );

		if ( time >= 1f )
		{
			IsAnimating = false;

			// Play finished sounds
			if ( AnimationTargetState == DoorState.Open && OpenFinishedSound is not null )
			{
				OpenFinishedSound.Broadcast( WorldPosition );
			}
			else if ( AnimationTargetState == DoorState.Closed && CloseFinishedSound is not null )
			{
				CloseFinishedSound.Broadcast( WorldPosition );
			}
		}
	}

	private void AnimateDoor( float curve )
	{
		var angle = ReverseDirection ? -TargetAngle : TargetAngle;

		switch ( Type )
		{
			case DoorType.Single:
				MainDoor.AnimateRotation( curve * angle );
				break;

			case DoorType.Double:
				if ( SecondDoor == null )
				{
					return;
				}

				MainDoor.AnimateRotation( curve * angle );
				SecondDoor.AnimateRotation( curve * -angle );
				break;

			case DoorType.Roller:
				MainDoor.AnimateHeight( curve * TargetHeight );
				break;
		}
	}
}
