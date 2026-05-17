using Dxura.RP.Shared;
using Sandbox.Movement;

namespace Dxura.RP.Game;

/// <summary>
///     A move mode that allows the player to move freely through the world without collisions.
/// </summary>
[Group( "Movement" )]
[Title( "MoveMode - Noclip" )]
[Description( "A move mode that allows the player to move freely through the world." )]
public class MoveModeNoClip : MoveMode
{
	/// <summary>
	///     Speed multiplier for faster movement while in noclip mode.
	/// </summary>
	[Property]
	public float SpeedMultiplier { get; set; } = 3.0f;

	/// <summary>
	///     Maximum speed while in noclip mode.
	/// </summary>
	[Property]
	public float MaxSpeed { get; set; } = 1000.0f;

	/// <summary>
	///     Normal speed in noclip mode
	/// </summary>
	[Property]
	public float NormalSpeed { get; set; } = 200.0f;

	/// <summary>
	///     Sprint speed in noclip mode
	/// </summary>
	[Property]
	public float SprintSpeed { get; set; } = 400.0f;

	/// <summary>
	///     Override the ground check to always return false.
	/// </summary>
	public override bool AllowGrounding => false;

	/// <summary>
	///     Override the falling check to always return false.
	/// </summary>
	public override bool AllowFalling => false;

	private Vector3.SmoothDamped _smoothedVelocity = new( 0.0f, 0.0f, 0.3f );

	/// <summary>
	///     Indicates if the player is currently noclipping.
	/// </summary>
	[Sync]
	public bool IsNoclipping { get; set; }

	/// <summary>
	///     Score function that determines if this move mode should be active.
	///     Return a high value when noclip is activated.
	/// </summary>
	public override int Score( PlayerController controller )
	{
		if ( IsProxy )
		{
			return IsNoclipping ? 100 : 0;
		}

		// Local player can toggle noclip with a keybind
		if ( CanNoclip() && Input.Pressed( "Noclip" ) )
		{
			IsNoclipping = !IsNoclipping;
		}

		return IsNoclipping ? 100 : 0;
	}


	/// <summary>
	///     Update the rigidbody when in noclip mode.
	/// </summary>
	public override void UpdateRigidBody( Rigidbody body )
	{
		if ( IsProxy )
		{
			return;
		}

		// Disable gravity and other physics-based behaviors
		body.Gravity = false;
		body.LinearDamping = 0.0f; // Remove damping as we'll handle it ourselves
		body.AngularDamping = 5.0f;

		// Disable collisions
		if ( body.PhysicsBody != null )
		{
			body.PhysicsBody.BodyType = PhysicsBodyType.Keyframed;
		}
	}

	private bool CanNoclip()
	{
		if ( Controller.Network.Owner == null )
		{
			return false;
		}

		return RankSystem.HasPermission( Controller.Network.Owner.SteamId, Permission.Noclip ) || Config.Current.Game.NoClip;
	}

	/// <summary>
	///     Handle movement input in noclip mode.
	/// </summary>
	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		// Don't use base.UpdateMove as it uses smoothing that we're handling differently
		// Instead, calculate the raw wish direction

		// Normalize input
		input = input.ClampLength( 1f );

		// Transform input to world space
		var wishDir = eyes * input;

		// Determine speed based on sprint state
		var targetSpeed = Input.Down( "Run" ) ? SprintSpeed : NormalSpeed;

		// Calculate horizontal movement
		var wishVelocity = wishDir * targetSpeed;

		// Add vertical movement based on jump and duck controls
		if ( Input.Down( "Jump" ) )
		{
			wishVelocity += Vector3.Up * targetSpeed;
		}

		if ( Input.Down( "Duck" ) )
		{
			wishVelocity += Vector3.Down * targetSpeed;
		}

		// Apply speed multiplier
		wishVelocity *= SpeedMultiplier;

		// Apply speed limit
		if ( wishVelocity.Length > MaxSpeed )
		{
			wishVelocity = wishVelocity.Normal * MaxSpeed;
		}

		return wishVelocity;
	}

	/// <summary>
	///     Override velocity application to apply smoothed velocity
	/// </summary>
	public override void AddVelocity()
	{
		// Smooth the transition between velocities
		_smoothedVelocity.Target = Controller.WishVelocity;

		// Adjust smoothing time based on whether accelerating or decelerating
		var currentSpeed = _smoothedVelocity.Current.Length;
		var targetSpeed = _smoothedVelocity.Target.Length;

		// Faster acceleration than deceleration
		_smoothedVelocity.SmoothTime = targetSpeed > currentSpeed ? 0.15f : 0.3f;

		// Update smoothing
		_smoothedVelocity.Update( Time.Delta );

		// Directly set velocity (no physics forces in noclip)
		Controller.Body.Velocity = _smoothedVelocity.Current;

		// If no input and nearly stopped, come to a complete stop
		if ( Controller.WishVelocity.IsNearZeroLength && _smoothedVelocity.Current.Length < 10f )
		{
			_smoothedVelocity.Current = Vector3.Zero;
			Controller.Body.Velocity = Vector3.Zero;
		}
	}

	/// <summary>
	///     Setup when entering noclip mode
	/// </summary>
	public override void OnModeBegin()
	{
		base.OnModeBegin();

		// Initialize smooth velocity with current velocity
		_smoothedVelocity.Current = Controller.Velocity;
		_smoothedVelocity.Target = Controller.Velocity;
	}

	/// <summary>
	///     Handle mode deactivation.
	/// </summary>
	public override void OnModeEnd( MoveMode next )
	{
		base.OnModeEnd( next );

		Controller.Renderer.Set( "b_noclip", false );
		Controller.Renderer.Set( "b_grounded", Controller.IsOnGround );

		// Ensure the rigidbody is restored to normal state
		if ( Controller.Body.PhysicsBody != null )
		{
			Controller.Body.PhysicsBody.BodyType = PhysicsBodyType.Dynamic;
		}
	}

	/// <summary>
	///     Update animation state to reflect noclip mode.
	/// </summary>
	protected override void OnUpdateAnimatorState( SkinnedModelRenderer renderer )
	{
		base.OnUpdateAnimatorState( renderer );

		// Set any noclip-specific animation parameters
		renderer.Set( "b_noclip", true );
		renderer.Set( "b_grounded", false ); // Never grounded in noclip
	}
}
