using Dxura.RP.Game.Entities;

namespace Dxura.RP.Game;

public class ShipmentCombineSystem : SingletonComponent<ShipmentCombineSystem>, IGameEvents
{
	private const float CombineRadius = 24f;
	private const float MaxCombineVelocity = 50f;
	private const int MinCombineCount = 2;

	private TimeSince _timeSinceLastCombine;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( GameManager.IsHeadless )
		{
			return;
		}

		RefreshCombineIndicators();
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost || _timeSinceLastCombine < 0.5f )
		{
			return;
		}

		_timeSinceLastCombine = 0;
		TryCombineNearbyDrops();
	}

	private void RefreshCombineIndicators()
	{
		var drops = Scene.GetAllComponents<DroppedEquipment>()
			.Where( drop => drop.IsValid() && drop.Resource != null )
			.ToList();

		var clusters = BuildCombineClusters( drops );

		foreach ( var drop in drops )
		{
			if ( clusters.TryGetValue( drop, out var cluster ) )
			{
				drop.SetCombineIndicator( cluster.Count, cluster.MaxQuantity );
			}
			else
			{
				drop.ClearCombineIndicator();
			}
		}
	}

	private void TryCombineNearbyDrops()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "shipment:combine", Config.Current.Game.ShipmentUseCooldown ) )
		{
			return;
		}

		var drops = Scene.GetAllComponents<DroppedEquipment>()
			.Where( drop => drop.IsValid() && drop.Resource != null && IsSettled( drop ) )
			.ToList();

		var processed = new HashSet<GameObject>();

		foreach ( var drop in drops )
		{
			if ( processed.Contains( drop.GameObject ) )
			{
				continue;
			}

			var cluster = FindCluster( drops, drop );
			if ( cluster.Count < MinCombineCount )
			{
				continue;
			}

			if ( ShipmentEntity.TryCreateFromDropsHost( cluster ) )
			{
				foreach ( var clusteredDrop in cluster )
				{
					processed.Add( clusteredDrop.GameObject );
				}
			}
		}
	}

	private static Dictionary<DroppedEquipment, (int Count, int MaxQuantity)> BuildCombineClusters(
		IReadOnlyList<DroppedEquipment> drops )
	{
		var clusters = new Dictionary<DroppedEquipment, (int Count, int MaxQuantity)>();
		var assigned = new HashSet<DroppedEquipment>();

		foreach ( var drop in drops )
		{
			if ( assigned.Contains( drop ) || !IsSettled( drop ) )
			{
				continue;
			}

			var cluster = FindCluster( drops, drop );
			if ( cluster.Count < MinCombineCount )
			{
				continue;
			}

			var maxQuantity = GameModeMarketItems.FindShipmentMarketItem( drop.Resource )?.Quantity ?? 10;
			var clusterInfo = (cluster.Count, maxQuantity);

			foreach ( var clusteredDrop in cluster )
			{
				clusters[clusteredDrop] = clusterInfo;
				assigned.Add( clusteredDrop );
			}
		}

		return clusters;
	}

	private static List<DroppedEquipment> FindCluster( IReadOnlyList<DroppedEquipment> drops, DroppedEquipment origin )
	{
		return drops
			.Where( drop => string.Equals( drop.Identifier, origin.Identifier, StringComparison.OrdinalIgnoreCase ) &&
			                drop.WorldPosition.Distance( origin.WorldPosition ) <= CombineRadius &&
			                !IsWithinAnyDepositZone( drop.Scene, drop.WorldPosition ) )
			.ToList();
	}

	private static bool IsSettled( DroppedEquipment drop )
	{
		if ( !drop.Rigidbody.IsValid() )
		{
			return true;
		}

		return drop.Rigidbody.Velocity.Length <= MaxCombineVelocity &&
		       drop.Rigidbody.AngularVelocity.Length <= MaxCombineVelocity;
	}

	private static bool IsWithinAnyDepositZone( Scene scene, Vector3 position )
	{
		foreach ( var shipment in scene.GetAllComponents<ShipmentEntity>() )
		{
			if ( shipment.IsValid() && shipment.ContainsDepositPoint( position ) )
			{
				return true;
			}
		}

		return false;
	}
}
