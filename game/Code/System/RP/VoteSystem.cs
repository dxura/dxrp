using Dxura.RP.Game.UI;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public enum VoteType
{
	Job,
	DemotePlayer,
	Custom,
	Test
}

public struct VoteInfo
{
	public long InitiatorId { get; set; }
	public long? TargetId { get; set; }
	public VoteType Type { get; set; }
	public string? Question { get; set; }
	public TimeSince StartTime { get; set; }
	public float Duration { get; set; }
	public string? CustomData { get; set; }
	public int YesVotes { get; set; }
	public int NoVotes { get; set; }
}

// New class to store vote type specific settings
public class VoteTypeSettings
{
	public int MinimumVotesRequired { get; set; } = 3;
	public float RequiredYesPercentage { get; set; } = 0.6f;
}

public class VoteSystem : SingletonComponent<VoteSystem>, IGameEvents
{
	[Property] public int DefaultVoteDuration { get; set; } = 30;

	// Default minimum votes if not specified for a vote type
	[Property] public int DefaultMinimumVotesRequired { get; set; } = 3;

	// Default percentage if not specified for a vote type
	[Property] public float DefaultRequiredYesPercentage { get; set; } = 0.6f;

	// Dictionary to store settings for each vote type
	private Dictionary<VoteType, VoteTypeSettings> VoteTypeConfigs { get; } = new();

	[Sync( SyncFlags.FromHost )] public NetDictionary<Guid, VoteInfo> ActiveVotes { get; set; } = new();

	// Simple string set to track which players have voted on which votes
	// Format: "voteId:playerId"
	[Sync( SyncFlags.FromHost )] private NetDictionary<string, bool> PlayerVoteRecords { get; set; } = new();

	[Property] [Group( "Effects" )] private SoundEvent? VoteStartSound { get; set; }

	protected override void OnStart()
	{
		if ( !Config.Current.Game.VotingEnabled )
		{
			Destroy();
			return;
		}

		// Initialize with default settings for each vote type
		InitializeVoteTypeSettings();
	}

	private void InitializeVoteTypeSettings()
	{
		// Initialize with default values
		foreach ( var voteType in Enum.GetValues<VoteType>() )
		{
			VoteTypeConfigs[voteType] = new VoteTypeSettings
			{
				MinimumVotesRequired = DefaultMinimumVotesRequired, RequiredYesPercentage = DefaultRequiredYesPercentage
			};
		}

		VoteTypeConfigs[VoteType.DemotePlayer] = new VoteTypeSettings
		{
			MinimumVotesRequired = 3, RequiredYesPercentage = 0.7f
		};
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		// Process active votes (end when time is up)
		List<Guid> votesToProcess = new();

		foreach ( var vote in ActiveVotes )
		{
			if ( vote.Value.StartTime > vote.Value.Duration )
			{
				votesToProcess.Add( vote.Key );
			}
		}

		foreach ( var voteId in votesToProcess )
		{
			ProcessVoteResult( voteId );
		}
	}

	private void ProcessVoteResult( Guid voteId )
	{
		if ( !ActiveVotes.TryGetValue( voteId, out var voteInfo ) )
		{
			return;
		}

		var totalVotes = voteInfo.YesVotes + voteInfo.NoVotes;
		var passed = false;

		// Get the settings for this vote type
		var settings = GetVoteTypeSettings( voteInfo.Type );

		// Check if minimum votes were cast and if yes percentage meets threshold
		if ( totalVotes >= settings.MinimumVotesRequired )
		{
			var yesPercentage = (float)voteInfo.YesVotes / totalVotes;
			passed = yesPercentage >= settings.RequiredYesPercentage;
		}

		// Execute vote result based on type
		if ( passed )
		{
			ExecuteVoteAction( voteInfo );
			Log.Info( $"Vote passed with {voteInfo.YesVotes} yes votes and {voteInfo.NoVotes} no votes" );
		}
		else
		{
			Log.Info( $"Vote failed with {voteInfo.YesVotes} yes votes and {voteInfo.NoVotes} no votes" );
		}

		if ( voteInfo.Type == VoteType.DemotePlayer && voteInfo.TargetId.HasValue )
		{
			var initiatorPlayer = GameUtils.GetPlayerById( voteInfo.InitiatorId );
			var targetPlayer = GameUtils.GetPlayerById( voteInfo.TargetId.Value );
			var initiatorName = initiatorPlayer?.SteamName ?? "Unknown";
			var targetName = targetPlayer?.SteamName ?? "Unknown";
			var result = passed ? "Passed" : "Failed";
			_ = ServerApiClient.Audit(
				"VoteDemote",
				$"{result} | Initiator: {initiatorName} ({voteInfo.InitiatorId}) | Target: {targetName} ({voteInfo.TargetId.Value}) | Votes: {voteInfo.YesVotes} Yes / {voteInfo.NoVotes} No",
				voteInfo.InitiatorId
			);
		}

		// Cleanup vote
		RemoveVote( voteId );
	}

	// Helper method to get settings for a vote type
	private VoteTypeSettings GetVoteTypeSettings( VoteType type )
	{
		if ( VoteTypeConfigs.TryGetValue( type, out var settings ) )
		{
			return settings;
		}

		// Fallback to default settings
		return new VoteTypeSettings
		{
			MinimumVotesRequired = DefaultMinimumVotesRequired, RequiredYesPercentage = DefaultRequiredYesPercentage
		};
	}

	private void RemoveVote( Guid voteId )
	{
		// Remove vote data
		ActiveVotes.Remove( voteId );

		// Remove player vote records for this vote
		var keysToRemove = PlayerVoteRecords.Keys.Where( k => k.StartsWith( $"{voteId}:" ) ).ToList();
		foreach ( var key in keysToRemove )
		{
			PlayerVoteRecords.Remove( key );
		}
	}

	public int CancelDemoteVotesHost( long targetId )
	{
		Assert.True( Networking.IsHost );

		var toCancel = ActiveVotes
			.Where( kv => kv.Value.Type == VoteType.DemotePlayer && kv.Value.TargetId == targetId )
			.Select( kv => kv.Key )
			.ToList();

		foreach ( var voteId in toCancel )
			RemoveVote( voteId );

		return toCancel.Count;
	}

	private void ExecuteVoteAction( VoteInfo voteInfo )
	{
		switch ( voteInfo.Type )
		{
			case VoteType.Job:
				var job = GameModeJobs.FindByReference( voteInfo.CustomData );
				if ( voteInfo.TargetId.HasValue && job != null )
				{
					var targetPlayer = GameUtils.GetPlayerById( voteInfo.TargetId.Value );
					targetPlayer?.AssignJobHost( job );
					Chat.Current?.BroadcastSystemText( $"{targetPlayer?.DisplayName} got the job!" );
				}

				break;

			case VoteType.DemotePlayer:
				if ( voteInfo.TargetId.HasValue )
				{
					var targetPlayer = GameUtils.GetPlayerById( voteInfo.TargetId.Value );
					if ( targetPlayer != null )
					{
						var demotedFromJob = targetPlayer.Job.Id;
						targetPlayer.AssignJobHost( GameModeJobs.Default );
						Cooldown.Current.StartCooldown( $"{voteInfo.TargetId.Value}:job:{demotedFromJob}", Config.Current.Game.DemoteJobReapplyCooldown );
						Cooldown.Current.StartCooldown( $"{voteInfo.TargetId.Value}:job:{demotedFromJob}:demote", Config.Current.Game.DemoteJobCooldown );
						Chat.Current?.BroadcastSystemText( $"{targetPlayer.DisplayName} has been demoted!" );
					}
				}

				break;
			case VoteType.Custom:
				// Handle custom vote types through custom data
				break;
		}
	}

	public bool StartVoteForPlayerHost( Player initiator, long? targetId, VoteType type, string? question = null, float duration = 0,
		string? customData = null )
	{
		Assert.True( Networking.IsHost );

		if ( !initiator.IsValid() )
		{
			return false;
		}

		if ( Cooldown.Current.IsOnCooldown( $"{initiator.SteamId}:vote" ) )
		{
			return false;
		}

		// For targeted votes, verify target exists
		if ( targetId.HasValue && type != VoteType.Custom &&
		     GameUtils.GetPlayerById( targetId.Value ) == null )
		{
			return false;
		}

		// For job votes, only allow players to change themselves
		if ( type == VoteType.Job && (!targetId.HasValue || targetId.Value != initiator.SteamId) )
		{
			initiator.Warn( "You can only create a job vote for yourself." );
			return false;
		}

		// For demote votes, require minimum playtime
		if ( type == VoteType.DemotePlayer && initiator.PlayTime < Config.Current.Game.DemoteMinPlaytime )
		{
			initiator.Warn( $"You need at least {Config.Current.Game.DemoteMinPlaytime} minutes of playtime to start a demote vote." );
			return false;
		}

		// For demote votes, ensure target isn't on cooldown (check only, don't start yet).
		if ( type == VoteType.DemotePlayer && targetId.HasValue &&
		     Cooldown.Current.IsOnCooldown( $"{targetId.Value}:job:{customData}:demote" ) )
		{
			initiator.Warn( "This player is on demote cooldown for this job." );
			return false;
		}

		// For demote votes, add per-initiator cooldown to prevent spam
		if ( type == VoteType.DemotePlayer && 
		     Cooldown.Current.IsOnCooldown( $"{initiator.SteamId}:vote:demote" ) )
		{
			initiator.Warn( "You are on cooldown for starting demote votes." );
			return false;
		}

		// For demote votes, ensure target is on demotable job.
		if ( type == VoteType.DemotePlayer && targetId.HasValue && GameUtils.GetPlayerById( targetId.Value ) is
			{
				Job.Demotable: false
			} )
		{
			initiator.Warn( "This player can't be demoted from this job" );
			return false;
		}

		// For job vote, ensure player isn't on cooldown.
		var jobCooldown = $"{initiator.SteamId}:job:vote:{customData}";
		if ( type == VoteType.Job && Cooldown.Current.IsOnCooldown( jobCooldown ) )
		{
			return false;
		}

		// For targeted votes, don't allow duplicate votes of same types
		if ( targetId.HasValue && ActiveVotes.Any( x => x.Value.TargetId == targetId.Value && x.Value.Type == type ) )
		{
			initiator.Warn( "This target player already has an active vote for this type." );
			return false;
		}

		// Use default duration if not specified
		if ( duration <= 0 )
		{
			duration = DefaultVoteDuration;
		}

		// Create vote info
		var voteInfo = new VoteInfo
		{
			InitiatorId = initiator.SteamId,
			TargetId = targetId,
			Type = type,
			Question = question,
			StartTime = 0,
			Duration = duration,
			CustomData = customData,
			YesVotes = 1, // Start with 1 yes vote (from the initiator)
			NoVotes = 0
		};

		// Add to active votes
		var voteId = Guid.NewGuid();
		ActiveVotes.Add( voteId, voteInfo );

		Cooldown.Current.StartCooldown( $"{initiator.SteamId}:vote", Config.Current.Game.VoteCooldown );

		if ( type == VoteType.Job && !string.IsNullOrWhiteSpace( customData ) )
		{
			Cooldown.Current.StartCooldown( $"{initiator.SteamId}:job:vote:{customData}", Config.Current.Game.JobVoteCooldown );
		}

		if ( type == VoteType.DemotePlayer )
		{
			Cooldown.Current.StartCooldown( $"{initiator.SteamId}:vote:demote", Config.Current.Game.VoteCooldown );
		}

		// Record the initiator's vote directly as "yes"
		var voteKey = $"{voteId}:{(type == VoteType.Job ? targetId : initiator.SteamId)}";
		PlayerVoteRecords[voteKey] = true; // true for "yes" vote

		Log.Info( $"Vote {type} started by {initiator.DisplayName} with an automatic yes vote" );
		_ = ServerApiClient.Audit( "Vote", $"{type} started by {initiator.SteamName} ({initiator.SteamId})", initiator.SteamId );

		if ( type == VoteType.DemotePlayer && targetId.HasValue )
		{
			var targetPlayer = GameUtils.GetPlayerById( targetId.Value );
			var targetName = targetPlayer?.SteamName ?? "Unknown";
			_ = ServerApiClient.Audit(
				"VoteDemote",
				$"Started | Initiator: {initiator.SteamName} ({initiator.SteamId}) | Target: {targetName} ({targetId.Value}) | Job: {customData}",
				initiator.SteamId
			);
		}

		var didPassEarly = CheckForEarlyCompletion( voteId, voteInfo );

		// Play sound for everyone that a vote has been created
		if ( !didPassEarly )
		{
			OnVoteStartEffects();

			// Notify reason for demote
			if ( type == VoteType.DemotePlayer )
			{

			}
		}

		return true;
	}

	[Rpc.Host]
	public void StartVoteHost( long? targetId, VoteType type, string? question = null, float duration = 0,
		string? customData = null )
	{
		Assert.True( Networking.IsHost );

		var initiator = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( initiator == null )
		{
			return;
		}

		StartVoteForPlayerHost( initiator, targetId, type, question, duration, customData );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void OnVoteStartEffects()
	{
		VoteStartSound.Play();
	}

	[Rpc.Host]
	public void CastVoteHost( Guid voteId, bool inFavor )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:action:quick", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		if ( !ActiveVotes.TryGetValue( voteId, out var voteInfo ) )
		{
			return;
		}

		var voter = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( voter == null )
		{
			return;
		}

		// Check if player has already voted
		var voteKey = $"{voteId}:{voter.SteamId}";
		if ( PlayerVoteRecords.TryGetValue( voteKey, out var previousVote ) )
		{
			// Already voted the same way
			if ( previousVote == inFavor )
			{
				return;
			}

			// Swap vote: undo the previous one
			if ( previousVote )
			{
				voteInfo.YesVotes--;
			}
			else
			{
				voteInfo.NoVotes--;
			}
		}

		// Record this vote
		PlayerVoteRecords[voteKey] = inFavor;

		// Update vote counts
		if ( inFavor )
		{
			voteInfo.YesVotes++;
		}
		else
		{
			voteInfo.NoVotes++;
		}

		// Update the vote info in the dictionary
		ActiveVotes[voteId] = voteInfo;

		Log.Info( $"{voter.DisplayName} voted {(inFavor ? "yes" : "no")}" );

		// Check for early completion
		CheckForEarlyCompletion( voteId, voteInfo );
	}

	private bool CheckForEarlyCompletion( Guid voteId, VoteInfo voteInfo )
	{
		// if ( Application.IsEditor && voteInfo.Type != VoteType.Test )
		// {
		// 	ProcessVoteResult( voteId );
		// 	return true;
		// }

		var totalPlayers = GameUtils.Players.Count();
		var totalVotes = voteInfo.YesVotes + voteInfo.NoVotes;

		// Get settings for this vote type
		var settings = GetVoteTypeSettings( voteInfo.Type );

		// Early pass: Enough yes votes to pass regardless of remaining votes
		if ( voteInfo.YesVotes >= settings.MinimumVotesRequired &&
		     (float)voteInfo.YesVotes / totalPlayers >= settings.RequiredYesPercentage )
		{
			ProcessVoteResult( voteId );
			return true;
		}

		// Early fail: Not enough possible yes votes to pass
		var remainingVotes = totalPlayers - totalVotes;
		var maxPossibleYesVotes = voteInfo.YesVotes + remainingVotes;

		if ( maxPossibleYesVotes < settings.MinimumVotesRequired ||
		     (float)maxPossibleYesVotes / totalPlayers < settings.RequiredYesPercentage )
		{
			ProcessVoteResult( voteId );

			GameUtils.GetPlayerById( voteInfo.InitiatorId )?
				.Warn( $"Not enough players for vote, requires {settings.MinimumVotesRequired} players" );
			return true;
		}

		return false;
	}

	// Helper methods for UI
	public bool HasPlayerVoted( long steamId, Guid voteId )
	{
		return PlayerVoteRecords.ContainsKey( $"{voteId}:{steamId}" );
	}

	public bool? GetPlayerVote( long steamId, Guid voteId )
	{
		return PlayerVoteRecords.TryGetValue( $"{voteId}:{steamId}", out var vote ) ? vote : null;
	}

	public int GetYesVotes( Guid voteId )
	{
		return ActiveVotes.TryGetValue( voteId, out var info ) ? info.YesVotes : 0;
	}

	public int GetNoVotes( Guid voteId )
	{
		return ActiveVotes.TryGetValue( voteId, out var info ) ? info.NoVotes : 0;
	}

	public float GetRemainingVoteTime( Guid voteId )
	{
		if ( ActiveVotes.TryGetValue( voteId, out var voteInfo ) )
		{
			return Math.Max( 0, voteInfo.Duration - voteInfo.StartTime );
		}

		return 0;
	}

	public int GetMinimumVotesRequired( VoteType type )
	{
		return GetVoteTypeSettings( type ).MinimumVotesRequired;
	}

	public float GetRequiredYesPercentage( VoteType type )
	{
		return GetVoteTypeSettings( type ).RequiredYesPercentage;
	}

	[Rpc.Host]
	public void CancelVoteHost( Guid voteId )
	{
		Assert.True( Networking.IsHost );

		var callerId = Rpc.CallerId;
		var caller = GameUtils.GetPlayerByConnectionId( callerId );
		if ( caller == null )
		{
			return;
		}

		// Check if the vote exists
		if ( !ActiveVotes.TryGetValue( voteId, out var voteInfo ) )
		{
			return;
		}

		// Only the initiator can cancel their own vote
		if ( voteInfo.InitiatorId != caller.SteamId )
		{
			caller.Warn( "You cannot cancel this vote because you did not initiate it." );
			return;
		}

		// Remove the vote
		RemoveVote( voteId );
		Chat.Current?.BroadcastSystemText( $"{caller.DisplayName} cancelled their vote." );
		Log.Info( $"Vote {voteInfo.Type} cancelled by {caller.DisplayName}" );
	}

	[ConCmd( "dx_dev_vote_test" )]
	public static void TestVote()
	{
		if ( !Application.IsEditor )
		{
			return;
		}

		var system = Instance;
		var initiator = GameUtils.Players.FirstOrDefault();
		if ( initiator == null )
		{
			return;
		}

		system.StartVoteHost( initiator.SteamId, VoteType.Test, "Test?", 60, "police" );
	}
}
