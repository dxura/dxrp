namespace Dxura.RP.Game.Statuses;

public class GaggedStatus : BaseStatus
{
	public override string Id => Constants.GaggedStatus;
	public override string Name => "#generic.gagged";
	public override string MaterialIcon => "voice_over_off";
	public override bool ShowOnNameplate => true;
	public override bool ShowOnPlayerList => true;
	public override Color Color => Color.FromRgb( 0x00B8D9 );
}
