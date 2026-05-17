namespace Dxura.RP.Game.Statuses;

public class GodStatus : BaseStatus
{
	public override string Id => Constants.GodStatus;
	public override string Name => "God";
	public override string? MaterialIcon => "volunteer_activism";
	public override Color Color => Color.FromRgb( 0x9E9E9E );

	public override bool Visible => true;

	public override void OnAddedServer( Player player )
	{
		player.HealthComponent.IsGodMode = true;
	}

	public override void OnRemovedServer( Player player )
	{
		player.HealthComponent.IsGodMode = false;
	}

}
