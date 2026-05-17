namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem : SingletonComponent<MinigameSystem>, IGameEvents
{
	[Property]
	private Vector3 Origin { get; set; } = new( 100_000, 100_000, 0 );

	[Property]
	public required GameObject LobbyPrefab { get; set; }

	public bool IsMinigameActive()
	{
		return CurrentMinigame != null;
	}

	protected override void OnStart()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( !Config.Current.Game.MinigamesEnabled )
		{
			Destroy();
			return;
		}

		OnStartAuto();
		CreateMinigameKillbox();
	}

	private void CreateMinigameKillbox()
	{
		// Create kill plane
		var killPlane = new GameObject( "Minigame Kill Plane" );
		killPlane.SetParent( GameObject );
		killPlane.WorldPosition = Origin + new Vector3( 0, 0, -2000f );
		var planeCollider = killPlane.AddComponent<BoxCollider>();
		planeCollider.Scale = new Vector3( 15_000f, 15_000f, 500f );
		planeCollider.IsTrigger = true;
		killPlane.AddComponent<KillBox>();
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		OnSecondlyUpdateAuto();
		OnSecondlyUpdateState();
		OnSecondlyUpdateEffects();

		if ( CurrentMinigame != null && CurrentState == MinigameState.Playing )
		{
			OnSecondlyUpdateWin();
			OnSecondlyUpdateRespawn();
		}
	}

	/// <summary>
	///  Clears the current minigame and resets all related data.
	/// </summary>
	private void Clear()
	{
		ClearState();
		ClearEffects();

		// Minor stuff
		_playerStashedState.Clear();
		_playerRespawnTimers.Clear();
		_lastSpawnIndex = 0;

		ClearWin();

		// TEMP REMOVE GIBS
		foreach ( var gib in Scene.GetAllComponents<Gib>() )
		{
			gib.GameObject.Destroy();
		}
	}
}
