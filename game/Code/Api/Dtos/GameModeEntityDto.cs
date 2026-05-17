namespace Dxura.RP.Shared;

public class GameModeEntityDto
{
	public Guid Id { get; init; }
	public Guid GameModeAddonContentId { get; init; }
	public string? NameOverride { get; init; }
	public string? DescriptionOverride { get; init; }
	public int Limit { get; init; }
	public bool HealthEnabled { get; init; }
	public float HealthAmount { get; init; }
	public bool DestroyOnDisconnect { get; init; }
	public bool DestroyOnJobChange { get; init; }
	public bool AllowOwnershipTransfer { get; init; }

#if ASPNETCORE
	public static GameModeEntityDto FromEntity( GameModeEntity entity ) => new()
	{
		Id = entity.Id,
		GameModeAddonContentId = entity.GameModeAddonContentId,
		NameOverride = entity.NameOverride,
		DescriptionOverride = entity.DescriptionOverride,
		Limit = entity.Limit,
		HealthEnabled = entity.HealthEnabled,
		HealthAmount = entity.HealthAmount,
		DestroyOnDisconnect = entity.DestroyOnDisconnect,
		DestroyOnJobChange = entity.DestroyOnJobChange,
		AllowOwnershipTransfer = entity.AllowOwnershipTransfer
	};

	public GameModeEntity ToEntity( GameMode gameMode ) => new()
	{
		Id = Id,
		TenantId = gameMode.TenantId,
		GameModeId = gameMode.Id,
		GameModeAddonContentId = GameModeAddonContentId,
		NameOverride = GameModeDtoHelpers.Trim( NameOverride ),
		DescriptionOverride = GameModeDtoHelpers.Trim( DescriptionOverride ),
		Limit = Limit,
		HealthEnabled = HealthEnabled,
		HealthAmount = HealthAmount,
		DestroyOnDisconnect = DestroyOnDisconnect,
		DestroyOnJobChange = DestroyOnJobChange,
		AllowOwnershipTransfer = AllowOwnershipTransfer
	};
#endif
}
