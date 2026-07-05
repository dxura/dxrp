namespace Dxura.RP.Game.Equipments;

public class TaserEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property]
	[Group( "Taser" )]
	public float MaxRange { get; set; } = 215f;

	[Property]
	[Group( "Taser" )]
	public float Delay { get; set; } = 1f;

	[Property]
	[Group( "Taser" )]
	public float DeployDelay { get; set; } = 0.5f;

	[Property]
	[Group( "Taser" )]
	public float DryShootDelay { get; set; } = 0.15f;

	[Property]
	[Group( "Taser" )]
	public float StunDuration { get; set; } = 10f;

	[Property]
	[Group( "Taser" )]
	public float Damage { get; set; } = 5f;

	[Property]
	[Group( "Effects" )]
	public SoundEvent? ShootSound { get; set; }

	[Property]
	[Group( "Effects" )]
	public SoundEvent? StunSound { get; set; }

	[Property]
	[Group( "Effects" )]
	public SoundEvent? DryFireSound { get; set; }

	[Property]
	[Group( "Effects" )]
	public GameObject? MuzzleEffectPrefab { get; set; }

	[Property]
	[Group( "Effects" )]
	public float ArcDuration { get; set; } = 0.15f;

	[Property]
	[Group( "Effects" )]
	public float ArcWidth { get; set; } = 0.35f;

	[Property]
	[Category( "Ammo" )]
	public AmmoComponent? AmmoComponent { get; set; }

	public TimeSince TimeSinceShoot { get; private set; }
	public TimeSince TimeSinceDeployed { get; private set; }

	private IEquipment Effector =>
		IsProxy || !Equipment.ViewModel.IsValid() ? Equipment : Equipment.ViewModel;

	public new void OnEquipmentDeployed( Equipment equipment )
	{
		TimeSinceDeployed = 0;
		base.OnEquipmentDeployed( equipment );
	}

	protected override void OnInputFixedUpdate()
	{
		if ( Player?.IsRunning ?? false )
		{
			return;
		}

		if ( !InputActions.Any( action => Input.Pressed( action ) ) )
		{
			return;
		}

		if ( !CanShoot() )
		{
			if ( AmmoComponent is { HasAmmo: false } && TimeSinceShoot >= DryShootDelay && !Tags.Has( "reloading" ) )
			{
				DryShoot();
			}

			return;
		}

		Shoot();
	}

	private bool CanShoot()
	{
		if ( !Equipment.IsValid() || !Equipment.Owner.IsValid() )
		{
			return false;
		}

		if ( TimeSinceDeployed < DeployDelay )
		{
			return false;
		}

		if ( Equipment.Tags.Has( "reloading" ) || Equipment.Tags.Has( "no_shooting" ) )
		{
			return false;
		}

		if ( TimeSinceShoot < Delay )
		{
			return false;
		}

		if ( AmmoComponent is not { HasAmmo: true } )
		{
			return false;
		}

		return true;
	}

	private void Shoot()
	{
		TimeSinceShoot = 0;

		var trace = GetTrace( MaxRange );
		var targetSteamId = 0L;
		var hitPosition = GetHitPosition( trace );

		if ( trace is { Hit: true } && trace.Value.GameObject.IsValid() )
		{
			var target = trace.Value.GameObject.Root.GetComponentInParent<Player>();
			if ( target.IsValid() )
			{
				targetSteamId = target.SteamId;
			}
		}

		DoShootEffects( hitPosition );
		DoShootHost( targetSteamId, hitPosition );

		Equipment.Components.Get<RecoilWeaponComponent>( FindMode.EnabledInSelfAndDescendants )?.Shoot();
	}

	private void DryShoot()
	{
		TimeSinceShoot = 0;
		DoDryShootEffects();
		BroadcastDryShootEffectsHost();
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void DoShootHost( long targetSteamId, Vector3 hitPosition )
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:taser:shoot", Delay * Config.Current.Game.TaserShootCooldownFactor ) )
		{
			return;
		}

		if ( !Equipment.Owner.IsValid() )
		{
			return;
		}

		var owner = Equipment.Owner;
		if ( owner.IsDead || owner.AimRay.Position.Distance( owner.WorldPosition ) > 150f )
		{
			return;
		}

		if ( AmmoComponent is not { HasAmmo: true } )
		{
			return;
		}

		AmmoComponent.Ammo = Math.Max( AmmoComponent.Ammo - 1, 0 );

		using ( Rpc.FilterExclude( Rpc.Caller ) )
		{
			BroadcastShootEffects( hitPosition );
		}

		if ( targetSteamId == 0 )
		{
			return;
		}

		var target = GameUtils.GetPlayerById( targetSteamId );
		if ( !CanStunTarget( owner, target ) )
		{
			return;
		}

		var serverTrace = GetTrace( MaxRange );
		if ( serverTrace is not { Hit: true } )
		{
			return;
		}

		var serverTarget = serverTrace.Value.GameObject.Root.GetComponentInParent<Player>();
		if ( !serverTarget.IsValid() || serverTarget.SteamId != targetSteamId )
		{
			return;
		}

		if ( hitPosition.Distance( serverTrace.Value.EndPosition ) > 32f )
		{
			return;
		}

		var stunDirection = (target.BodyRoot.WorldPosition - hitPosition).Normal;
		if ( stunDirection.IsNearZeroLength )
		{
			stunDirection = owner.AimRay.Forward;
		}

		target.DamageTakenPosition = hitPosition;
		target.DamageTakenForce = (stunDirection + Vector3.Up * 0.35f).Normal * 750f;

		target.GameObject.TakeDamageHost( new DamageInfo(
			owner,
			Damage,
			Equipment,
			serverTrace.Value.EndPosition,
			target.DamageTakenForce * 0.1f,
			serverTrace.Value.GetHitboxTags() ) );

		target.AddStatus( Constants.StunStatus, Config.Current.Game.StunDuration );
		StunSound?.Broadcast( target.WorldPosition, target.GameObject );
	}

	private bool CanStunTarget( Player owner, Player? target )
	{
		if ( !target.IsValid() || target == owner )
		{
			return false;
		}

		if ( target.HealthComponent.State != LifeState.Alive )
		{
			return false;
		}

		return true;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastShootEffects( Vector3 hitPosition )
	{
		if ( Application.IsDedicatedServer )
		{
			return;
		}

		DoShootEffects( hitPosition );
	}

	private Vector3 GetHitPosition( SceneTraceResult? trace )
	{
		if ( trace is { Hit: true } )
		{
			return trace.Value.EndPosition;
		}

		var aimRay = Equipment.Owner?.AimRay;
		if ( aimRay.HasValue && aimRay.Value.Forward.LengthSquared > 0.01f )
		{
			return aimRay.Value.Position + aimRay.Value.Forward * MaxRange;
		}

		return Equipment.WorldPosition + Equipment.WorldRotation.Forward * MaxRange;
	}

	private void DoShootEffects( Vector3 hitPosition )
	{
		ShootSound?.Play( Equipment.WorldPosition );

		var muzzle = Effector.Muzzle;
		var start = muzzle.IsValid() ? muzzle.WorldPosition : Effector.GameObject.WorldPosition;
		var forward = muzzle.IsValid()
			? muzzle.WorldRotation.Forward
			: Equipment.Owner?.AimRay.Forward ?? Equipment.WorldRotation.Forward;

		TaserArcEffect.Spawn( Scene, start, hitPosition, forward, ArcDuration, ArcWidth );

		if ( MuzzleEffectPrefab.IsValid() && muzzle.IsValid() )
		{
			MuzzleEffectPrefab.Clone( new CloneConfig
			{
				Parent = muzzle,
				Transform = new Transform(),
				StartEnabled = true,
				Name = $"Taser muzzle: {Equipment.GameObject}"
			} );
		}

		if ( Equipment.Owner.IsValid() && Equipment.Owner.Renderer.IsValid() )
		{
			Equipment.Owner.Renderer.Set( "b_attack", true );
		}

		if ( Equipment.ViewModel.IsValid() && Equipment.ViewModel.Enabled )
		{
			Equipment.ViewModel.ModelRenderer.Set( "b_attack", true );
		}
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void BroadcastDryShootEffectsHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:taser:dry",
			Config.Current.Game.ShootDryEffectsCooldown ) )
		{
			return;
		}

		using ( Rpc.FilterExclude( Rpc.Caller ) )
		{
			BroadcastDryShootEffects();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastDryShootEffects()
	{
		DoDryShootEffects();
	}

	private void DoDryShootEffects()
	{
		DryFireSound?.Play( Equipment.WorldPosition );
		Equipment.ViewModel?.ModelRenderer.Set( "b_attack_dry", true );
	}
}
