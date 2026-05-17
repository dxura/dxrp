using Dxura.RP.Game.Minigame.Minigames;
using Sandbox.Diagnostics;
namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem
{
	[Sync( SyncFlags.FromHost )]
	public bool IsWon { get; private set; } = false;

	[Sync( SyncFlags.FromHost )]
	public NetList<long> WinnerPlayers { get; private set; } = new();

	// Tracking win conditions
	private Dictionary<long, int> _playerKillCounts = new();

	public string GetWinnerPlayerNames()
	{
		if ( !WinnerPlayers.Any() )
		{
			return "No Winners";
		}

		var winnerNames = WinnerPlayers
			.Select( id => GameUtils.GetPlayerById( id )?.DisplayName ?? "Unknown" )
			.ToList();

		return string.Join( ", ", winnerNames );
	}

	private void OnSecondlyUpdateWin()
	{
		if ( CurrentState != MinigameState.Playing )
		{
			return;
		}

		CheckForWinConditionRuntime();
	}

	private void InitializeWinTracking()
	{
		switch ( CurrentMinigame?.WinCondition )
		{
			case MinigameWinCondition.MostKills:
				_playerKillCounts = _players.ToDictionary( p => p.SteamId, p => p.Kills );
				break;
		}
	}

	private void ClearWin()
	{
		IsWon = false;
		WinnerPlayers.Clear();
		_playerKillCounts.Clear();
	}

	private void CheckForWinConditionRuntime()
	{
		switch ( CurrentMinigame?.WinCondition )
		{
			case MinigameWinCondition.LastManStanding:
				if ( _players.Count <= 1 )
				{
					IsWon = true;
					WinnerPlayers.Clear();
					WinnerPlayers.AddRange( _players.Select( x => x.SteamId ) );

					SetState( MinigameState.PostLobby );
				}
				break;
		}
	}

	private void ProcessWinConditionEnd()
	{
		if ( IsWon )
		{
			return;
		}

		switch ( CurrentMinigame?.WinCondition )
		{
			case MinigameWinCondition.MostKills:
				// Calculate kill counts since minigame started
				var killCounts = _players.ToDictionary( p => p, p => p.Kills - _playerKillCounts.GetValueOrDefault( p.SteamId, 0 ) );

				// Only declare winners if there are kills
				if ( killCounts.Values.Any() && killCounts.Values.Max() > 0 )
				{
					var maxKills = killCounts.Values.Max();
					var winners = killCounts.Where( kvp => kvp.Value == maxKills ).Select( kvp => kvp.Key.SteamId ).ToList();
					IsWon = true;
					WinnerPlayers.Clear();
					WinnerPlayers.AddRange( winners );
				}
				break;
		}
	}

	public void NotifyWin( Player player )
	{
		Assert.True( Networking.IsHost );

		if ( IsWon )
		{
			Log.Info( $"[Minigame] Win already declared, ignoring win notification from {player?.DisplayName ?? "unknown"}" );
			return;
		}

		if ( !player.IsValid() )
		{
			Log.Warning( "[Minigame] NotifyWin called with invalid player" );
			return;
		}

		if ( !_players.Contains( player ) )
		{
			Log.Warning( $"[Minigame] {player.DisplayName} reached win trigger but is not in active players list (might be spectator)" );
			return;
		}

		Log.Info( $"[Minigame] {player.DisplayName} won the minigame!" );

		IsWon = true;
		WinnerPlayers.Clear();
		WinnerPlayers.Add( player.SteamId );

		SetState( MinigameState.PostLobby );
	}

	private void HandleRewards()
	{
		// Reward winners (stat win increment)
		foreach ( var winnerPlayer in WinnerPlayers )
		{
			var player = GameUtils.GetPlayerById( winnerPlayer );
			if ( player.IsValid() )
			{
				player.IncrementStat( "minigame-win", 1 );
			}
		}

		// Reward all participants (stat play increment)
		foreach ( var player in _players )
		{
			player.IncrementStat( "minigame-play", 1 );
		}
	}
}
