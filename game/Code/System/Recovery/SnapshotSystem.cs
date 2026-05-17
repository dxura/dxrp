using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Dxura.RP.Game.Tools;
using Dxura.RP.Shared;

namespace Dxura.RP.Game;

/// <summary>
/// Stores persistent world state that can be recovered after a server crash or restart.
/// </summary>
public class SnapshotSystem : GameObjectSystem<SnapshotSystem>, IGameEvents
{
	private const string LogPrefix = "[Snapshot]";

	private Dictionary<long, PlayerSnapshotData>? _playerData;

	private TimeSince _lastCleanupProcess = 0;
	private readonly Dictionary<long, TimeUntil> _cleanupSchedule = new();

	private TimeSince _timeSinceStateSave;

	private Snapshot? _pendingSnapshot;

	public SnapshotSystem( Scene scene ) : base( scene )
	{
		Log.Info( $"{LogPrefix} Initialized" );
		Listen( Stage.SceneLoaded, -1000, () => _ = OnSceneLoaded(), "Load" );
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost || Scene.IsEditor || !Networking.IsActive )
		{
			return;
		}
		
		// If any pending snapshot, process it after a short delay to ensure the scene is stable.
		if ( _pendingSnapshot != null && Time.Now > 5f )
		{
			var file = _pendingSnapshot;
			_pendingSnapshot = null;
			LoadSnapshot( file );
		}

		if ( !Config.Current.Game.SnapshotEnabled )
		{
			return;
		}

		if ( _lastCleanupProcess >= 60f )
		{
			ProcessCleanupSchedule();
			_lastCleanupProcess = 0;
		}

		if ( Time.Now >= Config.Current.Game.SnapshotInitialSaveGrace && 
		     _timeSinceStateSave >= Config.Current.Game.SnapshotSaveInterval 
		     && ServerApiLink.HasAuthorizationKey )
		{
			Log.Info( $"{LogPrefix} Auto-save interval reached ({_timeSinceStateSave:0}s), saving state" );
			SaveSnapshot();
		}
	}

	private void ProcessCleanupSchedule()
	{
		if ( _cleanupSchedule.Count == 0 )
		{
			return;
		}

		foreach ( var (steamId, timeUntil) in _cleanupSchedule.ToList() )
		{
			if ( timeUntil > 0 )
			{
				continue;
			}

			_cleanupSchedule.Remove( steamId );

			// Player reconnected, their cleanup is handled by CleanupSystem on disconnect
			if ( GameUtils.GetPlayerById( steamId ).IsValid() )
			{
				continue;
			}

			CleanupSystem.Current.CleanupPlayer( steamId );
		}
	}

	public async Task SaveSnapshot()
	{
		Log.Info( $"{LogPrefix} Saving world state..." );

		_timeSinceStateSave = 0;
		_playerData = null;
		_pendingSnapshot = null;

		// Collate all data on the main thread
		var snapshot = new Snapshot();

		var savedGameObjects = 0;
		var savedPlayers = 0;
		var errors = 0;

		await GameTask.MainThread();
		
		foreach ( var recoverable in Scene.GetAll<ISnapshot>() )
		{
			if ( !recoverable.GameObject.IsValid() || recoverable.GameObject.Tags.Has( Constants.MapTag ) )
			{
				continue;
			}

			try
			{
				switch ( recoverable.SnapshotType )
				{
					case SnapshotType.GameObject:
						// Serialize this as full game object (SingleNetwork so it doesn't do expensive diffing)
						snapshot.GameObjects.Add( recoverable.GameObject.Serialize(new GameObject.SerializeOptions()
						{
							SingleNetworkObject = true
						}).ToJsonString() );

						savedGameObjects++;
						break;
					case SnapshotType.Player:
						var snapshotPlayer = recoverable.Save();
						if ( snapshotPlayer is PlayerSnapshotData playerData )
						{
							snapshot.Players.Add( playerData );
							savedPlayers++;
						}
						break;
				}
			}
			catch ( Exception e )
			{
				errors++;
				Log.Warning( $"{LogPrefix} Failed to save recoverable: {e.Message}" );
			}
		}

		// Save door ownership state (matched by position on load)
		foreach ( var door in Scene.GetAllComponents<Door>() )
		{
			if ( door.Owner == 0 )
			{
				continue;
			}

			snapshot.Doors.Add( new DoorSnapshotData
			{
				Position = door.WorldPosition,
				Owner = door.Owner,
				Locked = door.Locked
			} );
		}

		await GameTask.Yield();
		await GameTask.MainThread();

		// Create a full server dupe of all constructs (captures wire connections)
		var savedConstructs = 0;
		try
		{
			var allConstructObjects = Scene.GetAll<IConstruct>()
				.Where( c => c.IsValid() && c.GameObject.IsValid() && !c.GameObject.Tags.Has( Constants.MapTag ) )
				.Select( c => c.GameObject )
				.ToList();

			var dupe = DuplicatorTool.Duplicate( allConstructObjects, Vector3.Zero, true );
			if ( dupe != null )
			{
				snapshot.WorldDupe = dupe;
				savedConstructs = dupe.Items.Count();
			}
		}
		catch ( Exception e )
		{
			errors++;
			Log.Warning( $"{LogPrefix} Failed to create server dupe: {e.Message}" );
		}

		await GameTask.WorkerThread();

		try
		{
			var json = Json.Serialize( snapshot );
			var success = await ServerApiClient.SaveSnapshot( new SaveSnapshotDto { Data = json } );

			if ( success )
			{
				Log.Info( $"{LogPrefix} Saved snapshot (gameObjects={savedGameObjects}, constructs={savedConstructs}, players={savedPlayers}, errors={errors})" );
			}
			else
			{
				Log.Error( $"{LogPrefix} Failed to save snapshot to API" );
			}
		}
		catch ( Exception e )
		{
			Log.Error( $"{LogPrefix} Failed to save snapshot: {e.Message}" );
		}
	}

	/// <summary>
	/// Fetches the most recent snapshot from the API and defers the actual restore to the first FixedUpdate tick.
	/// </summary>
	private async Task OnSceneLoaded()
	{
		if ( !Networking.IsHost || Scene.IsEditor || !ServerApiLink.HasAuthorizationKey )
		{
			return;
		}

		if ( Scene.Components.GetAll<WaitingRoom>( FindMode.EverythingInSelfAndDescendants ).Any() )
		{
			return;
		}

		if ( string.IsNullOrWhiteSpace( ServerApiLink.Token ) )
		{
			Log.Warning( $"{LogPrefix} Skipping snapshot load, missing server API token" );
			return;
		}

		Log.Info( $"{LogPrefix} Loading world state..." );

		// Fetch and deserialize on worker thread
		await GameTask.WorkerThread();

		SnapshotResponseDto? snapshot;
		try
		{
			snapshot = await ServerApiClient.GetSnapshot();
		}
		catch ( Exception e )
		{
			Log.Error( $"{LogPrefix} Failed to fetch snapshot from API: {e.Message}" );
			return;
		}

		if ( snapshot == null )
		{
			Log.Info( $"{LogPrefix} No snapshot found, starting with fresh state" );
			return;
		}
		
		Snapshot? file;
		try
		{
			file = Json.Deserialize<Snapshot>( snapshot.Data );
		}
		catch ( Exception e )
		{
			Log.Error( $"{LogPrefix} Failed to deserialize snapshot: {e.Message}" );
			return;
		}

		if ( file == null )
		{
			Log.Info( $"{LogPrefix} Snapshot was empty, skipping load" );
			return;
		}

		// Hop back to main thread to set pending snapshot
		await GameTask.MainThread();

		Log.Info( $"{LogPrefix} Loading from snapshot" );
		_pendingSnapshot = file;
	}

	/// <summary>
	/// Restores the full world state from a snapshot. Called from FixedUpdate once the scene is ready.
	/// </summary>
	private void LoadSnapshot( Snapshot file )
	{
		// Load game objects
		Log.Info( $"{LogPrefix} Restoring GameObjects..." );

		var gameObjectsAttempted = file.GameObjects.Count;
		var gameObjectsRestored = 0;
		var gameObjectsInvalid = 0;
		var gameObjectsErrors = 0;

		foreach ( var json in file.GameObjects )
		{
			try
			{
				if ( JsonNode.Parse( json ) is not JsonObject obj )
				{
					continue;
				}

				var go = new GameObject();
				go.Deserialize( obj );

				if ( !go.IsValid() )
				{
					gameObjectsInvalid++;
					Log.Warning( $"{LogPrefix} Failed to restore GameObject, deserialization resulted in invalid object" );
					continue;
				}

				// Remove rigidbody to prevent physics issues on spawn
				var rigidbody = go.GetComponent<Rigidbody>();
				if ( rigidbody.IsValid() )
				{
					rigidbody.Destroy();
				}

				if ( go.NetworkMode == NetworkMode.Object )
				{
					go.NetworkSpawn();
				}

				gameObjectsRestored++;
			}
			catch ( Exception e )
			{
				gameObjectsErrors++;
				Log.Warning( $"{LogPrefix} Failed to restore GameObject: {e.Message}" );
			}
		}

		Log.Info( $"{LogPrefix} GameObject restore complete (attempted={gameObjectsAttempted}, restored={gameObjectsRestored}, invalid={gameObjectsInvalid}, errors={gameObjectsErrors})" );

		// Load constructs + wire connections from dupe
		if ( file.WorldDupe != null )
		{
			var dupeItemCount = file.WorldDupe.Items.Count();
			if ( dupeItemCount > 0 )
			{
				Log.Info( $"{LogPrefix} Restoring {dupeItemCount} constructs and {file.WorldDupe.WireConnections.Count()} wire connections." );
				_ = Construct.Current.SpawnDupeItems(
					file.WorldDupe,
					null,
					enforceLimits: false,
					addUndo: true
				);
			}
			else
			{
				Log.Info( $"{LogPrefix} Snapshot dupe is empty, no constructs to restore" );
			}
		}
		else
		{
			Log.Info( $"{LogPrefix} No dupe data in snapshot file, no constructs to restore" );
		}

		// Restore door ownership by matching on position
		if ( file.Doors.Count > 0 )
		{
			var doors = Scene.GetAllComponents<Door>().ToList();
			var doorsRestored = 0;

			foreach ( var doorData in file.Doors )
			{
				var door = doors.FirstOrDefault( d => d.WorldPosition.AlmostEqual( doorData.Position, 1f ) );
				if ( door == null )
				{
					continue;
				}

				door.Owner = doorData.Owner;
				door.Locked = doorData.Locked;
				doorsRestored++;
			}

			Log.Info( $"{LogPrefix} Restored {doorsRestored}/{file.Doors.Count} door ownerships" );
		}

		// Load and prep players
		_playerData = file.Players.ToDictionary( p => p.SteamId, p => p );
		Log.Info( $"{LogPrefix} Loaded {_playerData.Count} player snapshot entries" );


		// Schedule cleanup
		foreach ( var owned in Scene.GetAll<IOwned>() )
		{
			if ( _cleanupSchedule.ContainsKey( owned.Owner ) )
			{
				continue;
			}

			_cleanupSchedule[owned.Owner] = Config.Current.Game.GraceReconnectTime * 2;
		}

		// Also cover all items (might still be spawning)
		foreach ( var dupeItem in file.WorldDupe?.Items ?? [] )
		{
			if ( _cleanupSchedule.ContainsKey( dupeItem.Owner ) )
			{
				continue;
			}

			_cleanupSchedule[dupeItem.Owner] = Config.Current.Game.GraceReconnectTime;
		}

		// Don't cleanup Id 0 as it's used for world-owned entities
		_cleanupSchedule.Remove( 0 );

		Log.Info( $"{LogPrefix} Scheduled cleanup for {_cleanupSchedule.Count} players (grace={Config.Current.Game.GraceReconnectTime}s)" );
	}

	public PlayerSnapshotData? TakePlayerData( long steamId )
	{
		if ( _playerData == null || !_playerData.Remove( steamId, out var data ) )
		{
			Log.Info( $"{LogPrefix} No snapshot data available for player {steamId}" );
			return null;
		}

		_cleanupSchedule.Remove( steamId );
		Log.Info( $"{LogPrefix} Provided snapshot data for player {steamId}" );
		return data;
	}
	
	[Rpc.Host]
	public void SnapshotManualHost()
	{
		if ( !Networking.IsHost || Scene.IsEditor )
		{
			return;
		}

		var caller = Rpc.Caller;
		if ( !RankSystem.HasPermission( caller.SteamId, Permission.ManageSnapshot ) )
		{
			return;
		}

		if ( !Config.Current.Game.SnapshotEnabled )
		{
			return;
		}

		SaveSnapshot();
		caller.SendLog( LogLevel.Info, "[Snapshot] Saved snapshot.json" );
	}
}
