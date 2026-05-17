namespace Dxura.RP.Shared;

public class ItemDto
{
	public Guid Id { get; init; }
	public required string Name { get; init; }
	public string? Description { get; init; }
	public string? ImageUrl { get; set; }
	public string? GrantIdentifier { get; init; }
	public ItemType Type { get; init; }
	public ItemRarity Rarity { get; init; }
	public bool IsStackable { get; init; }
	public int? MaxStack { get; init; }
	public bool IsTradable { get; init; }
	public bool IsMarketable { get; init; }
	public Dictionary<string, object>? Metadata { get; init; }

#if ASPNETCORE
	public static ItemDto FromEntity( Domain.Entities.Item entity ) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Description = entity.Description,
		GrantIdentifier = entity.GrantIdentifier,
		Type = entity.Type,
		Rarity = entity.Rarity,
		IsStackable = entity.IsStackable,
		MaxStack = entity.MaxStack,
		IsTradable = entity.IsTradable,
		IsMarketable = entity.IsMarketable,
		Metadata = entity.Metadata
	};
#endif
}
