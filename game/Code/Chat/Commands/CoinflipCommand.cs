using System.Threading;
using System.Threading.Tasks;
namespace Dxura.RP.Game.Commands;

public class CoinflipCommand : ICommand
{
	public const string Name = "coinflip";
	public string Command => Name;
	public string[] Aliases => new[]
	{
		"cf",
		"flip"
	};
	public string Help => "/coinflip <amount> - Start or join a coinflip bet";

	private static CoinflipSession? _activeSession;
	private static readonly SemaphoreSlim SessionSemaphore = new( 1, 1 );

	private class CoinflipSession
	{
		public required Player Initiator { get; set; }
		public uint Amount { get; set; }
		private RealTimeUntil Duration { get; set; } = Config.Current.Game.CoinFlipDuration;
		public bool IsActive => Duration > 0;
		public string SessionId { get; set; } = Guid.NewGuid().ToString();
	}

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( !Config.Current.Game.CoinFlipEnabled || !Config.Current.Game.MoneyEnabled )
		{
			caller.SendMessage( Language.GetPhrase( "command.coinflip.disabled" ) );
			return true;
		}

		// Parse arguments
		if ( args.Length < 1 || !uint.TryParse( args[0], out var betAmount ) )
		{
			return false;
		}

		if ( betAmount == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.coinflip.bet_zero" ) );
			return true;
		}

		// Handle async execution
		_ = ExecuteAsync( caller, betAmount );

		return true;
	}

	private async Task ExecuteAsync( Player caller, uint betAmount )
	{
		await SessionSemaphore.WaitAsync();

		try
		{
			// Check if there's an active session
			if ( _activeSession == null )
			{
				await StartCoinflip( caller, betAmount );
				return;
			}

			// Player trying to join existing session
			if ( _activeSession.Initiator == caller )
			{
				caller.SendMessage( Language.GetPhrase( "command.coinflip.self_join" ) );
				return;
			}

			if ( betAmount != _activeSession.Amount )
			{
				caller.SendMessage( string.Format( Language.GetPhrase( "command.coinflip.must_bet" ), _activeSession.Amount.ToString( "C0" ) ) );
				return;
			}

			await JoinCoinflip( caller, _activeSession, betAmount );
		}
		finally
		{
			SessionSemaphore.Release();
		}
	}

	private async Task StartCoinflip( Player caller, uint amount )
	{
		if ( amount < Config.Current.Game.CoinFlipMinimalBet )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "command.coinflip.min_bet" ), Config.Current.Game.CoinFlipMinimalBet.ToString( "C0" ) ) );
			return;
		}

		if ( caller.BankBalance + caller.WalletBalance < amount )
		{
			caller.SendMessage( "#notify.cash.poor" );
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "coinflip", Config.Current.Game.CoinFlipCooldown ) )
		{
			var remaining = Cooldown.Current.GetRemainingTime( "coinflip" );
			caller.SendMessage( string.Format( Language.GetPhrase( "command.coinflip.cooldown" ), remaining ) );
			return;
		}

		// Charge the initiator upfront to secure their bet
		var didCharge = await caller.ChargeHost( amount, "Coinflip Bet", true );
		if ( !didCharge )
		{
			caller.SendMessage( Language.GetPhrase( "command.coinflip.charge_failed" ) );
			return;
		}

		_activeSession = new CoinflipSession
		{
			Initiator = caller, Amount = amount
		};

		// Set up auto-cancel callback
		var sessionId = _activeSession.SessionId;
		_ = GameTask.RunInThreadAsync( async () =>
		{
			await GameTask.DelayRealtimeSeconds( Config.Current.Game.CoinFlipDuration );
			await CancelExpiredSession( sessionId, caller, amount );
		} );

		// Broadcast to all players
		Chat.Current.BroadcastChat( string.Format( Language.GetPhrase( "command.coinflip.started" ), caller.DisplayName, amount.ToString( "C0" ), amount ), MessageType.Generic, Color.Gray );

		Log.Info( $"Player {caller.SteamId} started coinflip for {amount:C0}" );
	}

	private async Task JoinCoinflip( Player caller, CoinflipSession session, uint amount )
	{
		if ( caller.WalletBalance + caller.BankBalance < amount )
		{
			caller.SendMessage( "#notify.cash.poor" );
			return;
		}

		// Double-check session is still active
		if ( !session.IsActive )
		{
			caller.SendMessage( Language.GetPhrase( "command.coinflip.expired" ) );
			return;
		}

		// Perform the coinflip
		await DoCoinFlip( caller, session.Initiator, amount );
	}

	private async Task DoCoinFlip( Player joiner, Player initiator, uint amount )
	{
		// Charge the joiner (initiator was already charged when starting)
		var didWithdrawJoiner = await joiner.ChargeHost( amount, "Coinflip Bet", true );
		if ( !didWithdrawJoiner )
		{
			joiner.SendMessage( Language.GetPhrase( "command.coinflip.withdraw_failed" ) );
			return;
		}

		// Perform the coinflip
		var winner = Sandbox.Game.Random.Int( 0, 1 ) == 0 ? initiator : joiner;
		var loser = winner == initiator ? joiner : initiator;
		var totalPot = amount * 2;

		// Award the winner
		await winner.PayHost( totalPot, "Coinflip Win", true );

		// Announce results to all players
		Chat.Current.BroadcastChat( string.Format( Language.GetPhrase( "command.coinflip.winner" ), winner.DisplayName, totalPot.ToString( "C0" ), loser.DisplayName ), MessageType.Generic, Color.Gray );

		Log.Info( $"Coinflip: {winner.SteamId} won {totalPot:C0} against {loser.SteamId}" );
		_ = ServerApiClient.Audit( "CoinFlip", $"{winner.SteamName} ({winner.SteamId}) won {totalPot:C0} in a coinflip against {loser.SteamName} ({loser.SteamId})", winner.SteamId );

		winner.IncrementStat( "coinflip-won", 1 );
		loser.IncrementStat( "coinflip-lose", 1 );

		_activeSession = null; // Clear for another session
	}

	private static async Task CancelExpiredSession( string sessionId, Player initiator, uint amount )
	{
		await GameTask.MainThread();

		await SessionSemaphore.WaitAsync();
		try
		{
			// Check if this is still the active session and it hasn't been completed
			if ( _activeSession?.SessionId == sessionId && _activeSession.Initiator == initiator )
			{
				// Session expired, refund the initiator
				if ( initiator.IsValid() )
				{
					await initiator.PayHost( amount, "Coinflip Refund - Expired", true );
					Chat.Current.BroadcastChat( string.Format( Language.GetPhrase( "command.coinflip.cancelled" ), initiator.DisplayName ), MessageType.Generic, Color.Gray );
				}

				_activeSession = null;
				Log.Info( $"Coinflip session {sessionId} expired and refunded {amount:C0} to {initiator.SteamId}" );
			}
		}
		finally
		{
			SessionSemaphore.Release();
		}
	}
}
