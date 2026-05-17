using Sandbox.Diagnostics;
namespace Dxura.RP.Game.Sentinel;

public partial class Sentinel
{

	private const float MinTeleportSampleElapsed = 0.25f;
	private const float MaxTeleportSampleElapsed = 3.0f;

	private readonly Dictionary<long, TeleportSample> _lastPlayerPositions = new();

	private readonly Dictionary<long, TimeUntil> _permittedTeleportPlayers = new();


	private TimeSince _lastProcessTeleport = 0;


	private void ProcessTeleport()
	{
		if ( !Config.Current.Game.SentinelTeleportEnabled )
		{
			_lastPlayerPositions.Clear();
			ClearExpiredPermittedTeleports();
			return;
		}

		if ( _lastProcessTeleport < 1 )
		{
			return;
		}
		_lastProcessTeleport = 0;

		DetectTeleportViolations();
		CollatePlayerPositions();

		ClearExpiredPermittedTeleports();
	}

	private void ClearExpiredPermittedTeleports()
	{
		var expired = _permittedTeleportPlayers.Where( x => x.Value.Relative <= 0 ).Select( x => x.Key ).ToList();
		foreach ( var key in expired )
		{
			_permittedTeleportPlayers.Remove( key );
		}
	}

	private void DetectTeleportViolations()
	{
		foreach ( var player in GameUtils.Players )
		{
			if ( !IsTeleportCheckEligible( player ) )
			{
				continue;
			}

			if ( !_lastPlayerPositions.TryGetValue( player.SteamId, out var lastPosition ) )
			{
				continue;
			}

			var elapsed = Math.Clamp( RealTime.Now - lastPosition.SampleTime, MinTeleportSampleElapsed, MaxTeleportSampleElapsed );
			var maxDistance = GetMaxTeleportDistance( elapsed );
			var distance = player.WorldPosition.WithZ( 0 ).Distance( lastPosition.Position.WithZ( 0 ) );

			if ( distance > maxDistance )
			{
				DisciplineTeleport( player, distance, maxDistance );
			}
		}
	}

	private void CollatePlayerPositions()
	{
		_lastPlayerPositions.Clear();

		foreach ( var player in GameUtils.Players )
		{
			if ( !IsTeleportCheckEligible( player ) )
			{
				continue;
			}

			_lastPlayerPositions[player.SteamId] = new TeleportSample( player.WorldPosition, RealTime.Now );
		}
	}

	private readonly record struct TeleportSample( Vector3 Position, float SampleTime );

	private bool IsTeleportCheckEligible( Player player )
	{
		return player.IsValid() &&
		       !IsExempt( player ) &&
		       !_permittedTeleportPlayers.ContainsKey( player.SteamId );
	}

	private float GetMaxTeleportDistance( float elapsed )
	{
		var maxSpeed = MathF.Max( GameConfig.RunSpeed, GameConfig.WalkSpeed );
		maxSpeed = MathF.Max( maxSpeed, GameConfig.DuckedSpeed );

		return maxSpeed * elapsed + Config.Current.Game.SentinelTeleportThreshold;
	}

	private void DisciplineTeleport( Player player, float distance, float maxDistance )
	{
		var config = Config.Current.Game;

		RecordViolation(
			player,
			"Teleport",
			$"Moved {distance:F1} units in one interval (allowed {maxDistance:F1})",
			config.SentinelTeleportReportingEnabled
		);

		if ( !config.SentinelTeleportPunishmentEnabled )
		{
			return;
		}

		if ( _lastPlayerPositions.TryGetValue( player.SteamId, out var lastPosition ) )
		{
			_permittedTeleportPlayers.Remove( player.SteamId );
			player.TeleportHost( new Transform( lastPosition.Position, player.WorldRotation ) );
		}
		else
		{
			player.KillHost();
		}
	}

	public void PermitPlayerTeleportHost( long steamId, float seconds )
	{
		Assert.True( Networking.IsHost );
		_permittedTeleportPlayers[steamId] = seconds;
	}
}
