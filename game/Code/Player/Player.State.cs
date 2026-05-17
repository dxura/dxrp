using Dxura.RP.Game.PostProcess;
using Dxura.RP.Game.UI;
using Sandbox.Diagnostics;
using Sandbox.Services;

namespace Dxura.RP.Game;

public enum RespawnState
{
	Not,
	Requested,
	Delayed,
	Approved,
	Immediate
}

public partial class Player : IDescription
{
	/// <summary>
	///     Our local player on this client.
	/// </summary>
	public static Player Local { get; private set; } = null!;

	/// <summary>
	///     The player's SteamID.
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	[Property]
	[ReadOnly]
	[Group( "State" )]
	public long SteamId { get; set; }

	/// <summary>
	///     The player's name, which might have to persist if they leave
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public string? SteamName { get; set; }

	/// <summary>
	///     The player's RP name (set via /rpname)
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public string? RpName { get; set; }

	/// <summary>
	///     The player's preferred title (set via inventory)
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public string? PreferredTitle { get; set; }

	/// <summary>
	///     What are we called?
	/// </summary>
	public string DisplayName
	{
		get
		{
			if ( Config.Current.Game.RpNameEnabled && !string.IsNullOrWhiteSpace( RpName ) )
			{
				return RpName[..Math.Min( Config.Current.Game.RpNameMaxLength, RpName.Length )];
			}

			var name = SteamName ?? "Unknown";
			return name[..Math.Min( 15, name.Length )];
		}
	}

	/// <summary>
	///     The player's playtime (in seconds)
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public int PlayTime { get; set; } = 1000;

	/// <summary>
	///     The connection of this player
	/// </summary>
	public Connection? Connection => Network.Owner;

	public Guid ConnectionId => Connection?.Id ?? Guid.Empty;
	public bool IsConnected => Connection != null && (Connection.IsActive || Connection.IsHost) || IsDebugPlayer;

	/// <summary>
	///     The player's disconnect time
	/// </summary>
	[Property]
	public RealTimeSince? DisconnectedSince { get; set; }

	/// <summary>
	///     How long since the player last respawned?
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public TimeSince TimeSinceLastRespawn { get; private set; }

	/// <summary>
	///     The job this player belongs to.
	/// </summary>
	[Property]
	[Group( "State" )]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnJobPropertyChanged ) )]
	public GameModeJobDto Job { get; set; } = null!;

	public string JobDisplayName => CustomJob ?? Job.DisplayName();

	public TimeSince TimeSinceRespawnStateChanged { get; private set; }

	public DamageInfo? LastDamageInfo { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public bool IsDebugPlayer { get; set; }

	/// <summary>
	///     Unique colour or team color of this player
	/// </summary>
	public Color PlayerColor => Job.ColorValue();

	[Property] public SceneTraceResult CachedEyeTrace { get; private set; }
	private TimeSince _timeSinceLastCachedEyeTrace = 0;

	/// <summary>
	///     Are we ready to respawn?
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnRespawnStateChanged ) )]
	public RespawnState RespawnState { get; set; } = RespawnState.Immediate;

	public bool IsRespawning => RespawnState is RespawnState.Delayed;

	private TimeSince _timeSinceLastTeleport = 0;


	// IDescription
	string IDescription.DisplayName => DisplayName;
	Color IDescription.Color => Job.ColorValue();


	private void OnKillStateHost( Component victim, DamageInfo damage )
	{
		Assert.True( Networking.IsHost );

		LastDamageInfo = damage;

		ArmorComponent.HasHelmet = false;
		ArmorComponent.Armor = 0f;

		RespawnState = RespawnState.Requested;
		TimeSinceRespawnStateChanged = 0f;

		OnDeathEquipment();

		ClearLoadoutHost();

		SetDead( true );
		CreateRagdollHost();
	}

	public float GetEffectiveRespawnElapsed()
	{
		return TimeSinceRespawnStateChanged;
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnKillStateOwner( Component victim, DamageInfo damage )
	{
		LastDamageInfo = damage;

		Achievements.Unlock( "die" );

		Holster();

		if ( EquipmentOverlay.Instance.IsValid() )
		{
			EquipmentOverlay.Instance.IsActive = false;
		}

		Controller.ThirdPerson = true;
	}

	private TimeSince _timeSinceCachedEyeTrace = 0;

	private void OnUpdateStateOwner()
	{
		if ( _timeSinceLastCachedEyeTrace < 0.2f )
		{
			return;
		}

		CachedEyeTrace = Scene.Trace.Ray( AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "invisible", "trigger" )
			.Run();

		_timeSinceLastCachedEyeTrace = 0;
	}

	public void AssignJobHost( GameModeJobDto job )
	{
		Assert.True( Networking.IsHost );

		if ( !job.AssignableTo( this ) )
		{
			return;
		}

		Job = job;

		SpawnHost();
	}

	public void AssignJobForcedHost( GameModeJobDto job )
	{
		Assert.True( Networking.IsHost );

		if ( !job.IsValid() || Job == job )
		{
			return;
		}

		Job = job;
		SpawnHost();
	}

	/// <summary>
	///     Called when <see cref="Job" /> changes across the network.
	/// </summary>
	private void OnJobPropertyChanged( GameModeJobDto before, GameModeJobDto after )
	{
		if ( Networking.IsHost && Config.Current.Game.DropWeaponsOnJobChange && !Restricted )
		{
			DropAllDroppableEquipment();
		}

		// Drop wallet on job change
		if ( Networking.IsHost && WalletBalance > 0 && Config.Current.Game.DropWalletOnJobChange && !Restricted )
		{
			var moneyToDrop = WalletBalance;
			ClearWalletHost();
			GameManager.Instance.DropMoneyHost( moneyToDrop, WorldPosition + Vector3.Up * 30f, $"Job change: {SteamName} ({SteamId})" );
		}

		var handler = JobChangedSound.Play( WorldPosition );
		if ( handler.IsValid() )
		{
			handler.Parent = GameObject;
			handler.FollowParent = true;
		}

		ApplyClothing();

		if ( Networking.IsHost )
		{
			CustomJob = null; // Reset custom job when changing to a different job
			IGameEvents.Post( x => x.OnPlayerJobChangedHost( this, before, after ) );
		}
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void Teleport( Transform transform )
	{
		Rigidbody.Velocity = Vector3.Zero;
		Rigidbody.AngularVelocity = Vector3.Zero;

		Controller.EyeAngles = transform.Rotation.Angles();
		Controller.WishVelocity = Vector3.Zero;
		Controller.GroundVelocity = Vector3.Zero;

		Transform.ClearInterpolation();
		WorldPosition = transform.Position;
		Transform.ClearInterpolation();

		// Teleports can leave occlusion stale until the camera settles on the client.
		OcclusionSystem.Current?.RequestForceCheck();
	}

	public void TeleportHost( Transform transform )
	{
		Assert.True( Networking.IsHost );

		ClearSitStateHost( returnToSavedPosition: false );

		Sentinel.Sentinel.Current.PermitPlayerTeleportHost( SteamId, 3f );

		_timeSinceLastTeleport = 0;

		// Send to client - let them handle the actual teleport
		Teleport( transform );
	}

	public void SpawnHost( bool inPlace = false, Transform? overrideSpawn = null )
	{
		Assert.True( Networking.IsHost );
		
		SetSit( null );

		if ( !inPlace )
		{
			var spawn = overrideSpawn ?? RespawnerSystem.Instance.GetSpawnPoint( this );
			TeleportHost( spawn );
		}

		RespawnState = RespawnState.Not;

		DamageTakenForce = Vector3.Zero;

		if ( HealthComponent.State != LifeState.Alive )
		{
			ArmorComponent.HasHelmet = false;
			ArmorComponent.Armor = 0f;
		}

		HealthComponent.Health = HealthComponent.MaxHealth;
		HealthComponent.State = LifeState.Alive;

		TimeSinceLastRespawn = 0f;

		ClearLoadoutHost();
		EquipDefaultLoadoutHost();

		ResetBody();

		IGameEvents.Post( x => x.OnPlayerSpawnedHost( this ) );

		OnClientSpawn();
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	public void UnlockAchievement( string achievement )
	{
		Achievements.Unlock( achievement );
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	public void IncrementStat( string name, int amount )
	{
		var statKey = DxStats.GetStatKey( name );
		
		// if ( Application.IsEditor )
		// {
		// 	Log.Info( $"[Stat] I would've incremented stat '{statKey}' by {amount} for player '{DisplayName}' but not anymore because we're in the editor." );
		// 	return;
		// }
		
		Stats.Increment( statKey, amount );
	}

	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnClientSpawn()
	{
		CantSwitch = false;
		Controller.UseInputControls = true;
		Controller.ThirdPerson = IsThirdPersonPreferred;
		Rigidbody.Velocity = Vector3.Zero;
		Rigidbody.GravityScale = 1f;

		// Reset status effects that may persist due to network sync race conditions
		HueRotateTarget = 0f;
		Controller.WalkSpeed = GameConfig.WalkSpeed;
		Controller.RunSpeed = GameConfig.RunSpeed;
		Controller.DuckedSpeed = GameConfig.DuckedSpeed;
		Controller.JumpSpeed = GameConfig.JumpSpeed;

		if ( Scene.Camera?.GameObject.Components.Get<DrunkPostProcess>() is { } drunkPostProcess )
		{
			drunkPostProcess.Destroy();
		}
	}

	private void OnRespawnStateChanged( RespawnState oldValue, RespawnState newValue )
	{
		TimeSinceRespawnStateChanged = 0f;
	}

	public void OnPlayerDisconnectHost( long steamId )
	{
		// Only drop money for the disconnecting player
		if ( steamId != SteamId )
		{
			return;
		}

		// Drop money if the player has any
		var moneyToDrop = WalletBalance;
		if ( moneyToDrop != 0 && !Restricted )
		{
			ClearWalletHost();
			GameManager.Instance.DropMoneyHost( moneyToDrop, WorldPosition + Vector3.Up * 30f, $"Player disconnect: {SteamName} ({SteamId})" );
		}
	}

	public Player? GetLastKiller()
	{
		if ( LastDamageInfo == null )
		{
			return null;
		}

		return GameUtils.GetPlayerFromComponent( LastDamageInfo.Attacker );
	}
}
