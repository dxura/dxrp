using Dxura.RP.Game.System.Events;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public partial class Governance
{
	public enum PdUpgradeType
	{
		Overheal,
		Mp5,
		Shotgun,
		M4,
		AmmoCache,
		RecruitmentDrive
	}

	// Each upgrade tracked as a nullable RealTimeSince (null = inactive)
	[Sync( SyncFlags.FromHost )]
	public RealTimeSince? PdUpgradeOverhealSince { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public RealTimeSince? PdUpgradeMp5Since { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public RealTimeSince? PdUpgradeShotgunSince { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public RealTimeSince? PdUpgradeM4Since { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public RealTimeSince? PdUpgradeAmmoCacheSince { get; private set; }

	[Rpc.Host]
	public void PurchasePdUpgradeHost( PdUpgradeType upgradeType )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:pd:upgrade", 2f ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );

		if ( caller == null || !caller.Job.IsMayoralRole() )
		{
			return;
		}

		if ( IsUpgradeActive( upgradeType ) )
		{
			caller.Error( "#governance.upgrade.already_active" );
			return;
		}

		var cost = GetUpgradeCost( upgradeType );

		if ( !SpendFromGovernmentBalance( cost ) )
		{
			caller.Error( "#governance.tax.insufficient_funds" );
			return;
		}

		var name = GetUpgradeDisplayName( upgradeType );

		// One-off upgrades trigger immediately without tracking duration
		if ( upgradeType == PdUpgradeType.RecruitmentDrive )
		{
			if ( EventSystem.Instance.IsValid() )
			{
				EventSystem.Instance.Toggle( RecruitmentDriveEvent.EventIdentifier );
			}
		}
		else
		{
			ActivateUpgrade( upgradeType );
		}

		BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.upgrade.purchased.announcement" ), name ) );
		Log.Info( $"Mayor {caller.SteamName} ({caller.SteamId}) purchased PD upgrade: {name} for ${cost}" );
	}

	public bool IsUpgradeActive( PdUpgradeType type )
	{
		if ( type == PdUpgradeType.RecruitmentDrive )
		{
			return EventSystem.Instance.IsValid() && EventSystem.Instance.IsEventActive( RecruitmentDriveEvent.EventIdentifier );
		}

		var since = GetUpgradeSince( type );
		return since.HasValue && since.Value < GetUpgradeDuration( type );
	}

	public float GetUpgradeTimeRemaining( PdUpgradeType type )
	{
		if ( type == PdUpgradeType.RecruitmentDrive )
		{
			return 0f;
		}

		var since = GetUpgradeSince( type );
		if ( !since.HasValue )
		{
			return 0f;
		}

		return Math.Max( 0f, GetUpgradeDuration( type ) - since.Value );
	}

	private float GetUpgradeDuration( PdUpgradeType type )
	{
		var config = Config.Current.Game;
		return type switch
		{
			PdUpgradeType.AmmoCache => config.PdUpgradeAmmoCacheDuration,
			_ => config.PdUpgradeDuration
		};
	}

	private RealTimeSince? GetUpgradeSince( PdUpgradeType type )
	{
		return type switch
		{
			PdUpgradeType.Overheal => PdUpgradeOverhealSince,
			PdUpgradeType.Mp5 => PdUpgradeMp5Since,
			PdUpgradeType.Shotgun => PdUpgradeShotgunSince,
			PdUpgradeType.M4 => PdUpgradeM4Since,
			PdUpgradeType.AmmoCache => PdUpgradeAmmoCacheSince,
			_ => null
		};
	}

	private void ActivateUpgrade( PdUpgradeType type )
	{
		Assert.True( Networking.IsHost );

		switch ( type )
		{
			case PdUpgradeType.Overheal:
				PdUpgradeOverhealSince = 0;
				break;
			case PdUpgradeType.Mp5:
				PdUpgradeMp5Since = 0;
				break;
			case PdUpgradeType.Shotgun:
				PdUpgradeShotgunSince = 0;
				break;
			case PdUpgradeType.M4:
				PdUpgradeM4Since = 0;
				break;
			case PdUpgradeType.AmmoCache:
				PdUpgradeAmmoCacheSince = 0;
				break;
		}
	}

	private void DeactivateUpgrade( PdUpgradeType type )
	{
		Assert.True( Networking.IsHost );

		switch ( type )
		{
			case PdUpgradeType.Overheal:
				PdUpgradeOverhealSince = null;
				break;
			case PdUpgradeType.Mp5:
				PdUpgradeMp5Since = null;
				break;
			case PdUpgradeType.Shotgun:
				PdUpgradeShotgunSince = null;
				break;
			case PdUpgradeType.M4:
				PdUpgradeM4Since = null;
				break;
			case PdUpgradeType.AmmoCache:
				PdUpgradeAmmoCacheSince = null;
				break;
		}
	}

	public uint GetUpgradeCost( PdUpgradeType type )
	{
		var config = Config.Current.Game;
		return type switch
		{
			PdUpgradeType.Overheal => config.PdUpgradeOverhealCost,
			PdUpgradeType.Mp5 => config.PdUpgradeMp5Cost,
			PdUpgradeType.Shotgun => config.PdUpgradeShotgunCost,
			PdUpgradeType.M4 => config.PdUpgradeM4Cost,
			PdUpgradeType.AmmoCache => config.PdUpgradeAmmoCacheCost,
			PdUpgradeType.RecruitmentDrive => config.PdUpgradeRecruitmentDriveCost,
			_ => 0
		};
	}

	public static string GetUpgradeDisplayName( PdUpgradeType type )
	{
		return type switch
		{
			PdUpgradeType.Overheal => Language.GetPhrase( "governance.upgrade.overheal" ),
			PdUpgradeType.Mp5 => Language.GetPhrase( "governance.upgrade.mp5" ),
			PdUpgradeType.Shotgun => Language.GetPhrase( "governance.upgrade.shotgun" ),
			PdUpgradeType.M4 => Language.GetPhrase( "governance.upgrade.m4" ),
			PdUpgradeType.AmmoCache => Language.GetPhrase( "governance.upgrade.ammocache" ),
			PdUpgradeType.RecruitmentDrive => Language.GetPhrase( "governance.upgrade.recruitmentdrive" ),
			_ => "Unknown"
		};
	}

	private void ApplyPdUpgradesOnSpawn( Player player )
	{
		Assert.True( Networking.IsHost );

		if ( !player.Job.IsPoliceRole() )
		{
			return;
		}

		// Overheal: set health to 150% of max
		if ( IsUpgradeActive( PdUpgradeType.Overheal ) )
		{
			player.HealthComponent.Health = player.HealthComponent.MaxHealth * 1.5f;
		}

		// MP5
		var mp5 = GameModeEquipments.FindByIdentifier( "mp5" );
		if ( IsUpgradeActive( PdUpgradeType.Mp5 ) && mp5 != null )
		{
			player.GiveHost( mp5, makeActive: false, canDrop: false );
		}

		var shotgun = GameModeEquipments.FindByIdentifier( "spaghelli" );
		if ( IsUpgradeActive( PdUpgradeType.Shotgun ) && shotgun != null )
		{
			player.GiveHost( shotgun, makeActive: false, canDrop: false );
		}

		var m4 = GameModeEquipments.FindByIdentifier( "m4a1" );
		if ( IsUpgradeActive( PdUpgradeType.M4 ) && m4 != null )
		{
			player.GiveHost( m4, makeActive: false, canDrop: false );
		}

	}

	private TimeSince _lastUpgradeDecay = 0;

	private void OnMayorAbsentPdUpgrades()
	{
		if ( _lastUpgradeDecay < Config.Current.Game.PdUpgradeDecayInterval )
		{
			return;
		}

		_lastUpgradeDecay = 0;

		// Remove one active upgrade per decay tick
		foreach ( var type in Enum.GetValues<PdUpgradeType>() )
		{
			if ( type == PdUpgradeType.RecruitmentDrive )
			{
				continue;
			}

			if ( !GetUpgradeSince( type ).HasValue )
			{
				continue;
			}

			DeactivateUpgrade( type );
			var name = GetUpgradeDisplayName( type );
			BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.upgrade.decayed" ), name ) );
			Log.Info( $"Mayor absent, PD upgrade decayed: {name}" );
			return;
		}
	}

	private void ResetUpgradeDecayTimer()
	{
		_lastUpgradeDecay = 0;
	}

	private void OnSecondlyUpdatePdUpgrades()
	{
		foreach ( var type in Enum.GetValues<PdUpgradeType>() )
		{
			if ( type == PdUpgradeType.RecruitmentDrive )
			{
				continue;
			}

			var duration = GetUpgradeDuration( type );
			var since = GetUpgradeSince( type );
			if ( since.HasValue && since.Value >= duration )
			{
				DeactivateUpgrade( type );
				var name = GetUpgradeDisplayName( type );
				BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.upgrade.expired" ), name ) );
				Log.Info( $"PD upgrade expired: {name}" );
			}
		}
	}
}
