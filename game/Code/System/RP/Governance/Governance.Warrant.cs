using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public partial class Governance
{
	public enum WarrantAction
	{
		Request,
		Approve,
		Deny
	}

	public record WarrantRequest
	{
		public long TargetId { get; set; }
		public long RequesterId { get; set; }
		public string Reason { get; set; } = "";
		public TimeUntil ExpiresAt { get; set; }
	}

	[Sync( SyncFlags.FromHost )]
	public NetDictionary<long, WarrantRequest> PendingWarrants { get; } = new();

	private void OnStartWarrant()
	{
		Assert.True( Networking.IsHost );
	}

	private void OnSecondlyUpdateWarrant()
	{
		Assert.True( Networking.IsHost );

		// Check for expired warrant requests
		var expiredWarrants = PendingWarrants
			.Where( kvp => kvp.Value.ExpiresAt )
			.Select( kvp => kvp.Key )
			.ToList();

		foreach ( var targetId in expiredWarrants )
		{
			if ( PendingWarrants.TryGetValue( targetId, out var request ) )
			{
				var requester = GameUtils.GetPlayerById( request.RequesterId );
				if ( requester.IsValid() )
				{
					requester.Error( "#governance.warrant.expired" );
				}

				PendingWarrants.Remove( targetId );
			}
		}
	}

	[Rpc.Host]
	public void Warrant( long targetId, WarrantAction action, string reason )
	{
		var callerId = Rpc.CallerId;
		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );
		var targetPlayer = GameUtils.GetPlayerById( targetId );

		if ( !callerPlayer.IsValid() ||
		     !targetPlayer.IsValid() )
		{
			return;
		}

		WarrantHost( callerPlayer, targetPlayer, action, reason );
	}

	public void WarrantHost( Player caller, Player target, WarrantAction action, string reason )
	{
		Assert.True( Networking.IsHost );

		if ( !caller.IsValid() ||
		     !target.IsValid() )
		{
			return;
		}

		var isGovernment = caller.Job.IsGovernmentRole();
		var isMayor = caller.Job.IsMayoralRole();
		var isChief = caller.Job.IsChiefRole();
		var canApprove = isMayor || isChief;
		var canRequest = isGovernment && !isMayor;

		if ( !isGovernment )
		{
			caller.Error( "#generic.permission" );
			return;
		}

		switch ( action )
		{
			case WarrantAction.Request when canRequest:
				RequestWarrant( caller, target, reason );
				break;
			case WarrantAction.Approve when canApprove:
				ApproveWarrant( caller, target );
				break;
			case WarrantAction.Deny when canApprove:
				DenyWarrant( caller, target );
				break;
			default:
				caller.Error( "#notify.warrant.invalid.action" );
				break;
		}
	}

	private void RequestWarrant( Player requester, Player target, string reason )
	{
		Assert.True( Networking.IsHost );

		if ( Cooldown.Current.CheckAndStartCooldown( $"{requester.SteamId}:warrant", Config.Current.Game.WarrantCooldown ) )
		{
			requester.Cooldown( $"{requester.SteamId}:warrant" );
			return;
		}

		// Cannot warrant government members
		if ( target.Job.IsGovernmentRole() )
		{
			requester.Error( "#notify.warrant.target.government" );
			return;
		}

		// Check if target already has a warrant
		if ( target.HasStatus( Constants.WarrantStatus ) )
		{
			requester.Error( "#notify.warrant.already.has" );
			return;
		}

		// Check if there's already a pending warrant request
		if ( PendingWarrants.ContainsKey( target.SteamId ) )
		{
			requester.Error( "#notify.warrant.already.pending" );
			return;
		}

		reason = GameManager.ModerateText( requester.SteamId, "warrant", reason );

		if ( Cooldown.Current.CheckAndStartCooldown( $"{target.SteamId}:warrant", Config.Current.Game.PlayerWarrantCooldown ) )
		{
			requester.Error( "#notify.warrant.player.cooldown" );
			return;
		}

		// Create pending warrant request
		var request = new WarrantRequest
		{
			TargetId = target.SteamId, RequesterId = requester.SteamId, Reason = reason, ExpiresAt = 300f // 5 minutes
		};

		PendingWarrants[target.SteamId] = request;

		_ = ServerApiClient.Audit( "Warrant",
			$"{requester.SteamName} ({requester.SteamId}) requested a warrant for {target.SteamName} ({target.SteamId}): {reason}",
			requester.SteamId );

		// Notify requester
		var isChiefRequesting = requester.Job.IsChiefRole();
		var successMessage = isChiefRequesting
			? string.Format( Language.GetPhrase( "governance.warrant.submitted_mayor" ), target.DisplayName )
			: string.Format( Language.GetPhrase( "governance.warrant.submitted" ), target.DisplayName );
		requester.Success( successMessage );

		// Notify Mayor and Chief (skip if they are the requester)
		var mayor = GameUtils.GetPlayersByJobTag( JobTag.Mayoral ).FirstOrDefault();
		var chief = GameUtils.GetPlayersByJobTag( JobTag.Chief ).FirstOrDefault();

		if ( mayor.IsValid() && mayor.SteamId != requester.SteamId )
		{
			mayor.Info( string.Format( Language.GetPhrase( "governance.warrant.request_notify" ), requester.DisplayName, target.DisplayName, reason ) );
			mayor.SendMessage( Language.GetPhrase( "governance.warrant.use_approve_deny" ) );
		}

		if ( chief.IsValid() && chief.SteamId != requester.SteamId )
		{
			chief.Info( string.Format( Language.GetPhrase( "governance.warrant.request_notify" ), requester.DisplayName, target.DisplayName, reason ) );
			chief.SendMessage( Language.GetPhrase( "governance.warrant.use_approve_deny" ) );
		}

		// Auto approve if no other government players are online
		if ( !GameUtils.GetPlayersByJobTag( JobTag.Government ).Any( p => p != requester ) )
		{
			// No other government players online, auto-approve the warrant
			ApproveWarrant( requester, target );
			return;
		}

		// Notify all government players about the warrant request
		var governmentPlayers = GameUtils.GetPlayersByJobTag( JobTag.Government )
			.Select( x => x.Connection )
			.ToHashSet();
		using ( Rpc.FilterInclude( c => governmentPlayers.Contains( c ) ) )
		{
			var requesterTitle = requester.Job.IsMayoralRole() ? string.Format( Language.GetPhrase( "governance.warrant.requester_title" ), Language.GetPhrase( "roleplay.job.mayor.name" ) ) :
				requester.Job.IsChiefRole() ? string.Format( Language.GetPhrase( "governance.warrant.requester_title" ), Language.GetPhrase( "roleplay.job.policechief.name" ) ) :
				requester.DisplayName;
			Chat.Current?.BroadcastSystemText( string.Format( Language.GetPhrase( "governance.warrant.request_pending" ), requesterTitle, target.DisplayName ) );
		}
	}

	private void ApproveWarrant( Player approver, Player target )
	{
		Assert.True( Networking.IsHost );

		// Check if there's a pending warrant request
		if ( !PendingWarrants.TryGetValue( target.SteamId, out var request ) )
		{
			approver.Error( "#notify.warrant.no.pending" );
			return;
		}

		var requester = GameUtils.GetPlayerById( request.RequesterId );

		// Prevent self-approval
		if ( request.RequesterId == approver.SteamId )
		{
			approver.Error( "#governance.warrant.self_approval" );
			return;
		}

		// Remove from pending
		PendingWarrants.Remove( target.SteamId );

		// Add warrant status
		target.AddStatus( Constants.WarrantStatus );

		_ = ServerApiClient.Audit( "Warrant",
			$"{approver.SteamName} ({approver.SteamId}) approved a warrant for {target.SteamName} ({target.SteamId})",
			approver.SteamId );

		// Notify
		BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.warrant.announcement" ), target.DisplayName ) );
		approver.Success( string.Format( Language.GetPhrase( "governance.warrant.approved" ), target.DisplayName ) );
		target.Info( "#notify.warrant.issued" );

		if ( requester.IsValid() )
		{
			requester.Success( string.Format( Language.GetPhrase( "governance.warrant.approved_requester" ), target.DisplayName, approver.DisplayName ) );
		}
	}

	private void DenyWarrant( Player denier, Player target )
	{
		Assert.True( Networking.IsHost );

		// Check if there's a pending warrant request
		if ( !PendingWarrants.TryGetValue( target.SteamId, out var request ) )
		{
			denier.Error( "#notify.warrant.no.pending" );
			return;
		}

		var requester = GameUtils.GetPlayerById( request.RequesterId );

		// Remove from pending
		PendingWarrants.Remove( target.SteamId );

		_ = ServerApiClient.Audit( "Warrant",
			$"{denier.SteamName} ({denier.SteamId}) denied a warrant request for {target.SteamName} ({target.SteamId})",
			denier.SteamId );

		// Notify
		denier.Success( string.Format( Language.GetPhrase( "governance.warrant.denied" ), target.DisplayName ) );
		if ( requester.IsValid() )
		{
			requester.Error( string.Format( Language.GetPhrase( "governance.warrant.denied_requester" ), target.DisplayName, denier.DisplayName ) );
		}
	}


	[Rpc.Host]
	public void UnWarrant( long targetId, bool notify = true )
	{
		var isHost = Rpc.Caller.IsHost;
		var callerId = Rpc.CallerId;

		if ( !isHost && Cooldown.Current.CheckAndStartCooldown( $"{callerId}:warrant", Config.Current.Game.WarrantCooldown ) )
		{
			return;
		}

		var targetPlayer = GameUtils.GetPlayerById( targetId );

		if ( !targetPlayer.IsValid() )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !isHost && (!callerPlayer.IsValid() || !callerPlayer.Job.IsGovernmentRole()) )
		{
			return;
		}

		Status.Current.RemoveStatus( targetId, Constants.WarrantStatus );

		if ( notify )
		{
			BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.unwarrant.announcement" ), targetPlayer.DisplayName ) );
		}

		targetPlayer.Info( "#notify.unwarrant.player" );
	}

}
