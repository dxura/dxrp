using Dxura.RP.Shared;
namespace Dxura.RP.Game.Sentinel;

public partial class Sentinel
{
	private TimeSince _lastProcessListener = 0;

	private void ProcessListener()
	{
		if ( !Config.Current.Game.SentinelListenerEnabled )
		{
			return;
		}

		if ( _lastProcessListener < 5 )
		{
			return;
		}
		_lastProcessListener = 0;

		DetectListenerOverrideViolations();
	}

	private void DetectListenerOverrideViolations()
	{
		foreach ( var player in GameUtils.Players )
		{
			if ( IsExempt( player ) )
			{
				continue;
			}

			if ( !player.ListenerTarget.IsValid() )
			{
				continue;
			}

			if ( RankSystem.HasPermission( player.SteamId, Permission.PlayerSpectate ) )
			{
				continue;
			}

			DisciplineListenerOverride( player );
		}
	}

	private void DisciplineListenerOverride( Player player )
	{
		var config = Config.Current.Game;

		RecordViolation(
			player,
			"Listener Override",
			$"ListenerTarget set to {player.ListenerTarget?.Name} without spectate permission",
			config.SentinelListenerReportingEnabled
		);

		if ( !config.SentinelListenerPunishmentEnabled )
		{
			return;
		}

		player.ListenerTarget = null;
	}
}
