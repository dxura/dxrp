namespace Dxura.RP.Game;

public class GlassRepairSystem : Component, IGameEvents
{
	[Property] private int RepairCycleSeconds { get; set; } = 600;

	[Property]
	public TimeSince LastRepairTime { get; set; } = 0;

	private readonly List<Glass> _worldWindows = new();

	protected override void OnStart()
	{
		if ( !Config.Current.Game.GlassRepairEnabled )
		{
			Destroy();
		}
	}

	public void OnMapFitted()
	{
		_worldWindows.Clear();
		_worldWindows.AddRange( Scene.GetAllComponents<Glass>() );
		Log.Info( $"[GlassRepairSystem] Registered {_worldWindows.Count} windows." );
	}

	public void OnSecondlyUpdate()
	{
		if ( GameManager.IsHeadless )
		{
			return;
		}

		// Do we need to repair?
		if ( LastRepairTime.Relative < RepairCycleSeconds )
		{
			return;
		}

		foreach ( var glass in _worldWindows )
		{
			glass.Repair();
		}

		LastRepairTime = 0;
		Log.Info( "[GlassRepairSystem] Repaired all windows" );
	}
}
