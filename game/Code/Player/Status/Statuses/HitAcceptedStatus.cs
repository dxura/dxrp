namespace Dxura.RP.Game.Statuses;

public class HitAcceptedStatus : BaseStatus
{
	public override string Id => Constants.HitAcceptedStatus;
	public override string Name => "Hit Accepted";
	public override string MaterialIcon => "card_travel";
	public override float? DefaultDuration => Config.Current.Game.HitmanActiveHitDuration;
	public override Color Color => Color.FromRgb( 0xFF0000 );
	public override bool ShowOnNameplate => true;
	public override bool ShowOnPlayerList => true;
	public override bool Visible => false;
	public override bool RemoveOnDeath => true;
	public override bool RemoveOnArrest => true;
	public override bool RemoveOnJobChange => true;
}
