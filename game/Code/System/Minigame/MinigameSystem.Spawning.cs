using Dxura.RP.Game.Minigame.Minigames;
namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem
{
	private readonly List<GameObject> _localMinigameObjects = new();

	/// <summary>
	/// Spawns the minigame prefabs locally. Same code path for host and clients.
	/// All objects are set to NetworkMode.Never so they stay out of the snapshot.
	/// </summary>
	private void SpawnMinigameLocally( MinigameResource resource, GameObject? secondaryPrefab )
	{
		DestroyLocalMinigameObjects();

		// Create main minigame object
		var root = resource.MainPrefab.IsValid()
			? resource.MainPrefab.Clone( new CloneConfig
			{
				Name = $"Minigame ({resource.Identifier})",
			} )
			: new GameObject( true, $"Minigame ({resource.Identifier})" );

		root.NetworkMode = NetworkMode.Never;
		root.Enabled = true;
		root.WorldPosition = Origin;

		_localMinigameObjects.Add( root );

		// Create secondary prefab (map)
		if ( secondaryPrefab != null && secondaryPrefab.IsValid() )
		{
			var secondaryObject = secondaryPrefab.Clone( new CloneConfig
			{
				Name = $"Minigame Secondary ({resource.Identifier})",
				Parent = root,
			} );

			secondaryObject.NetworkMode = NetworkMode.Never;
			secondaryObject.Enabled = true;

			_localMinigameObjects.Add( secondaryObject );
		}

		// Create lobby
		var lobbyObject = LobbyPrefab.Clone( new CloneConfig
		{
			Name = $"Minigame Lobby ({resource.Identifier})",
			Transform = new Transform( resource.LobbyOffset, Rotation.Identity ),
			Parent = root,
		} );

		lobbyObject.NetworkMode = NetworkMode.Never;
		lobbyObject.Enabled = true;

		_localMinigameObjects.Add( lobbyObject );
	}

	private void DestroyLocalMinigameObjects()
	{
		foreach ( var obj in _localMinigameObjects )
		{
			if ( obj.IsValid() )
				obj.Destroy();
		}
		_localMinigameObjects.Clear();
	}

	/// <summary>
	/// Broadcast RPC: tells all targeted connections to spawn the minigame locally.
	/// Pass the MinigameResource and secondary prefab by reference (serialized as GUID).
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastSpawnMinigame( MinigameResource resource, GameObject? secondaryPrefab )
	{
		SpawnMinigameLocally( resource, secondaryPrefab );
	}

	/// <summary>
	/// Broadcast RPC: tells all targeted connections to destroy their local minigame objects.
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastDestroyMinigame()
	{
		DestroyLocalMinigameObjects();
	}
}
