using Sandbox.Rendering;

namespace Dxura.RP.Game;

[Title( "2D Scope" )]
[Group( "Weapon Components" )]
public class ScopeWeaponComponent : InputWeaponComponent, IGameEvents
{
	[Property] public Material? ScopeOverlay { get; set; }
	[Property] public SoundEvent? ZoomSound { get; set; }
	[Property] public SoundEvent? UnzoomSound { get; set; }

	[Property] public bool UnzoomOnShot { get; set; } = true;

	private int ZoomLevel { get; set; }
	public bool IsZooming => ZoomLevel > 0;
	private float BlurLerp { get; set; } = 1.0f;
	[Property] private float AngleOffsetScale { get; } = 0.01f;
	[Property] public List<int> ZoomLevels { get; set; } = new();

	[Property] public bool AutoReScope { get; set; } = false;
	[Property] public bool HoldToScope { get; set; } = true;

	private Angles _anglesLerp;
	private Angles _lastAngles;
	private CommandList? _commandList;

	private bool WantsAim { get; set; } = false;

	protected void StartZoom( int level = 0 )
	{
		if ( _commandList is not null )
		{
			Scene.Camera.RemoveCommandList( _commandList );
			_commandList = null;
		}

		if ( !Equipment.IsValid() )
		{
			return;
		}

		if ( !Equipment.Owner.IsValid() )
		{
			return;
		}

		var camera = Equipment.Owner;

		if ( ScopeOverlay is not null )
		{
			_commandList = new CommandList( "Scope" );
			Scene.Camera.AddCommandList( _commandList, Stage.AfterTransparent );
		}

		ZoomSound?.Play( Equipment.GameObject.Transform.World.Position );

		ZoomLevel = level;
		Equipment.Tags.Add( "aiming" );

		if ( Equipment.ViewModel.IsValid() )
		{
			Equipment.ViewModel.RenderingEnabled = false;
		}
		Equipment.Owner.CanChangeView = false;

		Equipment.Owner.EnterFirstPerson( false );
	}

	protected void EndZoom()
	{
		if ( _commandList is not null )
		{
			Scene.Camera.RemoveCommandList( _commandList );
			_commandList = null;
		}

		if ( UnzoomSound is not null && Equipment.IsValid() )
		{
			UnzoomSound.Play( Equipment.GameObject.Transform.World.Position );
		}

		ZoomLevel = 0;

		if ( Equipment.Owner.IsValid() )
		{
			Equipment.Owner.CanChangeView = true;
			Equipment.Owner.UpdatePerspective();
		}


		if ( Equipment.IsValid() )
		{
			Equipment.Tags.Remove( "aiming" );

			if ( Equipment.ViewModel.IsValid() )
			{

				Equipment.ViewModel.RenderingEnabled = true;
			}
		}



		_anglesLerp = new Angles();
		BlurLerp = 1.0f;
	}

	protected override void OnInputDown()
	{
		if ( HoldToScope )
		{
			WantsAim = true;
			return;
		}
		WantsAim = !WantsAim;
	}

	protected override void OnInputUp()
	{
		if ( HoldToScope )
		{
			WantsAim = false;
		}
	}

	protected virtual bool CanAim()
	{
		return !(Tags.Has( "reloading" ) || Tags.Has( "bolting" ) && UnzoomOnShot || Equipment.Owner.IsValid() && Equipment.Owner.IsRunning);
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		EndZoom();
		WantsAim = false;
	}

	protected override void OnParentChanged( GameObject oldParent, GameObject newParent )
	{
		base.OnParentChanged( oldParent, newParent );
		EndZoom();
	}

	public float GetFov()
	{
		if ( ZoomLevel < 1 )
		{
			return 0f;
		}

		return ZoomLevels[Math.Clamp( ZoomLevel - 1, 0, ZoomLevels.Count )];
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( WantsAim && !IsZooming && CanAim() )
		{
			StartZoom( ZoomLevel + 1 );
		}

		if ( !IsZooming )
		{
			return;
		}

		var camera = Equipment?.Owner;
		if ( !camera.IsValid() )
		{
			return;
		}

		if ( ShouldEndZoom() )
		{
			EndZoom();
			if ( !AutoReScope && !HoldToScope )
			{
				WantsAim = false;
			}
		}

		if ( Equipment.IsValid() && Equipment.Owner.IsValid() )
		{
			Equipment.Owner.Controller.AimStrengthHead /= ZoomLevel * ZoomLevel + 1;
		}

		UpdateShader();
	}

	private void UpdateShader()
	{
		if ( !Equipment.Owner.IsValid() )
		{
			return;
		}

		var velocity = Equipment.Owner.Controller.Velocity.Length / 25.0f;
		var blur = 1.0f / (velocity + 1.0f);
		blur = blur.Clamp( 0.1f, 1.0f );

		if ( !Equipment.Owner.Controller.IsOnGround )
		{
			blur = 0.1f;
		}

		if ( blur > BlurLerp )
		{
			BlurLerp = BlurLerp.LerpTo( blur, Time.Delta * 1.0f );
		}
		else
		{
			BlurLerp = BlurLerp.LerpTo( blur, Time.Delta * 10.0f );
		}

		var angles = Equipment.Owner.Controller.EyeAngles;
		var delta = angles - _lastAngles;

		_anglesLerp = _anglesLerp.LerpTo( delta, Time.Delta * 10.0f );
		_lastAngles = angles;

		// Update the command list with new shader attributes
		if ( _commandList is not null && ScopeOverlay is not null )
		{
			_commandList.Reset();
			_commandList.Attributes.Set( "BlurAmount", BlurLerp );
			_commandList.Attributes.Set( "Offset", new Vector2( _anglesLerp.yaw, -_anglesLerp.pitch ) * AngleOffsetScale );
			_commandList.Blit( ScopeOverlay );
		}
	}

	private bool ShouldEndZoom()
	{
		if ( !Equipment.IsValid() || !Equipment.Owner.IsValid() )
		{
			return true;
		}

		// Unzoom if player unscopes, if reloading or running, or player is no longer holding the weapon

		return !WantsAim && IsZooming || !CanAim() || Equipment!.Owner.CurrentEquipment != Equipment;
	}

	void IGameEvents.OnWeaponShot()
	{
		if ( UnzoomOnShot )
		{
			EndZoom();
		}
	}
}
