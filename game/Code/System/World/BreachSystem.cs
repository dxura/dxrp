namespace Dxura.RP.Game;

public class BreachSystem : SingletonComponent<BreachSystem>, IGameEvents
{
	private readonly Dictionary<IBreachable, TimeSince> _breached = new();

	private TimeSince _timeSinceLastAutoRepairChecks = 0;


	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( _timeSinceLastAutoRepairChecks < 1f )
		{
			return;
		}

		_timeSinceLastAutoRepairChecks = 0;

		foreach ( var (breachable, timeSinceBreach) in _breached.ToArray() )
		{
			if ( !breachable.IsValid() || !breachable.CanAutoRepair() )
			{
				_breached.Remove( breachable );
				continue;
			}

			if ( timeSinceBreach < breachable.AutoRepairTime )
			{
				continue;
			}

			breachable.RepairHost();

			_breached.Remove( breachable );
		}
	}

	public void Breach( IBreachable breachable, Vector3 position )
	{
		if ( !breachable.CanBreach() || IsBreached( breachable ) )
		{
			return;
		}

		breachable.BreachHost( position );
		_breached[breachable] = 0;

		GameManager.Instance.BreachSound.Broadcast( position );

	}

	public void Repair( IBreachable breachable )
	{
		if ( !breachable.CanRepair() || !IsBreached( breachable ) )
		{
			return;
		}

		breachable.RepairHost();

		_breached.Remove( breachable );
	}

	[Rpc.Host]
	public void ChanceBreachHost( IBreachable breachable, Vector3 position )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:breach:chance", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );

		// Permission check to prevent abuse from non-government jobs
		if ( !caller.IsValid() || !caller.Job.IsGovernmentRole() )
		{
			return;
		}

		// Distance check to prevent remote breach attempts
		if ( caller.WorldPosition.Distance( position ) > Config.Current.Game.ReachDistance * 2 )
		{
			return;
		}

		// Warrant check: door owner must have an active warrant
		if ( breachable is IOwned owned )
		{
			var owner = GameUtils.GetPlayerById( owned.Owner );
			if ( !owner.IsValid() || !owner.HasStatus( Constants.WarrantStatus ) )
			{
				return;
			}
		}

		if ( !breachable.CanBreach() || IsBreached( breachable ) )
		{
			return;
		}

		var didBreach = Random.Shared.Next( 3 ) == 0;

		if ( !didBreach )
		{
			return;
		}

		Breach( breachable, position );
	}

	public static bool IsBreached( IBreachable breachable )
	{
		return Instance.IsValid() && Instance._breached.ContainsKey( breachable );
	}

	public static int GetRemainingBreachTime( IBreachable breachable )
	{
		if ( !IsBreached( breachable ) || !Instance.IsValid() )
		{
			return 0;
		}

		var timeSinceBreach = Instance._breached[breachable];
		return (int)MathF.Max( 0f, breachable.AutoRepairTime - timeSinceBreach );
	}
}
