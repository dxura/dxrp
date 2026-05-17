using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class HelpCommand : ICommand
{
	public string Command => "help";
	public string Help => "Lists commands";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		var commands = Chat.Current.GetRegisteredCommands()
			.Where( command => CanUseCommand( caller, command.Name ) );

		var commandList = string.Join( ", ", commands.Select( cmd => cmd.Name ) );

		caller.SendMessage( string.Format( Language.GetPhrase( "command.help.available" ), commandList ) );

		return true;
	}

	private static bool CanUseCommand( Player player, string commandName )
	{
		if ( !Chat.Current.TryGetCommand( commandName, out var command ) )
		{
			return false;
		}

		if ( player.IsDead && !command.IsUsableWhileDead )
		{
			return false;
		}

		if ( player.Restricted && !command.IsUsableWhileRestricted )
		{
			return false;
		}

		return command.RequiredPermissions
			.Select( permission => permission.ToId() )
			.Concat( command.RequiredPermissionIds )
			.Where( permission => !string.IsNullOrWhiteSpace( permission ) )
			.Distinct( StringComparer.OrdinalIgnoreCase )
			.All( permission => RankSystem.HasPermission( player.SteamId, permission ) );
	}
}
