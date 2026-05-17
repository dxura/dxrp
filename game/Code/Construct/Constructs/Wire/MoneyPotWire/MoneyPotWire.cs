using Dxura.RP.Game.Entities;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game.Wire;

[Title( "MoneyPot" )]
[Category( "Wire" )]
[Icon( "attach_money" )]
public class MoneyPotWire() : BaseWireConstruct( ConstructType.MoneyPotWire ), Component.IPressable, Component.ITriggerListener
{
	[WireOutput( "total_amount" )]
	private uint Amount { get; set; }

	[Property]
	public uint DisplayAmount { get; set; }

	private object MoneyLock { get; } = new();

	[Property] public GameObject InDropPoint { get; set; } = null!;
	[Property] public GameObject OutDropPoint { get; set; } = null!;

	[Property] public SoundEvent? CollectSound { get; set; }
	[Property] public SoundEvent? DropSound { get; set; }

	[WireInput( "drop_all" )]
	public bool DropAll
	{
		set
		{
			if ( value )
			{
				DropMoney( null, InDropPoint.WorldPosition );
			}

		}
		get => false; // This is just a trigger, no need to store state
	}

	[WireInput( "lock" )]
	public bool Lock { get; set; }

	[WireInput( "return" )]
	public float Return
	{
		set
		{
			if ( value <= 0 )
			{
				return;
			}

			DropMoney( (uint?)value, OutDropPoint.WorldPosition );
		}
		get => 0; // This is just a trigger, no need to store state
	}


	[WireOutput( "last_amount" )]
	public float LastAmount { get; private set; }

	public override string Name => $"Money Pot (${NumberUtils.FormatNumberWithSuffix( DisplayAmount )})";

	public bool Press( IPressable.Event e )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "moneypot:use", Config.Current.Game.ActionCooldown, true ) )
		{
			return false;
		}

		OnPressHost();

		return true;
	}

	[Rpc.Host]
	private void OnPressHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:moneypot:use", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		// Verify player is within interaction distance
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() || Vector3.DistanceBetween( player.WorldPosition, WorldPosition ) > Config.Current.Game.ReachDistance )
		{
			return;
		}

		DropMoney( null, InDropPoint.WorldPosition );
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !Networking.IsHost || Lock )
		{
			return;
		}

		if ( other.IsValid() && other.Components.TryGet<MoneyEntity>( out var moneyEntity ) )
		{
			CollectMoney( moneyEntity );
		}
	}

	private void CollectMoney( MoneyEntity moneyEntity )
	{
		if ( moneyEntity.Value <= 0 )
		{
			Log.Warning( $"Money Pot tried to collect invalid money entity with value {moneyEntity.Value}" );
			return;
		}

		MoneyEntity.PickupSemaphore.Wait();

		try
		{
			if ( !moneyEntity.IsValid() || moneyEntity.GameObject.IsDestroyed )
			{
				return;
			}

			lock ( MoneyLock )
			{
				Amount += moneyEntity.Value;
				LastAmount = moneyEntity.Value;
			}

			moneyEntity.GameObject.Destroy();

			// Play collect sound
			CollectSound?.Broadcast( WorldPosition );

			Log.Info( $"Money Pot collected ${moneyEntity.Value}, total: ${Amount}" );
			BroadcastDisplayAmount( Amount );
		}
		finally
		{
			MoneyEntity.PickupSemaphore.Release();
		}
	}

	private void DropMoney( uint? amount, Vector3 position )
	{
		Assert.True( Networking.IsHost );

		if ( Cooldown.Current.CheckAndStartCooldown( $"{Id}:moneypot:drop", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		if ( Amount <= 0 )
		{
			return;
		}

		uint droppedAmount;

		lock ( MoneyLock )
		{
			// Calculate amount to drop, clamped to available balance
			droppedAmount = amount.HasValue ? Math.Min( amount.Value, Amount ) : Amount;

			// Additional safety check
			if ( droppedAmount <= 0 || droppedAmount > Amount )
			{
				return;
			}

			Amount -= droppedAmount;

			GameManager.Instance.DropMoneyHost( droppedAmount, position, "Money pot drop" );
		}

		DropSound?.Broadcast( position );

		Log.Info( $"Money Pot dropped ${droppedAmount} at drop point, remaining: ${Amount}" );
		BroadcastDisplayAmount( Amount );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastDisplayAmount( uint amount )
	{
		DisplayAmount = amount;
	}

	protected override void OnDestroy()
	{
		if ( Networking.IsHost && !IsPreview & Amount > 0 )
		{
			lock ( MoneyLock )
			{
				GameManager.Instance.DropMoneyHost( Amount, WorldPosition, "Money pot destroyed" );
				Amount = 0;
			}
		}

		base.OnDestroy();
	}
}
