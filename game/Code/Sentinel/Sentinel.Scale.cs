namespace Dxura.RP.Game.Sentinel;

public partial class Sentinel
{
	private TimeSince _lastProcessScale = 0;


	private void ProcessScale()
	{
		if ( !Config.Current.Game.SentinelScaleEnabled )
		{
			return;
		}

		if ( _lastProcessScale < 15 )
		{
			return;
		}
		_lastProcessScale = 0;

		DetectScaleViolations();
	}
	private void DetectScaleViolations()
	{
		// Check all players for scale violations
		foreach ( var player in GameUtils.Players )
		{
			if ( IsExempt( player ) )
			{
				continue;
			}
			if ( player.WorldScale != Vector3.One ||
			     player.LocalScale != Vector3.One ||
			     player.BodyRoot.WorldScale != Vector3.One ||
			     player.BodyRoot.LocalScale != Vector3.One )
			{
				DisciplineScale( player, player.GameObject );
			}
		}
	}

	private void DisciplineScale( Player player, GameObject target )
	{
		var config = Config.Current.Game;

		RecordViolation(
			player,
			"Scale",
			$"{target.Name} with scale {target.WorldScale}",
			config.SentinelScaleReportingEnabled
		);

		if ( !config.SentinelScalePunishmentEnabled )
		{
			return;
		}

		GameNetworkManager.Instance.KickPlayer( player.Connection, "Potential exploitation" );
	}


}
