namespace Dxura.RP.Shared;

public class InventoryItemDto
{
	public Guid Id { get; init; }
	public required ItemDefinitionDto Definition { get; init; }
	public int Quantity { get; init; }

#if ASPNETCORE
	public static InventoryItemDto FromEntity( Domain.Entities.InventoryItem entity ) => new()
	{
		Id = entity.Id,
		Definition = ItemDefinitionDto.FromEntity( entity.Item! ),
		Quantity = entity.Quantity
	};
#endif
}
