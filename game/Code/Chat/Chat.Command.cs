using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public sealed partial class Chat
{
	private readonly Dictionary<string, ICommand> _commandRegistry = new( StringComparer.OrdinalIgnoreCase );

	private void RegisterCommands()
	{
		_commandRegistry.Clear();

		// Discover and instantiate all commands via TypeLibrary
		var cmdTypes = TypeLibrary.GetTypes<ICommand>();
		foreach ( var type in cmdTypes.Where( t => !t.IsAbstract && t.TargetType != null ) )
		{
			if ( TypeLibrary.Create<ICommand>( type.TargetType ) is {} instance )
			{
				// Register main command
				_commandRegistry[instance.Command] = instance;

				// Register all aliases
				foreach ( var alias in instance.Aliases )
				{
					_commandRegistry[alias] = instance;
				}
			}
		}
	}

	/// <summary>
	/// Try to run a command's local (client-side) handler. Returns true if the command
	/// was consumed locally and should NOT be sent to the host.
	/// </summary>
	public bool TryExecuteLocalCommand( string text )
	{
		if ( string.IsNullOrWhiteSpace( text ) || !text.StartsWith( '/' ) )
		{
			return false;
		}

		var parts = text[1..].Trim().Split( ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
		if ( parts.Length == 0 )
		{
			return false;
		}

		if ( !_commandRegistry.TryGetValue( parts[0], out var command ) )
		{
			return false;
		}

		var args = parts.Skip( 1 ).ToArray();
		return command.ExecuteLocal( args, text );
	}

	public IEnumerable<(string Name, string Help)> GetRegisteredCommands()
	{
		return _commandRegistry.Values
			.Distinct()
			.OrderBy( c => c.Command )
			.Select( c => (Name: c.Command, c.Help) );
	}

	public IReadOnlyList<string> GetCommandCompletions( Player player, string partial )
	{
		partial = partial?.Trim() ?? string.Empty;

		return _commandRegistry.Values
			.Distinct()
			.Where( command => command.Command.StartsWith( partial, StringComparison.OrdinalIgnoreCase ) )
			.Where( command => CanAccessCommand( player, command ) )
			.Select( command => command.Command )
			.Distinct( StringComparer.OrdinalIgnoreCase )
			.OrderBy( name => name )
			.ToArray();
	}

	public bool TryGetCommand( string commandName, out ICommand? command )
	{
		return _commandRegistry.TryGetValue( commandName, out command );
	}

	private bool CanPlayerExecuteCommand( Player player, ICommand command )
	{
		if ( player.IsDead && !command.IsUsableWhileDead )
		{
			player.SendMessage( "#command.dead" );
			return false;
		}

		if ( player.Restricted && !command.IsUsableWhileRestricted )
		{
			player.SendMessage( "#command.restricted" );
			return false;
		}

		if ( player.HasStatus( Constants.FreezeStatus ) && !command.IsUsableWhileFrozen )
		{
			player.SendMessage( "#command.frozen" );
			return false;
		}

		return true;
	}

	private bool CanAccessCommand( Player player, ICommand command )
	{
		if ( player.IsDead && !command.IsUsableWhileDead )
		{
			return false;
		}

		if ( player.Restricted && !command.IsUsableWhileRestricted )
		{
			return false;
		}

		if ( player.HasStatus( Constants.FreezeStatus ) && !command.IsUsableWhileFrozen )
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

	private bool CanUseCommand( Player player, ICommand command )
	{
		var requiredPermissions = command.RequiredPermissions
			.Select( p => p.ToId() )
			.Concat( command.RequiredPermissionIds )
			.Where( p => !string.IsNullOrWhiteSpace( p ) )
			.Distinct( StringComparer.OrdinalIgnoreCase )
			.ToArray();

		if ( requiredPermissions.Length == 0 )
		{
			return true;
		}

		var missingPermissions = requiredPermissions
			.Where( permission => !RankSystem.HasPermission( player.SteamId, permission ) )
			.ToArray();

		if ( missingPermissions.Length == 0 )
		{
			return true;
		}

		player.Error( $"Missing permission for /{command.Command}: {string.Join( ", ", missingPermissions )}" );
		return false;
	}

	[ConCmd( "dx_chat" )]
	public static void Say( params string[] words )
	{
		var text = string.Join( " ", words );

		if ( string.IsNullOrWhiteSpace( text ) )
		{
			return;
		}

		Current?.SubmitPlayerChat( text, MessageType.LocalChat );
	}

	[Rpc.Host]
	public void ExecuteCommandHost( string commandName, params string[] args )
	{
		if ( string.IsNullOrWhiteSpace( commandName ) )
		{
			return;
		}

		var callerId = Rpc.CallerId;

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !_commandRegistry.TryGetValue( commandName, out var command ) )
		{
			return;
		}

		var cooldown = command.CooldownOverride ?? Config.Current.Game.CommandCooldown;
		if ( cooldown > 0 && Cooldown.Current.CheckAndStartCooldown( $"{callerId}:command", cooldown ) )
		{
			return;
		}

		if ( !CanPlayerExecuteCommand( player, command ) )
		{
			return;
		}

		var raw = $"/{commandName} {string.Join( ' ', args )}".TrimEnd();
		if ( !CanUseCommand( player, command ) )
		{
			return;
		}

		command.ExecuteHost( player, args, raw );
	}

	private bool HandlePlayerCommands( Player caller, string message )
	{
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			return false;
		}

		message = message.Trim();

		// Detect command style
		if ( message.StartsWith( '@' ) )
		{
			// Replace only the first '@' with /staff, keeping the rest intact
			message = "/staff " + message[1..].TrimStart();
		}
		else if ( !message.StartsWith( '/' ) )
		{
			return false;
		}

		if ( message == "/" )
		{
			message = "/help";
		}

		var parts = message[1..].Trim().Split( ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
		if ( parts.Length == 0 )
		{
			return true;
		}

		var commandName = parts[0];
		var args = parts.Skip( 1 ).ToArray();

		if ( _commandRegistry.TryGetValue( commandName, out var command ) )
		{
			if ( !CanPlayerExecuteCommand( caller, command ) )
			{
				return true;
			}

			if ( !CanUseCommand( caller, command ) )
			{
				return true;
			}

			var didSucceed = command.ExecuteHost( caller, args, message );

			// Provide help to player for invalid command
			if ( !didSucceed )
			{
				caller.SendMessage( Language.GetPhrase( "command.invalid" ) + command.Help );
			}

			return true;
		}


		using ( Rpc.FilterInclude( c => c.Id == caller.ConnectionId ) )
		{
			BroadcastChat( "#command.unknown" );
		}

		return true;
	}
}
