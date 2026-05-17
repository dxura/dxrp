namespace Dxura.RP.Game;

/// <summary>
/// Fires a projectile prefab (thrown items, rockets, etc). Supports optional cooking/charge,
/// ammo checks, deploy delay, dry fire, and basic FX hooks similar to ShootWeaponComponent.
/// </summary>
[Icon( "track_changes" )]
[Title( "Projectile Weapon" )]
[Group( "Weapon Components" )]
public class ProjectileWeaponComponent : InputWeaponComponent, IEquipmentEvents
{
	public enum FireState
	{
		Idle,
		Cooking,
		Firing,
		Cooldown
	}

	public enum SpawnMode
	{
		/// <summary>
		/// Spawn near the aim ray (uses a short trace to avoid embedding in surfaces).
		/// </summary>
		AimTrace,
		/// <summary>
		/// Spawn from the equipment muzzle GameObject (falls back to AimTrace if missing).
		/// </summary>
		EquipmentMuzzle
	}

	public enum MuzzleAimMode
	{
		/// <summary>
		/// Use the muzzle GameObject's current rotation.
		/// </summary>
		UseMuzzleRotation,
		/// <summary>
		/// Use the player's aim ray forward (crosshair direction).
		/// </summary>
		UseAimRayForward,
		/// <summary>
		/// Trace from the camera aim ray and rotate the muzzle to point at the hit point.
		/// Helps reduce close-range parallax for launchers.
		/// </summary>
		AimAtCameraTraceHit
	}

	[Property]
	[Group( "Projectile" )]
	public required GameObject Prefab { get; set; }

	[Property]
	[Group( "Projectile" )]
	[EquipmentResourceProperty]
	public float ThrowPower { get; set; } = 1200f;

	[Property]
	[Group( "Projectile" )]
	public float UpwardBoost { get; set; } = 100f;

	[Property]
	[Group( "Projectile" )]
	public float OwnerVelocityScale { get; set; } = 0f;

	[Property]
	[Group( "Projectile" )]
	public SpawnMode SpawnFrom { get; set; } = SpawnMode.AimTrace;

	[Property]
	[Group( "Projectile" )]
	public MuzzleAimMode MuzzleAim { get; set; } = MuzzleAimMode.UseMuzzleRotation;

	[Property]
	[Group( "Projectile" )]
	public float MuzzleAimTraceDistance { get; set; } = 8192f;

	[Property]
	[Group( "Projectile" )]
	public Vector3 SpawnOffset { get; set; } = Vector3.Zero;

	[Property]
	[Group( "Projectile" )]
	public float SpawnTraceDistance { get; set; } = 10f;

	[Property]
	[Group( "Projectile" )]
	public float SpawnForwardDistance { get; set; } = 32f;

	[Property]
	[Group( "Projectile" )]
	public float SpawnSurfaceOffset { get; set; } = 5f;

	[Property]
	[Group( "Timing" )]
	public float CookTime { get; set; } = 0.15f;

	[Property]
	[Group( "Timing" )]
	public float Cooldown { get; set; } = 1.0f;

	[Property]
	[Group( "Timing" )]
	public float DeployDelay { get; set; } = 0.0f;

	[Property]
	[Group( "Timing" )]
	public float DryShootDelay { get; set; } = 0.15f;

	/// <summary>
	/// If true, requires Attack2 to cook and Attack1 to fire.
	/// If false, Attack1 fires immediately.
	/// </summary>
	[Property]
	[Group( "Input" )]
	public bool EnableCooking { get; set; } = true;

	/// <summary>
	/// If true and this weapon uses an AmmoComponent, remove the equipment after the last round is fired.
	/// Useful for throwable stacks that are implemented as a weapon + ammo count.
	/// </summary>
	[Property]
	[Group( "Equipment" )]
	public bool DestroyWhenOutOfAmmo { get; set; } = false;

	[Property]
	[Group( "Equipment" )]
	public bool ViewModelRenderHack { get; set; } = false;

	[Property]
	[Group( "Input" )]
	public bool BlockWhileRunning { get; set; } = false;

	[Property]
	[Group( "Ammo" )]
	public AmmoComponent? AmmoComponent { get; set; }

	[Property]
	[Group( "Ammo" )]
	public bool RequiresAmmoComponent { get; set; } = false;

	[Property]
	[Group( "Equipment" )]
	public bool DestroyOnThrow { get; set; } = true;

	[Property]
	[Group( "Effects" )]
	public SoundEvent? FireSound { get; set; }

	[Property]
	[Group( "Effects" )]
	public SoundEvent? DryFireSound { get; set; }

	[Property]
	[Group( "Effects" )]
	public GameObject? MuzzleFlashPrefab { get; set; }

	[Property]
	[Group( "Effects" )]
	public GameObject? EjectionPrefab { get; set; }

	[Property]
	[Group( "Effects" )]
	public string ChargeAnimParam { get; set; } = "b_charge";

	[Property]
	[Group( "Effects" )]
	public string FireAnimParam { get; set; } = "b_attack";

	[Property]
	[Group( "Effects" )]
	public string DryFireAnimParam { get; set; } = "b_attack_dry";

	[Property]
	[Group( "Effects" )]
	public bool PostWeaponShotEvent { get; set; } = false;

	[Property]
	[Group( "Cooking" )]
	private SoundEvent? CookSound { get; set; }

	[Property]
	[Group( "Cooking" )]
	public bool PlayCookSoundWhenCookingDisabled { get; set; } = true;

	[Property]
	[Group( "Cooking" )]
	public float CookingDisabledTauntCooldown { get; set; } = 4f;

	[Sync] public FireState CurrentState { get; private set; }

	private TimeSince TimeSinceAction { get; set; }
	private TimeSince TimeSinceCooldown { get; set; }
	private TimeSince TimeSinceDryFire { get; set; }
	public TimeSince TimeSinceDeployed { get; private set; }

	private bool _hasFired;

	public new void OnEquipmentDeployed( Equipment equipment )
	{
		TimeSinceDeployed = 0;
		// Allow firing immediately after deploy (DeployDelay still applies).
		TimeSinceCooldown = Cooldown;
		TimeSinceDryFire = DryShootDelay;
		base.OnEquipmentDeployed( equipment );
	}

	public new void OnEquipmentHolstered( Equipment equipment )
	{
		if ( !IsProxy && CurrentState != FireState.Cooldown )
		{
			ResetState();
		}

		base.OnEquipmentHolstered( equipment );
	}

	protected override void OnHolstered()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( CurrentState != FireState.Cooldown )
		{
			ResetState();
		}
	}

	protected override void OnInputUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( BlockWhileRunning && (Player?.IsRunning ?? false) )
		{
			return;
		}

		// Cooldown -> Idle transition
		if ( CurrentState == FireState.Cooldown && TimeSinceCooldown >= Cooldown )
		{
			ResetState();
		}

		if ( CurrentState == FireState.Idle )
		{
			if ( !CanBeginFire() )
			{
				return;
			}

			if ( EnableCooking )
			{
				if ( Input.Pressed( "Attack2" ) )
				{
					BeginCook();
				}
			}
			else
			{
				if ( Input.Pressed( "Attack1" ) )
				{
					BeginFire();
				}

				// Optional "taunt" cook sound when the user tries to cook while disabled.
				if ( PlayCookSoundWhenCookingDisabled && Input.Pressed( "Attack2" ) )
				{
					TryPlayCookingDisabledTaunt();
				}
			}
		}
		else if ( CurrentState == FireState.Cooking )
		{
			// Release to cancel
			if ( !Input.Down( "Attack2" ) && !_hasFired )
			{
				ResetState();
				return;
			}

			if ( Input.Pressed( "Attack1" ) )
			{
				BeginFire();
				return;
			}
		}
		else if ( CurrentState == FireState.Firing )
		{
			if ( TimeSinceAction >= CookTime )
			{
				// Execute
				DoFireLocalEffects();
				RequestFireHost();
				CurrentState = FireState.Cooldown;
				TimeSinceCooldown = 0;
			}
		}
	}

	protected virtual bool CanBeginFire()
	{
		if ( !Equipment.IsValid() || !Equipment.IsDeployed || !Equipment.Owner.IsValid() )
		{
			return false;
		}

		if ( TimeSinceDeployed < DeployDelay )
		{
			return false;
		}

		if ( TimeSinceCooldown < Cooldown )
		{
			return false;
		}

		if ( Equipment.Tags.Has( "reloading" ) || Equipment.Tags.Has( "bolting" ) || Equipment.Tags.Has( "no_shooting" ) )
		{
			return false;
		}

		if ( RequiresAmmoComponent )
		{
			if ( AmmoComponent == null || !AmmoComponent.HasAmmo )
			{
				DryFire();
				return false;
			}
		}

		return true;
	}

	private void BeginCook()
	{
		CurrentState = FireState.Cooking;
		TimeSinceAction = 0;
		_hasFired = false;

		// Match legacy throw: charge is first-person only.
		SetAnimBool( ChargeAnimParam, true, true );
		CookSound?.Broadcast( WorldPosition, Equipment.Owner?.GameObject ?? GameObject );
	}

	private void BeginFire()
	{
		if ( !CanBeginFire() )
		{
			return;
		}

		_hasFired = true;
		CurrentState = FireState.Firing;
		TimeSinceAction = 0;
	}

	private void DryFire()
	{
		if ( TimeSinceDryFire < DryShootDelay )
		{
			return;
		}

		TimeSinceDryFire = 0f;
		if ( DryFireSound is not null )
		{
			DryFireSound.Play( Equipment.Transform.World.Position );
		}

		SetAnimBool( DryFireAnimParam, true, true );
	}

	private void TryPlayCookingDisabledTaunt()
	{
		if ( CookSound is null )
		{
			return;
		}

		if ( !Game.Cooldown.Current?.CheckAndStartCooldown( "cook:taunt", CookingDisabledTauntCooldown ) ?? false )
		{
			CookSound.Broadcast( WorldPosition, Equipment.Owner?.GameObject ?? GameObject );
		}
	}

	protected virtual void ResetState()
	{
		CurrentState = FireState.Idle;
		TimeSinceAction = 0;
		_hasFired = false;

		SetAnimBool( ChargeAnimParam, false, true );

		// HACK: Some viewmodels need a recreate to fully clear charge state.
		if ( ViewModelRenderHack )
		{
			if ( Equipment is { IsDeployed: true, Owner.IsThirdPersonPreferred: false } && !string.IsNullOrWhiteSpace( ChargeAnimParam ) )
			{
				Equipment.ClearViewModel();
				Equipment.CreateViewModel( false );
			}
		}
	}

	private IEquipment Effector
	{
		get
		{
			if ( IsProxy || !Equipment.ViewModel.IsValid() )
			{
				return Equipment;
			}

			return Equipment.ViewModel;
		}
	}

	private void DoFireLocalEffects()
	{
		if ( Application.IsDedicatedServer )
		{
			return;
		}

		SetAnimBool( ChargeAnimParam, false, true );
		SetAnimBool( FireAnimParam, true );

		// Visual effects anchored to muzzle/ejection
		if ( Effector.ModelRenderer.IsValid() )
		{
			if ( MuzzleFlashPrefab.IsValid() && Effector.Muzzle.IsValid() )
			{
				MuzzleFlashPrefab.Clone( new CloneConfig
				{
					Parent = Effector.Muzzle, Transform = new Transform(), StartEnabled = true, Name = $"Muzzle flash: {Equipment.GameObject}"
				} );
			}

			if ( EjectionPrefab.IsValid() && Effector.EjectionPort.IsValid() )
			{
				EjectionPrefab.Clone( new CloneConfig
				{
					Parent = Effector.EjectionPort, Transform = new Transform(), StartEnabled = true, Name = $"Ejection: {Equipment.GameObject}"
				} );
			}
		}

		FireSound?.Play( Equipment.WorldPosition );

		if ( PostWeaponShotEvent )
		{
			IGameEvents.PostToGameObject( GameObject, x => x.OnWeaponShot() );
		}
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void RequestFireHost()
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;

		var player = Equipment.Owner;
		if ( !player.IsValid() )
		{
			return;
		}

		if ( Game.Cooldown.Current.CheckAndStartCooldown( $"{callerId}:{Id}:projectile", Cooldown ) )
		{
			return;
		}

		if ( RequiresAmmoComponent && (AmmoComponent == null || AmmoComponent.Ammo <= 0) )
		{
			return;
		}

		if ( AmmoComponent != null )
		{
			AmmoComponent.Ammo = Math.Max( AmmoComponent.Ammo - 1, 0 );
		}

		var (position, rotation) = GetSpawnTransform( player );
		var projectile = Prefab.Clone( position, rotation );
		if ( !projectile.IsValid() )
		{
			return;
		}

		projectile.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );
		projectile.NetworkSpawn( caller );

		var initialVelocity = GetInitialVelocity( player );
		// Ensure host has authoritative starting velocity too.
		var rb = projectile.GetComponent<Rigidbody>();
		if ( rb.IsValid() )
		{
			rb.Velocity = initialVelocity;
		}

		using ( Rpc.FilterInclude( x => x == caller ) )
		{
			BroadcastInitialVelocity( projectile, initialVelocity );
		}

		using ( Rpc.FilterExclude( caller ) )
		{
			BroadcastFireEffects();
		}

		if ( DestroyOnThrow )
		{
			var usesAmmo = AmmoComponent != null || RequiresAmmoComponent;
			if ( !usesAmmo )
			{
				Equipment.Owner?.RemoveEquipment( Equipment );
			}
			else if ( DestroyWhenOutOfAmmo && (AmmoComponent?.Ammo ?? 0) <= 0 )
			{
				Equipment.Owner?.RemoveEquipment( Equipment );
			}
		}
	}

	protected virtual (Vector3 Position, Rotation Rotation) GetSpawnTransform( Player player )
	{
		var aimRay = player.AimRay;

		if ( SpawnFrom == SpawnMode.EquipmentMuzzle && Equipment.Muzzle.IsValid() )
		{
			var pos = Equipment.Muzzle.WorldPosition;
			var rot = Equipment.Muzzle.WorldRotation;

			switch ( MuzzleAim )
			{
				case MuzzleAimMode.UseAimRayForward:
					rot = Rotation.LookAt( aimRay.Forward );
					break;
				case MuzzleAimMode.AimAtCameraTraceHit:
					{
						var aimTr = Scene.Trace.Ray( new Ray( aimRay.Position, aimRay.Forward ), MuzzleAimTraceDistance )
							.UseHitboxes()
							.IgnoreGameObjectHierarchy( GameObject.Root )
							.WithoutTags( Constants.TraceIgnoreTags )
							.Run();

						var target = aimTr.Hit
							? aimTr.HitPosition
							: aimRay.Position + aimRay.Forward * MuzzleAimTraceDistance;

						var forward = (target - pos).Normal;
						if ( forward.LengthSquared < 0.001f )
						{
							forward = aimRay.Forward;
						}

						rot = Rotation.LookAt( forward );
						break;
					}
				case MuzzleAimMode.UseMuzzleRotation:
				default:
					break;
			}

			pos += rot * SpawnOffset;
			return (pos, rot);
		}

		var tr = Scene.Trace.Ray( new Ray( aimRay.Position, aimRay.Forward ), SpawnTraceDistance )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.WithoutTags( Constants.TraceIgnoreTags )
			.Run();

		var position = tr.Hit
			? tr.HitPosition + tr.Normal * SpawnSurfaceOffset
			: aimRay.Position + aimRay.Forward * SpawnForwardDistance;

		var rotation = Rotation.LookAt( aimRay.Forward );
		position += rotation * SpawnOffset;
		return (position, rotation);
	}

	protected virtual Vector3 GetInitialVelocity( Player player )
	{
		var v = player.AimRay.Forward * ThrowPower;
		v += Vector3.Up * UpwardBoost;

		if ( OwnerVelocityScale != 0f )
		{
			v += player.Controller.Velocity * OwnerVelocityScale;
		}

		return v;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastInitialVelocity( GameObject projectile, Vector3 velocity )
	{
		if ( !projectile.IsValid() )
		{
			return;
		}

		var rb = projectile.GetComponent<Rigidbody>();
		if ( rb.IsValid() )
		{
			rb.Velocity = velocity;
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastFireEffects()
	{
		DoFireLocalEffects();
	}

	private void SetAnimBool( string param, bool value, bool firstPersonOnly = false )
	{
		if ( string.IsNullOrWhiteSpace( param ) )
		{
			return;
		}

		if ( !firstPersonOnly )
		{
			if ( Equipment.IsValid() && Equipment.Owner.IsValid() && Equipment.Owner.Renderer.IsValid() )
			{
				Equipment.Owner.Renderer.Set( param, value );
			}
		}

		Equipment?.ViewModel?.ModelRenderer?.Set( param, value );
	}
}
