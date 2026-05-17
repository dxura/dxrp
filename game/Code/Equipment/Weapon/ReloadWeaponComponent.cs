using System.Threading.Tasks;

namespace Dxura.RP.Game;

[Title( "Reload" )]
[Group( "Weapon Components" )]
public class ReloadWeaponComponent : InputWeaponComponent, IEquipmentEvents
{
	/// <summary>
	///     How long does it take to reload?
	/// </summary>
	[Property]
	public float ReloadTime { get; set; } = 1.0f;

	/// <summary>
	///     How long does it take to reload while empty?
	/// </summary>
	[Property]
	public float EmptyReloadTime { get; set; } = 2.0f;

	[Property]
	public bool SingleReload { get; set; } = false;

	/// <summary>
	///     This is really just the magazine for the weapon.
	/// </summary>
	[Property]
	public required AmmoComponent AmmoComponent { get; set; }

	private TimeUntil TimeUntilReload { get; set; }

	[Sync( SyncFlags.FromHost )]
	private bool IsReloading { get; set; }

	[Property] public Dictionary<float, SoundEvent> TimedReloadSounds { get; set; } = new();
	[Property] public Dictionary<float, SoundEvent> EmptyReloadSounds { get; set; } = new();

	private bool _queueCancel;

	/// <summary>
	///     Local guard to prevent the client from spamming the Host with "EndReload" 
	///     requests while waiting for the network to sync the state change.
	/// </summary>
	private bool _hasNotifiedEndReloadHost = false;

	public new void OnEquipmentHolstered( Equipment equipment )
	{
		if ( !IsProxy && IsReloading )
		{
			DoCancelReload();
			BroadcastCancelReloadHost();
		}
	}

	protected override void OnEnabled()
	{
		BindTag( "reloading", () => IsReloading );
	}

	protected override void OnInput()
	{
		if ( CanReload() )
		{
			BroadcastStartReloadHost();
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !Player.IsValid() )
		{
			return;
		}

		if ( Player.IsProxy )
		{
			return;
		}

		if ( SingleReload && IsReloading && Input.Pressed( "Attack1" ) )
		{
			_queueCancel = true;
		}

		// Check if the timer is up and we haven't sent the notification yet
		if ( IsReloading && TimeUntilReload && !_hasNotifiedEndReloadHost )
		{
			_hasNotifiedEndReloadHost = true;
			BroadcastEndReloadHost();
		}
	}

	private bool CanReload()
	{
		return !IsReloading &&
		       AmmoComponent.IsValid() &&
		       !AmmoComponent.IsFull &&
		       AmmoComponent.HasReserveAmmo &&
		       !Tags.Has( "bolting" );
	}

	private float GetReloadTime()
	{
		if ( !AmmoComponent.HasAmmo )
		{
			return EmptyReloadTime;
		}

		return ReloadTime;
	}

	private Dictionary<float, SoundEvent> GetReloadSounds()
	{
		if ( !AmmoComponent.HasAmmo )
		{
			return EmptyReloadSounds;
		}

		return TimedReloadSounds;
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void BroadcastStartReloadHost()
	{
		if ( !CanReload() )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{Rpc.CallerId}:reload:start", Config.Current.Game.ReloadStartCooldown ) )
		{
			return;
		}

		IsReloading = true;
		TimeUntilReload = GetReloadTime();

		BroadcastStartReload();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastStartReload()
	{
		DoStartReload();
	}

	private void DoStartReload()
	{
		_queueCancel = false;

		// IMPORTANT: Reset the notification guard so the client can finish the next reload/shell
		_hasNotifiedEndReloadHost = false;

		if ( !IsProxy )
		{
			IsReloading = true;
			TimeUntilReload = GetReloadTime();
		}

		if ( SingleReload )
		{
			Equipment.ViewModel?.ModelRenderer.Set( "b_reloading", true );

			var hasAmmo = AmmoComponent.HasAmmo;
			Equipment.ViewModel?.ModelRenderer.Set( !hasAmmo ? "b_reloading_first_shell" : "b_reloading_shell", true );
		}
		else
		{
			Equipment.ViewModel?.ModelRenderer.Set( "b_reload", true );
		}

		foreach ( var kv in GetReloadSounds() )
		{
			_ = PlayAsyncSound( kv.Key, kv.Value, () => IsReloading );
		}

		Equipment.Owner?.Renderer.Set( "b_reload", true );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void BroadcastCancelReloadHost()
	{
		if ( !IsReloading )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{Rpc.CallerId}:reload:cancel", Config.Current.Game.ReloadCancelCooldown ) )
		{
			return;
		}

		IsReloading = false;
		BroadcastCancelReload();
	}


	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastCancelReload()
	{
		DoCancelReload();
	}

	private void DoCancelReload()
	{
		if ( !IsProxy )
		{
			IsReloading = false;
		}

		Equipment.ViewModel?.ModelRenderer.Set( "b_reload", false );
		Equipment.Owner?.Renderer.Set( "b_reload", false );
		Equipment.ViewModel?.ModelRenderer.Set( "b_reloading", false );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void BroadcastEndReloadHost()
	{
		if ( !IsReloading )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{Rpc.CallerId}:reload:end", Config.Current.Game.ReloadEndCooldown ) )
		{
			return;
		}

		if ( SingleReload )
		{
			AmmoComponent.HostReloadMagazine( 1 );

			// If we still have room and haven't queued a cancel, start the next shell
			if ( !_queueCancel && AmmoComponent.Ammo < AmmoComponent.MaxAmmo && AmmoComponent.HasReserveAmmo )
			{
				TimeUntilReload = GetReloadTime();
				BroadcastStartReload();
				return;
			}
		}
		else
		{
			var needed = AmmoComponent.MaxAmmo - AmmoComponent.Ammo;
			AmmoComponent.HostReloadMagazine( needed );
		}

		IsReloading = false;
		BroadcastEndReload();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastEndReload()
	{
		DoEndReload();
	}

	private void DoEndReload()
	{
		if ( !IsProxy )
		{
			Equipment.ViewModel?.ModelRenderer.Set( "b_reloading", false );

			if ( !SingleReload || AmmoComponent.Ammo >= AmmoComponent.MaxAmmo || _queueCancel )
			{
				IsReloading = false;
			}
		}

		Equipment.ViewModel?.ModelRenderer.Set( "b_reload", false );
		Equipment.Owner?.Renderer.Set( "b_reload", false );
	}

	private async Task PlayAsyncSound( float delay, SoundEvent snd, Func<bool>? playCondition = null )
	{
		await GameTask.DelaySeconds( delay );

		if ( playCondition != null && !playCondition.Invoke() )
		{
			return;
		}

		if ( snd.IsValid() && GameObject.IsValid() )
		{
			GameObject.PlaySound( snd );
		}
	}
}
