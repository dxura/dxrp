using Dxura.RP.Game.Minigame.Minigames;
using Dxura.RP.Shared;
namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem
{
	private List<Transform> LobbySpawnPoints { get; set; } = new();
	private int _lastSpawnIndex;

	private void TeleportPlayersToLobby()
	{
		if ( !LobbySpawnPoints.Any() )
		{
			Log.Warning( "[Minigame] Cannot teleport players to lobby: No lobby spawn points found!" );
			return;
		}

		// Teleport all players to random spawn points
		foreach ( var player in _players )
		{
			TeleportPlayerToLobby( player );
		}
	}

	private void TeleportPlayersToMinigame()
	{
		if ( _minigameSpawnPoints.Count == 0 )
		{
			return;
		}

		foreach ( var player in _players )
		{
			TeleportPlayerToMinigame( player );
		}
	}

	private void TeleportPlayerToLobby( Player player )
	{
		if ( !player.IsValid() )
		{
			Log.Warning( "[Minigame] Skipping invalid player during lobby teleport" );
			return;
		}

		var randomSpawn = Random.Shared.FromList( LobbySpawnPoints! );

		Log.Info( $"[Minigame] About to teleport {player.DisplayName} to lobby at {randomSpawn.Position} (current state: {CurrentState})" );
		player.TeleportHost( randomSpawn );
		PreparePlayerBaseline( player );

		Log.Info( $"[Minigame] Teleported {player.DisplayName} to lobby" );
	}

	private void TeleportPlayerToMinigame( Player player )
	{
		if ( !player.IsValid() )
		{
			Log.Warning( "[Minigame] Skipping invalid player during lobby teleport" );
			return;
		}

		var playerSpawn = GetNextSpawnPoint();
		if ( !playerSpawn.HasValue )
		{
			return;
		}

		PreparePlayerBaseline( player );
		PreparePlayerForMinigame( player );

		player.TeleportHost( playerSpawn.Value );
	}

	private Transform? GetNextSpawnPoint()
	{
		if ( _minigameSpawnPoints.Count == 0 )
		{
			return null;
		}

		var spawnMethod = CurrentMinigame?.SpawnMethod ?? MinigameSpawnMethod.RoundRobin;

		switch ( spawnMethod )
		{
			case MinigameSpawnMethod.RoundRobin:
				// Use round-robin for fair spawning
				var spawn = _minigameSpawnPoints[_lastSpawnIndex % _minigameSpawnPoints.Count];
				_lastSpawnIndex++;
				return spawn;

			case MinigameSpawnMethod.Random:
				// Random spawn selection
				return Random.Shared.FromList( _minigameSpawnPoints! );

			default:
				return Random.Shared.FromList( _minigameSpawnPoints! );
		}
	}
}
