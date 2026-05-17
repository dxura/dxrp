namespace Dxura.RP.Game.Statuses;

public class WarrantStatus : BaseStatus
{
	public override string Id => Constants.WarrantStatus;
	public override string Name => "Warrant";
	public override string MaterialIcon => "gavel";
	public override float? DefaultDuration => Config.Current.Game.WarrantTime;
	public override Color Color => Color.FromRgb( 0xb9946e );
	public override bool ShowOnPlayerList => true;
}
