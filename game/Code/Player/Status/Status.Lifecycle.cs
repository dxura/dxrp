namespace Dxura.RP.Game;

public partial class Status
{
	public void OnSecondlyUpdate()
	{
		if ( Scene.IsEditor || !Networking.IsHost )
		{
			return;
		}

		foreach ( var (playerId, statuses) in _activeStatuses )
		{
			var player = GameUtils.GetPlayerById( playerId );
			if ( !player.IsValid() )
			{
				continue;
			}

			var toRemove = new List<IStatus>();
			foreach ( var status in statuses )
			{
				if ( status.IsExpired )
				{
					toRemove.Add( status );
					continue;
				}

				status.OnSecondlyUpdateServer( player );
			}

			// Remove expired statuses
			foreach ( var status in toRemove )
			{
				RemoveStatus( player, status.Id );
			}
		}
	}

}
