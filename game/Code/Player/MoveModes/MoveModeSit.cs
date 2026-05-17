using Sandbox.Movement;

namespace Dxura.RP.Game;

/// <summary>
///     A move mode that disables all player movement, allowing the player to remain still for animations like sitting.
/// </summary>
[Group( "Movement" )]
[Title( "MoveMode - Sit" )]
[Description( "Mode used for sitting." )]
public class MoveModeSit : MoveMode
{
	/// <summary>
	///     Override the ground check to always return true to prevent falling.
	/// </summary>
	public override bool AllowGrounding => true;

	/// <summary>
	///     Override the falling check to always return false to prevent falling.
	/// </summary>
	public override bool AllowFalling => false;

	private Vector3 _sitPosition = Vector3.Zero;

	public override void OnModeBegin()
	{
		_sitPosition = Controller.WorldPosition;
	}

	/// <summary>
	///     Score function that determines if this move mode should be active.
	///     Return a high value when a player is in a seat.
	/// </summary>
	public override int Score( PlayerController controller )
	{
		// The player object
		var player = controller.GameObject.Components.Get<Player>();
		if ( !player.IsValid() )
		{
			return -1000;
		}

		return player.Sitting ? 2000 : -1000; // Higher priority than noclip
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

		// Reset animation states
		Controller.Renderer.Set( "sit", 0 );
		Controller.Renderer.Set( "b_grounded", Controller.IsOnGround );
	}

	/// <summary>
	///     Update animation state to show sitting.
	/// </summary>
	protected override void OnUpdateAnimatorState( SkinnedModelRenderer renderer )
	{
		base.OnUpdateAnimatorState( renderer );

		// Set animation parameters for sitting
		renderer.Set( "sit", 1 );

		renderer.Set( "b_grounded", true ); // Always grounded when sitting
	}
}
