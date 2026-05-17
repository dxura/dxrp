using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public partial class Governance : IGameEvents
{
	[Property]
	[Sync( SyncFlags.FromHost )]
	public NetDictionary<long, RealTimeUntil> Prisoners { get; set; } = new();

	private TimeSince _lastCheck = 0;

	private void OnSecondlyUpdateJail()
	{
		if ( !Networking.IsHost || Prisoners.Count == 0 || _lastCheck < 1 )
		{
			return;
		}

		_lastCheck = 0;

		// Check for prisoners that have served their time
		foreach ( var prisoner in Prisoners.Keys.ToList() )
		{
			// Buffer time to avoid instant release
			if ( Prisoners[prisoner].Passed <= 2.5f )
			{
				continue;
			}

			// Release prisoner if due
			if ( Prisoners[prisoner] <= 0 )
			{
				Release( prisoner );
				continue;
			}

			// Check if prisoner has escaped (ignore political prisoners)
			var player = GameUtils.GetPlayerById( prisoner );
			if ( player.IsValid() && !player.Job.IsPoliticalPrisonerRole() )
			{
				var tr = Scene.Trace.Ray( player.WorldPosition, player.WorldPosition + Vector3.Up * 1000 )
					.IgnoreGameObject( player.GameObject )
					.WithoutTags( Constants.TraceIgnoreTags )
					.WithoutTags( Constants.PlayerTag, Constants.RagdollTag, Constants.NoCollideTag )
					.Run();
				
				if ( !tr.Hit || tr.Hit && tr.GameObject.Tags.Contains( "skybox" ) )
				{
					Release( prisoner, true );
				}
			}
		}
	}

	public bool ValidateArrest( Player targetPlayer, Player arrestingPlayer )
	{
		// Why would someone not in government be arresting?
		if ( !arrestingPlayer.Job.IsGovernmentRole() )
		{
			return false;
		}

		// Cannot arrest political prisoners
		if ( targetPlayer.Job.IsPoliticalPrisonerRole() )
		{
			arrestingPlayer.Error( "#governance.jail.political.cannot_arrest" );
			return false;
		}

		// Check if arresting player is mayor
		var isMayor = arrestingPlayer.Job.IsMayoralRole();

		// Mayor can only arrest police officers
		if ( isMayor )
		{
			if ( !targetPlayer.Job.IsPoliceRole() )
			{
				arrestingPlayer.Error( "#governance.jail.mayor.only_police" );
				return false;
			}
		}
		else
		{
			// Non-mayor government officials cannot arrest other government
			if ( targetPlayer.Job.IsGovernmentRole() )
			{
				arrestingPlayer.Error( "#governance.jail.government.cannot_arrest" );
				return false;
			}
		}

		// Check dist
		var distance = arrestingPlayer.WorldPosition.Distance( targetPlayer.WorldPosition );

		if ( distance > Config.Current.Game.ReachDistance * 0.80f )
		{
			arrestingPlayer.Error( "#governance.jail.arrest.too_far" );
			return false;
		}


		return true;
	}

	public void Arrest( long steamId, float jailTime, GameModeJobDto? jobOverride = null )
	{
		Assert.True( Networking.IsHost );

		var player = GameUtils.GetPlayerById( steamId );
		if ( !player.IsValid() )
		{
			return;
		}

		// If already arrested
		if ( Prisoners.ContainsKey( steamId ) )
		{
			player.Restricted = true;
			Prisoners[steamId] = jailTime;

			// If this is a political arrest, ensure the political prisoner state is fully applied.
			if ( jobOverride.IsPoliticalPrisonerRole() )
			{
				player.Job = jobOverride;
				player.RemoveStatus( Constants.PrisonerStatus );
				player.AddStatus( Constants.GaggedStatus, jailTime );
			}
			else
			{
				player.AddStatus( Constants.PrisonerStatus );
			}

			player.SpawnHost();
			return;
		}

		// Override job if specified (for political prisoners)
		if ( jobOverride != null )
		{
			player.Job = jobOverride;
		}

		player.Restricted = true;

		// Political prisoners are gagged; normal arrests just get the prisoner status
		if ( jobOverride.IsPoliticalPrisonerRole() )
		{
			player.AddStatus( Constants.GaggedStatus, jailTime );
		}
		else
		{
			player.AddStatus( Constants.PrisonerStatus );
		}

		// Put in jail cell
		player.SpawnHost();

		// Add to prisoner list
		Prisoners.Add( steamId, jailTime );

		// Clear wanted status for  arrests
		Unwanted( steamId, false );
	}

	[Rpc.Host]
	public void ArrestHost( long steamId )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:{steamId}:arrest", Config.Current.Game.ArrestCooldown ) )
		{
			return;
		}

		var arrestedPlayer = GameUtils.GetPlayerById( steamId );
		var arrestingPlayer = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !arrestedPlayer.IsValid() || !arrestingPlayer.IsValid() )
		{
			return;
		}

		var validArrest = ValidateArrest( arrestedPlayer, arrestingPlayer );
		if ( !validArrest )
		{
			return;
		}

		// If target is s, force them out
		arrestedPlayer.SetSit( null );

		// Check if mayor is arresting a police officer
		var isMayorArrest = arrestingPlayer.Job.IsMayoralRole() && arrestedPlayer.Job.IsPoliceRole();

		if ( isMayorArrest )
		{
			// Demote police officer to citizen instead of jailing
			arrestedPlayer.AssignJobHost( GameModeJobs.Default );

			arrestingPlayer.Success( string.Format( Language.GetPhrase( "governance.jail.demoted.actor" ), arrestedPlayer.DisplayName ) );
			arrestedPlayer.Warn( string.Format( Language.GetPhrase( "governance.jail.demoted.target" ), arrestingPlayer.DisplayName ) );

			Log.Info( $"{arrestingPlayer.DisplayName} has demoted {arrestedPlayer.DisplayName} from police." );
			_ = ServerApiClient.Audit( "Demote", $"{arrestingPlayer.SteamName} ({arrestingPlayer.SteamId}) has demoted {arrestedPlayer.SteamName} ({arrestedPlayer.SteamId}) from police (Mayor).", arrestedPlayer.SteamId );
			BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.jail.demoted.announcement" ), arrestedPlayer.DisplayName, arrestingPlayer.DisplayName ) );
		}
		else
		{
			// Normal arrest behavior
			Arrest( steamId, Config.Current.Game.JailTime );

			arrestingPlayer.Success( string.Format( Language.GetPhrase( "governance.jail.arrested.actor" ), arrestedPlayer.DisplayName ) );
			arrestedPlayer.Warn( string.Format( Language.GetPhrase( "governance.jail.arrested.target" ), arrestingPlayer.DisplayName ) );

			Log.Info(
				$"{arrestingPlayer.DisplayName} has arrested {arrestedPlayer.DisplayName}. Jail time: {Config.Current.Game.JailTime} seconds." );
			_ = ServerApiClient.Audit( "Arrest", $"{arrestingPlayer.SteamName} ({arrestingPlayer.SteamId}) has arrested {arrestedPlayer.SteamName} ({arrestedPlayer.SteamId}) for {Config.Current.Game.JailTime} seconds.", arrestingPlayer.SteamId );
			BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.jail.arrested.announcement" ), arrestedPlayer.DisplayName, arrestingPlayer.DisplayName, Config.Current.Game.JailTime / 60 ) );
		}
	}

	[Rpc.Host]
	public void ReleaseHost( long steamId, bool inPlace = false )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:{steamId}:unarrest", Config.Current.Game.UnArrestCooldown ) )
		{
			return;
		}

		var unarrestingPlayer = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !unarrestingPlayer.IsValid() || !unarrestingPlayer.Job.IsGovernmentRole() )
		{
			return;
		}

		var prisoner = GameUtils.GetPlayerById( steamId );

		if ( !prisoner.IsValid() || prisoner.Job.IsPoliticalPrisonerRole() )
		{
			return;
		}

		// Check dist
		var distance = unarrestingPlayer.WorldPosition.Distance( prisoner.WorldPosition );

		if ( distance > Config.Current.Game.ReachDistance )
		{
			unarrestingPlayer.Error( "#governance.jail.release.too_far" );
			return;
		}

		Release( steamId, inPlace );
	}

	public void Release( long steamId, bool inPlace = false )
	{
		Assert.True( Networking.IsHost );

		if ( !Prisoners.ContainsKey( steamId ) )
		{
			return;
		}

		var prisoner = GameUtils.GetPlayerById( steamId );
		if ( prisoner.IsValid() )
		{
			prisoner.Restricted = false;
			prisoner.RemoveStatus( Constants.PrisonerStatus );

			// If political prisoner, set to citizen and lift the gag
			if ( prisoner.Job.IsPoliticalPrisonerRole() )
			{
				prisoner.Job = GameModeJobs.GetByTagOrFallback( JobTag.Citizen, "Citizen" );
				prisoner.RemoveStatus( Constants.GaggedStatus );
			}

			if ( inPlace )
			{
				prisoner.ClearLoadoutHost();
				prisoner.EquipDefaultLoadoutHost();
			}
			else
			{
				prisoner.SpawnHost();
			}

			Log.Info( $"{prisoner.DisplayName} has been released from jail." );
		}

		Prisoners.Remove( steamId );
	}

	public void ArrestPolitical( long steamId, TimeSpan duration, string reason, bool silent = false )
	{
		Assert.True( Networking.IsHost );

		var player = GameUtils.GetPlayerById( steamId );
		if ( !player.IsValid() )
		{
			return;
		}

		// Use shared arrest logic
		Arrest( steamId, (float)duration.TotalSeconds, GameModeJobs.GetByTagOrFallback( JobTag.PoliticalPrisoner, "Political Prisoner" ) );

		var durationText = $"for {duration.TotalMinutes} minutes";
		if ( !silent )
		{
			Log.Info( $"{player.DisplayName} has been made a political prisoner {durationText}." );
			_ = ServerApiClient.Audit( "PoliticalPrisoner", $"{player.SteamName} ({player.SteamId}) has been made a political prisoner {durationText}." );

			player.SendMessage( string.Format( Language.GetPhrase( "governance.jail.political.target" ), duration.TotalMinutes, reason ) );
		}
	}

	public double GetLocalJailTimeRemaining()
	{
		var jailTimeRemaining = !Prisoners.TryGetValue( Player.Local.SteamId, out var value ) ? 0 : value.Relative;
		if ( jailTimeRemaining < 0 )
		{
			jailTimeRemaining = 0;
		}

		return jailTimeRemaining;
	}

	[ConCmd( "dx_dev_jail_political" )]
	private static void DxDevJailPolitical()
	{
#if !DEBUG
			Log.Info("Command only available in debug mode.");
			return;
#endif

		var player = Player.Local;
		if ( !player.IsValid() )
		{
			return;
		}

		Log.Info( "Jailing local player politically for 60 seconds." );
		Current.ArrestPolitical( player.SteamId, TimeSpan.FromMinutes( 1 ), "RDM" );
	}

	[ConCmd( "dx_dev_jail" )]
	private static void DxDevJail()
	{
#if !DEBUG
			Log.Info("Command only available in debug mode.");
			return;
#endif

		var player = Player.Local;
		if ( !player.IsValid() )
		{
			return;
		}

		Log.Info( "Jailing local player for 60 seconds." );
		Current.Arrest( player.SteamId, 60 );
	}
}
