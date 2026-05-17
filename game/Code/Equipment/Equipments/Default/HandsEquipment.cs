namespace Dxura.RP.Game.Equipments;

/// <summary>
/// Interface defining hand interaction events that can be triggered on game objects.
/// </summary>
public interface IHandEvents : ISceneEvent<IHandEvents>
{
	void OnHandLmb( Player player ) {}
	void OnHandRmb( Player player ) {}
	void OnHandMmb( Player player ) {}
}

/// <summary>
/// Handles the functionality for picking up, holding, rotating, and throwing objects in the game.
/// </summary>
public class HandsEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property] private float ThrowForce { get; set; } = 500f;
	[Property] private float MaxReleaseVelocity { get; set; } = 500f;
	[Property] private float RotateSpeed { get; set; } = 1f;
	[Property] private float HoldDistance { get; set; } = 55f;

	private const float HandsGrabCooldown = 0.25f;

	private const float DeltaPickupTime = 0.20f; // Minimum time between pickup and release
	private const float SmoothDampTime = 0.075f; // Time factor for smooth movement

	private GameObject? _held;
	private float _heldDistance;
	private Rigidbody? _heldRigid;
	private IConstruct? _heldConstruct;
	private Rotation _heldRotation = Rotation.Identity;
	private float _lastPickupTime;
	private DeadBody? _draggedDeadBody;

	/// <summary>
	/// Checks if the player is currently holding an object.
	/// </summary>
	public bool IsHolding()
	{
		return _held != null;
	}

	/// <summary>
	/// Checks if the player is holding a specific object and optionally if rotating it.
	/// </summary>
	/// <param name="target">The GameObject to check if holding</param>
	/// <param name="checkRotating">If true, also checks if currently rotating the object</param>
	/// <returns>True if holding the target object (and rotating if specified)</returns>
	public bool IsHolding( GameObject target, bool checkRotating = false )
	{
		if ( !IsHolding() || !target.IsValid() || _held != target )
		{
			return false;
		}

		if ( checkRotating )
		{
			return Input.Down( "Use" );
		}

		return true;
	}

	protected override void OnDisabled()
	{
		if ( !Equipment.Owner.IsValid() || !Equipment.Owner.IsLocalPlayer || !IsHolding() )
		{
			return;
		}

		Release();
	}

	public new void OnEquipmentHolstered( Equipment equipment )
	{
		if ( !Equipment.Owner.IsValid() || !Equipment.Owner.IsLocalPlayer )
		{
			return;
		}

		Release();
	}

	protected override void OnInputFixedUpdate()
	{
		if ( !Equipment.Owner.IsValid() || !Equipment.Owner.IsLocalPlayer || !IsHolding() )
		{
			return;
		}

		if ( !IsHeldObjectValid() )
		{
			Release();
			return;
		}

		if ( Input.Down( "Use" ) )
		{
			RotateHeldObject();
		}
		else
		{
			Equipment.Owner.LockCamera = false;
		}

		UpdateHeldObjectPosition();
	}

	/// <summary>
	/// Checks if the currently held object is still valid.
	/// </summary>
	private bool IsHeldObjectValid()
	{
		if ( !_held.IsValid() )
		{
			return false;
		}

		// Allow a grace period for ownership transfer to complete after grabbing
		var timeSinceGrab = RealTime.Now - _lastPickupTime;
		var allowOwnershipGrace = timeSinceGrab < 1.0f; // 1 second grace period

		var heldValid = _held.IsValid() && !_held.IsDestroyed && _held.Enabled;
		var heldRigidValid = _heldRigid.IsValid() && _heldRigid.Enabled;
		var heldConstructValid = _heldConstruct != null && _heldConstruct.IsValid();
		var heldConstructNotFrozen = _heldConstruct is not { IsFrozen: true };

		var hasPermission =
			allowOwnershipGrace ||
			GameUtils.HasPermission( Equipment.Owner?.SteamId ?? 0, _held ) ||
			heldConstructValid &&
			heldConstructNotFrozen &&
			_heldConstruct!.NetworkOwner == Connection.Local.Id;

		return heldValid &&
		       heldRigidValid &&
		       hasPermission &&
		       heldConstructNotFrozen;
	}

	/// <summary>
	/// Updates the position and rotation of the held object.
	/// </summary>
	private void UpdateHeldObjectPosition()
	{
		// Calculate the offset from the object's position to its center
		var centerOffset = _heldRigid?.PhysicsBody.MassCenter - _heldRigid?.WorldPosition ?? Vector3.Zero;

		// Calculate the target position, adjusting for the center offset
		var holdPosition = Player!.Controller.EyePosition +
			Player.AimRay.Forward * _heldDistance - centerOffset;

		// Check if the object is too far away from the hold position
		var heldDistance = Vector3.DistanceBetween( _held!.WorldPosition, holdPosition );
		if ( heldDistance > Config.Current.Game.ReachDistance )
		{
			Release();
			return;
		}

		if ( !_heldRigid.IsValid() || !_heldRigid.Enabled )
		{
			Release();
			return;
		}

		// Apply smooth movement to the held object
		UpdateObjectVelocity( holdPosition );
		UpdateObjectRotation();
	}

	/// <summary>
	/// Updates the object's velocity to move toward the target position.
	/// </summary>
	private void UpdateObjectVelocity( Vector3 targetPosition )
	{
		if ( !_heldRigid.IsValid() || !_heldRigid.Enabled )
		{
			Release();
			return;
		}

		var velocity = _heldRigid.Velocity;
		Vector3.SmoothDamp( _heldRigid.WorldPosition, targetPosition, ref velocity, SmoothDampTime, Time.Delta );
		_heldRigid.Velocity = velocity;
	}

	/// <summary>
	/// Updates the object's rotation to match the target rotation.
	/// </summary>
	private void UpdateObjectRotation()
	{
		if ( !_heldRigid.IsValid() || !_heldRigid.Enabled )
		{
			Release();
			return;
		}

		var angularVelocity = _heldRigid.AngularVelocity;
		Rotation.SmoothDamp( _heldRigid.WorldRotation, _heldRotation, ref angularVelocity, SmoothDampTime, Time.Delta );
		_heldRigid.AngularVelocity = angularVelocity;
	}


	protected override void OnInputDown()
	{
		if ( !IsHolding() )
		{
			HandleInputDownWhileNotHolding();
		}
	}

	/// <summary>
	/// Handles input actions when the player is not holding an object.
	/// </summary>
	private void HandleInputDownWhileNotHolding()
	{
		if ( Input.Pressed( "attack1" ) && AttemptGrab() )
		{
			return;
		}

		var trace = GetTrace( Config.Current.Game.ReachDistance );

		if ( trace is not { Hit: true } || !trace.Value.GameObject.IsValid() )
		{
			return;
		}

		var targetGo = trace.Value.GameObject.Root;

		if ( targetGo.Components.Get<IHandEvents>() != null )
		{
			TriggerHandEvents( targetGo );
			return;
		}

		// Ragdoll dragging
		if ( _draggedDeadBody == null && targetGo.Tags.Has( Constants.RagdollTag ) )
		{
			var deadBody = targetGo.Root.GetComponent<DeadBody>();

			if ( !deadBody.IsValid() || !Input.Down( "attack1" ) )
			{
				return;
			}

			deadBody.StartDrag();
			_draggedDeadBody = deadBody;

			return;
		}

		// Otherwise do pocket actions
		if ( Config.Current.Game.PocketEnabled && Input.Down( "attack2" ) )
		{
			if ( Cooldown.Current.CheckAndStartCooldown( "pocket", Config.Current.Game.PocketCooldown, true ) )
			{
				return;
			}

			if ( targetGo.Tags.Has( Constants.PocketItemTag ) && !targetGo.Tags.Has( Constants.PocketTag ) )
			{
				PocketSystem.Instance.PickupHost();
			}
			else
			{
				PocketSystem.Instance.DropHost();
			}
		}
	}

	/// <summary>
	/// Handles the releasing of buttons to drop or throw objects.
	/// </summary>
	protected override void OnInputUp()
	{
		// Handle releasing a dragged dead body
		if ( _draggedDeadBody != null && Input.Released( "attack1" ) )
		{
			if ( _draggedDeadBody.IsValid() )
			{
				_draggedDeadBody.StopDrag();
			}

			_draggedDeadBody = null;
			return;
		}

		if ( !IsHolding() || IsTooSoonAfterPickup() )
		{
			return;
		}

		if ( Input.Released( "attack2" ) )
		{
			Release( ThrowForce );
		}
		else if ( Input.Released( "attack1" ) )
		{
			Release();
		}
	}

	/// <summary>
	/// Checks if it's too soon after pickup to release the object.
	/// </summary>
	private bool IsTooSoonAfterPickup()
	{
		return RealTime.Now - _lastPickupTime <= DeltaPickupTime;
	}

	/// <summary>
	/// Triggers hand events on the object the player is looking at.
	/// </summary>
	private void TriggerHandEvents( GameObject go )
	{
		if ( !Player.IsValid() || !go.IsValid() )
		{
			return;
		}

		if ( Input.Down( "attack1" ) )
		{
			IHandEvents.PostToGameObject( go.Root, x => x.OnHandLmb( Player ) );
		}
		else if ( Input.Down( "attack2" ) )
		{
			IHandEvents.PostToGameObject( go.Root, x => x.OnHandRmb( Player ) );
		}
		else if ( Input.Down( "attack3" ) )
		{
			IHandEvents.PostToGameObject( go.Root, x => x.OnHandMmb( Player ) );
		}
	}

	/// <summary>
	/// Attempts to grab an object in the player's line of sight.
	/// </summary>
	private bool AttemptGrab()
	{
		if ( !Player.IsValid() )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "hands:grab", HandsGrabCooldown, true ) )
		{
			return false;
		}

		var trace = Scene.Trace.Ray( Player.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( Player.GameObject )
			.WithoutTags( "invisible", "trigger", Constants.NoCollideTag )
			.UseHitboxes()
			.Run();

		if ( !trace.Hit || !trace.GameObject.IsValid() || trace.StartedSolid )
		{
			return false;
		}

		var rootObject = trace.GameObject.Root;
		if ( !rootObject.Tags.Has( Constants.HandsInteractTag ) )
		{
			return false;
		}

		Grab( rootObject );
		return true;
	}


	/// <summary>
	/// Grabs an object and prepares it for holding.
	/// </summary>
	private void Grab( GameObject target )
	{
		var construct = target.GetComponent<IConstruct>();

		if ( !GameManager.Instance.RequestOwnership( target ) )
		{
			// Try to request ownership via construct
			if ( construct != null && construct.IsValid() && !construct.RequestNetworkOwnership() )
			{
				Notify.Error( "#generic.permission" );
				return;
			}
		}

		// Don't allow grabbing frozen constructs
		if ( construct != null && construct.IsValid() && construct.IsFrozen )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "grab", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var rigidBody = target.GetOrAddComponent<Rigidbody>();
		if ( !rigidBody.IsValid() )
		{
			return;
		}

		rigidBody.RigidbodyFlags |= RigidbodyFlags.DisableCollisionSounds;

		// Calculate hold distance based on object size
		var bounds = target.GetLocalBounds();
		var boundsExtents = bounds.Extents;
		_heldDistance = HoldDistance + Math.Max( Math.Max( boundsExtents.x, boundsExtents.y ), boundsExtents.z );
		_heldRotation = target.WorldRotation;

		_held = target;
		_heldRigid = rigidBody;
		_heldConstruct = construct;

		_heldRigid.MotionEnabled = true;

		if ( Player.IsValid() )
		{
			Player.CantSwitch = false;
		}

		_lastPickupTime = RealTime.Now;
		if ( ShouldPlayPickupAnimation( _held ) )
		{
			Equipment.HoldType = AnimationHelper.HoldTypes.HoldItem;
		}


		OnGrabChangedHost( _held, false );
	}

	/// <summary>
	/// Releases the currently held object, optionally applying a throwing force.
	/// </summary>
	private void Release( float throwingForce = 0 )
	{
		if ( _heldRigid.IsValid() )
		{
			// Cap the velocity
			LimitReleaseVelocity();

			// Apply throwing force if specified
			if ( throwingForce > 0 )
			{
				ApplyThrowForce( throwingForce );
			}
		}

		Equipment.HoldType = AnimationHelper.HoldTypes.None;
		OnGrabChangedHost( _held, true );

		_held = null;
		_heldRigid = null;
		_heldConstruct = null;

		if ( Player.IsValid() )
		{
			Player.CantSwitch = false;
			Player.LockCamera = false;
		}
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void OnGrabChangedHost( GameObject? target, bool isRelease )
	{
		var caller = Rpc.Caller;
		var playPickupAnimation = ShouldPlayPickupAnimation( target );

		using ( Rpc.FilterExclude( x => x == caller ) )
		{
			OnGrabChangedAnimation( isRelease, playPickupAnimation );
		}

		if ( !target.IsValid() )
		{
			return;
		}

		if ( !GameUtils.HasPermission( caller, target ) )
		{
			// Fallback to construct unfrozen condition
			var construct = target.GetComponent<IConstruct>();
			if ( construct == null || !construct.IsValid() || construct.IsFrozen )
			{
				return;
			}
		}

		if ( !Config.Current.Game.PreventPropExploits || !GameManager.Instance.IsValid() )
		{
			return;
		}

		if ( isRelease )
		{
			GameManager.Instance.BroadcastTagHost( target, false, Constants.GrabbedTag );

			var guard = target.GetOrAddComponent<CollideGuard>();
			guard.ResetTimer();
		}
		else
		{
			GameManager.Instance.BroadcastTagHost( target, true, Constants.GrabbedTag );

			var player = GameUtils.GetPlayerByConnectionId( caller.Id );
			target.OnPlayerInteractHost( player );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnGrabChangedAnimation( bool isRelease, bool playPickupAnimation )
	{
		if ( !isRelease && !playPickupAnimation )
		{
			return;
		}

		Equipment.HoldType = isRelease ? AnimationHelper.HoldTypes.None : AnimationHelper.HoldTypes.HoldItem;
	}

	private static bool ShouldPlayPickupAnimation( GameObject? target )
	{
		return target.IsValid() && !target.Tags.Has( Constants.RestrictedEntity );
	}

	/// <summary>
	/// Limits the release velocity of the held object.
	/// </summary>
	private void LimitReleaseVelocity()
	{
		if ( !_heldRigid.IsValid() || !_heldRigid.Enabled )
		{
			return;
		}

		var currentVelocity = _heldRigid.Velocity;
		if ( currentVelocity.Length > MaxReleaseVelocity )
		{
			currentVelocity = currentVelocity.Normal * MaxReleaseVelocity;
			_heldRigid.Velocity = currentVelocity;
		}
	}

	/// <summary>
	/// Applies a throwing force to the held object.
	/// </summary>
	private void ApplyThrowForce( float force )
	{
		if ( !_heldRigid.IsValid() || !_heldRigid.Enabled )
		{
			return;
		}

		_heldRigid.ApplyImpulse( Scene.Camera!.Transform.World.Forward * _heldRigid.Mass * force );
	}

	/// <summary>
	/// Rotates the held object based on mouse input.
	/// </summary>
	private void RotateHeldObject()
	{
		var input = Input.MouseDelta * RotateSpeed;

		if ( !Player.IsValid() )
		{
			return;
		}

		Player.LockCamera = true;

		var eyeRot = Player.Controller.EyeAngles.ToRotation();

		// Create rotation around local X and Y axes
		var rotX = Rotation.FromAxis( eyeRot * Vector3.Right, input.y );
		var rotY = Rotation.FromAxis( eyeRot * Vector3.Up, input.x );

		// Combine rotations
		var newRot = rotY * rotX;

		// Apply to current held rotation
		_heldRotation = newRot * _heldRotation;
	}
}
