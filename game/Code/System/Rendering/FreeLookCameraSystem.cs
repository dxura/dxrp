namespace Dxura.RP.Game;

/// <summary>
///     Overrides the camera position/rotation for free-look after the PlayerController
///     has finished its camera setup.
/// </summary>
public class FreeLookCameraSystem : GameObjectSystem<FreeLookCameraSystem>
{
	private struct CameraFocus
	{
		public Vector3 EyePosition { get; init; }
		public Rotation BaseRotation { get; init; }
		public Vector3 Offset { get; init; }
		public float FallbackDistance { get; init; }
		public GameObject IgnoreA { get; init; }
		public GameObject? IgnoreB { get; init; }
		public Player? HiddenPlayer { get; init; }
		public bool UseDirectEyePosition { get; init; }
		public bool UseTrackedFreeLook { get; init; }
	}

	private bool _isTrackedFreeLooking;
	private Angles _trackedFreeLookAngles;
	private Angles _trackedFreeLookBodyAngles;
	private bool _wasTrackedFreeLookDown;

	private Player? _hiddenTarget;
	private bool _addedInvisibleTagToHiddenTarget;

	public FreeLookCameraSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.FinishUpdate, 100, ApplyFreeLookCamera, "FreeLookCamera" );
	}

	private void ApplyFreeLookCamera()
	{
		if ( !TryGetLocalCameraContext( out var player, out var camera ) )
		{
			ResetTrackedState();
			return;
		}

		if ( !TryGetCameraFocus( player, out var focus ) )
		{
			ResetTrackedState();
			return;
		}

		var rotation = focus.UseTrackedFreeLook
			? ResolveTrackedFreeLookRotation( player, focus.BaseRotation.Angles() )
			: player.FreeLookAngles.ToRotation();

		ApplyCameraFocus( camera, focus, rotation );
	}

	private bool TryGetLocalCameraContext( out Player player, out CameraComponent camera )
	{
		player = Player.Local;
		camera = Scene.Camera;
		return player.IsValid() && player.Controller.IsValid() && camera.IsValid();
	}

	private bool TryGetCameraFocus( Player player, out CameraFocus focus )
	{
		if ( TryGetSpectateTargetPlayer( player, out var targetPlayer ) )
		{
			var hasController = targetPlayer.Controller.IsValid() && targetPlayer.Controller.Enabled;
			var eyePosition = hasController
				? targetPlayer.Controller.EyePosition
				: targetPlayer.WorldPosition + Vector3.Up * 64f;
			var baseRotation = hasController
				? targetPlayer.Controller.EyeAngles.ToRotation()
				: targetPlayer.WorldRotation;
			var isThirdPerson = player.IsThirdPersonPreferred;

			focus = new CameraFocus
			{
				EyePosition = eyePosition,
				BaseRotation = baseRotation,
				Offset = isThirdPerson ? targetPlayer.ThirdPersonCameraOffset : Vector3.Zero,
				FallbackDistance = isThirdPerson ? targetPlayer.ThirdPersonCameraOffset.Length * 1.5f : 0f,
				IgnoreA = player.GameObject.Root,
				IgnoreB = targetPlayer.GameObject.Root,
				HiddenPlayer = !isThirdPerson ? targetPlayer : null,
				UseDirectEyePosition = !isThirdPerson,
				UseTrackedFreeLook = true
			};
			return true;
		}

		if ( !player.IsFreeLooking )
		{
			focus = default;
			return false;
		}

		focus = new CameraFocus
		{
			EyePosition = player.Controller.EyePosition,
			BaseRotation = player.FreeLookAngles.ToRotation(),
			Offset = player.Controller.CameraOffset,
			FallbackDistance = player.ThirdPersonCameraOffset.Length,
			IgnoreA = player.GameObject.Root,
			UseDirectEyePosition = player.Controller.CameraOffset.Length < 1f
		};
		return true;
	}

	private void ApplyCameraFocus( CameraComponent camera, CameraFocus focus, Rotation rotation )
	{
		UpdateHiddenTargetVisibility( focus.HiddenPlayer, focus.UseDirectEyePosition );

		if ( focus.UseDirectEyePosition )
		{
			camera.WorldPosition = focus.EyePosition;
			camera.WorldRotation = rotation;
			return;
		}

		var cameraDistance = focus.Offset.Length;
		if ( cameraDistance < 1f )
		{
			cameraDistance = focus.FallbackDistance;
		}

		if ( cameraDistance < 1f )
		{
			cameraDistance = 125f;
		}

		var desiredPos = focus.EyePosition - rotation.Forward * cameraDistance + rotation.Up * focus.Offset.z;
		var trace = Scene.Trace.Ray( focus.EyePosition, desiredPos )
			.Radius( 8f )
			.IgnoreGameObjectHierarchy( focus.IgnoreA );

		if ( focus.IgnoreB != null && focus.IgnoreB.IsValid() )
		{
			trace = trace.IgnoreGameObjectHierarchy( focus.IgnoreB );
		}

		var result = trace
			.WithoutTags( "player", "trigger" )
			.Run();

		camera.WorldPosition = result.Hit ? result.EndPosition : desiredPos;
		camera.WorldRotation = rotation;
	}

	private Rotation ResolveTrackedFreeLookRotation( Player player, Angles baseAngles )
	{
		var freeLookDown = Input.Down( "FreeLook" ) || IsSpectateFreelookEnabled( player );

		if ( freeLookDown && !_wasTrackedFreeLookDown )
		{
			_isTrackedFreeLooking = true;
			_trackedFreeLookBodyAngles = player.Controller.EyeAngles;
			_trackedFreeLookAngles = baseAngles;
		}

		if ( _isTrackedFreeLooking && freeLookDown )
		{
			_trackedFreeLookAngles += new Angles( Input.AnalogLook.pitch, Input.AnalogLook.yaw, 0f );
			_trackedFreeLookAngles = _trackedFreeLookAngles.WithPitch( _trackedFreeLookAngles.pitch.Clamp( -89f, 89f ) );

			player.Controller.EyeAngles = _trackedFreeLookBodyAngles;
		}

		if ( _isTrackedFreeLooking && !freeLookDown && _wasTrackedFreeLookDown )
		{
			_isTrackedFreeLooking = false;
		}

		_wasTrackedFreeLookDown = freeLookDown;
		return _isTrackedFreeLooking ? _trackedFreeLookAngles.ToRotation() : baseAngles.ToRotation();
	}

	private void ResetTrackedState()
	{
		_isTrackedFreeLooking = false;
		_wasTrackedFreeLookDown = false;
		UpdateHiddenTargetVisibility( null, false );
	}

	public static bool IsFreelookEnabled => Current?.GetIsFreelookEnabled() ?? false;

	private bool GetIsFreelookEnabled()
	{
		return TryGetLocalCameraContext( out var player, out _ ) && IsSpectateFreelookEnabled( player );
	}

	private static bool IsSpectateFreelookEnabled( Player player )
	{
		var spectateMode = player.Controller.Components.Get<MoveModeSpectate>();
		return spectateMode.IsValid() && player.Controller.Mode == spectateMode && spectateMode.IsFreeLookToggled;
	}
	
	private static bool TryGetSpectateTargetPlayer( Player player, out Player targetPlayer )
	{
		targetPlayer = null!;

		var spectateMode = player.Controller.Components.Get<MoveModeSpectate>();
		if ( !spectateMode.IsValid() || player.Controller.Mode != spectateMode || !spectateMode.SpectateTarget.IsValid() )
		{
			return false;
		}

		targetPlayer = spectateMode.SpectateTarget.Components.Get<Player>();
		return targetPlayer.IsValid();
	}

	private void UpdateHiddenTargetVisibility( Player? targetPlayer, bool shouldHideTarget )
	{
		if ( _hiddenTarget.IsValid() && (_hiddenTarget != targetPlayer || !shouldHideTarget) )
		{
			if ( _addedInvisibleTagToHiddenTarget )
			{
				_hiddenTarget.GameObject.Tags.Remove( Constants.InvisibleTag );
			}

			_hiddenTarget = null;
			_addedInvisibleTagToHiddenTarget = false;
		}

		if ( !shouldHideTarget || !targetPlayer.IsValid() || _hiddenTarget == targetPlayer )
		{
			return;
		}

		_hiddenTarget = targetPlayer;
		_addedInvisibleTagToHiddenTarget = !targetPlayer.GameObject.Tags.Has( Constants.InvisibleTag );

		if ( _addedInvisibleTagToHiddenTarget )
		{
			targetPlayer.GameObject.Tags.Add( Constants.InvisibleTag );
		}
	}
}
