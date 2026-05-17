using System.Threading.Tasks;
namespace Dxura.RP.Game.Commands;

public class DropMoneyCommand : ICommand
{
	public const string Name = "dropmoney";
	public string Command => Name;
	public string Help => "/dropmoney <amount|*>";
	public bool IsUsableWhileDead => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:money", Config.Current.Game.MoneyCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return true;
		}

		if ( args.Length < 1 || !TryParseAmount( caller, args[0], out var amount ) || amount == 0 )
		{
			return false;
		}

		var useBankForExcess = Config.Current.Game.DropMoneyUsesBankForExcess;

		if ( Config.Current.Game.MoneyEnabled )
		{
			var available = useBankForExcess ? caller.WalletBalance + caller.BankBalance : caller.WalletBalance;
			if ( available < amount )
			{
				caller.Error( "#notify.cash.poor" );
				return true;
			}
		}

		_ = DropAsync( caller, amount, useBankForExcess );
		return true;
	}

	private static bool TryParseAmount( Player caller, string arg, out uint amount )
	{
		if ( arg == "*" )
		{
			amount = caller.WalletBalance;
			return true;
		}

		return uint.TryParse( arg, out amount );
	}

	private static async Task DropAsync( Player caller, uint amount, bool useBankForExcess )
	{
		bool charged;
		if ( useBankForExcess && Config.Current.Game.MoneyEnabled && caller.WalletBalance < amount )
		{
			var walletPortion = caller.WalletBalance;
			var bankPortion = amount - walletPortion;

			if ( walletPortion > 0 && !await caller.ChargeHost( walletPortion, "Drop Money (wallet)" ) )
			{
				caller.Error( "#generic.error" );
				return;
			}

			charged = await caller.ChargeHost( bankPortion, "Drop Money (bank)", true );
		}
		else
		{
			charged = await caller.ChargeHost( amount, "Drop Money" );
		}

		await GameTask.MainThread();

		if ( !caller.IsValid() )
		{
			return;
		}

		if ( !charged )
		{
			caller.Error( "#generic.error" );
			return;
		}

		Log.Info( $"Player {caller.SteamId} dropped money (${amount})" );
		GameManager.Instance.DropMoneyHost( amount, GameUtils.GetSpawnPosition( caller.AimRay ), $"Player drop: {caller.SteamName} ({caller.SteamId})" );
	}
}
