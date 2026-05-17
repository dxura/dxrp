namespace Dxura.RP.Shared;

public class RestartServerActionDto : BaseServerActionDto
{
	/// <summary>
	/// Delay in seconds before restarting
	/// </summary>
	public int DelaySeconds { get; set; }

	public string? Reason { get; set; }

	public bool RefundEntities { get; set; } = true;

	public bool BankWallets { get; set; } = true;
}
