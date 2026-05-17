using Dxura.RP.Game.UI;
using System.Threading.Tasks;
using Sandbox.Rendering;
using Sandbox.Utility;

namespace Dxura.RP.Game.Equipments;

public class CameraEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property] [Group( "Effects" )] private SoundEvent? PhotoTakenSound { get; set; }

	private TimeSince _timeSinceShareButtonDown;
	private bool _isShareButtonDown;
	private const float ShareHoldDuration = 0.5f;

	private float _fov = Preferences.FieldOfView;
	private float _roll;

	// Camera limits
	private const float MinFov = 5f;
	private const float MaxFov = 120f;
	private const float MaxRoll = 45f;


	private DepthOfField? _dof;
	private Vector3 _focusPoint;
	private bool _hasZoomedBefore;

	public new void OnEquipmentDeployed( Equipment equipment )
	{
		base.OnEquipmentDeployed( equipment );

		if ( IsProxy )
		{
			return;
		}

		if ( equipment != Equipment )
		{
			return;
		}

		HUD.Visible = false;
		Notifications.Visible = false;

		// Force first person mode for camera usage
		if ( Player.IsValid() )
		{
			Player.EnterFirstPerson( false );
			Player.CanChangeView = false;
			Player.AutoAdjustFov = false;
		}

		_dof = Scene.Camera.Components.GetOrCreate<DepthOfField>();
		_dof.Flags |= ComponentFlags.NotNetworked;

		// Disable DOF by default - only enable after user zooms
		_dof.Enabled = false;
		_hasZoomedBefore = false;
	}

	public new void OnEquipmentHolstered( Equipment equipment )
	{
		if ( equipment != Equipment )
		{
			return;
		}

		CameraCleardown();
	}

	protected override void OnInputDown()
	{
		// Take photo immediately
		if ( Input.Down( "attack1" ) )
		{
			TakePhoto();
		}

		// Start holding share button
		if ( Input.Down( "attack3" ) )
		{
			_timeSinceShareButtonDown = 0;
			_isShareButtonDown = true;
		}
	}

	protected override void OnInputUp()
	{
		// Reset button state when released
		if ( Input.Released( "attack3" ) )
		{
			_isShareButtonDown = false;

			// If released before the hold duration and sharing hasn't triggered
			if ( _timeSinceShareButtonDown < ShareHoldDuration )
			{
				Notify.Error( "#equipment.camera.share.hold_longer" );
			}
		}
	}

	protected override void OnInputUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		// Reset camera settings
		if ( Input.Pressed( "reload" ) )
		{
			_fov = Preferences.FieldOfView;
			_roll = 0;

			if ( _dof?.IsValid() == true )
			{
				_dof.Enabled = false;
			}

			_hasZoomedBefore = false;
		}

		// Zoom controls (attack2 = right click)
		var isZooming = Input.Down( "attack2" );

		if ( isZooming )
		{
			// Disable player look controls when zooming
			if ( Player.IsValid() )
			{
				Player.LockCamera = true;
			}

			_fov += Input.AnalogLook.pitch;
			_fov = _fov.Clamp( MinFov, MaxFov );
			_roll -= Input.AnalogLook.yaw;
			_roll = _roll.Clamp( -MaxRoll, MaxRoll );

			// Enable DOF effects once user has zoomed for the first time
			if ( !_hasZoomedBefore )
			{
				_hasZoomedBefore = true;
				if ( _dof?.IsValid() == true )
				{
					_dof.Enabled = true;
				}
			}
		}
		else
		{
			// Re-enable player look controls when not zooming
			if ( Player.IsValid() )
			{
				Player.LockCamera = false;
			}
		}

		// Update camera FOV and rotation
		if ( Scene.Camera.IsValid() )
		{
			Scene.Camera.FieldOfView = _fov;
			Scene.Camera.WorldRotation *= new Angles( 0, 0, _roll );

			// Add camera shake when not zooming
			var trumble = 12.0f;
			var strumble = 1.0f;

			var x = Noise.Perlin( Time.Now * trumble, 3, 5 ).Remap( 0, 1, -1, 1 ) * strumble;
			var y = Noise.Perlin( Time.Now * trumble * 0.8f, 3, 4 ).Remap( 0, 1, -1, 1 ) * strumble;

			Scene.Camera.WorldRotation *= new Angles( x, y, 0 );
		}

		// Update depth of field only if user has zoomed before
		if ( _dof?.IsValid() == true && _hasZoomedBefore )
		{
			UpdateDepthOfField( _dof );
		}
	}

	protected override void OnInputFixedUpdate()
	{
		if ( !_isShareButtonDown || IsProxy )
		{
			return;
		}

		// Automatically trigger the share when hold duration is reached
		if ( _timeSinceShareButtonDown < ShareHoldDuration || !_isShareButtonDown )
		{
			return;
		}

		_isShareButtonDown = false; // Prevent triggering multiple times

		if ( !Cooldown.Current.CheckAndStartCooldown( "photo:share", Config.Current.Game.SharePhotoCooldown, true ) )
		{
			_ = GameTask.RunInThreadAsync( SharePhoto );
		}
	}

	private void TakePhoto()
	{
		Sandbox.Game.TakeScreenshot();
		PhotoTakenSound.Broadcast( WorldPosition );
	}

	private async Task SharePhoto()
	{
		PhotoTakenSound.Broadcast( WorldPosition );

		var texture = Texture.CreateRenderTarget().WithSize( 1920, 1080 ).Create();

		try
		{
			await GameTask.MainThread();
			var player = Player.Local;
			if ( !player.IsValid() )
			{
				return;
			}

			// Disable renderer for first person view to avoid rendering the player model for the screenshot HACK
			if ( player is { IsThirdPersonPreferred: false } )
			{
				player.GameObject.Tags.Add( "invisible" );
			}

			await Task.FixedUpdate();

			Scene.Camera.RenderToTexture( texture );

			player.GameObject.Tags.Remove( "invisible" );
			Notify.Info( "#equipment.camera.share.upload" );

			await GameTask.WorkerThread();

			var bitmap = texture.GetBitmap( 0 );
			var payload = bitmap.ToPng();

			// Send screenshot to server API
			var success = await PlayerApiClient.ShareScreenshot( payload );

			if ( success )
			{
				Notify.Success( "#equipment.camera.share.success" );
			}
			else
			{
				Notify.Error( "#equipment.camera.share.failure" );
			}
		}
		finally
		{
			texture.Dispose();
		}
	}

	private void UpdateDepthOfField( DepthOfField dof )
	{
		dof.BlurSize = Scene.Camera.FieldOfView.Remap( MinFov, MaxFov, 25, 5 );
		dof.FocusRange = 1024;
		dof.FrontBlur = false;

		var tr = Scene.Trace.Ray( Scene.Camera.Transform.World.ForwardRay, 5000 )
			.Radius( 8 )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Run();

		_focusPoint = tr.EndPosition;

		var target = Scene.Camera.WorldPosition.Distance( _focusPoint ) + 32;

		dof.FocalDistance = dof.FocalDistance.LerpTo( target, Time.Delta * 10.0f );
	}

	private void CameraCleardown()
	{
		if ( IsProxy )
		{
			return;
		}

		HUD.Visible = true;
		Notifications.Visible = true;

		// Re-enable player look controls and view switching when holstering
		if ( Player.IsValid() )
		{
			Player.LockCamera = false;
			Player.CanChangeView = true;
			Player.AutoAdjustFov = true;
		}

		_dof?.Destroy();
		_dof = null;
	}



	protected override void OnDisabled()
	{
		if ( IsProxy )
		{
			return;
		}

		CameraCleardown();
	}

	public void OnEquipmentDestroyed( Equipment equipment )
	{
		if ( equipment != Equipment )
		{
			return;
		}

		CameraCleardown();
	}


}
