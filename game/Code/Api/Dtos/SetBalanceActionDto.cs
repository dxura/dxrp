namespace Dxura.RP.Shared;

public class SetBalanceActionDto : BaseServerActionDto
{
	public required long PlayerId { get; set; }
	public required uint Balance { get; set; }
}
