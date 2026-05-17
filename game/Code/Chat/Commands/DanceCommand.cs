namespace Dxura.RP.Game.Commands;

public class DanceCommand : ICommand
{
	public string Command => "dance";
	public string[] Aliases => [];
	public string Help => "Dance!";
	public bool IsUsableWhileDead => false;
	public bool IsUsableWhileFrozen => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		// Toggle dance off if already emoting
		if ( caller.CurrentEmote.IsValid() )
		{
			caller.StopEmoteHost();
			return true;
		}

		var emote = EmoteResource.All
			.FirstOrDefault( e => e.SequenceName.Equals( "emote_dance", StringComparison.OrdinalIgnoreCase ) );

		if ( emote == null )
		{
			caller.SendMessage( Language.GetPhrase( "command.dance.not_found" ) );
			return true;
		}

		caller.PlayEmoteHost( emote );
		return true;
	}
}
