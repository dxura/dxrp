namespace Dxura.RP.Shared;

public class CreateFactionRoleDto
{
	public required string Name { get; init; }
	public string? Description { get; init; }
	public int Order { get; init; }
	public FactionPermission Permission { get; init; }
}
