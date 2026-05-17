using Sandbox.Movement;

namespace Dxura.RP.Game;

/// <summary>
///     A move mode that handles kneeling animations and movement restrictions.
///     Supports both basic kneeling and revive kneeling with detailed IK and pose control.
/// </summary>
[Group( "Movement" )]
[Title( "MoveMode - Kneel" )]
[Description( "Mode used for kneeling and kneeling revive actions." )]
public class MoveModeKneel : MoveMode
{
	/// <summary>
	///     Static reference to the active kneel move mode for compression control
	/// </summary>
	public static MoveModeKneel? ActiveInstance { get; private set; }

	/// <summary>
	///     Override the ground check to always return true to prevent falling.
	/// </summary>
	public override bool AllowGrounding => true;

	/// <summary>
	///     Override the falling check to always return false to prevent falling.
	/// </summary>
	public override bool AllowFalling => false;

	private GameObject? _leftHandIkTarget;
	private GameObject? _rightHandIkTarget;

	[Sync( SyncFlags.FromHost )]
	private bool IsCompressing { get; set; }

	public override void OnModeBegin()
	{
		// Set as active instance when mode begins
		ActiveInstance = this;
	}

	/// <summary>
	///     Score function that determines if this move mode should be active.
	///     Return a high value when a player is kneeling or kneeling for revive.
	/// </summary>
	public override int Score( PlayerController controller )
	{
		// The player object
		var player = controller.GameObject.Components.Get<Player>();
		if ( !player.IsValid() )
		{
			return -1000;
		}

		return player.Kneeling || player.KneelingRevive ? 2100 : -1000;
	}

	/// <summary>
	///     Update the rigidbody to disable movement.
	/// </summary>
	public override void UpdateRigidBody( Rigidbody body )
	{
		if ( IsProxy )
		{
			return;
		}

		// Freeze motion without breaking trigger interactions.
		if ( body.PhysicsBody != null )
		{
			body.PhysicsBody.BodyType = PhysicsBodyType.Keyframed;
		}
	}

	/// <summary>
	///     No movement is allowed in this mode.
	/// </summary>
	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		// Return zero to disable all movement
		return Vector3.Zero;
	}

	/// <summary>
	///     Override velocity application to stop all movement.
	/// </summary>
	public override void AddVelocity()
	{
		// Set velocity to zero
		Controller.Body.Velocity = Vector3.Zero;
	}

	/// <summary>
	///     Handle mode deactivation.
	/// </summary>
	public override void OnModeEnd( MoveMode next )
	{
		base.OnModeEnd( next );

		Controller.WishVelocity = Vector3.Zero;
		Controller.GroundVelocity = Vector3.Zero;
		Controller.Body.Velocity = Vector3.Zero;

		if ( Controller.Body.PhysicsBody.IsValid() )
		{
			Controller.Body.PhysicsBody.BodyType = PhysicsBodyType.Dynamic;
		}

		// Clear static reference when mode ends
		if ( ActiveInstance == this )
		{
			ActiveInstance = null;
		}

		// Clear all animation parameters using AnimationHelper
		var player = Controller.GameObject.Components.Get<Player>();
		var animationHelper = player?.AnimationHelper;
		if ( animationHelper != null )
		{
			// Reset animation states
			animationHelper.SitType = null;
			animationHelper.SitPose = 0;
			animationHelper.IsGrounded = Controller.IsOnGround;

			// Clear all kneeling-specific animation parameters
			animationHelper.HoldType = AnimationHelper.HoldTypes.None;
			animationHelper.HoldTypePose = 0;
			animationHelper.HoldTypePoseHand = 0;
			animationHelper.MoveGroundSpeed = 100;

			// Clear aiming parameters
			animationHelper.AimBody = Vector3.Zero;
			animationHelper.AimEyes = Vector3.Zero;
			animationHelper.AimHead = Vector3.Zero;

			// Clear IK parameters and destroy IK target GameObjects
			if ( _leftHandIkTarget.IsValid() )
			{
				_leftHandIkTarget.Destroy();
				_leftHandIkTarget = null;
			}
			if ( _rightHandIkTarget.IsValid() )
			{
				_rightHandIkTarget.Destroy();
				_rightHandIkTarget = null;
			}
			animationHelper.IkLeftHand = null;
			animationHelper.IkRightHand = null;

			// Reset compression state
			IsCompressing = false;
		}
	}

	/// <summary>
	///     Update animation state to show kneeling or kneeling revive.
	/// </summary>
	protected override void OnUpdateAnimatorState( SkinnedModelRenderer renderer )
	{
		base.OnUpdateAnimatorState( renderer );

		var player = Controller.GameObject.Components.Get<Player>();
		if ( !player.IsValid() )
		{
			return;
		}

		var animationHelper = player.AnimationHelper;
		if ( animationHelper == null )
		{
			return;
		}

		if ( player.KneelingRevive )
		{
			SetKneelingReviveAnimation( animationHelper );

			animationHelper.MoveGroundSpeed = GetCompression();
		}
		else
		{
			SetKneelingAnimation( animationHelper );
		}

		renderer.Set( "b_grounded", true );
	}

	/// <summary>
	///     Set kneeling animation parameters.
	/// </summary>
	private void SetKneelingAnimation( AnimationHelper animationHelper )
	{
		animationHelper.SitType = SitType.Kneel;
		animationHelper.SitPose = 4;
	}

	/// <summary>
	///     Set complete kneeling revive animation with all pose and IK parameters.
	/// </summary>
	private void SetKneelingReviveAnimation( AnimationHelper animationHelper )
	{
		// Base pose
		animationHelper.SitType = SitType.KneelRevive;
		animationHelper.SitPose = 4;

		// Body/head aiming control
		animationHelper.AimBody = new Vector3( 0, 0, -1 );
		animationHelper.AimEyes = new Vector3( 0, 0, 0 );
		animationHelper.AimHead = new Vector3( 1, 0, 0 );

		// Hold type and poses for revive actions
		animationHelper.HoldType = AnimationHelper.HoldTypes.HoldItem;
		animationHelper.HoldTypePose = 5;
		animationHelper.HoldTypePoseHand = 0;

		// Create and assign IK target GameObjects dynamically (only once)
		if ( _leftHandIkTarget == null || _rightHandIkTarget == null )
		{
			CreateAndAssignIkTargets( animationHelper );
		}
	}

	/// <summary>
	///     Get compression for the revive animation.
	///     Returns 0 during compression (offset animation) and 100 for normal offset.
	/// </summary>
	private int GetCompression()
	{
		return IsCompressing ? 0 : 100;
	}

	/// <summary>
	///     Start compression (compressed offset).
	/// </summary>
	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	public void StartCompression()
	{
		IsCompressing = true;
	}

	/// <summary>
	///     End compression (normal offset).
	/// </summary>
	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	public void EndCompression()
	{
		IsCompressing = false;
	}

	/// <summary>
	///     Create and assign IK target GameObjects to AnimationHelper for revive hand positioning.
	/// </summary>
	private void CreateAndAssignIkTargets( AnimationHelper animationHelper )
	{
		// Get the player's body GameObject
		var player = Controller.GameObject.Components.Get<Player>();
		var bodyGameObject = player?.BodyRoot ?? Controller.GameObject;

		// Create left hand IK target
		var leftHandTarget = new GameObject
		{
			Name = "IK_LeftHand_CPR", Parent = bodyGameObject, LocalPosition = new Vector3( 15, 0, 0 ), LocalRotation = Rotation.From( 0, -25, -180 )
		};

		// Create right hand IK target  
		var rightHandTarget = new GameObject
		{
			Name = "IK_RightHand_CPR", Parent = bodyGameObject, LocalPosition = new Vector3( 15, 0, 0 ), LocalRotation = Rotation.From( 0, 25, 0 )
		};

		// Store references and assign to AnimationHelper
		_leftHandIkTarget = leftHandTarget;
		_rightHandIkTarget = rightHandTarget;
		animationHelper.IkLeftHand = leftHandTarget;
		animationHelper.IkRightHand = rightHandTarget;
	}
}
