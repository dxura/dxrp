using Dxura.RP.Game.Minigame.Minigames;
using Dxura.RP.Game.System.Minigame;
namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem
{

	private string? _desiredMinigameSecondaryHint;
	private GameObject? _selectedSecondaryPrefab;

	public void StartMinigame( MinigameResource? resource = null, string? secondaryHint = null )
	{
		resource ??= AutoPickMinigameResource();

		if ( resource == null )
		{
			Log.Error( "Failed to start minigame: No suitable minigame resource found." );
			return;
		}

		Log.Info( $"Starting minigame: {resource.Identifier}" );

		CurrentMinigame = resource;
		_desiredMinigameSecondaryHint = secondaryHint;

		SetState( MinigameState.Seeding );
	}

	private MinigameResource? AutoPickMinigameResource()
	{
		var activePlayers = GameUtils.GetActivePlayerCount();
		var availableMinigames = MinigameResource.All
			.Where( x => x.IsSelectable && x.MinPlayers <= activePlayers )
			.ToList();

		if ( availableMinigames.Count == 0 )
		{
			return null;
		}

		var totalWeight = availableMinigames.Sum( x => x.Weight );
		var randomValue = Random.Shared.Next( totalWeight );
		var cumulativeWeight = 0;

		foreach ( var minigame in availableMinigames )
		{
			cumulativeWeight += minigame.Weight;
			if ( randomValue < cumulativeWeight )
			{
				return minigame;
			}
		}

		return availableMinigames.Last();
	}

	/// <summary>
	/// Creates the minigame: resolves the secondary prefab, spawns locally on host,
	/// and caches all spawn point data needed for game logic.
	/// </summary>
	private void CreateMinigame( string? secondaryHint )
	{
		if ( CurrentMinigame == null )
		{
			Log.Error( "Cannot create minigame: No current minigame resource." );
			return;
		}

		if ( _pendingPlayers.Count + _players.Count < CurrentMinigame.MinPlayers && !Application.IsEditor )
		{
			Log.Warning( $"Not enough players to start minigame '{CurrentMinigame.Identifier}'. Required: {CurrentMinigame.MinPlayers}, Present: {_pendingPlayers.Count + _players.Count}" );
			NotifyMinigameParticipants( $"Not enough players to start minigame '{CurrentMinigame.Name}'. It has been cancelled." );
			StopMinigame( false );
			return;
		}

		// Resolve which secondary prefab (map) to use
		_selectedSecondaryPrefab = null;
		if ( CurrentMinigame.SecondaryPrefabs is { Count: > 0 } )
		{
			if ( !string.IsNullOrEmpty( secondaryHint ) )
			{
				_selectedSecondaryPrefab = CurrentMinigame.SecondaryPrefabs.FirstOrDefault( p => p.Name.Contains( secondaryHint, StringComparison.OrdinalIgnoreCase ) );
			}

			_selectedSecondaryPrefab ??= Random.Shared.FromList( CurrentMinigame.SecondaryPrefabs! );
		}

		// Spawn locally on host using the shared path
		SpawnMinigameLocally( CurrentMinigame, _selectedSecondaryPrefab );

		// Extract and cache game logic data from the spawned objects
		var root = _localMinigameObjects.FirstOrDefault();
		if ( root == null )
		{
			Log.Error( $"Failed to spawn minigame prefab for '{CurrentMinigame.Identifier}'." );
			return;
		}

		// Cache minigame spawn points
		var minigameSpawnPointComponents = root.GetComponentsInChildren<MinigameSpawnPoint>();
		_minigameSpawnPoints = minigameSpawnPointComponents.Select( x => new Transform( x.WorldPosition, x.WorldRotation ) ).ToList();

		// Cache lobby spawn points and position
		LobbySpawnPoints = root.GetComponentsInChildren<MinigameLobbySpawnPoint>().Select( x => new Transform( x.WorldPosition, x.WorldRotation ) ).ToList();
		_lobbyPosition = Origin + CurrentMinigame.LobbyOffset;
	}

	public void SkipMinigameStage()
	{
		if ( CurrentMinigame == null )
		{
			Log.Warning( "[Minigame] No active minigame to skip stage." );
			return;
		}

		switch ( CurrentState )
		{
			case MinigameState.Seeding:
				Log.Info( "[Minigame] Skipping to PreLobby stage." );
				SetState( MinigameState.Creating );
				SetState( MinigameState.PreLobby );
				break;
			case MinigameState.PreLobby:
				Log.Info( "[Minigame] Skipping to Playing stage." );
				SetState( MinigameState.Playing );
				break;
			case MinigameState.Playing:
				Log.Info( "[Minigame] Skipping to Finished stage." );
				SetState( MinigameState.PostLobby );
				break;
			case MinigameState.PostLobby:
				Log.Info( "[Minigame] Skipping to Finished stage." );
				SetState( MinigameState.Finished );
				break;
			case MinigameState.Finished:
				Log.Info( "[Minigame] Minigame is already in Finished stage." );
				break;
			case MinigameState.Idle:
			default:
				Log.Warning( $"[Minigame] Cannot skip stage in state {CurrentState}" );
				break;
		}
	}

	public void StopMinigame( bool restorePlayers = true )
	{
		if ( CurrentMinigame == null )
		{
			Log.Warning( "[Minigame] No active minigame to stop." );
			return;
		}

		if ( restorePlayers )
		{
			RestoreAllPlayersState();
		}

		Log.Info( "[Minigame] Stopping current minigame." );

		BroadcastDestroyMinigame();
		CurrentMinigame = null;

		SetState( MinigameState.Idle );
		Clear();
	}

	public bool IsPlayerInMinigame( Player player )
	{
		return _players.Contains( player ) || _pendingPlayers.Contains( player ) || _spectators.Contains( player );
	}

	private void UpdatePlayerCount()
	{
		TotalPlayerCount = _players.Count + _pendingPlayers.Count;
	}

	private void NotifyMinigameParticipants( string message )
	{
		var minigameConnections = _players
			.Concat( _pendingPlayers )
			.Concat( _spectators )
			.Where( p => p.IsValid() )
			.Select( p => p.Connection )
			.ToHashSet();

		if ( minigameConnections.Count == 0 )
		{
			return;
		}

		using ( Rpc.FilterInclude( c => minigameConnections.Contains( c ) ) )
		{
			Notify.BroadcastInfo( message );
		}
	}


	public bool AddPlayerToMinigame( Player player )
	{
		switch ( CurrentState )
		{
			case MinigameState.Seeding:
				{
					// Add to pending players during seeding
					if ( !_pendingPlayers.Contains( player ) )
					{
						Log.Info( $"Player {player.DisplayName} joined the minigame queue." );
						NotifyMinigameParticipants( $"{player.DisplayName} joined the minigame" );

						_pendingPlayers.Add( player );
						UpdatePlayerCount();
					}
					break;
				}
			case MinigameState.PreLobby:
			case MinigameState.Playing:
				{
					if ( _spectators.Contains( player ) || _players.Contains( player ) )
					{
						return false;
					}

					// Add directly to players or spectators based on current state
					if ( CurrentState == MinigameState.Playing )
					{
						_spectators.Add( player );
					}
					else
					{
						_players.Add( player );
					}

					// Spawn minigame on the joining player
					if ( CurrentMinigame != null )
					{
						using ( Rpc.FilterInclude( c => c == player.Connection ) )
						{
							BroadcastSpawnMinigame( CurrentMinigame, _selectedSecondaryPrefab );
						}
					}

					StorePlayerState( player );
					PreparePlayerBaseline( player );

					// Teleport to lobby
					if ( LobbySpawnPoints.Any() )
					{
						var randomSpawn = Random.Shared.FromList( LobbySpawnPoints );
						player.TeleportHost( randomSpawn );
					}

					UpdatePlayerCount();
					Log.Info( $"Player {player.DisplayName} joined the minigame and was teleported to lobby." );
					NotifyMinigameParticipants( $"{player.DisplayName} joined the minigame" );
					break;
				}
			case MinigameState.Finished:
			case MinigameState.Idle:
			case MinigameState.Creating:
			default:
				Log.Warning( $"Cannot join minigame in state {CurrentState}" );
				return false;
		}

		return true;
	}

	public void RemovePlayerFromMinigame( Player player )
	{
		var wasInMinigame = false;

		if ( _players.Contains( player ) )
		{
			_players.Remove( player );
			wasInMinigame = true;
		}
		else if ( _pendingPlayers.Contains( player ) )
		{
			_pendingPlayers.Remove( player );
			wasInMinigame = true;
		}
		else if ( _spectators.Contains( player ) )
		{
			_spectators.Remove( player );
			wasInMinigame = true;
		}

		if ( !wasInMinigame )
		{
			Log.Warning( $"Player {player.DisplayName} is not in the minigame." );
			return;
		}

		// Destroy minigame on the leaving player
		using ( Rpc.FilterInclude( c => c == player.Connection ) )
		{
			BroadcastDestroyMinigame();
		}

		RestorePlayerState( player );
		UpdatePlayerCount();

		Log.Info( $"Player {player.DisplayName} removed from minigame." );
		NotifyMinigameParticipants( $"{player.DisplayName} left the minigame" );
	}
}
