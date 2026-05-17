namespace Dxura.RP.Shared;

public class SanctionActionDto : BaseServerActionDto
{
	public SanctionType Type { get; set; }
	public SanctionModifier Modifiers { get; set; } = SanctionModifier.None;
	public bool Silent { get; set; }

	public required long PlayerId { get; set; }

	public string Reason { get; set; } = null!;

	public TimeSpan? Duration { get; set; }
}
