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

		return GameModeEntities.FindByDtoId( item.ReferenceId.Value );
	}

	public static GameModeEquipmentDto? ResolveEquipment( GameModeMarketItemDto? item )
	{
		if ( item?.Type != GameModeMarketItemType.Equipment || !item.ReferenceId.HasValue )
		{
			return null;
		}

		return GameModeEquipments.FindByDtoId( item.ReferenceId.Value );
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

	public static GameModeMarketItemDto? FindShipmentMarketItem( GameModeEquipmentDto? equipment )
	{
		if ( equipment == null )
		{
			return null;
		}

		return All.FirstOrDefault( item =>
			item.Type == GameModeMarketItemType.Equipment &&
			item.ReferenceId == equipment.Id &&
			item.Quantity > 1 );
	}

	public static int GetOwnedCount( Player player, GameModeMarketItemDto? item )
	{
		if ( !player.IsValid() || item == null )
		{
			return 0;
		}

		if ( item.Type == GameModeMarketItemType.Entity )
		{
			return GetOwnedEntityCount( player, ResolveEntity( item )?.GameModeAddonContentId );
		}

		var worldOwned = player.Scene.GetAllComponents<ShipmentEntity>()
			.Count( entity => entity.IsValid() &&
			                 entity.MarketItemId == item.Id &&
			                 entity.Owner == player.SteamId );
		var equippedOwned = player.Equipment.Count( equipment => equipment.IsValid() && equipment.Enabled && equipment.MarketItemId == item.Id );
		return worldOwned + equippedOwned;
	}

	public static int GetOwnedEntityCount( Player player, Guid? entityId )
	{
		if ( !player.IsValid() || !entityId.HasValue || entityId == Guid.Empty )
		{
			return 0;
		}
		
		return player.Scene.Components.GetAll<BaseEntity>( FindMode.EverythingInChildren )
			.Count( entity => entity.IsValid() &&
			                 entity.Owner == player.SteamId &&
			                 entity.EntityId == entityId.Value );
	}

	public static IEnumerable<(GameModeMarketItemDto MarketItem, GameModeEquipmentDto Equipment)> UniqueSpawnableEquipment()
	{
		var seenEquipment = new HashSet<Guid>();

		foreach ( var marketItem in All
			         .Where( item => item.Type == GameModeMarketItemType.Equipment && IsSpawnable( item ) )
			         .OrderBy( DisplayName ) )
		{
			var equipment = ResolveEquipment( marketItem );
			if ( equipment != null && seenEquipment.Add( equipment.Id ) )
			{
				yield return (marketItem, equipment);
			}
		}
	}

	public static bool CanAdminSpawn( Player player, GameModeMarketItemDto? item )
	{
		if ( !player.IsValid() || item == null || !RankSystem.HasPermission( player.SteamId, Permission.CommandSpawnEntity ) )
		{
			return false;
		}

		return IsSpawnable( item );
	}

	public static bool CanPurchase( Player player, GameModeMarketItemDto? item )
	{
		if ( !player.IsValid() || item == null || player.Restricted )
		{
			return false;
		}

		if ( !IsSpawnable( item ) )
		{
			return false;
		}

		switch ( item.Type )
		{
			case GameModeMarketItemType.Entity:
				var entity = ResolveEntity( item );
				if ( entity!.Limit > 0 && GetOwnedEntityCount( player, entity.Id ) >= entity.Limit )
				{
					return false;
				}
				if ( entity.Limit > 0 && GetOwnedEntityCount( player, entity.GameModeAddonContentId ) >= entity.Limit )
				{
					return false;
				}
				break;

			case GameModeMarketItemType.Equipment:
				var equipment = ResolveEquipment( item );
				if ( item.Quantity == 1 && player.CanTake( equipment! ) == Player.PickupResult.None )
				{
					return false;
				}
				break;
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

		var equipmentLimit = ResolveEquipment( item )?.Limit ?? 0;
		return item.Type != GameModeMarketItemType.Equipment || equipmentLimit <= 0 || GetOwnedCount( player, item ) < equipmentLimit;
	}

	public static bool IsSpawnable( GameModeMarketItemDto? item )
	{
		if ( item == null )
		{
			return false;
		}

		switch ( item.Type )
		{
			case GameModeMarketItemType.Entity:
				var entity = ResolveEntity( item );
				return entity != null && !string.IsNullOrWhiteSpace( entity.PrefabPath() );

			case GameModeMarketItemType.Equipment:
				var equipment = ResolveEquipment( item );
				if ( equipment == null || item.Quantity <= 0 || string.IsNullOrWhiteSpace( equipment.PrefabPath() ) )
				{
					return false;
				}

				if ( item.Quantity > 1 )
				{
					return GameObject.GetPrefab( ShipmentPrefabPath ).IsValid();
				}

				return true;

			default:
				return false;
		}
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
