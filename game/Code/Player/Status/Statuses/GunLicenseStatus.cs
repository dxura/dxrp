namespace Dxura.RP.Game.Statuses;

public class GunLicenseStatus : BaseStatus
{
	public override string Id => Constants.GunLicenseStatus;
	public override string Name => "Gun";
	public override string LongName => "Gun License";
	public override string MaterialIcon => "badge";

	public override bool ShowOnPlayerList => true;

	public override Color Color => Color.FromRgb( 0x5C6BC0 );

	public override bool RemoveOnArrest => true;
	public override bool RemoveOnDeath => true;
}
