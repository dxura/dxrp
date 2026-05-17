namespace Dxura.RP.Game;

[Icon( "track_changes" )]
[Title( "Melee" )]
[Group( "Weapon Components" )]
public class MeleeWeaponComponent : InputWeaponComponent
{
	[Property]
	[Category( "Config" )]
	[EquipmentResourceProperty]
	public float BaseDamage { get; set; } = 25.0f;

	[Property]
	[Category( "Config" )]
	[EquipmentResourceProperty]
	public float FireRate { get; set; } = 0.2f;

	[Property]
	[Category( "Config" )]
	[EquipmentResourceProperty]
	public float MaxRange { get; set; } = 1024000;

	[Property] [Category( "Config" )] public float Size { get; set; } = 1.0f;

	[Property] [Group( "Sounds" )] public SoundEvent? SwingSound { get; set; }

	private TimeSince TimeSinceSwing { get; set; }

	// Callbacks for subclasses
	protected virtual void OnSwingImpact( GameObject hitObject, Surface surface, Vector3 pos, Vector3 normal ) {}

	/// <summary>
	///     Fetches the desired model renderer that we'll focus effects on like trail effects, muzzle flashes, etc.
	/// </summary>
	protected SkinnedModelRenderer EffectsRenderer
	{
		get
		{
			if ( IsProxy || !Equipment.ViewModel.IsValid() )
			{
				return Equipment.ModelRenderer;
			}

			return Equipment.ViewModel.ModelRenderer;
		}
	}

	protected virtual Ray? WeaponRay => Equipment.Owner?.AimRay;

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Unreliable )]
	private void BroadcastSwingEffectsHost()
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:swing", Config.Current.Game.SwingCooldown ) )
		{
			return;
		}

		using ( Rpc.FilterExclude( caller ) )
		{
			BroadcastSwingEffects();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastSwingEffects()
	{
		DoSwingEffects();
	}

	protected virtual float GetDamage( GameObject hitObject, Player attacker )
	{
		return BaseDamage;
	}

	private void DoSwingEffects()
	{
		SwingSound.Play( Equipment.Transform.World.Position );

		// Third person
		Equipment?.Owner?.Renderer?.Set( "b_attack", true );

		// First person
		Equipment?.ViewModel?.ModelRenderer.Set( "b_attack", true );
	}

	private void CreateImpactEffects( GameObject hitObject, Surface surface, Vector3 pos, Vector3 normal )
	{
		var impact = surface.PrefabCollection.BluntImpact?.Clone( new CloneConfig
		{
			Parent = Scene, Transform = new Transform( pos, Rotation.LookAt( -normal ) ), StartEnabled = true, Name = $"Melee impact: {Equipment.GameObject}"
		} );

		if ( impact.IsValid() )
		{
			impact.DestroyAsync( 3f );
		}

		Sound.Play( surface.SoundCollection.ImpactHard, pos );
	}

	private void Swing()
	{
		TimeSinceSwing = 0f;

		var clientTrace = GetTrace( MaxRange, Size );
		DoSwingEffects();
		BroadcastSwingEffectsHost();

		if ( clientTrace is not { Hit: true } )
		{
			return;
		}

		var tr = clientTrace.Value;
		CreateImpactEffects( tr.GameObject, tr.Surface, tr.EndPosition, tr.Normal );
		OnSwingImpact( tr.GameObject, tr.Surface, tr.EndPosition, tr.Normal );

		InflictMeleeDamageHost( tr.GameObject, tr.HitPosition, tr.GetHitboxTags() );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void InflictMeleeDamageHost( GameObject clientHit, Vector3 clientHitPosition, HitboxTags clientHitbox )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:damage", Config.Current.Game.DamageCooldown )
		     || Cooldown.Current.CheckAndStartCooldown( $"{callerId}:melee", FireRate * 0.90f ) )
		{
			return;
		}

		if ( !clientHit.IsValid() )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !callerPlayer.IsValid() )
		{
			return;
		}

		var hitGameObject = clientHit.Root;
		if ( !hitGameObject.IsValid() )
		{
			return;
		}

		// The only validation we do: attacker must be close enough.
		var maxDistance = MaxRange + Size + 100f; // Add some leniency for latency and client-server desync.
		if ( Vector3.DistanceBetween( callerPlayer.WorldPosition, clientHitPosition ) > maxDistance )
		{
			Log.Info( "Out of range" );
			return;
		}

		// Don't allow hitting yourself.
		if ( hitGameObject == callerPlayer.GameObject.Root )
		{
			return;
		}

		if ( hitGameObject.IsValid() )
		{
			var damage = GetDamage( hitGameObject, callerPlayer );
			var direction = clientHitPosition - callerPlayer.WorldPosition;
			var force = direction.Length > 0.001f ? direction.Normal * damage * 80f : Vector3.Zero;

			hitGameObject.TakeDamageHost( new DamageInfo(
				callerPlayer,
				damage,
				Equipment,
				clientHitPosition,
				force,
				clientHitbox == HitboxTags.None ? HitboxTags.UpperBody : clientHitbox,
				DamageFlags.Melee ) );
		}
	}

	/// <summary>
	///     Can we shoot this gun right now?
	/// </summary>
	protected virtual bool CanSwing()
	{
		// Delay checks
		return TimeSinceSwing >= FireRate;
	}

	protected override void OnInput()
	{
		if ( CanSwing() )
		{
			Swing();
		}
	}
}
