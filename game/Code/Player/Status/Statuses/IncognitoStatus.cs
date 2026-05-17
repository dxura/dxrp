namespace Dxura.RP.Game.Statuses;

public class IncognitoStatus : BaseStatus
{
	public override string Id => Constants.IncognitoStatus;
	public override string Name => "Incog";
	public override string? MaterialIcon => "do_not_disturb_on_total_silence";
	public override Color Color => Color.FromRgb( 0x607D8B );

	public override bool Visible => true;

	public override bool RemoveOnDeath => false;
}
