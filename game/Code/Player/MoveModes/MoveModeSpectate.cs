using Sandbox.Movement;

namespace Dxura.RP.Game;

/// <summary>
///     A move mode that locks the player in place while spectating another player.
///     The camera follows the spectate target via FreeLookCameraSystem.
/// </summary>
[Group( "Movement" )]
[Title( "MoveMode - Spectate" )]
[Description( "A move mode that allows spectating another player's perspective." )]
public class MoveModeSpectate : MoveMode
{
	public override bool AllowGrounding => false;
	public override bool AllowFalling => false;

	/// <summary>
	///     The player currently being spectated. Null means not spectating.
	/// </summary>
	public GameObject? SpectateTarget => _spectateTarget;
	public bool IsFreeLookToggled => _isFreeLookToggled;

	private GameObject? _spectateTarget;
	private Player? _spectateTargetPlayer;
	private bool _isFreeLookToggled;

	public override int Score( PlayerController controller )
	{
		if ( !IsProxy && SpectateTarget.IsValid() && Input.Pressed( "attack3" ) )
		{
			_isFreeLookToggled = !_isFreeLookToggled;
		}

		if ( !IsProxy && SpectateTarget.IsValid() && Input.Pressed( "Jump" ) )
		{
			StopSpectatingHost();
		}

		return SpectateTarget.IsValid() ? 3000 : 0;
	}

	public override void UpdateRigidBody( Rigidbody body )
	{
		if ( IsProxy )
		{
			return;
		}

		body.Gravity = false;
		body.LinearDamping = 0.0f;
		body.AngularDamping = 5.0f;

		if ( body.PhysicsBody.IsValid() )
		{
			body.PhysicsBody.BodyType = PhysicsBodyType.Keyframed;
		}
	}
	
	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		return Vector3.Zero;
	}

	public override void AddVelocity()
	{
		Controller.Body.Velocity = Vector3.Zero;
	}

	public override void OnModeBegin()
	{
		base.OnModeBegin();

		ClearLocalMovement();
		
		var player = GameObject.Root.GetComponent<Player>();
		if ( !player.IsValid() )
			return;

		player.Holster();
		player.CantSwitch = true;
	}

	public override void OnModeEnd( MoveMode next )
	{
		base.OnModeEnd( next );

		_isFreeLookToggled = false;
		SetSpectateTarget( null );

		if ( Controller.Body.PhysicsBody.IsValid())
		{
			Controller.Body.PhysicsBody.BodyType = PhysicsBodyType.Dynamic;
		}

		Controller.WishVelocity = Vector3.Zero;
		Controller.GroundVelocity = Vector3.Zero;
		Controller.Body.Velocity = Vector3.Zero;

		var player = GameObject.Root.GetComponent<Player>();
		if ( !player.IsValid() )
			return;

		player.CantSwitch = false;

		if ( player.IsLocalPlayer )
		{
			player.UpdatePerspective();
		}
	}

	public void StartSpectating( GameObject target )
	{
		_isFreeLookToggled = true;
		ClearLocalMovement();
		SetSpectateTarget( target );
		SetSpectateTargetOwner( target );

		var player = GameObject.Root.GetComponent<Player>();
		if ( player.IsValid() )
		{
			player.ListenerTarget = target;
		}
	}

	[Rpc.Host( NetFlags.OwnerOnly )]
	public void StopSpectatingHost()
	{
		var player = GameObject.Root.GetComponent<Player>();
		if ( player.IsValid() )
		{
			player.SendMessage( "Stopped spectating." );
			player.ListenerTarget = null;
		}

		SetSpectateTarget( null );
		SetSpectateTargetOwner( null );
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void SetSpectateTargetOwner( GameObject? target )
	{
		SetSpectateTarget( target );

		if ( target.IsValid() )
		{
			OcclusionSystem.Current?.RequestForceCheck();
		}
	}

	private void SetSpectateTarget( GameObject? target )
	{
		_spectateTarget = target;
		_spectateTargetPlayer = target?.Components.Get<Player>();

		if ( !target.IsValid() )
		{
			_isFreeLookToggled = false;
		}
	}

	private void ClearLocalMovement()
	{
		Controller.WishVelocity = Vector3.Zero;
		Controller.GroundVelocity = Vector3.Zero;
		Controller.Body.Velocity = Vector3.Zero;
	}

	public bool TryGetSpectateTargetPlayer( out Player targetPlayer )
	{
		targetPlayer = _spectateTargetPlayer!;
		return targetPlayer.IsValid();
	}

	protected override void OnUpdateAnimatorState( SkinnedModelRenderer renderer )
	{
		base.OnUpdateAnimatorState( renderer );
		renderer.Set( "b_grounded", true );
	}
}
