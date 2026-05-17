using Dxura.RP.Shared;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public sealed partial class Player : Component, IEquipmentEvents, IDamageEvents, IAreaDamageReceiver, IGameEvents,
	PlayerController.IEvents, IGameObjectNetworkEvents, ISnapshot
{
	[Property] public required PlayerController Controller { get; set; }
	public bool IsRunning => Controller.Velocity.Length >= Controller.RunSpeed - 1;


	/// <summary>
	///     Is this the local player for this client
	/// </summary>
	public bool IsLocalPlayer => !IsProxy && Connection == Connection.Local;

	protected override void OnStart()
	{
		OnStartBody();
		OnStartInteract();

		ApplyClothing();

		if ( !IsLocalPlayer )
		{
			return;
		}

		// Set Local reference since this is our player
		Local = this;

		SetupCamera();

		OnStartInventory();
	}

	protected override void OnUpdate()
	{
		OnUpdateBody();
		OnUpdateEffects();
		OnUpdateEmote();

		if ( !IsLocalPlayer )
		{
			return;
		}

		OnUpdateEquipment();
		OnUpdateCamera();
		OnUpdatePresence();
		OnUpdateRoleplay();
		OnStatusesUpdateOwner();
		OnUpdateAimRay();
		OnUpdateEquipmentSpread();
		OnUpdateStateOwner();
	}

	public void OnSecondlyUpdate()
	{
		if ( !IsLocalPlayer )
		{
			return;
		}

		OnSecondlyUpdateApi();
	}

	public void NetworkOwnerChanged( Connection? newOwner, Connection? previousOwner )
	{
		var hasNetworkOwner = newOwner != null;
		var isLocalPlayer = newOwner == Connection.Local;

		DisconnectedSince = !hasNetworkOwner ? 0 : null;

		if ( !hasNetworkOwner && Networking.IsHost && CurrentEquipment.IsValid() )
		{
			HolsterCurrent();
		}

		if ( Controller.IsValid() )
		{
			if ( !hasNetworkOwner && Networking.IsHost )
			{
				Controller.WishVelocity = Vector3.Zero;
			}

			if ( Controller.Body.IsValid() )
			{
				Controller.Body.MotionEnabled = hasNetworkOwner;
			}

			Controller.Enabled = hasNetworkOwner && !IsDead && !GameManager.IsHeadless;
		}

		if ( hasNetworkOwner )
		{
			ApplyClothing();
		}

		if ( isLocalPlayer )
		{
			Local = this;
			SetupCamera();
		}
	}

	SnapshotType ISnapshot.SnapshotType => SnapshotType.Player;

	SnapshotData ISnapshot.Save()
	{
		var data = new PlayerSnapshotData
		{
			SteamId = SteamId,
			Position = WorldPosition,
			WalletBalance = WalletBalance,
			JobPath = Job?.Id.ToString(),
			Health = HealthComponent.Health
		};

		foreach ( var equipment in Equipment )
		{
			if ( !equipment.CanDrop )
			{
				continue;
			}

			var ammoComponent = equipment.GameObject.GetComponentInChildren<AmmoComponent>( true );
			var ammoCount = ammoComponent.IsValid() ? ammoComponent.Ammo : 0;
			var reserveAmmoCount = ammoComponent.IsValid() ? ammoComponent.ReserveAmmo : 0;

			data.Equipment.Add( new EquipmentSnapshotData
			{
				ResourcePath = equipment.Resource.PrefabPath(), Ammo = ammoCount, ReserveAmmo = reserveAmmoCount
			} );
		}

		return data;
	}

	void ISnapshot.Load( SnapshotData data )
	{
		if ( data is not PlayerSnapshotData playerData )
		{
			Log.Warning( $"Failed to restore player {DisplayName}, invalid snapshot data" );
			return;
		}

		if ( playerData.JobPath != null )
		{
			var job = GameModeJobs.FindByReference( playerData.JobPath );
			if ( job != null )
			{
				Job = job;
			}
		}

		WalletBalance = playerData.WalletBalance;

		if ( playerData.Health > 0 )
		{
			HealthComponent.Health = playerData.Health;
		}

		SpawnHost( false, new Transform( playerData.Position, Rotation.Identity ) );

		// Restore droppable equipment
		foreach ( var equipmentData in playerData.Equipment )
		{
			var resource = GameModeEquipments.FindByPrefabPath( equipmentData.ResourcePath );
			if ( resource == null )
			{
				continue;
			}

			var equipment = GiveHost( resource, false );
			if ( !equipment.IsValid() )
			{
				continue;
			}

			var ammoComponent = equipment.GameObject.GetComponentInChildren<AmmoComponent>( true );
			if ( ammoComponent.IsValid() )
			{
				ammoComponent.Ammo = equipmentData.Ammo;
				ammoComponent.ReserveAmmo = equipmentData.ReserveAmmo;
			}
		}
	}

	public void InitalizeHost( uint bankBalance, int playtime, int level, string? rpName = null )
	{
		Assert.True( Networking.IsHost );

		BankBalance = bankBalance;
		PlayTime = playtime;
		Level = level;
		RpName = rpName;
	}

	public void OnKillHost( Component victim, DamageInfo damage )
	{
		OnKillScoreHost( victim, damage );
		OnKillStateHost( victim, damage );
		OnKillStateOwner( victim, damage );

		if ( Networking.IsHost )
		{
			IGameEvents.Post( x => x.OnPlayerKillHost( this ) );

			var killer = GameUtils.GetPlayerFromComponent( damage.Attacker );
			if ( killer.IsValid() )
				Sentinel.Sentinel.NotifyKill( killer, this );
		}
	}

	protected override void OnDestroy()
	{
		OnDestroyBody();

		base.OnDestroy();
	}
}
