namespace Dxura.RP.Game;

public static class CommandHelper
{
	/// <summary>
	/// Parses a duration string like "10m", "1h", "7d" into a TimeSpan.
	/// Returns null if the format is invalid.
	/// </summary>
	public static TimeSpan? ParseDuration( string input )
	{
		if ( input.Length < 2 )
			return null;

		var unit = input[^1];
		if ( !int.TryParse( input[..^1], out var value ) || value <= 0 )
			return null;

		return unit switch
		{
			'm' => TimeSpan.FromMinutes( value ),
			'h' => TimeSpan.FromHours( value ),
			'd' => TimeSpan.FromDays( value ),
			_ => null
		};
	}

	/// <summary>
	/// Resolves a player by Steam ID or name. Sends error messages to the caller on failure.
	/// </summary>
	public static Player? ResolvePlayer( Player caller, string identifier )
	{
		Player? target = null;

		if ( long.TryParse( identifier, out var steamId ) )
		{
			target = GameUtils.GetPlayerById( steamId );
		}

		if ( target == null || !target.IsValid() )
		{
			var matches = GameUtils.GetPlayersByName( identifier );

			if ( matches.Count == 0 )
			{
				caller.SendMessage( string.Format( Language.GetPhrase( "command.player.not_found" ), identifier ) );
				return null;
			}

			if ( matches.Count > 1 )
			{
				var names = string.Join( ", ", matches.Select( p => p.DisplayName ) );
				caller.SendMessage( string.Format( Language.GetPhrase( "command.player.multiple" ), identifier, names ) );
				return null;
			}

			target = matches[0];
		}

		if ( !target.IsValid() )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.player.could_not_find" ), identifier ) );
			return null;
		}

		return target;
	}
}
