namespace Dxura.RP.Shared;

public class FactionRoleDto
{
	public Guid Id { get; set; }
	public string Name { get; set; } = null!;
	public string? Description { get; set; }
	public int Order { get; set; }
	public FactionPermission Permission { get; set; }

#if ASPNETCORE
	public static FactionRoleDto FromEntity( Domain.Entities.FactionRole entity ) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Description = entity.Description,
		Order = entity.Order,
		Permission = entity.Permission
	};
#endif
}
