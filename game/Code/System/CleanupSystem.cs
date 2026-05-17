using Dxura.RP.Shared;
using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public class CleanupSystem( Scene scene ) : GameObjectSystem<CleanupSystem>( scene ), IGameEvents
{
	private TimeSince LastDisconnectCleanupTime { get; set; } = 0;

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost || Scene.IsEditor || !Networking.IsActive )
		{
			return;
		}

		if ( !(LastDisconnectCleanupTime.Relative > 1) )
		{
			return;
		}

		// Cleanup disconnected players
		foreach ( var player in GameUtils.Players
			.Where( player => player.DisconnectedSince.HasValue &&
			                  (!Config.Current.Game.GraceReconnectEnabled || player.DisconnectedSince.Value.Relative > Config.Current.Game.GraceReconnectTime) ) )
		{
			CleanupPlayer( player.SteamId );
			Log.Info( $"Cleaned up disconnected player {player.DisplayName} ({player.SteamId})" );
		}

		LastDisconnectCleanupTime = 0;
	}

	[Rpc.Host]
	public void ClearConstructsHost( ConstructType? type )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:clear:constructs", Config.Current.Game.UtilityClearCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		CleanupConstructs( player.SteamId, type );
		player.Success( "Cleared" );
		Log.Info( $"Player {player.DisplayName} cleaned up {type?.ToString() ?? "all"} constructs" );
	}

	public void CleanupPlayer( long steamId )
	{
		Assert.True( Networking.IsHost );

		Log.Info( $"Cleaning up player {steamId}" );

		SellDoors( steamId );
		CleanupConstructs( steamId );
		CleanupEntities( steamId );

		IGameEvents.Post( x => x.OnPlayerDisconnectHost( steamId ) );

		var player = GameUtils.GetPlayerById( steamId );
		GameNetworkManager.Instance.Players.Remove( steamId );

		if ( player.IsValid() )
		{
			player.GameObject.Root.Destroy();
		}
	}

	private void SellDoors( long steamId )
	{
		foreach ( var door in Scene.GetAll<Door>() )
		{
			if ( door.Owner == steamId )
			{
				door.ForceSell();
			}
		}
	}

	public void CleanupConstructs( long steamId, ConstructType? type = null )
	{
		foreach ( var construct in Scene.GetAll<IConstruct>().Where( x => x.Owner == steamId ) )
		{
			if ( type.HasValue && construct.Type != type.Value )
			{
				continue;
			}

			construct.Destroy();
		}
	}

	public void CleanupEntities( long steamId, bool ignoreConditions = false )
	{
		foreach ( var entity in Scene.GetAll<BaseEntity>() )
		{
			if ( entity.Owner != steamId )
			{
				continue;
			}

			if ( !ignoreConditions && entity.Resource is { DestroyOnDisconnect: false } )
			{
				continue;
			}

			entity.GameObject.Root.Destroy();
		}
	}

	public void CleanupAllConstructs( ConstructType? type = null )
	{
		foreach ( var construct in Scene.GetAll<IConstruct>().ToList() )
		{
			if ( construct.Owner == 0 )
				continue;

			if ( type.HasValue && construct.Type != type.Value )
				continue;

			construct.Destroy();
		}
	}

	public void CleanupAllEntities()
	{
		foreach ( var entity in Scene.GetAll<BaseEntity>().ToList() )
		{
			if ( entity.Owner == 0 )
				continue;

			entity.GameObject.Root.Destroy();
		}
	}

	public void CleanupJobEntities( long steamId, GameModeJobDto newJob )
	{
		foreach ( var entity in Scene.GetAll<BaseEntity>() )
		{
			if ( entity.Owner != steamId )
			{
				continue;
			}

			if ( !entity.DestroyOnJobChange )
			{
				continue;
			}

			entity.GameObject.Root.Destroy();
		}
	}
}
