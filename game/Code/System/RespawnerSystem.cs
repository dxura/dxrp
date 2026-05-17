namespace Dxura.RP.Game;

public class RespawnerSystem : SingletonComponent<RespawnerSystem>, IGameEvents
{
	private List<Transform> SpawnPoints { get; } = new();
	private Dictionary<GameModeJobDto, List<Transform>> JobSpawnPoints { get; } = new();

	private bool _mapFitted;

	protected override void OnStart()
	{
		// Without a server authorization key, there's no API-provided map fitting - allow spawning immediately.
		if ( !ServerApiLink.HasAuthorizationKey )
		{
			_mapFitted = true;
		}

		RefreshSpawnPoints();
	}

	public Transform GetSpawnPoint( Player? player = null )
	{
		if ( player is not null )
		{
			var jobSpecificSpawns = JobSpawnPoints.GetValueOrDefault( player.Job );
			if ( jobSpecificSpawns is { Count: > 0 } )
			{
				var jobSpawnPoint = Sandbox.Game.Random.FromList( jobSpecificSpawns );
				return new Transform( jobSpawnPoint.Position, jobSpawnPoint.Rotation );
			}

			// If player is restricted, use jail spawn points if available
			if ( player.Restricted )
			{
				var jailSpawns = JobSpawnPoints.GetValueOrDefault( GameModeJobs.GetByTagOrFallback( JobTag.PoliticalPrisoner, "Political Prisoner" ) );
				if ( jailSpawns is { Count: > 0 } )
				{
					var jailSpawnPoint = Sandbox.Game.Random.FromList( jailSpawns );
					return new Transform( jailSpawnPoint.Position, jailSpawnPoint.Rotation );
				}
			}
		}

		var spawnPoint = Sandbox.Game.Random.FromList( SpawnPoints );
		return new Transform( spawnPoint.Position, spawnPoint.Rotation );
	}

	public void OnMapFitted()
	{
		_mapFitted = true;
		RefreshSpawnPoints();
	}

	private void RefreshSpawnPoints()
	{
		SpawnPoints.Clear();
		JobSpawnPoints.Clear();

		var officialSpawns = Scene.GetAllComponents<WorldSpawnPoint>().ToList();

		// Prioritize official WorldSpawnPoint components if available
		if ( officialSpawns.Count > 0 )
		{
			foreach ( var spawnPoint in officialSpawns )
			{
				if ( spawnPoint.Job is not null )
				{
					JobSpawnPoints.TryAdd( spawnPoint.Job, new List<Transform>() );
					JobSpawnPoints[spawnPoint.Job].Add( new Transform( spawnPoint.WorldPosition, spawnPoint.WorldRotation ) );
				}
				else
				{
					SpawnPoints.Add( new Transform( spawnPoint.WorldPosition, spawnPoint.WorldRotation ) );
				}
			}
		}

		// Fallback to legacy spawn points if no official ones found
		if ( SpawnPoints.Count == 0 )
		{
			var backupSpawns = Scene.GetAllComponents<SpawnPoint>().ToList();
			foreach ( var spawnPoint in backupSpawns )
			{
				SpawnPoints.Add( new Transform( spawnPoint.WorldPosition, spawnPoint.WorldRotation ) );
			}
		}

		// Last resort fallback if no spawn points found at all
		if ( SpawnPoints.Count == 0 )
		{
			Log.Warning( "No spawn points found in the scene!" );
			SpawnPoints.Add( new Transform( WorldPosition, WorldRotation ) );
		}
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		foreach ( var player in GameUtils.Players.ToList() )
		{
			switch ( player.RespawnState )
			{
				case RespawnState.Requested:
					player.RespawnState = RespawnState.Delayed;
					break;

				case RespawnState.Delayed:
					if ( player.GetEffectiveRespawnElapsed() > Config.Current.Game.RespawnTime )
					{
						var newState = player.IsDebugPlayer ? RespawnState.Immediate : RespawnState.Approved;

						HandleDemotions( player );

						player.RespawnState = newState;
					}

					break;

				case RespawnState.Immediate:
					if ( Config.Current.Game.MapFittingEnabled && !_mapFitted )
						break;
					player.SpawnHost();
					break;
			}
		}

	}

	[Rpc.Host]
	public void RequestRespawn()
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:respawn:request", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		if ( player.RespawnState != RespawnState.Approved )
		{
			player.Warn( "You can't respawn yet..." );
			return;
		}

		player.RespawnState = RespawnState.Immediate;
	}


	private void HandleDemotions( Player player )
	{
		if ( !player.IsValid() || !player.Job.DemoteOnRespawn )
		{
			return;
		}

		Log.Info( $"Demoting player {player.DisplayName} due to death" );

		// Demote to citizen
		player.Job = GameModeJobs.GetByTagOrFallback( JobTag.Citizen, "Citizen" );
	}
}
