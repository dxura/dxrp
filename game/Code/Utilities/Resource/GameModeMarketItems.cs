using Dxura.RP.Shared;
using Dxura.RP.Game.Entities;

namespace Dxura.RP.Game;

public static class GameModeMarketItems
{
	public const string ShipmentPrefabPath = "gameplay/entities/shipment/shipment.prefab";

	public static IReadOnlyList<GameModeMarketItemDto> All => Config.Current.GameMode.MarketItems;

	public static GameModeMarketItemDto? FindById( Guid? id )
	{
		if ( !id.HasValue || id.Value == Guid.Empty )
		{
			return null;
		}

		return All.FirstOrDefault( item => item.Id == id.Value );
	}

	public static GameModeEntityDto? ResolveEntity( GameModeMarketItemDto? item )
	{
		if ( item?.Type != GameModeMarketItemType.Entity || !item.ReferenceId.HasValue )
		{
			return null;
		}

		return GameModeEntities.FindById( item.ReferenceId.Value );
	}

	public static GameModeEquipmentDto? ResolveEquipment( GameModeMarketItemDto? item )
	{
		if ( item?.Type != GameModeMarketItemType.Equipment || !item.ReferenceId.HasValue )
		{
			return null;
		}

		return GameModeEquipments.FindById( item.ReferenceId.Value );
	}

	public static string DisplayName( GameModeMarketItemDto? item )
	{
		if ( item == null )
		{
			return string.Empty;
		}

		return item.Type switch
		{
			GameModeMarketItemType.Entity => ResolveEntity( item )?.DisplayName() ?? "Unnamed Entity",
			GameModeMarketItemType.Equipment => ResolveEquipment( item ) is { } equipment
				? GetEquipmentDisplayName( equipment, item.Quantity )
				: "Unnamed Equipment",
			_ => "Unknown Item"
		};
	}

	public static string DisplayDescription( GameModeMarketItemDto? item )
	{
		if ( item == null )
		{
			return string.Empty;
		}

		return item.Type switch
		{
			GameModeMarketItemType.Entity => ResolveEntity( item )?.DisplayDescription() ?? string.Empty,
			GameModeMarketItemType.Equipment => ResolveEquipment( item )?.DisplayDescription() ?? string.Empty,
			_ => string.Empty
		};
	}

	public static string Grouping( GameModeMarketItemDto? item )
	{
		if ( item == null || string.IsNullOrWhiteSpace( item.Grouping ) )
		{
			return string.Empty;
		}

		return item.Grouping.Trim();
	}

	public static int GetOwnedCount( Player player, GameModeMarketItemDto? item )
	{
		if ( !player.IsValid() || item == null )
		{
			return 0;
		}

		if ( item.Type == GameModeMarketItemType.Entity )
		{
			return GetOwnedEntityCount( player, item.ReferenceId );
		}

		var worldOwned = player.Scene.GetAllComponents<ShipmentEntity>()
			.Count( entity => entity.IsValid() &&
			                 entity.MarketItemId == item.Id &&
			                 entity.Owner == player.SteamId );
		var equippedOwned = player.Equipment.Count( equipment => equipment.IsValid() && equipment.Enabled && equipment.MarketItemId == item.Id );
		return worldOwned + equippedOwned;
	}

	public static int GetOwnedEquipmentCount( Player player, Guid? equipmentId )
	{
		if ( !player.IsValid() || !equipmentId.HasValue || equipmentId == Guid.Empty )
		{
			return 0;
		}

		var marketItemIds = All
			.Where( item => item.Type == GameModeMarketItemType.Equipment && item.ReferenceId == equipmentId.Value )
			.Select( item => item.Id )
			.ToHashSet();

		var worldOwned = player.Scene.GetAllComponents<ShipmentEntity>()
			.Count( entity => entity.IsValid() &&
			                 marketItemIds.Contains( entity.MarketItemId ) &&
			                 entity.Owner == player.SteamId );
		var equippedOwned = player.Equipment.Count( equipment => equipment.IsValid() && equipment.Enabled && marketItemIds.Contains( equipment.MarketItemId ) );
		return worldOwned + equippedOwned;
	}

	public static int GetOwnedEntityCount( Player player, Guid? entityId )
	{
		if ( !player.IsValid() || !entityId.HasValue || entityId == Guid.Empty )
		{
			return 0;
		}

		return player.Scene.GetAllComponents<BaseEntity>()
			.Count( entity => entity.IsValid() &&
			                 entity.Owner == player.SteamId &&
			                 entity.GameModeEntityId == entityId.Value );
	}

	public static bool CanPurchase( Player player, GameModeMarketItemDto? item )
	{
		if ( !player.IsValid() || item == null || player.Restricted )
		{
			return false;
		}

		switch ( item.Type )
		{
			case GameModeMarketItemType.Entity:
				var entity = ResolveEntity( item );
				if ( entity == null || string.IsNullOrWhiteSpace( entity.PrefabPath() ) )
				{
					return false;
				}
				if ( entity.Limit > 0 && GetOwnedEntityCount( player, entity.Id ) >= entity.Limit )
				{
					return false;
				}
				break;

			case GameModeMarketItemType.Equipment:
				var equipment = ResolveEquipment( item );
				if ( equipment == null || item.Quantity <= 0 )
				{
					return false;
				}
				if ( item.Quantity == 1 && (player.CanTake( equipment ) == Player.PickupResult.None || string.IsNullOrWhiteSpace( equipment.PrefabPath() )) )
				{
					return false;
				}
				if ( equipment.Limit > 0 && GetOwnedEquipmentCount( player, equipment.Id ) >= equipment.Limit )
				{
					return false;
				}
				break;

			default:
				return false;
		}

		if ( item.BlacklistJobIds.Contains( player.Job.Id ) )
		{
			return false;
		}

		if ( item.BlacklistJobTags.Any( tag => JobTags.HasNamedTag( player.Job, tag ) ) )
		{
			return false;
		}

		var hasWhitelist = item.WhitelistJobIds.Length > 0 || item.WhitelistJobTags.Length > 0;
		if ( hasWhitelist )
		{
			var matchesWhitelistedJob = item.WhitelistJobIds.Contains( player.Job.Id );
			var matchesWhitelistedTag = item.WhitelistJobTags.Any( tag => JobTags.HasNamedTag( player.Job, tag ) );
			if ( !matchesWhitelistedJob && !matchesWhitelistedTag )
			{
				return false;
			}
		}

		return true;
	}

	private static string GetEquipmentDisplayName( GameModeEquipmentDto equipment, int quantity )
	{
		if ( quantity <= 1 )
		{
			return equipment.DisplayName();
		}

		var shipmentKey = $"entity.shipment.{equipment.Identifier().ToLowerInvariant()}.name";
		var localizedShipmentName = Language.GetPhrase( shipmentKey );
		if ( !string.Equals( localizedShipmentName, shipmentKey, StringComparison.Ordinal ) )
		{
			return localizedShipmentName;
		}

		return string.Format( Language.GetPhrase( "entity.shipment.generic.name" ), equipment.DisplayName() );
	}
}
