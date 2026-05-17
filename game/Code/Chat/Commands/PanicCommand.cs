namespace Dxura.RP.Game.Commands;

public class PanicCommand : ICommand
{
	public static string Name => "panic";
	public string Command => Name;
	public string[] Aliases => new[]
	{
		"911",
		"112",
		"000",
		"999"
	};
	public string Help => "Send a panic alert to police";

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:panic", Config.Current.Game.PanicCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		if ( !caller.IsValid() )
		{
			return false;
		}

		var panicReceivers = GameUtils.GetPlayersByJobTag( JobTag.Government )
			.Where( x => !x.Job.IsMayoralRole() )
			.Select( x => x.Connection )
			.ToList();

		panicReceivers.AddRange( GameUtils.GetPlayersByJobTag( JobTag.Medic ).Select( x => x.Connection ) );

		var panicReceiversHashSet = new HashSet<Connection?>( panicReceivers );

		if ( panicReceiversHashSet.Count == 0 )
		{
			caller.Error( Language.GetPhrase( "command.panic.no_one" ) );
			return true;
		}

		// Broadcast panic to all police
		using ( Rpc.FilterInclude( c => panicReceiversHashSet.Contains( c ) ) )
		{
			GameManager.Instance.BroadcastPanic( caller.WorldPosition );
		}

		caller.Success( Language.GetPhrase( "command.panic.alerted" ) );

		return true;
	}
}
