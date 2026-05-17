namespace Dxura.RP.Game.Statuses;

public class BandageStatus : BaseStatus
{
	public override string Id => Constants.BandageStatus;
	public override string Name => "";
	public override string MaterialIcon => "healing";
	public override float? DefaultDuration => 60;
	public override Color Color => Color.Red;

	public override void OnAddedServer( Player player )
	{
		player.HealthComponent.Health += 50;
		player.HealthComponent.Health = MathF.Min( player.HealthComponent.Health, player.HealthComponent.MaxHealth );
	}
}
