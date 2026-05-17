namespace Dxura.RP.Game;

public interface IBreachable
{
	bool CanBreach()
	{
		return true;
	}
	bool CanRepair()
	{
		return true;
	}
	bool CanAutoRepair()
	{
		return true;
	}

	float BreachTime => Config.Current.Game.PryDuration;
	float RepairTime => Config.Current.Game.RepairDuration;
	float AutoRepairTime => Config.Current.Game.BreachDuration;

	void BreachHost( Vector3 position );
	void RepairHost() {}

	bool IsValid();
}
