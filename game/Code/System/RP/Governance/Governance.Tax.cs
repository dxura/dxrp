using Sandbox.Diagnostics;
using System.Text.RegularExpressions;

namespace Dxura.RP.Game;

public partial class Governance
{
	[Sync( SyncFlags.FromHost )]
	public float TaxRate { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public uint GovernmentBalance { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public string? TownName { get; private set; }

	private bool _taxDefault = true;
	private bool _mayorSeen;

	private void OnStartTax()
	{
		TaxRate = Config.Current.Game.TaxRateDefault;
	}

	[Rpc.Host]
	public void SetTaxRateHost( float rate )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:tax:rate", Config.Current.Game.TaxRateCooldown ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );

		if ( caller == null || !caller.Job.IsMayoralRole() )
		{
			return;
		}

		_mayorSeen = true;
		rate = Math.Clamp( rate, 0f, Config.Current.Game.TaxRateMax );

		TaxRate = rate;
		_taxDefault = rate == 0f;

		var percentDisplay = $"{rate * 100f:0.#}%";
		BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.tax.rate.set.announcement" ), percentDisplay ) );

		Log.Info( $"Mayor {caller.SteamName} ({caller.SteamId}) set tax rate to {percentDisplay}" );
	}

	[Rpc.Host]
	public void SetTownNameHost( string name )
	{
		const int minTownNameLength = 3;

		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:tax:townname", 5f ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );

		if ( caller == null || !caller.Job.IsMayoralRole() )
		{
			return;
		}

		_mayorSeen = true;
		var maxLength = Config.Current.Game.TownNameMaxLength;
		name = Regex.Replace( name, @"\s+", " " ).Trim();
		name = GameManager.ModerateText( caller.SteamId, "TOWN NAME", name ).Trim();

		if ( name.Length < minTownNameLength || name.Length > maxLength )
		{
			caller.Error( string.Format( Language.GetPhrase( "notify.townname.length" ), maxLength ) );
			return;
		}

		var cost = Config.Current.Game.TownNameCost;
		if ( !SpendFromGovernmentBalance( cost ) )
		{
			caller.Error( "#governance.tax.insufficient_funds" );
			return;
		}
		
		_ = ServerApiClient.Audit( "MayorTown",  $"Mayor {caller.SteamName} ({caller.SteamId}) set town name to: {name} for ${cost}", caller.SteamId );

		TownName = name;
		_taxDefault = false;

		BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.tax.townname.renamed.announcement" ), name ) );
		Log.Info( $"Mayor {caller.SteamName} ({caller.SteamId}) renamed town to: {name} for ${cost}" );
	}

	private void ResetTaxHost( bool announce )
	{
		Assert.True( Networking.IsHost );

		var defaultRate = Config.Current.Game.TaxRateDefault;

		if ( _taxDefault && GovernmentBalance == 0 && TownName == null )
		{
			return;
		}

		TaxRate = defaultRate;
		TownName = null;
		_taxDefault = true;
		_mayorSeen = false;

		if ( Config.Current.Game.TaxResetTreasuryOnMayorDeath )
		{
			GovernmentBalance = 0;
		}

		if ( announce )
		{
			var percentDisplay = $"{defaultRate * 100f:0.#}%";
			Log.Info( $"Mayor missing, tax rate reset to {percentDisplay}." );
			BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.tax.reset.announcement" ), percentDisplay ) );
		}
	}

	private void OnMayorAbsentTax()
	{
		if ( !_mayorSeen )
		{
			return;
		}

		if ( !_taxDefault || GovernmentBalance > 0 )
		{
			ResetTaxHost( announce: true );
		}
	}

	public bool IsExemptFromTax( Player player )
	{
		if ( Config.Current.Game.TaxExemptGovernment && player.Job.IsGovernmentRole() )
		{
			return true;
		}

		if ( player.PlayTime < Config.Current.Game.TaxExemptPlayTimeThreshold )
		{
			return true;
		}

		if ( player.BankBalance < Config.Current.Game.TaxExemptBankBalanceThreshold )
		{
			return true;
		}

		return false;
	}

	public void AddToGovernmentBalance( uint amount )
	{
		Assert.True( Networking.IsHost );
		GovernmentBalance += amount;
	}

	public bool SpendFromGovernmentBalance( uint amount )
	{
		Assert.True( Networking.IsHost );

		if ( GovernmentBalance < amount )
		{
			return false;
		}

		GovernmentBalance -= amount;
		return true;
	}

	/// <summary>
	///     Stub for future bank raid mechanic. Returns the amount distributable to raiders.
	/// </summary>
	public uint RobGovernmentBalance( float vanishPercent )
	{
		Assert.True( Networking.IsHost );

		var total = GovernmentBalance;
		if ( total == 0 )
		{
			return 0;
		}

		var vanished = (uint)(total * vanishPercent);
		var distributable = total - vanished;
		GovernmentBalance = 0;

		return distributable;
	}
}
