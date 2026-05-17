namespace Dxura.RP.Shared;

public class CreateFactionDto
{
	public required string Name { get; init; }
	public required string Tag { get; init; }
	public string? Description { get; init; }

}
