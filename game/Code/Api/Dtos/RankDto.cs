namespace Dxura.RP.Shared;

public class RankDto
{
	public Guid Id { get; init; }
	public required string Name { get; init; }
	public uint Color { get; init; }
	public int Order { get; init; }
	public bool IsDefault { get; init; }
	public required List<string> Permissions { get; init; }
	public Guid? InheritsFromId { get; init; }
	public RankFlags Flags { get; init; }
	public List<Guid> ServerIds { get; init; } = [];

#if ASPNETCORE
	public static RankDto FromEntity( Domain.Entities.Rank entity ) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Color = entity.Color,
		Order = entity.Order,
		IsDefault = entity.IsDefault,
		Permissions = entity.Permissions,
		InheritsFromId = entity.InheritsFromId,
		Flags = entity.Flags,
		ServerIds = entity.ServerIds
	};
#endif
}
