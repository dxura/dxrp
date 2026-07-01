namespace Dxura.RP.Shared;

public class GameModeDto
{
	public Guid Id { get; init; }
	public required string Name { get; init; }
	public string? Description { get; init; }
	public Visibility Visibility { get; init; }

	public Guid DefaultJobId { get; init; }
	public uint StartingBalance { get; init; }
	public Guid[] DefaultEquipmentIds { get; init; } = [];

	public List<GameModeAddonDto> Addons { get; init; } = [];

	public List<GameModeEquipmentDto> Equipments { get; init; } = [];
	public List<GameModeEntityDto> Entities { get; init; } = [];
	public List<GameModeMarketItemDto> MarketItems { get; init; } = [];

	public List<GameModeJobDto> Jobs { get; init; } = [];
	public List<GameModeJobGroupDto> JobGroups { get; init; } = [];

	public DateTimeOffset Created { get; init; }
	public DateTimeOffset LastModified { get; init; }

#if ASPNETCORE
	public static GameModeDto FromEntity( Domain.Entities.Content.GameMode entity ) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Description = entity.Description,
		Visibility = entity.Visibility,

		DefaultJobId = entity.DefaultJobId ?? entity.Jobs.FirstOrDefault()?.Id ?? Guid.Empty,
		StartingBalance = entity.StartingBalance,
		DefaultEquipmentIds = entity.DefaultEquipmentIds,

		Addons = entity.Addons.Select( GameModeAddonDto.FromEntity ).ToList(),

		Equipments = entity.Equipments.Select( GameModeEquipmentDto.FromEntity ).ToList(),
		Entities = entity.Entities.Select( GameModeEntityDto.FromEntity ).ToList(),
		MarketItems = entity.MarketItems.Select( GameModeMarketItemDto.FromEntity ).ToList(),

		Jobs = entity.Jobs.Select( GameModeJobDto.FromEntity ).ToList(),
		JobGroups = entity.JobGroups.Select( GameModeJobGroupDto.FromEntity ).ToList(),

		Created = entity.Created,
		LastModified = entity.LastModified
	};
#endif
}
