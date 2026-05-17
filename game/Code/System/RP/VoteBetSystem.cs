using Dxura.RP.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public struct VoteBetInfo
{
	public Guid Id { get; set; }
	public long InitiatorId { get; set; }
	public string Description { get; set; }
	public List<string> Outcomes { get; set; }
	public bool IsActive { get; set; }
	public bool IsLocked { get; set; }
	public TimeSince StartTime { get; set; }
}

public struct VoteBetPlayerBet
{
	public long PlayerId { get; set; }
	public int OutcomeIndex { get; set; }
	public uint BetAmount { get; set; }
}

public class VoteBetSystem : SingletonComponent<VoteBetSystem>
{
	public const int MaxOutcomes = 15;
	public const int MinOutcomes = 2;
	public const uint MaxBetAmount = 250000;
	public const int MaxDescriptionLength = 200;
	public const int MaxOutcomeLength = 100;

	[Sync( SyncFlags.FromHost )] public NetDictionary<Guid, VoteBetInfo> ActiveVoteBets { get; set; } = new();
	[Sync( SyncFlags.FromHost )] private NetDictionary<string, VoteBetPlayerBet> PlayerBets { get; set; } = new();

	private readonly SemaphoreSlim _betLock = new( 1, 1 );

	protected override void OnStart()
	{
		if ( !Config.Current.Game.MoneyEnabled )
		{
			Destroy();
			return;
		}
	}

	public async void StartVoteBet( Player caller, string description, List<string> outcomes )
	{
		if ( string.IsNullOrWhiteSpace( description ) )
		{
			caller.SendMessage( Language.GetPhrase( "system.votebet.description_empty" ) );
			return;
		}

		if ( description.Length > MaxDescriptionLength )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "system.votebet.description_too_long" ), MaxDescriptionLength ) );
			return;
		}

		if ( outcomes == null || outcomes.Count < MinOutcomes )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "system.votebet.min_outcomes" ), MinOutcomes ) );
			return;
		}

		if ( outcomes.Count > MaxOutcomes )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "system.votebet.max_outcomes" ), MaxOutcomes ) );
			return;
		}

		if ( outcomes.Any( o => o.Length > MaxOutcomeLength ) )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "system.votebet.outcome_too_long" ), MaxOutcomeLength ) );
			return;
		}

		await _betLock.WaitAsync();
		try
		{
			if ( ActiveVoteBets.Any( x => x.Value.IsActive ) )
			{
				caller.SendMessage( Language.GetPhrase( "system.votebet.already_active" ) );
				return;
			}

			var voteBetId = Guid.NewGuid();
			var voteBetInfo = new VoteBetInfo
			{
				Id = voteBetId,
				InitiatorId = caller.SteamId,
				Description = description,
				Outcomes = outcomes,
				IsActive = true,
				StartTime = 0
			};

			ActiveVoteBets[voteBetId] = voteBetInfo;

			Chat.Current.BroadcastChat( Language.GetPhrase( "system.votebet.title" ), MessageType.VoteBet );

			Log.Info( $"Vote bet started by {caller.DisplayName}: {description}" );
			_ = ServerApiClient.Audit( "VoteBet", $"Started by {caller.SteamName} ({caller.SteamId}): {description}", caller.SteamId );
		}
		catch ( Exception e )
		{
			Log.Error( $"Error starting vote bet: {e.Message}" );
		}
		finally
		{
			_betLock.Release();
		}
	}

	public async void JoinVoteBet( Player caller, Guid voteBetId, int outcomeIndex, uint betAmount )
	{
		if ( betAmount == 0 )
		{
			caller.SendMessage( Language.GetPhrase( "command.votebet.bet_zero" ) );
			return;
		}

		if ( betAmount > MaxBetAmount )
		{
			caller.SendMessage( string.Format( Language.GetPhrase( "system.votebet.max_bet" ), $"{MaxBetAmount:N0}" ) );
			return;
		}

		await _betLock.WaitAsync();
		try
		{
			if ( !ActiveVoteBets.TryGetValue( voteBetId, out var voteBetInfo ) || !voteBetInfo.IsActive )
			{
				caller.SendMessage( Language.GetPhrase( "system.votebet.no_longer_active" ) );
				return;
			}

			if ( voteBetInfo.IsLocked )
			{
				caller.SendMessage( Language.GetPhrase( "command.votebet.locked" ) );
				return;
			}

			if ( outcomeIndex < 0 || outcomeIndex >= voteBetInfo.Outcomes.Count )
			{
				caller.SendMessage( Language.GetPhrase( "system.votebet.invalid_outcome_selected" ) );
				return;
			}

			var betKey = $"{voteBetId}:{caller.SteamId}";
			if ( PlayerBets.ContainsKey( betKey ) )
			{
				caller.SendMessage( Language.GetPhrase( "command.votebet.already_bet" ) );
				return;
			}

			if ( caller.BankBalance + caller.WalletBalance < betAmount )
			{
				caller.SendMessage( "#notify.cash.poor" );
				return;
			}

			var didCharge = await caller.ChargeHost( betAmount, $"Vote Bet: {voteBetInfo.Description}", true );
			if ( !didCharge )
			{
				caller.SendMessage( Language.GetPhrase( "system.votebet.bet_failed" ) );
				return;
			}

			PlayerBets[betKey] = new VoteBetPlayerBet
			{
				PlayerId = caller.SteamId, OutcomeIndex = outcomeIndex, BetAmount = betAmount
			};

			var outcomeName = voteBetInfo.Outcomes[outcomeIndex];
			caller.SendMessage( string.Format( Language.GetPhrase( "system.votebet.bet_placed" ), $"{betAmount:N0}", outcomeName ) );
			Log.Info( $"{caller.DisplayName} bet ${betAmount:N0} on outcome {outcomeIndex} ({outcomeName})" );
		}
		catch ( Exception e )
		{
			Log.Error( $"Error joining vote bet: {e.Message}" );
		}
		finally
		{
			_betLock.Release();
		}
	}

	public async void EndVoteBet( Player caller, Guid voteBetId, int winningOutcomeIndex )
	{
		await _betLock.WaitAsync();
		try
		{
			if ( !ActiveVoteBets.TryGetValue( voteBetId, out var voteBetInfo ) || !voteBetInfo.IsActive )
			{
				caller.SendMessage( Language.GetPhrase( "system.votebet.not_active" ) );
				return;
			}

			if ( winningOutcomeIndex < 0 || winningOutcomeIndex >= voteBetInfo.Outcomes.Count )
			{
				caller.SendMessage( Language.GetPhrase( "system.votebet.invalid_winning_outcome" ) );
				return;
			}

			voteBetInfo.IsActive = false;
			ActiveVoteBets[voteBetId] = voteBetInfo;

			var allBets = PlayerBets.Where( x => x.Key.StartsWith( $"{voteBetId}:" ) ).Select( x => x.Value ).ToList();
			var totalPot = (uint)allBets.Sum( x => (long)x.BetAmount );
			var winners = allBets.Where( x => x.OutcomeIndex == winningOutcomeIndex ).ToList();
			var winningOutcome = voteBetInfo.Outcomes[winningOutcomeIndex];

			if ( winners.Count == 0 )
			{
				Chat.Current.BroadcastChat( string.Format( Language.GetPhrase( "system.votebet.won_no_bets" ), winningOutcome, $"{totalPot:N0}" ), MessageType.Generic, Color.Yellow );
			}
			else
			{
				var winnerPool = (uint)winners.Sum( x => x.BetAmount );

				Chat.Current.BroadcastChat( string.Format( Language.GetPhrase( "system.votebet.won_with_winners" ), winningOutcome, winners.Count, $"{totalPot:N0}" ), MessageType.Generic, Color.Yellow );

				uint totalPaidOut = 0;
				for ( var i = 0; i < winners.Count; i++ )
				{
					var winner = winners[i];
					var winnerPlayer = GameUtils.GetPlayerById( winner.PlayerId );
					if ( winnerPlayer != null )
					{
						uint payout;
						if ( i == winners.Count - 1 )
						{
							payout = totalPot - totalPaidOut;
						}
						else
						{
							payout = (uint)((ulong)winner.BetAmount * totalPot / winnerPool);
						}

						totalPaidOut += payout;
						await winnerPlayer.PayHost( payout, $"Vote Bet Win: {voteBetInfo.Description}", true );
						var profit = (int)payout - (int)winner.BetAmount;
						winnerPlayer.SendMessage( string.Format( Language.GetPhrase( "system.votebet.winner_message" ), $"{payout:N0}", profit.ToString( "+#,##0;-#,##0;0" ) ) );
						winnerPlayer.IncrementStat( "votebet-won", 1 );
					}
				}

				var losers = allBets.Where( x => x.OutcomeIndex != winningOutcomeIndex ).ToList();
				foreach ( var loser in losers )
				{
					var loserPlayer = GameUtils.GetPlayerById( loser.PlayerId );
					if ( loserPlayer != null )
					{
						loserPlayer.SendMessage( string.Format( Language.GetPhrase( "system.votebet.loser_message" ), $"{loser.BetAmount:N0}" ) );
						loserPlayer.IncrementStat( "votebet-lost", 1 );
					}
				}
			}

			var betsToRemove = PlayerBets.Keys.Where( k => k.StartsWith( $"{voteBetId}:" ) ).ToList();
			foreach ( var key in betsToRemove )
			{
				PlayerBets.Remove( key );
			}
			ActiveVoteBets.Remove( voteBetId );

			Log.Info( $"Vote bet ended by {caller.DisplayName}. Winning outcome: {winningOutcome}" );
			_ = ServerApiClient.Audit( "VoteBet", $"Ended by {caller.SteamName}. Winning outcome: '{winningOutcome}'. {winners.Count} winner(s), pot: ${totalPot:N0}", caller.SteamId );
		}
		catch ( Exception e )
		{
			Log.Error( $"Error ending vote bet: {e.Message}" );
		}
		finally
		{
			_betLock.Release();
		}
	}

	public async void LockVoteBet( Player caller, Guid voteBetId )
	{
		await _betLock.WaitAsync();
		try
		{
			if ( !ActiveVoteBets.TryGetValue( voteBetId, out var voteBetInfo ) || !voteBetInfo.IsActive )
			{
				caller.SendMessage( Language.GetPhrase( "system.votebet.not_active" ) );
				return;
			}

			voteBetInfo.IsLocked = !voteBetInfo.IsLocked;
			ActiveVoteBets[voteBetId] = voteBetInfo;

			var state = voteBetInfo.IsLocked
				? Language.GetPhrase( "system.votebet.locked_state" )
				: Language.GetPhrase( "system.votebet.unlocked_state" );
			Chat.Current.BroadcastChat( string.Format( Language.GetPhrase( "system.votebet.lock_toggled" ), state ), MessageType.Generic, Color.Yellow );
			Log.Info( $"Vote bet {state} by {caller.DisplayName}" );
		}
		catch ( Exception e )
		{
			Log.Error( $"Error toggling vote bet lock: {e.Message}" );
		}
		finally
		{
			_betLock.Release();
		}
	}

	public VoteBetInfo? GetActiveVoteBet()
	{
		var activeVoteBet = ActiveVoteBets.FirstOrDefault( x => x.Value.IsActive );
		if ( activeVoteBet.Key == Guid.Empty )
		{
			return null;
		}
		return activeVoteBet.Value;
	}

	public bool HasPlayerBet( long steamId, Guid voteBetId )
	{
		return PlayerBets.ContainsKey( $"{voteBetId}:{steamId}" );
	}

	public uint GetTotalPot( Guid voteBetId )
	{
		return (uint)PlayerBets.Where( x => x.Key.StartsWith( $"{voteBetId}:" ) )
			.Sum( x => x.Value.BetAmount );
	}
}
