using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public partial class Governance
{
	[Rpc.Host]
	public void Wanted( long targetId, string reason )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:wanted", Config.Current.Game.WantedCooldown ) )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );
		var targetPlayer = GameUtils.GetPlayerById( targetId );

		if ( !callerPlayer.IsValid() ||
		     !targetPlayer.IsValid() )
		{
			return;
		}

		WantedHost( callerPlayer, targetPlayer, reason );
	}

	public void WantedHost( Player wanter, Player wanted, string reason )
	{
		Assert.True( Networking.IsHost );

		if ( !wanter.IsValid() ||
		     !wanted.IsValid() )
		{
			return;
		}

		if ( wanted.Job.IsGovernmentRole() ||
		     !wanter.Job.IsGovernmentRole() )
		{
			wanter.Error( "#generic.forbidden" );
			return;
		}

		reason = GameManager.ModerateText( wanter.SteamId, "wanted", reason );

		if ( Cooldown.Current.CheckAndStartCooldown( $"{wanted.SteamId}:wanted", Config.Current.Game.PlayerWantedCooldown ) )
		{
			wanter.Error( "#notify.wanted.player.cooldown" );
			return;
		}

		InternalWanted( wanter, wanted, reason );
	}

	private void InternalWanted( Player wanter, Player target, string reason )
	{
		Assert.True( Networking.IsHost );

		var isAlreadyWanted = target.HasStatus( Constants.WantedStatus );

		if ( isAlreadyWanted )
		{
			return;
		}

		target.AddStatus( Constants.WantedStatus );
		BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.wanted.announcement" ), target.DisplayName, reason ) );
		target.Info( "#notify.wanted.player" );
		_ = ServerApiClient.Audit( "Wanted", $"{wanter.SteamName} ({wanter.SteamId}) wanted {target.SteamName} ({target.SteamId}): {reason}", wanter.SteamId );

	}


	[Rpc.Host]
	public void Unwanted( long targetId, bool notify = true )
	{
		var isHost = Rpc.Caller.IsHost;
		var callerId = Rpc.CallerId;

		if ( !isHost && Cooldown.Current.CheckAndStartCooldown( $"{callerId}:wanted", Config.Current.Game.WantedCooldown ) )
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

		Status.Current.RemoveStatus( targetId, Constants.WantedStatus );

		if ( notify )
		{
			BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.unwanted.announcement" ), targetPlayer.DisplayName ) );
		}

		targetPlayer.Info( "#notify.unwanted.player" );
	}

}
