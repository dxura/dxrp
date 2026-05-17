namespace Dxura.RP.Shared;

public class GameModeJobDto
{
	public Guid Id { get; init; }
	public Guid? GameModeJobGroupId { get; init; }
	public Guid? PrerequisiteJobId { get; init; }
	public required string Name { get; init; }
	public required string Description { get; init; }
	public uint Color { get; init; }
	public required string Model { get; init; }
	public string[] Clothes { get; init; } = [];
	public string[] JobTags { get; init; } = [];
	public int Salary { get; init; }
	public bool IncludeDefaultEquipment { get; init; }
	public Guid[] GameModeEquipmentIds { get; init; } = [];
	public int Health { get; init; }
	public bool DemoteOnRespawn { get; init; }
	public string? Interaction { get; init; }
	public bool Selectable { get; init; }
	public bool Demotable { get; init; }
	public int? PlayTime { get; init; }
	public int MaxCount { get; init; }
	public bool VoteRequired { get; init; }
	public bool ElectionRequired { get; init; }

#if ASPNETCORE
	public static GameModeJobDto FromEntity( GameModeJob entity ) => new()
	{
		Id = entity.Id,
		GameModeJobGroupId = entity.GameModeJobGroupId,
		PrerequisiteJobId = entity.PrerequisiteJobId,
		Name = entity.Name,
		Description = entity.Description,
		Color = entity.Color,
		Model = entity.Model,
		Clothes = entity.Clothes,
		JobTags = entity.JobTags,
		Salary = entity.Salary,
		IncludeDefaultEquipment = entity.IncludeDefaultEquipment,
		GameModeEquipmentIds = entity.GameModeEquipmentIds,
		Health = entity.Health,
		DemoteOnRespawn = entity.DemoteOnRespawn,
		Interaction = entity.Interaction,
		Selectable = entity.Selectable,
		Demotable = entity.Demotable,
		PlayTime = entity.PlayTime,
		MaxCount = entity.MaxCount,
		VoteRequired = entity.VoteRequired,
		ElectionRequired = entity.ElectionRequired
	};

	public GameModeJob ToEntity( GameMode gameMode ) => new()
	{
		Id = Id,
		TenantId = gameMode.TenantId,
		GameModeId = gameMode.Id,
		GameModeJobGroupId = GameModeJobGroupId,
		PrerequisiteJobId = PrerequisiteJobId,
		Name = Name,
		Description = Description,
		Color = Color,
		Model = Model,
		Clothes = Clothes,
		JobTags = GameModeDtoHelpers.Dedup( JobTags ),
		Salary = Salary,
		IncludeDefaultEquipment = IncludeDefaultEquipment,
		GameModeEquipmentIds = GameModeDtoHelpers.Dedup( GameModeEquipmentIds ),
		Health = Health,
		DemoteOnRespawn = DemoteOnRespawn,
		Interaction = Interaction,
		Selectable = Selectable,
		Demotable = Demotable,
		PlayTime = PlayTime,
		MaxCount = MaxCount,
		VoteRequired = VoteRequired,
		ElectionRequired = ElectionRequired
	};
#endif
}
