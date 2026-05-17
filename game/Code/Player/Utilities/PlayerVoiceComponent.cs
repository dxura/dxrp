namespace Dxura.RP.Game;

public class PlayerVoiceComponent : Voice
{
	protected override IEnumerable<Connection> ExcludeFilter()
	{
		return Connection.All.Where( x =>
		{
			if ( x == Connection.Local )
			{
				return true;
			}

			var player = GameUtils.GetPlayerByConnectionId( x.Id );

			if ( !player.IsValid() )
			{
				return true;
			}

			return player.GetListenerPosition().Distance( GameObject.WorldPosition ) >= Distance;
		} );
	}

	protected override bool ShouldHearVoice( Connection connection )
	{
		var listener = GameUtils.GetPlayerByConnectionId( connection.Id );
		var speaker = GameUtils.GetPlayer( this );

		if ( !listener.IsValid() || !speaker.IsValid() )
		{
			return false;
		}

		if ( speaker.HasStatus( Constants.GaggedStatus ) || Status.Current.HasStatus( speaker.SteamId, Constants.GaggedStatus ) )
		{
			return false;
		}

		return true;
	}
}
