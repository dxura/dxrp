namespace Dxura.RP.Game;

public partial class Player
{
	/// <summary>
	///     Should camera be locked
	/// </summary>
	[Property]
	[Feature( "Camera" )]
	public bool LockCamera { get; set; }

	[Property]
	[Feature( "Camera" )]
	[Group( "Third Person" )]
	public Vector3 ThirdPersonCameraOffset { get; set; }

	[Property]
	[Feature( "Camera" )]
	[Group( "Third Person" )]
	public Vector3 ThirdPersonAimCameraOffset { get; set; }

	[Property]
	[Feature( "Camera" )]
	[Group( "Third Person" )]
	public bool IsThirdPersonPreferred { get; set; }

	[Property]
	[Feature( "Camera" )]
	[Group( "Third Person" )]
	public float ThirdPersonAimFovOffset { get; set; } = -30f;

	[Property] [Feature( "Camera" )] public bool HideHud { get; set; }

	[Property]
	[Feature( "Camera" )]
	[Group( "Config" )]
	public float RespawnProtectionSaturation { get; set; } = 0.35f;

	[Property]
	[Feature( "Camera" )]
	[Group( "Config" )]
	public float AimFovOffset { get; set; } = -5f;

	[ConVar( "dx_render_distance", ConVarFlags.Saved, Min = 500, Max = 10000 )]
	[Change( nameof( OnRenderDistanceChanged ) )]
	public static float RenderDistance { get; set; } = 10000f;

	[ConVar( "dx_ambient_occlusion", ConVarFlags.Saved )]
	[Change( nameof( OnGraphicsChanged ) )]
	public static bool DxAmbientOcclusion { get; set; } = false;

	[ConVar( "dx_screen_space_reflections", ConVarFlags.Saved )]
	[Change( nameof( OnGraphicsChanged ) )]
	public static bool DxScreenSpaceReflections { get; set; } = false;

	// Camera Effects
	private ColorAdjustments? ColorAdjustments { get; set; }
	private ScreenShaker? ScreenShaker { get; set; }
	private ChromaticAberration? ChromaticAberration { get; set; }
	private Pixelate? Pixelate { get; set; }
	
	// Enhancements
	private ScreenSpaceReflections? ScreenSpaceReflections { get; set; }
	private AmbientOcclusion? AmbientOcclusion { get; set; }

	/// <summary>
	/// Can the player change perspective (First/Third)
	/// </summary>
	public bool CanChangeView { get; set; } = true;

	/// <summary>
	/// Should FOV be changed for current state (Damage/Zoom/Scope)
	/// </summary>
	public bool AutoAdjustFov { get; set; } = true;

	public float HueRotateTarget { get; set; } = 1f;
	
	/// <summary>
	///     Constructs a ray using the controller's eye position and angles.
	/// </summary>
	[Sync]
	public Ray AimRay { get; private set; }

	private float _defaultSaturation = 1f;

	private bool _fetchedInitial;

	private float _fieldOfViewOffset;

	// Free-look state
	public bool IsFreeLooking => _isFreeLooking;
	public Angles FreeLookAngles => _freeLookAngles;

	private bool _isFreeLooking;
	private Angles _freeLookAngles;
	private Angles _freeLookBodyAngles;
	private Angles _freeLookLastEyeAngles;

	private const float FirstPersonFreeLookYawLimit = 95f;
	private const float FirstPersonFreeLookPitchLimit = 38f;

	private void SetupCamera()
	{
		if ( !Controller.IsValid() )
		{
			return;
		}

		EnterFirstPerson();

		if ( Controller.ThirdPerson )
		{
			ClearViewModel();
		}
		else
		{
			CreateViewModel( false );
		}

		// Effects
		Pixelate = Scene.Camera.GameObject.Components.GetOrCreate<Pixelate>();
		ChromaticAberration = Scene.Camera.GameObject.Components.GetOrCreate<ChromaticAberration>();
		ScreenShaker = Scene.Camera.GameObject.Components.GetOrCreate<ScreenShaker>();
		ColorAdjustments = Scene.Camera.GameObject.Components.Get<ColorAdjustments>();

		// Graphics
		AmbientOcclusion = Scene.Camera.GameObject.Components.GetOrCreate<AmbientOcclusion>();
		ScreenSpaceReflections = Scene.Camera.GameObject.Components.GetOrCreate<ScreenSpaceReflections>();
		
		ApplyGraphics();
	}

	private void OnUpdateCamera()
	{
		UpdateFreeLook();

		var spectateMode = Controller.Components.Get<MoveModeSpectate>();
		var isSpectatingOther =
			spectateMode.IsValid()
			&& Controller.Mode == spectateMode
			&& spectateMode.SpectateTarget.IsValid();

		Controller.UseLookControls = !LockCamera && !isSpectatingOther;

		if ( HealthComponent.State == LifeState.Alive && Input.Pressed( "View" ) && CanChangeView )
		{
			IsThirdPersonPreferred = !IsThirdPersonPreferred;
			UpdatePerspective();
		}

		UpdateView();
	}

	private void UpdateFreeLook()
	{
		if ( HealthComponent.State == LifeState.Dead )
		{
			_isFreeLooking = false;
			return;
		}

		if ( Input.Pressed( "FreeLook" ) )
		{
			_isFreeLooking = true;
			_freeLookBodyAngles = Controller.EyeAngles;
			_freeLookAngles = Controller.EyeAngles;
			_freeLookLastEyeAngles = Controller.EyeAngles;
		}

		if ( _isFreeLooking && Input.Down( "FreeLook" ) )
		{
			// Capture the look delta the controller applied this frame
			var currentEyeAngles = Controller.EyeAngles;
			var delta = currentEyeAngles - _freeLookLastEyeAngles;
			_freeLookAngles += delta;

			if ( !Controller.ThirdPerson )
			{
				var yawDelta = Angles.NormalizeAngle( _freeLookAngles.yaw - _freeLookBodyAngles.yaw )
					.Clamp( -FirstPersonFreeLookYawLimit, FirstPersonFreeLookYawLimit );
				var pitchDelta = (_freeLookAngles.pitch - _freeLookBodyAngles.pitch)
					.Clamp( -FirstPersonFreeLookPitchLimit, FirstPersonFreeLookPitchLimit );

				_freeLookAngles = _freeLookAngles
					.WithYaw( _freeLookBodyAngles.yaw + yawDelta )
					.WithPitch( _freeLookBodyAngles.pitch + pitchDelta );
			}

			_freeLookAngles = _freeLookAngles.WithPitch( _freeLookAngles.pitch.Clamp( -89f, 89f ) );

			// Restore body direction so the player model doesn't rotate
			Controller.EyeAngles = _freeLookBodyAngles;
			_freeLookLastEyeAngles = _freeLookBodyAngles;
		}

		if ( _isFreeLooking && Input.Released( "FreeLook" ) )
		{
			_isFreeLooking = false;
		}
	}

	private void OnUpdateAimRay()
	{
		AimRay = new Ray( Controller.EyePosition, Controller.EyeAngles.ToRotation().Forward );
	}

	public void AddFieldOfViewOffset( float degrees )
	{
		_fieldOfViewOffset -= degrees;
	}

	private void ApplyScope()
	{
		if ( !CurrentEquipment.IsValid() )
		{
			return;
		}

		if ( CurrentEquipment?.Components.Get<ScopeWeaponComponent>( FindMode.EnabledInSelfAndDescendants ) is
			{} scope )
		{
			var fov = scope.GetFov();
			_fieldOfViewOffset -= fov;
		}
	}

	private void UpdateView()
	{
		var baseFov = Preferences.FieldOfView.AlmostEqual( 90f, 0.5f ) ? 110f : Preferences.FieldOfView;
		baseFov = Math.Clamp( baseFov, Config.Current.Game.MinFov, Config.Current.Game.MaxFov );

		_fieldOfViewOffset = 0;

		if ( !IsValid )
		{
			return;
		}

		var isAiming = false;
		if ( CurrentEquipment.IsValid() )
		{
			if ( CurrentEquipment?.Tags.Has( "aiming" ) ?? false )
			{
				_fieldOfViewOffset += IsThirdPersonPreferred ? ThirdPersonAimFovOffset : AimFovOffset;
				Controller.CameraOffset = Controller.CameraOffset.LerpTo( ThirdPersonAimCameraOffset, 5f * Time.Delta );
				isAiming = true;
			}
		}

		if ( IsThirdPersonPreferred && !isAiming )
		{
			// Transition to third person position
			if ( Controller.CameraOffset != ThirdPersonCameraOffset )
			{
				Controller.CameraOffset = ThirdPersonCameraOffset;
			}
		}

		if ( ColorAdjustments.IsValid() )
		{
			if ( !_fetchedInitial )
			{
				_defaultSaturation = ColorAdjustments.Saturation;
				_fetchedInitial = true;
			}

			ColorAdjustments.Saturation = HealthComponent.IsGodMode
				? RespawnProtectionSaturation
				: float.Lerp( ColorAdjustments.Saturation, _defaultSaturation, 1f );

			ColorAdjustments.HueRotate = float.Lerp( ColorAdjustments.HueRotate, HueRotateTarget, 0.5f * Time.Delta );
		}


		ApplyRecoil();
		ApplyScope();

		var timeSinceDamage = _timeSinceDamageTaken.Relative;
		var shortDamageUi = timeSinceDamage.LerpInverse( 0.1f, 0.0f );

		if ( AutoAdjustFov && Scene.Camera.IsValid() && ChromaticAberration.IsValid() && Pixelate.IsValid() )
		{
			ChromaticAberration.Scale = shortDamageUi * 1f;
			Pixelate.Scale = shortDamageUi * 0.2f;
			ScreenShaker?.Apply( Scene.Camera );

			var desiredFov = Scene.Camera.FieldOfView.LerpTo( baseFov + _fieldOfViewOffset, Time.Delta * 5f );

			Scene.Camera.FieldOfView = desiredFov;
		}
	}

	public void UpdatePerspective()
	{
		if ( !IsThirdPersonPreferred && Controller.ThirdPerson )
		{
			EnterFirstPerson();
		}
		else if ( IsThirdPersonPreferred && !Controller.ThirdPerson )
		{
			EnterThirdPerson();
		}
	}

	public void EnterFirstPerson( bool useViewmodel = true )
	{
		// Nothing to do if we're already first-person.
		if (!Controller.ThirdPerson) return;
		
		if ( DxDisableRendererFp )
		{
			ClearClothing();
			Renderer.Enabled = false;
		}

		Controller.ThirdPerson = false;
		Controller.CameraOffset = Vector3.Zero; // Ensure exact zero position
		
		if ( useViewmodel )
		{
			CreateViewModel( false );
		}
	}

	public void EnterThirdPerson()
	{
		if ( !Renderer.Enabled )
		{
			Renderer.Enabled = true;
			ApplyClothing();
		}

		Controller.ThirdPerson = true;
		CurrentEquipment?.UpdateRenderMode();
		ClearViewModel();
	}

	private void ApplyRecoil()
	{
		if ( !CurrentEquipment.IsValid() )
		{
			return;
		}

		if ( CurrentEquipment?.Components.Get<RecoilWeaponComponent>( FindMode.EnabledInSelfAndDescendants ) is
			{} fn )
		{
			Controller.EyeAngles += fn.Current;
		}
	}

	private static void OnRenderDistanceChanged( float oldValue, float newValue )
	{
		if ( Sandbox.Game.ActiveScene.IsValid() && Sandbox.Game.ActiveScene.Camera is not null )
		{
			Sandbox.Game.ActiveScene.Camera.ZFar = newValue;
		}
	}

	private void ApplyGraphics()
	{
		if ( !Scene.Camera.IsValid() )
		{
			return;
		}

		if ( AmbientOcclusion.IsValid() )
		{
			AmbientOcclusion.Enabled = DxAmbientOcclusion;
		}

		if ( ScreenSpaceReflections.IsValid() )
		{
			ScreenSpaceReflections.Enabled = DxScreenSpaceReflections;
		}
		
		Scene.Camera.ZFar = RenderDistance;
	}

	private static void OnGraphicsChanged( bool oldValue, bool newValue )
	{
		var localPlayer = Local;
		if ( !localPlayer.IsValid() )
		{
			return;
		}

		localPlayer.ApplyGraphics();
	}

	[ConVar( "dx_disable_renderer_fp" )]
	private static bool DxDisableRendererFp { get; set; } = false;
}
