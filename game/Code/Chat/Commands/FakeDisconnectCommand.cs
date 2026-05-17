using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class FakeDisconnectCommand : ICommand
{
	public const string Name = "fakedisconnect";

	public string Command => Name;
	public string Help => Language.GetPhrase( "command.fakedisconnect.help" );
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.CommandFakeDisconnect];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( args.Length != 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.fakedisconnect.usage" ) );
			return true;
		}

		var displayName = caller.Connection?.DisplayName ?? caller.DisplayName;
		Chat.Current?.BroadcastSystemText( string.Format( Language.GetPhrase( "system.player.left" ), displayName ) );
		caller.Success( Language.GetPhrase( "command.fakedisconnect.success" ) );
		_ = ServerApiClient.Audit( "Fake Disconnect", $"{caller.SteamName} ({caller.SteamId}) used /{Name}", caller.SteamId );

		return true;
	}
}
