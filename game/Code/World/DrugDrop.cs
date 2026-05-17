using Dxura.RP.Game.UI;
using System.Threading.Tasks;
namespace Dxura.RP.Game;

public class DrugDrop : Component, Component.ITriggerListener, IContextualObject, IGameEvents
{
	[Property]
	[Sync( SyncFlags.FromHost )]
	private uint PaymentPerDrop { get; set; }

	private TimeUntil _nextPaymentChange;
	private readonly Dictionary<GameObject, RealTimeUntil> _objectsInTrigger = new();
	private TimeUntil _nextPayout = 1f;
	private uint _pendingPayoutTotal;
	private readonly Dictionary<Player, int> _pendingSoldCounts = new();

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( _nextPaymentChange )
		{
			PaymentPerDrop = (uint)Sandbox.Game.Random.Next( (int)Config.Current.Game.DrugDropMinPrice, (int)Config.Current.Game.DrugDropMaxPrice );
			_nextPaymentChange = Config.Current.Game.DrugDropPriceChangeCycle;
		}

		// Check all objects in trigger
		var objectsToRemove = new List<GameObject>();
		foreach ( var (gameObject, timeUntilSell) in _objectsInTrigger.ToList() )
		{

			if ( !gameObject.IsValid() )
			{
				objectsToRemove.Add( gameObject );
				continue;
			}

			// Check if ready to sell
			if ( !timeUntilSell )
			{
				continue;
			}

			var dropPlayer = GameUtils.GetPlayerByConnectionId( gameObject.Network.OwnerId );
			if ( dropPlayer.IsValid() )
			{
				QueueBundlePayout( gameObject, dropPlayer );
				GameManager.Instance.PurchaseSound.BroadcastHost( gameObject.WorldPosition );
			}

			objectsToRemove.Add( gameObject );
		}

		// Clean up sold or invalid objects
		foreach ( var gameObject in objectsToRemove )
		{
			_objectsInTrigger.Remove( gameObject );
		}

		if ( _nextPayout && _pendingPayoutTotal > 0 )
		{
			GameManager.Instance.DropMoneyHost( _pendingPayoutTotal, WorldPosition + Vector3.Up * 50f, Language.GetPhrase( "payment.drugdrop.payout" ) );

			foreach ( var (player, soldCount) in _pendingSoldCounts )
			{
				player.IncrementStat( "weed-sold", soldCount );
			}

			_pendingPayoutTotal = 0;
			_pendingSoldCounts.Clear();
			_nextPayout = 1f;
		}
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		other = other.Root;

		// Don't allow building on drop zone
		if ( other.Tags.Has( Constants.ConstructTag ) )
		{
			var player = GameUtils.GetPlayerByConnectionId( other.Network.OwnerId );
			if ( player.IsValid() )
			{
				player.Error( "#notify.drugdrop.no_build" );
			}

			other.Destroy();
			return;
		}

		if ( !other.Tags.Contains( "weed_brick" ) )
		{
			return;
		}

		var dropPlayer = GameUtils.GetPlayerByConnectionId( other.Network.OwnerId );
		if ( !dropPlayer.IsValid() )
		{
			return;
		}

		// Start tracking this object
		if ( !_objectsInTrigger.ContainsKey( other ) )
		{
			_objectsInTrigger[other] = Config.Current.Game.DrugDropSellTime;
		}

		BroadcastToggleCountdownContext( other, false );
	}

	public void OnTriggerExit( GameObject other )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		_objectsInTrigger.Remove( other );

		if ( !other.IsDestroyed )
		{
			BroadcastToggleCountdownContext( other, true );
		}
	}


	private void QueueBundlePayout( GameObject bundle, Player player )
	{
		bundle.Root.Destroy();

		_pendingPayoutTotal += PaymentPerDrop;
		_pendingSoldCounts[player] = _pendingSoldCounts.GetValueOrDefault( player ) + 1;
	}


	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastToggleCountdownContext( GameObject target, bool isRemove )
	{
		var countdownContext = target.GetOrAddComponent<CountdownContext>();

		if ( isRemove )
		{
			countdownContext.Destroy();

		}
		else
		{
			countdownContext.CountdownTime = Config.Current.Game.DrugDropSellTime;
			countdownContext.DisplayPrefix = "#drugdrop.context.selling_in";
		}
	}

	// Contextual
	public float ContextMaxDistance => 200;
	public Vector3 ContextPosition => WorldPosition + Vector3.Up * 25;

	public bool LookOpacity => false;
	public string DisplayText => string.Format( Language.GetPhrase( "drugdrop.context.per_drop" ), $"${PaymentPerDrop:N0}" );
}
