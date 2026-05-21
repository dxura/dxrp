namespace Dxura.RP.Shared;

public class GameModeMarketItemDto
{
	public Guid Id { get; init; }
	public GameModeMarketItemType Type { get; init; }
	public Guid? ReferenceId { get; init; }
	public string? Grouping { get; init; }
	public int Cost { get; init; }
	public int Limit { get; init; }
	public int Color { get; init; }
	public int Quantity { get; init; } = 1;
	public Guid[] WhitelistJobIds { get; init; } = [];
	public Guid[] BlacklistJobIds { get; init; } = [];
	public string[] WhitelistJobTags { get; init; } = [];
	public string[] BlacklistJobTags { get; init; } = [];

#if ASPNETCORE
	public static GameModeMarketItemDto FromEntity( GameModeMarketItem entity ) => new()
	{
		Id = entity.Id,
		Type = entity.Type,
		ReferenceId = entity.ReferenceId,
		Grouping = entity.Grouping,
		Cost = entity.Cost,
		Limit = entity.Limit,
		Color = entity.Color,
		Quantity = entity.Quantity,
		WhitelistJobIds = entity.WhitelistJobIds,
		BlacklistJobIds = entity.BlacklistJobIds,
		WhitelistJobTags = entity.WhitelistJobTags,
		BlacklistJobTags = entity.BlacklistJobTags
	};

	public GameModeMarketItem ToEntity( GameMode gameMode ) => new()
	{
		Id = Id,
		TenantId = gameMode.TenantId,
		GameModeId = gameMode.Id,
		Type = Type,
		ReferenceId = ReferenceId,
		Grouping = GameModeDtoHelpers.Trim( Grouping ),
		Cost = Cost,
		Limit = Limit,
		Color = Color,
		Quantity = Quantity,
		WhitelistJobIds = GameModeDtoHelpers.Dedup( WhitelistJobIds ),
		BlacklistJobIds = GameModeDtoHelpers.Dedup( BlacklistJobIds ),
		WhitelistJobTags = GameModeDtoHelpers.Dedup( WhitelistJobTags ),
		BlacklistJobTags = GameModeDtoHelpers.Dedup( BlacklistJobTags )
	};
#endif
}
