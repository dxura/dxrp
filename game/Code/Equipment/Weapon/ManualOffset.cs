namespace Dxura.RP.Game;

public interface IViewModelOffset
{
	public Vector3 PositionOffset { get; }
	public Angles AngleOffset { get; }
}

[Title( "ADS (w/ Aim Offset)" )]
[Group( "Weapon Components" )]
public class ManualOffset : AimWeaponComponent, IViewModelOffset
{
	[Property] public Vector3 AimOffset { get; set; }
	[Property] public Angles AimAngleOffset { get; set; }

	Vector3 IViewModelOffset.PositionOffset => IsAiming ? AimOffset : Vector3.Zero;
	Angles IViewModelOffset.AngleOffset => IsAiming ? AimAngleOffset : Angles.Zero;
}
