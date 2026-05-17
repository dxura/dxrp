namespace Dxura.RP.Shared;

public class GameModeEquipmentDto
{
	public Guid Id { get; init; }
	public Guid GameModeAddonContentId { get; init; }
	public string? NameOverride { get; init; }
	public string? DescriptionOverride { get; init; }

#if ASPNETCORE
	public static GameModeEquipmentDto FromEntity( GameModeEquipment entity ) => new()
	{
		Id = entity.Id,
		GameModeAddonContentId = entity.GameModeAddonContentId,
		NameOverride = entity.NameOverride,
		DescriptionOverride = entity.DescriptionOverride
	};

	public GameModeEquipment ToEntity( GameMode gameMode ) => new()
	{
		Id = Id,
		TenantId = gameMode.TenantId,
		GameModeId = gameMode.Id,
		GameModeAddonContentId = GameModeAddonContentId,
		NameOverride = GameModeDtoHelpers.Trim( NameOverride ),
		DescriptionOverride = GameModeDtoHelpers.Trim( DescriptionOverride )
	};
#endif
}
