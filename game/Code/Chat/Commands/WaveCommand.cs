namespace Dxura.RP.Game.Commands;

public class WaveCommand : ICommand
{
	public string Command => "wave";
	public string[] Aliases => [];
	public string Help => "Wave!";
	public bool IsUsableWhileDead => false;
	public bool IsUsableWhileFrozen => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		// Toggle wave off if already emoting
		if ( caller.CurrentEmote.IsValid() )
		{
			caller.StopEmoteHost();
			return true;
		}

		var emote = EmoteResource.All
			.FirstOrDefault( e => e.SequenceName.Equals( "emote_wave", StringComparison.OrdinalIgnoreCase ) );

		if ( emote == null )
		{
			caller.SendMessage( Language.GetPhrase( "command.wave.not_found" ) );
			return true;
		}

		caller.PlayEmoteHost( emote );
		return true;
	}
}
