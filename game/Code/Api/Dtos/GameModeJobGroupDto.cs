namespace Dxura.RP.Shared;

public class GameModeJobGroupDto
{
	public Guid Id { get; init; }
	public required string Name { get; init; }
	public required string Description { get; init; }
	public uint Color { get; init; }

#if ASPNETCORE
	public static GameModeJobGroupDto FromEntity( GameModeJobGroup entity ) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Description = entity.Description,
		Color = entity.Color
	};

	public GameModeJobGroup ToEntity( GameMode gameMode ) => new()
	{
		Id = Id,
		TenantId = gameMode.TenantId,
		GameModeId = gameMode.Id,
		Name = Name,
		Description = Description,
		Color = Color
	};
#endif
}
