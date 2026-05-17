namespace Dxura.RP.Game.Statuses;

public class WantedStatus : BaseStatus
{
	public override string Id => Constants.WantedStatus;
	public override string Name => "#generic.wanted";
	public override string MaterialIcon => "crisis_alert";
	public override float? DefaultDuration => Config.Current.Game.WantedTime;
	public override bool ShowOnNameplate => true;
	public override bool ShowOnPlayerList => true;
	public override Color Color => Color.FromRgb( 16723248 );
}
