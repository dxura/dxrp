using Dxura.RP.Shared;

namespace Dxura.RP.Game.Commands;

public class SetHealthCommand : ICommand
{
	public string Command => "sethealth";
	public string Help => "Set a player's health to a specific value, ignoring max health limits (Staff only)";
	public bool IsUsableWhileDead => true;
	public Permission[] RequiredPermissions => [Permission.CommandSetHealth];

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		// Validate arguments
		if ( args.Length < 2 )
		{
			caller.SendMessage( Language.GetPhrase( "command.sethealth.usage" ) );
			return true;
		}

		// Parse the target identifier (username or Steam ID)
		var targetIdentifier = args[0];
		
		// Parse the health amount
		if ( !float.TryParse( args[1], out var healthAmount ) )
		{
			caller.SendMessage( Language.GetPhrase( "command.sethealth.invalid_amount" ) );
			return true;
		}

		// Validate health amount (must be positive)
		if ( healthAmount <= 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.sethealth.must_positive" ) );
			return true;
		}

		// Try to find the target player
		Player? targetPlayer = null;

		// First, try to parse as Steam ID
		if ( long.TryParse( targetIdentifier, out var steamId ) )
		{
			targetPlayer = GameUtils.GetPlayerById( steamId );
		}

		// If not found by Steam ID, try to find by name
		if ( targetPlayer == null || !targetPlayer.IsValid() )
		{
			var matchingPlayers = GameUtils.GetPlayersByName( targetIdentifier );

			if ( matchingPlayers.Count == 0 )
			{
				caller.SendMessage( string.Format( Language.GetPhrase( "command.sethealth.not_found" ), targetIdentifier ) );
				return true;
			}

			if ( matchingPlayers.Count > 1 )
			{
				// Multiple matches found, show list
				var playerNames = string.Join( ", ", matchingPlayers.Select( p => p.DisplayName ) );
				caller.SendMessage( string.Format( Language.GetPhrase( "command.sethealth.multiple" ), targetIdentifier, playerNames ) );
				return true;
			}

			targetPlayer = matchingPlayers[0];
		}

		// Validate target player
		if ( !targetPlayer.IsValid() )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.sethealth.could_not_find" ), targetIdentifier ) );
			return true;
		}

		// Check if target has a health component
		if ( !targetPlayer.HealthComponent.IsValid() )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.sethealth.no_health" ), targetPlayer.DisplayName ) );
			return true;
		}

		// Set the player's health, ignoring max health constraints
		targetPlayer.HealthComponent.Health = healthAmount;

		// If the player is dead and health is set above 0, revive them
		if ( targetPlayer.HealthComponent.State == LifeState.Dead && healthAmount > 0 )
		{
			// Revive the player by spawning them
			targetPlayer.SpawnHost( inPlace: true );
		}

		// Notify the caller
		caller.SendMessage( string.Format( Language.GetPhrase( "command.sethealth.set" ), targetPlayer.DisplayName, healthAmount ) );

		// Notify the target player
		targetPlayer.SendMessage( string.Format( Language.GetPhrase( "command.sethealth.set_target" ), healthAmount ) );

		// Log the action
		Log.Info( $"[COMMAND] {caller.DisplayName} ({caller.SteamId}) set {targetPlayer.DisplayName} ({targetPlayer.SteamId})'s health to {healthAmount}" );

		// Log to Discord
		_ = ServerApiClient.Audit( "SetHealth", $"{caller.SteamName} ({caller.SteamId}) set {targetPlayer.SteamName} ({targetPlayer.SteamId})'s health to {healthAmount}", caller.SteamId );

		return true;
	}
}
