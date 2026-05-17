namespace Dxura.RP.Game;

/// <summary>
///     A weapon's viewmodel. Its responsibility is to listen to events from a weapon.
///     It should only exist on the client for the currently possessed pawn.
/// </summary>
public class ViewModel : Component, IEquipment, PlayerController.IEvents
{
	/// <summary>
	///     A reference to the <see cref="Equipment" /> we want to listen to.
	/// </summary>
	public required Equipment Equipment { get; set; }

	/// <summary>
	///     A reference to the viewmodel's arms.
	/// </summary>
	[Property]
	[Group( "Components" )]
	public SkinnedModelRenderer? Arms { get; set; }

	[Property]
	[Range( 0, 1 )]
	[Group( "Configuration" )]
	public float IronsightsFireScale { get; set; } = 0.2f;

	[Property] [Group( "Configuration" )] public bool UseMovementInertia { get; set; } = true;
	[Property]
	[Group( "Configuration" )] public bool CanADS { get; set; } = true;

	private float YawInertiaScale => 2f;
	private float PitchInertiaScale => 2f;

	private IEnumerable<IViewModelOffset>? Offsets =>
		Equipment.Components.GetAll<IViewModelOffset>( FindMode.EverythingInSelfAndDescendants );

	/// <summary>
	///     Should we play deploy effects?
	/// </summary>
	public bool PlayDeployEffects
	{
		set
		{
			if ( !ModelRenderer.IsValid() )
			{
				return;
			}

			ModelRenderer.Set( "b_deploy", value );
			ModelRenderer.Set( "b_deploy_skip", !value );
		}
	}

	private bool _activateInertia;

	private float _fieldOfViewOffset;
	private float _lastPitch;
	private float _lastYaw;

	private Vector3 _lerpedLocalPosition;
	private Rotation _lerpedlocalRotation;

	private Vector3 _lerpedWishMove;

	private Vector3 _localPosition;
	private Rotation _localRotation;
	private float _pitchInertia;
	private float _targetFieldOfView = 90f;
	private float _yawInertia;

	[Property] [Group( "GameObjects" )] public GameObject? Muzzle { get; set; }
	[Property] [Group( "GameObjects" )] public GameObject? EjectionPort { get; set; }

	[Property] [Group( "Components" )] public SkinnedModelRenderer? ModelRenderer { get; set; }

	[Property] public bool RenderingEnabled { get; set; } = true;

	public void OnJumped()
	{
		if ( ModelRenderer.IsValid() )
		{
			ModelRenderer?.Set( "b_jump", true );
		}
	}

	protected override void OnStart()
	{
		// Somehow this can happen?
		if ( !Equipment.IsValid() )
		{
			return;
		}

		if ( Equipment.Components.Get<ShootWeaponComponent>( FindMode.EverythingInSelfAndDescendants ) is {} shoot )
		{
			OnFireMode( shoot.CurrentFireMode );
		}
	}

	private void ApplyAnimationTransform()
	{
		if ( !ModelRenderer.IsValid() )
		{
			return;
		}

		if ( !ModelRenderer.Enabled )
		{
			return;
		}

		var bone = ModelRenderer.SceneModel.GetBoneLocalTransform( "camera" );
		var camera = Scene.Camera;

		if ( camera == null )
		{
			return;
		}

		camera.LocalPosition += bone.Position;
		camera.LocalRotation *= bone.Rotation;
	}

	private void ApplyOffsets()
	{
		if ( Offsets == null )
		{
			return;
		}

		foreach ( var offset in Offsets )
		{
			_localPosition += offset.PositionOffset;
			_localRotation *= offset.AngleOffset.ToRotation();
		}
	}

	private void ApplyInertia()
	{
		var camera = Scene.Camera;

		if ( camera == null )
		{
			return;
		}

		var inRot = camera.WorldRotation;

		// Need to fetch data from the camera for the first frame
		if ( !_activateInertia )
		{
			_lastPitch = inRot.Pitch();
			_lastYaw = inRot.Yaw();
			_yawInertia = 0;
			_pitchInertia = 0;
			_activateInertia = true;
		}

		var newPitch = camera.WorldRotation.Pitch();
		var newYaw = camera.WorldRotation.Yaw();

		_pitchInertia = Angles.NormalizeAngle( newPitch - _lastPitch );
		_yawInertia = Angles.NormalizeAngle( _lastYaw - newYaw );

		_lastPitch = newPitch;
		_lastYaw = newYaw;
	}

	private void ApplyVelocity()
	{
		var moveVel = Player.Local.Controller.Velocity;
		var moveLen = moveVel.Length;

		var wishMove = Player.Local.Controller.WishVelocity.Normal * 1f;
		if ( Equipment.IsValid() && Equipment.Tags.Has( "aiming" ) )
		{
			wishMove = 0;
		}

		if ( Player.Local.Controller.IsDucking )
		{
			moveLen *= 0.5f;
		}

		_lerpedWishMove = _lerpedWishMove.LerpTo( wishMove, Time.Delta * 7.0f );
		ModelRenderer!.Set( "move_bob", moveLen.Remap( 0, 300, 0, 1, true ) );

		if ( UseMovementInertia )
		{
			_yawInertia += _lerpedWishMove.y * 10f;
		}

		ModelRenderer.Set( "aim_yaw_inertia", _yawInertia * YawInertiaScale );
		ModelRenderer.Set( "aim_pitch_inertia", _pitchInertia * PitchInertiaScale );
	}

	private void ApplyAnimationParameters()
	{
		if ( !ModelRenderer.IsValid() || !ModelRenderer.Enabled || !Equipment.IsValid() )
		{
			return;
		}

		ModelRenderer.Set( "b_sprint", Player.Local.IsRunning );
		ModelRenderer.Set( "b_grounded", Player.Local.Controller.IsOnGround );

		if ( CanADS )
		{
			// Ironsights
			ModelRenderer.Set( "ironsights", Equipment.Tags.Has( "aiming" ) ? 1 : 0 );
			ModelRenderer.Set( "ironsights_fire_scale", Equipment.Tags.Has( "aiming" ) ? IronsightsFireScale : 0f );
		}


		// Handedness
		ModelRenderer.Set( "b_twohanded", true );

		// Weapon state
		var empty = !Equipment.Components.Get<AmmoComponent>( FindMode.EnabledInSelfAndDescendants )?.HasAmmo ?? false;
		ModelRenderer.Set( "b_empty", empty );
	}

	protected override void OnUpdate()
	{
		// Reset every frame
		_localRotation = Rotation.Identity;
		_localPosition = Vector3.Zero;

		if ( !Player.Local.IsValid() || !ModelRenderer.IsValid() || !Equipment.IsValid() ||
		     !ModelRenderer.Enabled )
		{
			return;
		}

		ApplyAnimationParameters();

		ApplyVelocity();
		ApplyAnimationTransform();
		ApplyInertia();
		ApplyOffsets();

		_targetFieldOfView = _targetFieldOfView.LerpTo( 80 + _fieldOfViewOffset, Time.Delta * 10f );
		_fieldOfViewOffset = 0;

		_lerpedLocalPosition = _lerpedLocalPosition.LerpTo( _localPosition, Time.Delta * 10f );
		_lerpedlocalRotation = Rotation.Lerp( _lerpedlocalRotation, _localRotation, Time.Delta * 10f );

		LocalPosition = _lerpedLocalPosition;
		LocalRotation = _lerpedlocalRotation;

		if ( Arms.IsValid() && ModelRenderer.IsValid() )
		{
			Arms.Tint = Arms.Tint.WithAlpha( RenderingEnabled ? 1f : 0f );
			ModelRenderer.Tint = ModelRenderer.Tint.WithAlpha( RenderingEnabled ? 1f : 0f );

		}

	}

	public void OnFireMode( FireMode currentFireMode )
	{
		var mode = currentFireMode switch
		{
			FireMode.Semi => 1,
			FireMode.Automatic => 3,
			FireMode.Burst => 2,
			_ => 0
		};

		if ( ModelRenderer.IsValid() )
		{
			ModelRenderer.Set( "firing_mode", mode );
		}
	}
}
