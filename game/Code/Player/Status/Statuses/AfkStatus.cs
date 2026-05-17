namespace Dxura.RP.Game.Statuses;

public class AfkStatus : BaseStatus
{
	public override string Id => Constants.AfkStatus;
	public override string Name => "AFK";
	public override string? MaterialIcon => "pause";
	public override Color Color => Color.FromRgb( 0xFFC107 );
}
