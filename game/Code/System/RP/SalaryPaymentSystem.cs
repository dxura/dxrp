using Sandbox.Diagnostics;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public class SalaryPaymentSystem : Component, IGameEvents
{
	[Property] private int PayCycleSeconds { get; set; } = 300;
	[Property] private int MaxConcurrentSalaryPayments { get; set; } = 8;

	private TimeSince LastPaidTime { get; set; } = 0;

	protected override void OnStart()
	{
		if ( !Config.Current.Game.SalaryPaymentEnabled )
		{
			Destroy();
			return;
		}
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		// Do we need to pay?
		if ( LastPaidTime.Relative < PayCycleSeconds )
		{
			return;
		}
		LastPaidTime = 0;

		_ = HandlePayouts();
	}

	private async Task HandlePayouts()
	{
		var taxEnabled = Config.Current.Game.GovernanceTaxEnabled;
		var taxRate = taxEnabled ? Governance.Current.TaxRate : 0f;
		var maxConcurrentPayments = Math.Max( 1, MaxConcurrentSalaryPayments );
		var players = GameUtils.Players
			.Where( player => player.IsValid() && player.IsConnected && !player.IsDebugPlayer )
			.ToList();

		for ( var i = 0; i < players.Count; i += maxConcurrentPayments )
		{
			var payments = players
				.Skip( i )
				.Take( maxConcurrentPayments )
				.Select( player => PaySalaryHost( player, taxRate ) )
				.ToList();

			foreach ( var payment in payments )
			{
				var (player, taxAmount, paid) = await payment;
				if ( !paid )
				{
					continue;
				}

				if ( taxAmount > 0 )
				{
					Governance.Current.AddToGovernmentBalance( taxAmount );
					player.Info( string.Format( Language.GetPhrase( "notify.salary.paid.tax" ), $"{taxAmount:N0}" ), 3f );
				}
				else
				{
					player.Info( "#notify.salary.paid", 3f );
				}

				player.PlayTime++; // Temporary, until we can do it via server pulse
			}
		}
	}

	private static async Task<(Player player, uint taxAmount, bool paid)> PaySalaryHost( Player player, float taxRate )
	{
		var isAfk = player.HasStatus( Constants.AfkStatus );
		var baseSalary = (uint)(player.Job.Salary * GameManager.Instance.SalaryMultiplier * (isAfk ? 0.5 : 1));

		uint taxAmount = 0;
		if ( taxRate > 0f && !Governance.Current.IsExemptFromTax( player ) )
		{
			taxAmount = (uint)(baseSalary * taxRate);
		}

		var netSalary = baseSalary - taxAmount;

		if ( !await player.PayHost( netSalary, "Salary", true ) )
		{
			return (player, taxAmount, false);
		}

		return (player, taxAmount, true);
	}
}
