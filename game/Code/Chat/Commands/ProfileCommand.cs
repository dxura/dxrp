using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Commands;

public class ProfileCommand : ICommand
{
	public const string Name = "profile";

	public string Command => Name;
	public string[] Aliases => ["p"];
	public string Help => "Open your profile or another player's profile in the tab menu. Usage: /profile [name]";
	public bool IsUsableWhileDead => true;
	public bool IsUsableWhileRestricted => true;

	public bool ExecuteLocal( string[] args, string raw )
	{
		if ( args.Length == 0 )
		{
			TabMenu.RequestOpenProfile( Player.Local.SteamId );
			return true;
		}

		var identifier = string.Join( " ", args );
		var target = ResolveLocal( identifier );
		if ( target == null )
		{
			return true;
		}

		TabMenu.RequestOpenProfile( target.SteamId );
		return true;
	}

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		// Handled entirely client-side via ExecuteLocal.
		return true;
	}

	private static Player? ResolveLocal( string identifier )
	{
		if ( long.TryParse( identifier, out var steamId ) )
		{
			var byId = GameUtils.GetPlayerById( steamId );
			if ( byId.IsValid() )
			{
				return byId;
			}
		}

		var matches = GameUtils.Players
			.Where( p => p.IsValid() && p.DisplayName.Contains( identifier, StringComparison.OrdinalIgnoreCase ) )
			.ToList();

		if ( matches.Count == 0 )
		{
			Chat.Current?.Echo( string.Format( Language.GetPhrase( "command.player.not_found" ), identifier ) );
			return null;
		}

		if ( matches.Count > 1 )
		{
			var names = string.Join( ", ", matches.Select( p => p.DisplayName ) );
			Chat.Current?.Echo( string.Format( Language.GetPhrase( "command.player.multiple" ), identifier, names ) );
			return null;
		}

		return matches[0];
	}
}
