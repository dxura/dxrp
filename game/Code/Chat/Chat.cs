namespace Dxura.RP.Game;

/// <summary>
/// Central chat system. Split into partials: state, RPC handlers, and command registry.
/// </summary>
public sealed partial class Chat : GameObjectSystem<Chat>, IGameEvents
{
	public Chat( Scene scene ) : base( scene )
	{
		Listen( Stage.SceneLoaded, 0, RegisterCommands, "Register Chat Commands" );
		Listen( Stage.FinishUpdate, 0, TickCommands, "Command Frame Tick" );
	}

	private void TickCommands()
	{
		foreach ( var command in _commandRegistry.Values.Distinct() )
			command.OnFrame();
	}
}

public static partial class PlayerExtensions
{
	public static void SendMessage( this Player player, string message )
	{
		// Using RPC filtering to only send to a specific player
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			Chat.Current.BroadcastChat( message );
		}
	}
}
