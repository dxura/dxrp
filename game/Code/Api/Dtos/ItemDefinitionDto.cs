namespace Dxura.RP.Shared;

public class ItemDefinitionDto
{
	public Guid Id { get; init; }
	public required string Name { get; init; }
	public string? Description { get; init; }
	public string? ImageUrl { get; set; }
	public string? GrantIdentifier { get; init; }
	public ItemType Type { get; init; }
	public ItemRarity Rarity { get; init; }
	public bool IsTradable { get; init; }
	public bool IsStackable { get; init; }
	public int? MaxStack { get; init; }

#if ASPNETCORE
	public static ItemDefinitionDto FromEntity( Domain.Entities.Item entity ) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		Description = entity.Description,
		GrantIdentifier = entity.GrantIdentifier,
		Type = entity.Type,
		Rarity = entity.Rarity,
		IsTradable = entity.IsTradable,
		IsStackable = entity.IsStackable,
		MaxStack = entity.MaxStack
	};
#endif
}
