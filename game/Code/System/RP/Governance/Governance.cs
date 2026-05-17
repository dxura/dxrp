using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public partial class Governance : GameObjectSystem<Governance>, IConfigEvents
{
	private bool _initialized;

	private const int MayorCheckInterval = 5;
	private TimeSince _lastMayorCheck = 0;

	public Governance( Scene scene ) : base( scene )
	{
	}


	void IConfigEvents.OnConfigAppliedHost()
	{
		if ( _initialized )
		{
			return;
		}

		Assert.True(Networking.IsHost);

		_initialized = true;
		OnStartGovernance();
	}

	void IGameEvents.OnPlayerJobChangedHost( Player player, GameModeJobDto before, GameModeJobDto after )
	{
		if ( !Config.Current.Game.GovernanceTaxEnabled )
		{
			return;
		}

		if ( before.IsMayoralRole() )
		{
			ResetTaxHost( announce: true );
		}

		if ( after.IsMayoralRole() && Config.Current.Game.TaxResetTreasuryOnMayorElect )
		{
			GovernmentBalance = 0;
		}
	}

	void IGameEvents.OnPlayerSpawnedHost( Player player )
	{
		if ( Config.Current.Game.GovernanceTaxEnabled )
		{
			ApplyPdUpgradesOnSpawn( player );
		}
	}

	private void OnStartGovernance()
	{
		if ( Config.Current.Game.GovernanceLawEnabled )
		{
			OnStartLaw();
		}

		OnStartWarrant();

		if ( Config.Current.Game.GovernanceTaxEnabled )
		{
			OnStartTax();
		}
	}

	void IGameEvents.OnSecondlyUpdate()
	{
		if ( Scene.IsEditor || !Networking.IsHost )
		{
			return;
		}

		// Shared mayor-presence poll
		if ( _lastMayorCheck >= MayorCheckInterval )
		{
			_lastMayorCheck = 0;

			if ( !GameUtils.GetPlayersByJobTag( JobTag.Mayoral ).Any() )
			{
				OnMayorAbsentHost();
			}
			else
			{
				_mayorSeen = true;
				ResetUpgradeDecayTimer();
			}
		}

		if ( Config.Current.Game.GovernanceJailEnabled )
		{
			OnSecondlyUpdateJail();
		}

		OnSecondlyUpdateWarrant();

		if ( Config.Current.Game.GovernanceTaxEnabled )
		{
			OnSecondlyUpdatePdUpgrades();
		}
	}

	private void OnMayorAbsentHost()
	{
		if ( Config.Current.Game.GovernanceLawEnabled )
		{
			OnMayorAbsentLaw();
		}

		if ( Config.Current.Game.GovernanceTaxEnabled )
		{
			OnMayorAbsentTax();
			OnMayorAbsentPdUpgrades();
		}
	}

	protected void BroadcastGovernanceAnnouncementHost( string message )
	{
		var duration = Config.Current.Game.GovernanceAnnouncementDuration;
		if ( duration <= 0f )
		{
			return;
		}

		GameManager.Instance.BroadcastAnnouncementHost( message, duration: duration );
	}
}
