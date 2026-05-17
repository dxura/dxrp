namespace Dxura.RP.Shared;

public class PermissionDefinitionDto
{
	public required string Identifier { get; init; }
	public required string Name { get; init; }
	public required string Description { get; init; }
	public required string Category { get; init; }

#if ASPNETCORE
	public static PermissionDefinitionDto FromPermissionMeta( PermissionMetaAttribute meta ) => new()
	{
		Identifier = meta.Id,
		Name = meta.Name,
		Description = meta.Description,
		Category = meta.Category
	};
#endif
}
