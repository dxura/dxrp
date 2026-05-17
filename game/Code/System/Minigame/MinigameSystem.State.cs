using Dxura.RP.Game.Minigame.Minigames;
using Dxura.RP.Game.System.Minigame;
namespace Dxura.RP.Game.Minigame;

public enum MinigameState
{
	Idle,
	Seeding,
	Creating,
	PreLobby,
	Playing,
	PostLobby,
	Finished
}

public partial class MinigameSystem
{
	[Sync( SyncFlags.FromHost )]
	public MinigameResource? CurrentMinigame { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public MinigameState CurrentState { get; private set; } = MinigameState.Idle;

	[Sync( SyncFlags.FromHost )]
	public int TotalPlayerCount { get; private set; } = 0;

	private TimeSince _timeSinceStateChange = 0;

	private List<Transform> _minigameSpawnPoints = new();

	private List<Player> _players = new();
	private List<Player> _spectators = new();

	private List<Player> _pendingPlayers = new();

	private Vector3 _lobbyPosition = Vector3.Zero;

	private void OnSecondlyUpdateState()
	{
		// No minigame active, do nothing
		if ( CurrentState == MinigameState.Idle )
		{
			return;
		}

		// State Transitions

		if ( CurrentState == MinigameState.Seeding && _timeSinceStateChange > 30f )
		{
			SetState( MinigameState.Creating );
			return;
		}

		if ( CurrentState == MinigameState.Creating && _timeSinceStateChange > 3f )
		{
			SetState( MinigameState.PreLobby );
			return;
		}

		if ( CurrentState == MinigameState.PreLobby && _timeSinceStateChange > CurrentMinigame?.LobbyDuration )
		{
			SetState( MinigameState.Playing );
			return;
		}

		if ( CurrentState == MinigameState.Playing && _timeSinceStateChange > CurrentMinigame?.Duration )
		{
			SetState( MinigameState.PostLobby );
			return;
		}

		if ( CurrentState == MinigameState.PostLobby && _timeSinceStateChange > CurrentMinigame?.LobbyDuration )
		{
			SetState( MinigameState.Finished );
			return;
		}
	}

	private void HandleStateChange( MinigameState oldState, MinigameState newState )
	{
		if ( oldState == newState )
		{
			return;
		}

		switch ( newState )
		{
			case MinigameState.Seeding:
				Chat.Current.BroadcastChat( "MINIGAME", MessageType.Minigame );
				break;
			case MinigameState.Creating:
				CreateMinigame( _desiredMinigameSecondaryHint );
				break;
			case MinigameState.PreLobby:
				// Move pending players to active players
				_players.AddRange( _pendingPlayers );
				_pendingPlayers.Clear();

				_ = ServerApiClient.Audit( "Minigame", $"Started {CurrentMinigame?.Name} with {_players.Count} players." );

				UpdatePlayerCount();

				// Spawn minigame on all participating players
				if ( CurrentMinigame != null )
				{
					var connections = _players
						.Where( p => p.IsValid() )
						.Select( p => p.Connection )
						.Where( c => c != null )
						.ToHashSet();

					if ( connections.Count > 0 )
					{
						using ( Rpc.FilterInclude( c => connections.Contains( c ) ) )
						{
							BroadcastSpawnMinigame( CurrentMinigame, _selectedSecondaryPrefab );
						}
					}
				}

				StoreAllPlayersState();

				// Teleport only the active players to lobby (restricted)
				TeleportPlayersToLobby();
				break;
			case MinigameState.Playing:
				// Teleport players to minigame spawn points
				TeleportPlayersToMinigame();

				InitializeWinTracking();
				break;
			case MinigameState.PostLobby:

				// If no winners during gameplay, detect winners now
				if ( !IsWon )
				{
					ProcessWinConditionEnd();
				}

				TeleportPlayersToLobby();

				HandleRewards();

				DoMinigameOverEffects();

				var winnerInfo = string.Join( ", ", WinnerPlayers.Select( id =>
				{
					var p = GameUtils.GetPlayerById( id );
					return $"{p?.SteamName ?? "Unknown"} ({id})";
				} ) );
				_ = ServerApiClient.Audit( "Minigame", $"{CurrentMinigame?.Name} has finished with winners: {winnerInfo}" );
				break;
			case MinigameState.Finished:
				StopMinigame();
				break;
		}
	}

	private void SetState( MinigameState newState )
	{
		var oldState = CurrentState;
		CurrentState = newState;
		_timeSinceStateChange = 0;

		HandleStateChange( oldState, newState );
	}

	private void ClearState()
	{
		_minigameSpawnPoints = [];
		_players = [];
		_spectators = [];
		_pendingPlayers = [];
		TotalPlayerCount = 0;
		_desiredMinigameSecondaryHint = null;
		_selectedSecondaryPrefab = null;
	}
}
