namespace Dxura.RP.Game;

public class MinigameObjectAreaSpawner : Component
{
	[Property]
	public GameObject Prefab { get; set; }

	[Property]
	private float Frequency { get; set; } = 1;

	[Property] public BoxCollider SpawnArea { get; set; }

	private TimeSince _timeSinceLastSpawn = 0;

	protected override void OnFixedUpdate()
	{
		if ( _timeSinceLastSpawn < Frequency )
		{
			return;
		}
		_timeSinceLastSpawn = 0;

		var spawnPos = SpawnArea.LocalBounds.RandomPointInside;

		var spawnedObject = Prefab.Clone( new CloneConfig
		{
			Name = "Chaos Ball", Parent = GameObject, Transform = new Transform( spawnPos, Rotation.Random ),
		} );
		spawnedObject.NetworkMode = NetworkMode.Never;

	}
}
