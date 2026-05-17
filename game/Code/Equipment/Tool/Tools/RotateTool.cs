using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.rotate.name", "#tool.rotate.description", "#tool.group.construction" )]
public class RotateTool : BaseTool
{
	[Property]
	[Title( "Rotation Degrees" )]
	[Range( 1f, 180f )]
	[Step( 1f )]
	private float RotationDegrees { get; set; } = 45f;

	private RotationAxis CurrentAxis { get; set; } = RotationAxis.Up;

	public override string Attack1Control => "#tool.rotate.attack1";
	public override string Attack2Control => "#tool.rotate.attack2";
	public override string ReloadControl => "#tool.rotate.reload";

	public enum RotationAxis { Up, Right, Forward }

	public override void PrimaryUseStart()
	{
		RotateTarget( 1f );
	}

	public override void SecondaryUseStart()
	{
		RotateTarget( -1f );
	}

	public override void ReloadUseStart()
	{
		CycleAxis();
	}

	private void RotateTarget( float direction )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:rotate:use",
			Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.Body.IsValid() )
		{
			return;
		}

		var root = tr.GameObject.Root;

		if ( root.Tags.HasAny( Constants.GrabbedTag, Constants.PlayerTag, Constants.MapTag ) || !root.Tags.Has( Constants.ConstructTag ) )
		{
			Notify.Warn( "#generic.forbidden" );
			return;
		}

		if ( !GameManager.Instance.RequestOwnership( root ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var construct = root.GetComponent<IConstruct>();
		if ( !construct.IsValid() )
		{
			Notify.Warn( "#generic.forbidden" );
			return;
		}

		var axis = GetRotationAxis();
		var degrees = RotationDegrees * direction;
		var rotation = Rotation.FromAxis( axis, degrees );

		construct.Freeze( construct.GameObject.WorldPosition, construct.GameObject.WorldRotation * rotation );

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );

		var dir = direction > 0 ? "clockwise" : "counter-clockwise";
		Notify.Info( $"{Math.Abs( degrees )}° {dir} ({CurrentAxis})" );
	}

	private void CycleAxis()
	{
		// Cycle through enum values: Up → Right → Forward → Up
		CurrentAxis = (RotationAxis)(((int)CurrentAxis + 1) % 3);
		Notify.Info( $"Rotation Axis: {CurrentAxis}" );
	}

	private Vector3 GetRotationAxis()
	{
		return CurrentAxis switch
		{
			RotationAxis.Up => Vector3.Up,
			RotationAxis.Right => Vector3.Right,
			_ => Vector3.Forward
		};
	}
}
