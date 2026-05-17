using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public sealed class Recycler : Component, Component.ITriggerListener, IGameEvents
{
	[Property]
	public required GameObject DropPoint { get; set; }

	[Property]
	public required GameObject Model { get; set; }

	[Property]
	public required SoundEvent FinishSound { get; set; }

	[Property]
	public required ContinuousSoundPoint ProcessingSoundPoint { get; set; }

	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnIsProcessingChanged ) )]
	private bool IsProcessing { get; set; }

	private readonly Queue<uint> _queue = new();
	private TimeUntil? ProcessedTime { get; set; }


	protected override void OnUpdate()
	{
		if ( !GameManager.IsHeadless && IsProcessing )
		{
			Animate();
		}
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( ProcessedTime is not { Relative: <= 0 } )
		{
			return;
		}

		ProcessDrop();
		ProcessedTime = _queue.Count > 0 ? Config.Current.Game.RecyclerProcessInterval : null;

		FinishSound.BroadcastHost( WorldPosition );

		if ( _queue.Count == 0 )
		{
			IsProcessing = false;
		}
	}

	private void Animate()
	{
		if ( GameObject.Tags.Contains( Constants.OccludeTag ) )
		{
			return;
		}

		Model.LocalPosition = new Vector3(
			(float)Math.Sin( Time.Now * 70f ) * 0.2f,
			(float)Math.Sin( Time.Now * 78f ) * 0.2f,
			0
		);
	}

	private void OnIsProcessingChanged( bool oldValue, bool newValue )
	{
		ProcessingSoundPoint.Enabled = newValue;
	}

	private void ProcessDrop()
	{
		// Drop refund if there is one for this item
		if ( _queue.Count > 0 )
		{
			var refundAmount = _queue.Dequeue();
			if ( refundAmount > 0 )
			{
				GameManager.Instance.DropMoneyHost( refundAmount, DropPoint.WorldPosition, "Recycler refund", WorldRotation );
				return;
			}
		}

		// No refund, roll for random drop

		var entries = new (float Weight, Action Drop)[]
		{
			(Config.Current.Game.RecyclerGarbageKnifeDropChance, DropKnife),
			(Config.Current.Game.RecyclerGarbagePistolDropChance, DropPistol),
			(Config.Current.Game.RecyclerGarbageMoneyDropChance, DropMoney)
		};

		var total = entries.Sum( e => e.Weight );
		if ( total <= 0f )
		{
			return;
		}

		var roll = (float)Random.Shared.NextDouble() * total;
		foreach ( var e in entries )
		{
			if ( roll < e.Weight )
			{
				e.Drop();
				return;
			}
			roll -= e.Weight;
		}
	}

	private void DropKnife()
	{
		var knifeResource = GameModeEquipments.FindByIdentifier( "knife" );
		if ( knifeResource == null )
		{
			return;
		}

		DroppedEquipment.CreateHost( knifeResource, DropPoint.WorldPosition, Rotation.Random );
	}

	private void DropPistol()
	{
		var pistolResource = GameModeEquipments.FindByIdentifier( "usp" );
		if ( pistolResource == null )
		{
			return;
		}

		DroppedEquipment.CreateHost( pistolResource, DropPoint.WorldPosition, Rotation.Random );
	}

	private void DropMoney()
	{
		var amount = Random.Shared.Next( Config.Current.Game.RecyclerGarbageMoneyMinAmount, Config.Current.Game.RecyclerGarbageMoneyMaxAmount + 1 );
		GameManager.Instance.DropMoneyHost( (uint)amount, DropPoint.WorldPosition, "Recycler random drop", WorldRotation );
	}

	public void OnTriggerEnter( GameObject other )
	{
		var root = other.Root;

		if ( !Networking.IsHost || root.IsDestroyed || _queue.Count >= Config.Current.Game.RecyclerMaxQueue )
		{
			return;
		}

		// Only process valid items
		if ( !root.Tags.HasAny( Config.Current.Game.RecyclerAcceptedTags ) )
		{
			return;
		}

		if ( !CanRecycle( root ) )
		{
			return;
		}

		// Calculate and queue refund before destroying
		var refundAmount = CalculateRefund( root );

		// If this is an entity and the refund is 0, skip processing
		if ( root.Tags.Has( Constants.EntityTag ) && !root.Tags.Has( Constants.GarbageTag ) && refundAmount == 0 )
		{
			return;
		}

		var recyclerPlayer = GameUtils.GetPlayerByConnectionId( root.Network.OwnerId );
		_ = ServerApiClient.Audit( "Recycler", $"{root.Name} recycled. Refund: {(refundAmount > 0 ? refundAmount.ToString() : "Random roll")} by {recyclerPlayer?.SteamName ?? "Unknown"} ({recyclerPlayer?.SteamId.ToString() ?? "Unknown"})", recyclerPlayer?.SteamId );

		_queue.Enqueue( refundAmount );

		IsProcessing = true;
		root.Destroy();

		ProcessedTime ??= Config.Current.Game.RecyclerProcessInterval;
	}

	private static bool CanRecycle( GameObject gameObject )
	{
		return !gameObject.Tags.Has( Constants.NonRecyclableTag );
	}

	private uint CalculateRefund( GameObject gameObject )
	{
		// Check for ShipmentEntity (needs to be checked first as it inherits from BaseEntity)
		var shipmentEntity = gameObject.Components.Get<Entities.ShipmentEntity>();
		if ( shipmentEntity.IsValid() )
		{
			// Calculate value based on remaining quantity
			var shipmentMarketItem = GameModeMarketItems.FindById( shipmentEntity.MarketItemId );
			var shipmentPrice = GetRefundablePrice( shipmentMarketItem );
			var remainingValue = shipmentPrice * shipmentEntity.Quantity / shipmentEntity.MaxQuantity;
			return (uint)(remainingValue * Config.Current.Game.RecyclerEntityRefundPercent);
		}

		// Check for BaseEntity (entities like printers, etc.)
		var baseEntity = gameObject.Components.Get<BaseEntity>();
		if ( baseEntity is { GameModeEntityId: var entityId } && entityId != Guid.Empty )
		{
			var marketItem = GameModeMarketItems.All
				.FirstOrDefault( x => x.Type == GameModeMarketItemType.Entity && x.ReferenceId == entityId );
			var entityPrice = GetRefundablePrice( marketItem );
			return (uint)(entityPrice * Config.Current.Game.RecyclerEntityRefundPercent);
		}

		// TODO: Will need to get price from shipped equipment if we want to support DroppedEquipment refunds
		// // Check for DroppedEquipment
		// var droppedEquipment = gameObject.Components.Get<DroppedEquipment>();
		// if ( droppedEquipment is not null )
		// {
		// 	return (uint)(droppedEquipment.Resource.Price * RefundPercentage);
		// }

		return 0;
	}

	private static float GetRefundablePrice( GameModeMarketItemDto? marketItem )
	{
		return (float)Math.Max( 0, (int)MathF.Ceiling( (marketItem?.Cost ?? 0) * GameManager.Instance.EntityPriceMultiplier ) );
	}

}
