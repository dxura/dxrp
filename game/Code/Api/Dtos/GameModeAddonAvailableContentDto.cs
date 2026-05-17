namespace Dxura.RP.Shared;

public class GameModeAddonAvailableContentDto
{
	public Guid Id { get; init; }
	public string? Name { get; init; }
	public string? Description { get; init; }
	public AddonContentType Type { get; init; }
	public string? PrimaryReference { get; init; }
	public string? Grouping { get; init; }

#if ASPNETCORE
	public static GameModeAddonAvailableContentDto FromEntity( AddonContent entity ) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Description = entity.Description,
		Type = entity.Type,
		PrimaryReference = entity.PrimaryReference,
		Grouping = entity.Grouping
	};
#endif
}
