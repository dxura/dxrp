namespace Dxura.RP.Game.Statuses;

public class PrisonerStatus : BaseStatus
{
	public override string Id => Constants.PrisonerStatus;
	public override string Name => "#generic.prisoner";
	public override string MaterialIcon => "gavel";
	public override float? DefaultDuration => Config.Current.Game.JailTime;
	public override bool ShowOnNameplate => true;
	public override bool ShowOnPlayerList => true;
	public override bool Visible => false;
	public override Color Color => Color.FromRgb( 16753920 );
}
