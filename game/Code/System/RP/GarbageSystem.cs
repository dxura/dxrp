namespace Dxura.RP.Game;

public class GarbageSystem : SingletonComponent<GarbageSystem>, IGameEvents
{
	[Property]
	public List<Model> GarbageModels { get; set; } = new();

	private TimeUntil NextSpawnTime { get; set; }
	private List<GarbagePoint> _garbagePoints = new();

	protected override void OnStart()
	{
		if ( !Config.Current.Game.GarbageEnabled )
		{
			Destroy();
			return;
		}

		NextSpawnTime = Config.Current.Game.GarbageSpawnInterval;

		RefreshGarbagePoints();
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		// Do we need to spawn garbage?
		if ( !NextSpawnTime )
		{
			return;
		}
		NextSpawnTime = Config.Current.Game.GarbageSpawnInterval;

		if ( _garbagePoints.Count == 0 )
		{
			return;
		}

		var garbageCount = Scene.FindAllWithTag( Constants.GarbageTag ).Count();
		if ( garbageCount >= Config.Current.Game.MaxGarbageCount )
		{
			return; // Don't spawn more garbage if we reached the limit
		}

		// Pick a random garbage point
		var garbagePoint = Random.Shared.FromList( _garbagePoints! );
		if ( !garbagePoint.IsValid() )
		{
			return;
		}

		// Spawn garbage at the point
		var garbage = new GameObject
		{
			Name = "Garbage", WorldPosition = garbagePoint.WorldPosition, WorldRotation = Rotation.Random, NetworkMode = NetworkMode.Object
		};
		garbage.Tags.Add( Constants.GarbageTag );
		garbage.Tags.Add( Constants.EntityTag );
		garbage.Tags.Add( Constants.HandsInteractTag );
		garbage.Tags.Add( Constants.PocketItemTag );
		garbage.Tags.Add( Constants.OccludableTag );

		garbage.Network.SetOrphanedMode( NetworkOrphaned.Host );

		var timedDestroy = garbage.AddComponent<TimedDestroyComponent>();
		timedDestroy.ServerSideOnly = true;
		timedDestroy.Time = Config.Current.Game.GarbageDespawnTime;

		var garbageModelRenderer = garbage.AddComponent<ModelRenderer>();
		var selectedModel = Random.Shared.FromList( GarbageModels! );
		garbageModelRenderer.Model = selectedModel;

		garbage.AddComponent<ModelCollider>();
		var rigidbody = garbage.AddComponent<Rigidbody>();
		rigidbody.MotionEnabled = false;
		rigidbody.RigidbodyFlags |= RigidbodyFlags.DisableCollisionSounds;

		garbage.NetworkSpawn();

		// Enable motion after spawning for the server to simulate
		rigidbody.MotionEnabled = true;
		rigidbody.ApplyImpulse( Vector3.Random * 500f * rigidbody.Mass );
	}

	public void OnMapFitted()
	{
		RefreshGarbagePoints();
	}

	private void RefreshGarbagePoints()
	{
		_garbagePoints = Scene.GetAllComponents<GarbagePoint>().ToList();
	}
}
