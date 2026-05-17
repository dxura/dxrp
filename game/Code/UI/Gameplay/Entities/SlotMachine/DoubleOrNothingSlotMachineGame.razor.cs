using System.Threading.Tasks;

namespace Dxura.RP.Game.UI;

public partial class DoubleOrNothingSlotMachineGame : BaseSlotMachineGame
{
	public enum GameState
	{
		Idle,
		Revealing,
		Result
	}

	/// <summary>
	/// Current game state (synced to all clients)
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public GameState CurrentState { get; private set; } = GameState.Idle;

	/// <summary>
	/// Current winnings amount (synced to all clients)
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public uint CurrentWinnings { get; private set; }

	/// <summary>
	/// Current multiplier (synced to all clients)
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public int CurrentMultiplier { get; private set; } = 1;

	/// <summary>
	/// Whether the last spin was a win (synced to all clients)
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public bool IsWin { get; private set; }

	/// <summary>
	/// The amount that was lost (for display purposes)
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public uint LostAmount { get; private set; }

	/// <summary>
	/// Check if the local player can play (used for UI state)
	/// </summary>
	private bool CanPlay => CurrentState == GameState.Idle && 
	                        Player.Local.IsValid() && 
	                        Player.Local.BankBalance >= BetAmount;

	/// <summary>
	/// Check if player can continue (after a win)
	/// </summary>
	private bool CanContinue => CurrentState == GameState.Result && IsWin && IsLocalPlayerPlaying;

	/// <summary>
	/// Check if player can cash out
	/// </summary>
	private bool CanCashOut => CurrentState == GameState.Result && IsWin && CurrentWinnings > 0 && IsLocalPlayerPlaying;

	public void OnPlayClicked()
	{
		if ( !CanPlay )
		{
			Player.Local.Error( "#notify.cash.poor" );
			return;
		}

		StartGameHost();
	}

	public void OnContinueClicked()
	{
		if ( !CanContinue )
		{
			return;
		}

		ContinueGameHost();
	}

	public void OnCashOutClicked()
	{
		if ( !CanCashOut )
		{
			return;
		}

		CashOutHost();
	}

	public void OnPlayAgainClicked()
	{
		ResetGameHost();
	}

	/// <summary>
	/// Request to start a new game - called from client, executed on host
	/// </summary>
	[Rpc.Host]
	private void StartGameHost()
	{
		var callerId = Rpc.CallerId;

		// Validate the caller
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		// Can only start if idle
		if ( CurrentState != GameState.Idle )
		{
			return;
		}

		// Check if player has enough money
		if ( player.BankBalance < BetAmount )
		{
			player.Error( "#notify.cash.poor" );
			return;
		}

		_ = StartGameAsync( player );
	}

	private async Task StartGameAsync( Player player )
	{
		// Charge the player
		var success = await ChargePlayer( player, BetAmount, "Double or Nothing bet" );
		if ( !success )
		{
			return;
		}

		// Set the current player and start the game
		SetCurrentPlayer( player );
		CurrentState = GameState.Revealing;
		CurrentWinnings = BetAmount;
		CurrentMultiplier = 1;

		_ = DoRoundAsync();
	}

	/// <summary>
	/// Request to continue (double or nothing) - called from client, executed on host
	/// </summary>
	[Rpc.Host]
	private void ContinueGameHost()
	{
		var callerId = Rpc.CallerId;
		
		if (Cooldown.Current.CheckAndStartCooldown( $"{callerId}:slot:continue", Config.Current.Game.ActionCooldown ))
		{
			return;
		}

		// Validate the caller is the current player
		if ( !ValidateCurrentPlayer( callerId ) )
		{
			return;
		}

		// Can only continue after a win
		if ( CurrentState != GameState.Result || !IsWin )
		{
			return;
		}

		_ = DoRoundAsync();
	}

	private async Task DoRoundAsync()
	{
		// Store the amount being risked
		var amountAtRisk = CurrentWinnings;
		var refundPlayer = CurrentPlayer;

		try
		{
			CurrentState = GameState.Revealing;

			BroadcastProcessEffects( 3 );

			await GameTask.DelayRealtimeSeconds( 3 );

			// Calculate win chance based on current multiplier
			// Starts at 45%, decreases by 5% each round
			var winChance = 0.45f - (CurrentMultiplier - 1) * 0.05f;
			winChance = Math.Max( winChance, 0.10f ); // Minimum 10% chance

			IsWin = Sandbox.Game.Random.Float( 0, 1 ) < winChance;

			// TODO: Multiplier cap is temporary :) shhhh

			if ( IsWin && CurrentMultiplier < 8 )
			{
				CurrentMultiplier++;
				CurrentWinnings *= 2;
				LostAmount = 0;

				BroadcastWinEffects();
			}
			else
			{
				LostAmount = amountAtRisk;
				BroadcastLoseEffects();
			}

			CurrentState = GameState.Result;
		}
		catch ( Exception ex )
		{
			// Without this, a swallowed exception in the fire-and-forget round leaves
			// CurrentState stuck on Revealing and the machine shows "Processing" forever.
			Log.Warning( $"Double or Nothing round failed, refunding and resetting: {ex}" );

			if ( refundPlayer.IsValid() && amountAtRisk > 0 )
			{
				await PayPlayer( refundPlayer, amountAtRisk, "Double or Nothing refund (round failed)" );
			}

			ResetGame();
		}
	}

	/// <summary>
	/// Request to cash out
	/// </summary>
	[Rpc.Host]
	private void CashOutHost()
	{
		var callerId = Rpc.CallerId;
		
		if (Cooldown.Current.CheckAndStartCooldown( $"{callerId}:slot:cashout", Config.Current.Game.ActionCooldown ))
		{
			return;
		}

		// Validate the caller is the current player
		if ( !ValidateCurrentPlayer( callerId ) )
		{
			return;
		}

		// Can only cash out after a win with winnings
		if ( CurrentState != GameState.Result || !IsWin || CurrentWinnings <= 0 )
		{
			return;
		}

		_ = CashOutAsync();
	}

	private async Task CashOutAsync()
	{
		if ( !CurrentPlayer.IsValid() )
		{
			return;
		}

		var winnings = CurrentWinnings;
		var player = CurrentPlayer;

		// Give player their winnings
		await PayPlayer( player, winnings, "Double or Nothing winnings" );

		// Reset the game
		ResetGame();
	}

	/// <summary>
	/// Reset game to idle state
	/// </summary>
	[Rpc.Host]
	private void ResetGameHost()
	{
		var callerId = Rpc.CallerId;
		
		// Only allow reset when in result state (after loss) or by the current player
		if ( CurrentState == GameState.Result && !IsWin )
		{
			// Anyone can reset after a loss
			ResetGame();
		}
		else if ( ValidateCurrentPlayer( callerId ) )
		{
			// Current player can always reset
			ResetGame();
		}
	}

	private void ResetGame()
	{
		SetCurrentPlayer( null );
		CurrentState = GameState.Idle;
		CurrentWinnings = 0;
		CurrentMultiplier = 1;
		IsWin = false;
		LostAmount = 0;
	}

	protected override void OnGameReset()
	{
		CurrentState = GameState.Idle;
		CurrentWinnings = 0;
		CurrentMultiplier = 1;
		IsWin = false;
		LostAmount = 0;
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( base.BuildHash(), CurrentState, CurrentWinnings, CurrentMultiplier, IsWin, LostAmount );
	}
}
