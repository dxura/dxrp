using Dxura.RP.Game.Minigame.Minigames;
namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem
{

	// Respawn tracking
	private Dictionary<long, TimeSince> _playerRespawnTimers = new();

	private void OnSecondlyUpdateRespawn()
	{
		if ( CurrentState != MinigameState.Playing )
		{
			return;
		}

		CheckForDeadPlayers();
		CheckRespawnTimers();
	}

	private void CheckForDeadPlayers()
	{
		foreach ( var player in _players.ToList() )
		{
			// Skip if player is alive or already has a respawn timer
			if ( player.HealthComponent.State != LifeState.Dead || _playerRespawnTimers.ContainsKey( player.SteamId ) )
			{
				continue;
			}

			// Handle based on spawn ruleset
			switch ( CurrentMinigame?.SpawnRuleset )
			{
				case MinigameSpawnRuleset.OneLife:
					// Make spectator or remove from minigame
					if ( CurrentMinigame?.MakeDeadPlayersSpectators == true )
					{
						_players.Remove( player );
						_spectators.Add( player );

						TeleportPlayerToLobby( player );

						Log.Info( $"{player.DisplayName} eliminated from minigame (OneLife mode)" );
					}
					else
					{
						RemovePlayerFromMinigame( player );
					}

					break;

				case MinigameSpawnRuleset.Respawn:
					// Start respawn timer
					_playerRespawnTimers[player.SteamId] = 0;
					Log.Info( $"{player.DisplayName} died, will respawn in {CurrentMinigame?.RespawnDuration} seconds" );
					break;
			}
		}
	}

	private void CheckRespawnTimers()
	{
		foreach ( var kvp in _playerRespawnTimers.ToList() )
		{
			var playerId = kvp.Key;
			var timeSinceDeath = kvp.Value;

			// Find the player
			var player = _players.FirstOrDefault( p => p.SteamId == playerId );
			if ( !player.IsValid() )
			{
				// Player left, remove timer
				_playerRespawnTimers.Remove( playerId );
				continue;
			}

			// Check if respawn time has elapsed
			if ( timeSinceDeath >= CurrentMinigame?.RespawnDuration )
			{
				var spawnPoint = GetNextSpawnPoint();


				// Respawn the player
				player.SpawnHost( false, spawnPoint );

				PreparePlayerBaseline( player );
				PreparePlayerForMinigame( player );

				// Remove timer
				_playerRespawnTimers.Remove( playerId );

				Log.Info( $"{player.DisplayName} respawned after {CurrentMinigame?.RespawnDuration} seconds" );
			}
		}
	}
}
