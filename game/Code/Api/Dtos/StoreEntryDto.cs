namespace Dxura.RP.Shared;

public class StoreEntryDto
{
	public string Key { get; init; } = null!;
	public string Value { get; init; } = null!;
	public DateTimeOffset? ExpiresAt { get; init; }
}
