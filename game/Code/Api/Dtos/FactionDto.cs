namespace Dxura.RP.Shared;

public class FactionDto
{
	public Guid Id { get; set; }
	public string Name { get; set; } = null!;
	public string Tag { get; set; } = null!;
	public string? Description { get; set; }
	public uint Balance { get; set; }
	public uint Level { get; set; }
	public uint Experience { get; set; }
	public uint MaxMembers { get; set; }
	public int MemberCount { get; set; }
	public List<FactionRoleDto> Roles { get; set; } = [];

#if ASPNETCORE
	public static FactionDto FromEntity( Domain.Entities.Faction entity ) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Tag = entity.Tag,
		Description = entity.Description,
		Balance = entity.Balance,
		Level = entity.Level,
		Experience = entity.Experience,
		MaxMembers = entity.MaxMembers,
		MemberCount = entity.TenantPlayers.Count,
		Roles = entity.Roles.Select( FactionRoleDto.FromEntity ).ToList()
	};
#endif
}
