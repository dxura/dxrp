using Dxura.RP.Shared;
namespace Dxura.RP.Game.Sentinel;

public partial class Sentinel
{
	private TimeSince _lastProcessFlight = 0;

	private readonly Dictionary<long, TimeUntil> _exemptFlightPlayers = new();

	private void ProcessFlight()
	{
		if ( !Config.Current.Game.SentinelFlightEnabled && !Config.Current.Game.SentinelNoclipEnabled )
		{
			_exemptFlightPlayers.Clear();
			return;
		}

		if ( _lastProcessFlight < 1 )
		{
			return;
		}
		_lastProcessFlight = 0;

		DetectFlightViolations();

		ClearExpiredFlightExemptions();
	}

	private void DetectFlightViolations()
	{
		foreach ( var player in GameUtils.Players )
		{
			if ( !player.IsValid() || !player.Controller.IsValid() )
			{
				continue;
			}

			// Alert for noclipping players
			if ( Config.Current.Game.SentinelNoclipEnabled && player.GetComponent<MoveModeNoClip>().IsNoclipping && !CanNoclip( player ) )
			{
				DisciplineNoclip( player );
				continue;
			}

			if ( !Config.Current.Game.SentinelFlightEnabled )
			{
				continue;
			}
			
			// Skip for disabled controllers, below is dependent on it.
			if (!player.Controller.Enabled) continue;

			// Add 10s exemption after using non-grounding controller mode (e.g. ladders)
			if ( !player.Controller.Mode.AllowGrounding )
			{
				_exemptFlightPlayers[player.SteamId] = 10;
			}

			if ( IsExempt( player ) ||
			     _exemptFlightPlayers.ContainsKey( player.SteamId ) ||
			     player.TimeSinceLastRespawn < 5 ||
			     player.HealthComponent.State != LifeState.Alive ||
			     player.Sitting
			)
			{
				continue;
			}

			if ( player.Controller.TimeSinceGrounded.Relative <= Config.Current.Game.SentinelMaxFlightTime )
			{
				continue;
			}

			// Ignore if near solid map geometry (e.g. on an elevator)
			// DebugOverlaySystem.Current.Sphere(  new Sphere( player.WorldPosition, 50f ), Color.Red, 30f );
			var nearSolid = Scene.FindInPhysics( new Sphere( player.WorldPosition, 50f ) );
			if ( nearSolid.Any( x => x.Root != player.GameObject ) )
			{
				continue;
			}

			// Do raycast down to find the ground
			var trace = Scene.Trace.Ray( player.WorldPosition, Vector3.Down * 8000 )
				.IgnoreGameObjectHierarchy( player.GameObject )
				.Run();

			float? groundDistance = trace.Hit ? player.WorldPosition.Distance( trace.HitPosition ) : null;

			if ( groundDistance.HasValue && groundDistance.Value < Config.Current.Game.SentinelFlightMinDistance )
			{
				continue;
			}

			DisciplineFlight( player, trace.HitPosition );
		}
	}

	private bool CanNoclip( Player player )
	{
		return RankSystem.HasPermission( player.SteamId, Permission.Noclip ) || Config.Current.Game.NoClip;
	}

	private void ClearExpiredFlightExemptions()
	{
		var expired = _exemptFlightPlayers.Where( x => x.Value.Relative <= 0 ).Select( x => x.Key ).ToList();
		foreach ( var key in expired )
		{
			_exemptFlightPlayers.Remove( key );
		}
	}

	private void DisciplineNoclip( Player player )
	{
		var config = Config.Current.Game;

		RecordViolation(
			player,
			"Flight (Noclip)",
			"Player is using move mode NOCLIP without propper permission",
			config.SentinelNoclipReportingEnabled
		);

		if ( !config.SentinelNoclipPunishmentEnabled )
		{
			return;
		}

		GameNetworkManager.Instance.KickPlayer( player.Connection, "Noclipping is not allowed." );
	}

	private void DisciplineFlight( Player player, Vector3? safeLocation )
	{
		var config = Config.Current.Game;

		RecordViolation(
			player,
			"Flight",
			$"(TimeSinceGrounded: {player.Controller.TimeSinceGrounded.Relative:0.00}s)",
			config.SentinelFlightReportingEnabled
		);

		if ( !config.SentinelFlightPunishmentEnabled )
		{
			return;
		}

		if ( safeLocation.HasValue )
		{
			player.TeleportHost( new Transform( safeLocation.Value, player.WorldRotation ) );
		}
		else
		{
			player.KillHost();
		}
	}
}
