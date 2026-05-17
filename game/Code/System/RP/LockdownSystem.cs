using Dxura.RP.Game.UI;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public class LockdownSystem : SingletonComponent<LockdownSystem>, IGameEvents
{
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnLockdownChanged ) )]
	public bool Lockdown { get; set; }

	[Property]
	public required SoundEvent LockdownStartSoundEvent { get; set; }

	[Property]
	public required SoundEvent LockdownOngoingSoundEvent { get; set; }

	[Property]
	public required SoundEvent AlarmSound { get; set; }

	private TimeSince? _lockdownTimeSince;
	private TimeSince? _ongoingSoundTimeSince;
	private SoundPointComponent? _alarmSoundPoint;


	protected override void OnStart()
	{
		if ( !Config.Current.Game.GovernanceLockdownEnabled )
		{
			Destroy();
			return;
		}

		OnLockdownChanged( false, Lockdown );
	}

	public void OnSecondlyUpdate()
	{
		if ( !Lockdown )
		{
			return;
		}

		// Handle ongoing sounds every 20 seconds
		if ( Config.Current.Game.LockdownDoAnnouncements && _ongoingSoundTimeSince > 40f )
		{

			if ( LockdownOngoingSoundEvent.IsValid() )
			{
				LockdownOngoingSoundEvent.Play();
			}

			_ongoingSoundTimeSince = 0;
		}


		if ( !Networking.IsHost )
		{
			return;
		}

		// Check if lockdown duration has passed
		if ( _lockdownTimeSince.HasValue && _lockdownTimeSince.Value > Config.Current.Game.LockdownDuration )
		{
			Lockdown = false;

			Log.Info( "Lockdown has ended." );
		}
	}


	private void OnLockdownChanged( bool oldValue, bool newValue )
	{
		if ( newValue )
		{
			// IN LOCKDOWN

			// Do start announcement
			if ( Config.Current.Game.LockdownDoAnnouncements )
			{
				if ( LockdownStartSoundEvent.IsValid() )
				{
					LockdownStartSoundEvent.Play();
				}
			}

			if ( Config.Current.Game.LockdownDoAlarm )
			{
				var alarmSoundPoint = GetOrAddComponent<SoundPointComponent>();
				AlarmSound.UI = true;
				alarmSoundPoint.SoundEvent = AlarmSound;
				alarmSoundPoint.Repeat = true;
				_alarmSoundPoint = alarmSoundPoint;
			}

			_lockdownTimeSince = 0;
			_ongoingSoundTimeSince = 0;

			if ( Networking.IsHost )
			{
				Chat.Current?.BroadcastSystemText( "#governance.lockdown.started" );
			}
		}
		else
		{
			_lockdownTimeSince = null;
			_ongoingSoundTimeSince = null;

			if ( _alarmSoundPoint.IsValid() )
			{
				_alarmSoundPoint.Destroy();
				_alarmSoundPoint = null;
			}

			if ( Networking.IsHost )
			{
				Chat.Current?.BroadcastSystemText( "#governance.lockdown.ended" );
			}
		}
	}

	[Rpc.Host]
	public void StartLockdownHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:lockdown", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );

		if ( caller == null || !caller.Job.IsMayoralRole() )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "global:lockdown", Config.Current.Game.LockdownCooldown ) )
		{
			caller.Error( "#governance.lockdown.cooldown" );
			return;
		}

		Lockdown = true;

		Log.Info( $"Mayor {caller.DisplayName} started a lockdown" );
		caller.UnlockAchievement( "lockdown" );
	}

	[Rpc.Host]
	public void StopLockdownHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:lockdown:stop", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );

		if ( caller == null || !caller.Job.IsMayoralRole() )
		{
			return;
		}

		Lockdown = false;

		Log.Info( $"Mayor {caller.DisplayName} stopped the lockdown" );
	}

}
