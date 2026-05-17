using System.Threading.Tasks;
namespace Dxura.RP.Game;

public sealed class Atm : Component
{
	[Property] [Group( "Effects" )]
	public required SoundEvent? WithdrawSound { get; set; }

	[Property] [Group( "Effects" )]
	public required SoundEvent? DepositSound { get; set; }

	[Property] [Group( "Effects" )]
	public required SoundEvent? ErrorSound { get; set; }

	[Rpc.Host]
	public void Deposit( uint amount )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:atm", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		// Require player to be within range
		if ( WorldPosition.Distance( player.WorldPosition ) > Config.Current.Game.ReachDistance )
		{
			return;
		}

		if ( player.WalletBalance < amount )
		{
			player.Warn( "#notify.cash.poor" );
			ErrorSound.BroadcastHost( WorldPosition );

			return;
		}

		_ = DepositMoney( player, amount );

		DepositSound.BroadcastHost( WorldPosition );

	}
	
	private async Task<bool> DepositMoney( Player player, uint amount )
	{
		if ( !await player.ChargeHost( amount, "ATM Deposit" ) )
		{
			player.Error( "#generic.error" );
			return true;
		}

		// Calculate tax
		uint taxAmount = 0;
		if ( Config.Current.Game.GovernanceTaxEnabled && Governance.Current.TaxRate > 0f && !Governance.Current.IsExemptFromTax( player ) )
		{
			taxAmount = (uint)(amount * Governance.Current.TaxRate);
		}

		var netAmount = amount - taxAmount;
		var didSucceed = await player.PayHost( netAmount, "ATM Deposit", true );

		if ( !didSucceed )
		{
			await player.PayHost( amount, "ATM Deposit Fail" );
			player.Error( "#generic.error" );
			ErrorSound.BroadcastHost( WorldPosition );

			return true;
		}

		if ( taxAmount > 0 )
		{
			Governance.Current.AddToGovernmentBalance( taxAmount );
			player.Info( string.Format( Language.GetPhrase( "governance.tax.deducted" ), $"{taxAmount:N0}" ), 5f );
		}

		_ = ServerApiClient.Audit( "ATM", $"{player.SteamName} ({player.SteamId}) has deposited {netAmount} (tax: {taxAmount}) into their bank account, total balance: {player.BankBalance}", player.SteamId );
		return false;
	}

	[Rpc.Host]
	public void Withdraw( uint amount )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:atm", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		// Require player to be within range
		if ( WorldPosition.Distance( player.WorldPosition ) > Config.Current.Game.ReachDistance )
		{
			return;
		}

		if ( player.BankBalance < amount )
		{
			player.Warn( "#notify.cash.poor" );
			ErrorSound.BroadcastHost( WorldPosition );

			return;
		}

		_ = WithdrawMoney( player, amount );

		WithdrawSound.BroadcastHost( WorldPosition );
	}
	
	private static async Task<bool> WithdrawMoney( Player player, uint amount )
	{

		if ( !await player.ChargeHost( amount, "ATM Withdraw", true ) )
		{
			player.Error( "#generic.error" );
			return true;
		}

		if ( await player.PayHost( amount, "ATM Withdraw" ) )
		{
			_ = ServerApiClient.Audit( "ATM", $"{player.SteamName} ({player.SteamId}) has withdrawn {amount} from their bank account, total balance: {player.BankBalance}", player.SteamId );
			return false;
		}

		await player.PayHost( amount, "ATM Withdraw Fail", true );

		player.Error( "#generic.error" );
		return true;
	}
}
