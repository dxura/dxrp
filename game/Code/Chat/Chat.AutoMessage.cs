namespace Dxura.RP.Game;

public sealed partial class Chat
{
	private TimeSince _lastAutoMessage;

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		var config = Config.Current.Game;

		if ( !config.AutoMessagesEnabled || config.AutoMessages.Length == 0 )
		{
			return;
		}

		if ( _lastAutoMessage < config.AutoMessagesInterval )
		{
			return;
		}

		_lastAutoMessage = 0;

		var message = Sandbox.Game.Random.FromArray( config.AutoMessages );
		
		if (string.IsNullOrEmpty(message ))
		{
			return;
		}
		
		BroadcastSystemText( message );
	}
}
