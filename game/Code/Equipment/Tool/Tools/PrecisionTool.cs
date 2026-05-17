using Dxura.RP.Game.Utilities;
using System;
using System.Collections.Generic;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.precision.name", "#tool.precision.description", "#tool.group.construction" )]
public class PrecisionTool : BaseTool
{
	public override string Attack1Control => IsGhosting ? "#tool.precision.place" : "#tool.precision.grab";
	public override string Attack2Control => IsGhosting ? "#tool.precision.rotate" : "#tool.precision.push";
	public override string ReloadControl => IsGhosting ? "#tool.precision.cycle_axis" : "#tool.precision.pull";

	#region Settings

	[Property] [Title( "Offset" )] [Range( 0.01f, 200f )] [Step( 0.01f )]
	private float Offset { get; set; } = 25f;

	[Property] [Title( "Precise Mode" )]
	private bool PreciseMode { get; set; } = false;

	[Property] [Title( "Precise Offset" )] [Range( 0.01f, 1f )] [Step( 0.01f )]
	private float PreciseOffset { get; set; } = 0.1f;

	[Property] [Title( "Rotation Degrees" )] [Range( -360f, 360f )] [Step( 1f )]
	private float RotationDegrees { get; set; } = 90f;

	[Property] [Title( "Rotation Axis" )]
	private RotationAxis QuickRotationAxis { get; set; } = RotationAxis.Up;

	[Property] [Title( "Snap Position" )]
	private bool SnapPosition { get; set; } = false;

	[Property] [Title( "Position Snap Units" )] [Range( 0.1f, 50f )] [Step( 0.1f )]
	private float PositionSnapUnits { get; set; } = 5f;

	[Property] [Title( "Ghost Opacity" )] [Range( 0.1f, 1.0f )] [Step( 0.01f )]
	private float GhostOpacity { get; set; } = 0.5f;

	[Property] [Title( "Axis Lock" )]
	private AxisLockMode AxisLock { get; set; } = AxisLockMode.None;

	#endregion

	#region State & Constants

	private const float MinHold = 50f;
	private const float MaxHold = 500f;

	public enum RotationAxis { Up, Right, Forward }
	public enum AxisLockMode { None, XOnly, YOnly, ZOnly }

	private IConstruct? _targetConstruct;
	private GameObject? _targetObject;
	private Vector3 _grabOffset;
	private float _holdDistance = 120f;
	private List<(ModelRenderer renderer, Color originalTint)> _originalTints = new();
	private Vector3 _originalPosition;
	private Rotation _originalRotation;

	private bool IsGhosting => _targetConstruct != null && _targetConstruct.IsValid();

	#endregion

	#region Overrides

	public override void OnToolUpdate()
	{
		if ( _targetConstruct != null && !_targetConstruct.IsValid() )
		{
			Cancel();
			return;
		}

		if ( !IsGhosting )
		{
			return;
		}

		if ( Input.Pressed( "use" ) )
		{
			Cancel();
			return;
		}

		HandleMouseWheel();
		UpdateGhostPosition();
	}

	public override void PrimaryUseStart()
	{
		if ( IsGhosting )
		{
			Complete();
			return;
		}

		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var root = tr.GameObject.Root;
		if ( root.Tags.HasAny( Constants.GrabbedTag, Constants.PlayerTag, Constants.MapTag ) )
		{
			return;
		}
		if ( !TryGetConstruct( root, out var construct ) )
		{
			return;
		}

		var dist = Player.Local.WorldPosition.Distance( construct.GameObject.WorldPosition );
		if ( dist > MaxHold )
		{
			Notify.Warn( "#tool.precision.too_far" );
			return;
		}

		StartGhost( tr, construct, root );
	}

	public override void SecondaryUseStart()
	{
		if ( IsGhosting )
		{
			Rotate();
		}
		else
		{
			Nudge( true );
		}
	}

	public override void ReloadUseStart()
	{
		if ( IsGhosting )
		{
			CycleAxis();
		}
		else
		{
			Nudge( false );
		}
	}

	public override void OnUnequip()
	{
		Cancel();
		base.OnUnequip();
	}

	#endregion

	#region Ghost Mode

	private void StartGhost( SceneTraceResult tr, IConstruct construct, GameObject root )
	{
		_targetConstruct = construct;
		_targetObject = root;

		_grabOffset = root.WorldTransform.PointToLocal( tr.HitPosition );
		_holdDistance = Math.Clamp( Vector3.DistanceBetween( Player.Local.AimRay.Position, tr.HitPosition ), MinHold, MaxHold );

		_originalPosition = root.WorldPosition;
		_originalRotation = root.WorldRotation;

		BroadcastGrabbed( root, true );

		// Remove any rigidbodies 
		var rigidBody = root.GetComponent<Rigidbody>();
		if ( rigidBody.IsValid() )
		{
			rigidBody.Destroy();
		}

		ApplyGhostAppearance();

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		Notify.Info( "#tool.precision.ghost_mode" );
		Player.Local.CantSwitch = true;
	}

	private void UpdateGhostPosition()
	{
		if ( !_targetObject.IsValid() )
		{
			return;
		}

		var aim = Player.Local.AimRay;
		var targetPoint = aim.Position + aim.Forward * _holdDistance;

		var currentGrabPos = _targetObject.WorldTransform.PointToWorld( _grabOffset );
		var offsetFromCenter = currentGrabPos - _targetObject.WorldPosition;
		var pos = targetPoint - offsetFromCenter;

		pos = ApplyAxisLock( pos, _targetObject.WorldPosition );
		if ( SnapPosition )
		{
			pos = Snap( pos );
		}

		_targetObject.WorldPosition = pos;
	}

	private void Rotate()
	{
		if ( !IsGhosting || !_targetObject.IsValid() )
		{
			return;
		}

		var dir = Input.Down( "run" ) ? -1f : 1f;
		var deg = RotationDegrees * dir;

		var angles = _targetObject.WorldRotation.Angles();

		switch ( QuickRotationAxis )
		{
			case RotationAxis.Up: angles = angles.WithYaw( angles.yaw + deg ); break;
			case RotationAxis.Right: angles = angles.WithPitch( angles.pitch + deg ); break;
			case RotationAxis.Forward: angles = angles.WithRoll( angles.roll + deg ); break;
		}

		_targetObject.WorldRotation = angles.ToRotation();
		Tool.DoUseEffects( true, _targetObject.WorldPosition, GetRotationAxisVector() );

		var counterClockwise = dir > 0 ? "" : "counter-";
		Notify.Info( $"{Math.Abs( deg )} {counterClockwise} {QuickRotationAxis}" );
	}

	private void Complete()
	{
		if ( !IsGhosting || !_targetObject.IsValid() || !_targetConstruct.IsValid() )
		{
			Cancel();
			return;
		}

		RestoreAppearance();

		// Sync position before removing grabbed tag to prevent ownership race
		_targetConstruct.Freeze( _targetObject.WorldPosition, _targetObject.WorldRotation );

		BroadcastGrabbed( _targetObject, false );

		Tool.DoUseEffects( false, _targetObject.WorldPosition, Vector3.Up );
		Cleanup();
		Notify.Success( "#tool.precision.placement_complete" );
	}

	private void Cancel()
	{
		if ( _targetObject.IsValid() )
		{
			_targetObject.WorldPosition = _originalPosition;
			_targetObject.WorldRotation = _originalRotation;

			RestoreAppearance();

			// Sync original position back to server before removing grabbed tag
			if ( _targetConstruct != null && _targetConstruct.IsValid() )
			{
				_targetConstruct.Freeze( _originalPosition, _originalRotation );
			}

			BroadcastGrabbed( _targetObject, false );
			Notify.Info( "#tool.precision.placement_cancelled" );
		}

		Cleanup();
	}

	private void Cleanup()
	{
		Player.Local.CantSwitch = false;
		_targetConstruct = null;
		_targetObject = null;
	}

	#endregion

	#region Appearance & Nudge

	private void ApplyGhostAppearance()
	{
		if ( !_targetObject.IsValid() )
		{
			return;
		}
		_originalTints.Clear();

		foreach ( var renderer in _targetObject.GetComponentsInChildren<ModelRenderer>() )
		{
			if ( renderer.IsValid() )
			{
				_originalTints.Add( (renderer, renderer.Tint) );
				renderer.Tint = renderer.Tint.WithAlpha( GhostOpacity );
			}
		}

		var outline = _targetObject.AddComponent<HighlightOutline>();
		outline.Width = 0.5f;
		outline.Color = new Color( 0.043f, 0.682f, 0.859f );
	}

	private void RestoreAppearance()
	{
		if ( !_targetObject.IsValid() )
		{
			return;
		}

		foreach ( var (renderer, originalTint) in _originalTints )
		{
			if ( renderer.IsValid() )
			{
				renderer.Tint = originalTint;
			}
		}
		_originalTints.Clear();

		var outline = _targetObject.GetComponent<HighlightOutline>();
		outline?.Destroy();
	}

	private void Nudge( bool push )
	{
		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var tgt = tr.GameObject.Root;
		if ( !TryGetConstruct( tgt, out var construct ) )
		{
			return;
		}

		var dir = push ? -tr.Normal : tr.Normal;
		dir = ApplyAxisLockToDirection( dir );

		var amt = PreciseMode ? PreciseOffset : Offset;
		var newPos = tgt.WorldPosition + dir * amt;
		if ( SnapPosition )
		{
			newPos = Snap( newPos );
		}

		construct.Freeze( newPos, tgt.WorldRotation );
		BroadcastGrabbed( tgt, false );

		Tool.DoUseEffects( !push, tr.HitPosition, tr.Normal );
	}

	#endregion

	#region Helpers

	private void HandleMouseWheel()
	{
		if ( Input.MouseWheel.y == 0 )
		{
			return;
		}
		var step = PreciseMode ? PreciseOffset * 10f : 5f;
		if ( Input.Down( "Run" ) )
		{
			step = PreciseMode ? 0.5f : 2f;
		}

		_holdDistance = Math.Clamp( _holdDistance + Input.MouseWheel.y * step, MinHold, MaxHold );
	}

	private Vector3 ApplyAxisLock( Vector3 pos, Vector3 originalPos )
	{
		return AxisLock switch
		{
			AxisLockMode.XOnly => new Vector3( pos.x, originalPos.y, originalPos.z ),
			AxisLockMode.YOnly => new Vector3( originalPos.x, pos.y, originalPos.z ),
			AxisLockMode.ZOnly => new Vector3( originalPos.x, originalPos.y, pos.z ),
			_ => pos
		};
	}

	private Vector3 ApplyAxisLockToDirection( Vector3 dir )
	{
		return AxisLock switch
		{
			AxisLockMode.XOnly => Vector3.Right * dir.Dot( Vector3.Right ),
			AxisLockMode.YOnly => Vector3.Up * dir.Dot( Vector3.Up ),
			AxisLockMode.ZOnly => Vector3.Forward * dir.Dot( Vector3.Forward ),
			_ => dir
		};
	}

	private Vector3 Snap( Vector3 v )
	{
		return new Vector3(
			MathF.Round( v.x / PositionSnapUnits ) * PositionSnapUnits,
			MathF.Round( v.y / PositionSnapUnits ) * PositionSnapUnits,
			MathF.Round( v.z / PositionSnapUnits ) * PositionSnapUnits
		);
	}

	private Vector3 GetRotationAxisVector()
	{
		return QuickRotationAxis switch
		{
			RotationAxis.Up => Vector3.Up,
			RotationAxis.Right => Vector3.Right,
			_ => Vector3.Forward
		};
	}

	private void CycleAxis()
	{
		QuickRotationAxis = (RotationAxis)(((int)QuickRotationAxis + 1) % 3);
		Notify.Info( $"{QuickRotationAxis}" );
	}

	private bool TryGetConstruct( GameObject root, out IConstruct construct )
	{
		construct = root.GetComponent<IConstruct>();
		if ( construct == null || !GameManager.Instance.RequestOwnership( root ) )
		{
			Notify.Error( "#generic.forbidden" );
			return false;
		}
		return true;
	}

	#endregion
}
