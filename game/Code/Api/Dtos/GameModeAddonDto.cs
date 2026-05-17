namespace Dxura.RP.Shared;

public class GameModeAddonDto
{
	public Guid Id { get; init; }
	public Guid AddonId { get; init; }
	public Guid AddonRevisionId { get; init; }

	public string? AddonName { get; init; }
	public string? AddonDescription { get; init; }
	public int RevisionNumber { get; init; }
	public string? SboxVersion { get; init; }

	public string? GlobalBaseConfig { get; init; }
	public string? GlobalConfigOverride { get; init; }

	public List<GameModeAddonContentDto> Contents { get; init; } = [];

	public DateTimeOffset Created { get; init; }

#if ASPNETCORE
	public static GameModeAddonDto FromEntity( GameModeAddon entity ) => new()
	{
		Id = entity.Id,
		AddonId = entity.AddonId,
		AddonRevisionId = entity.AddonRevisionId,
		AddonName = entity.Addon?.Name,
		AddonDescription = entity.Addon?.Description,
		RevisionNumber = entity.AddonRevision?.RevisionNumber ?? 0,
		SboxVersion = entity.AddonRevision?.SboxVersion,
		GlobalBaseConfig = entity.AddonRevision?.GlobalBaseConfig?.RootElement.GetRawText(),
		GlobalConfigOverride = entity.GlobalConfigOverride?.RootElement.GetRawText(),
		Contents = entity.Contents.Select( GameModeAddonContentDto.FromEntity ).ToList(),
		Created = entity.Created
	};

	public GameModeAddon ToEntity( GameMode gameMode ) => new()
	{
		Id = Id,
		TenantId = gameMode.TenantId,
		GameModeId = gameMode.Id,
		AddonId = AddonId,
		AddonRevisionId = AddonRevisionId,
		GlobalConfigOverride = GameModeDtoHelpers.ParseJson( GlobalConfigOverride ),
		Contents = Contents.Select( c => c.ToEntity( gameMode, Id, AddonRevisionId ) ).ToList()
	};
#endif
}
