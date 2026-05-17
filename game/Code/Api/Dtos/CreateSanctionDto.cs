namespace Dxura.RP.Shared;

public class CreateSanctionDto
{
	public required SanctionType Type { get; set; }

	public string Reason { get; set; } = null!;
	public string? Notes { get; set; }
	public SanctionFlags Flags { get; set; } = SanctionFlags.None;

	public bool IsGlobal { get; set; }

	public TimeSpan? Duration { get; set; }

	public SanctionModifier Modifiers { get; set; } = SanctionModifier.None;
}
