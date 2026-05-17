namespace Dxura.RP.Shared;

public class SetLevelActionDto : BaseServerActionDto
{
	public required long PlayerId { get; set; }
	public required int Level { get; set; }
}
