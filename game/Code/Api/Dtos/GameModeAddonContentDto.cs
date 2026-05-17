namespace Dxura.RP.Shared;

public class GameModeAddonContentDto
{
	public Guid Id { get; init; }
	public Guid AddonContentId { get; init; }
	public string? Label { get; init; }
	public string? Name { get; init; }
	public string? Description { get; init; }
	public AddonContentType Type { get; init; }
	public string? PrimaryReference { get; init; }
	public string? SecondaryReference { get; init; }
	public string? Grouping { get; init; }
	public string? IconPath { get; init; }
	public string? WorldModelPath { get; init; }
	public float? WorldModelScale { get; init; }
	public string? BaseConfig { get; init; }
	public string? ConfigOverride { get; init; }

#if ASPNETCORE
	public static GameModeAddonContentDto FromEntity( GameModeAddonContent entity ) => new()
	{
		Id = entity.Id,
		AddonContentId = entity.AddonContentId,
		Label = entity.Label,
		Name = entity.AddonContent?.Name,
		Description = entity.AddonContent?.Description,
		Type = entity.AddonContent?.Type ?? default,
		PrimaryReference = entity.AddonContent?.PrimaryReference,
		SecondaryReference = entity.AddonContent?.SecondaryReference,
		Grouping = entity.AddonContent?.Grouping,
		IconPath = entity.AddonContent?.IconPath,
		WorldModelPath = entity.AddonContent?.WorldModelPath,
		WorldModelScale = entity.AddonContent?.WorldModelScale,
		BaseConfig = entity.AddonContent?.BaseConfig?.RootElement.GetRawText(),
		ConfigOverride = entity.ConfigOverride?.RootElement.GetRawText()
	};

	public GameModeAddonContent ToEntity( GameMode gameMode, Guid gameModeAddonId, Guid addonRevisionId ) => new()
	{
		Id = Id,
		TenantId = gameMode.TenantId,
		GameModeId = gameMode.Id,
		GameModeAddonId = gameModeAddonId,
		AddonContentId = AddonContentId,
		AddonRevisionId = addonRevisionId,
		Label = GameModeDtoHelpers.Trim( Label ),
		ConfigOverride = GameModeDtoHelpers.ParseJson( ConfigOverride )
	};
#endif
}
