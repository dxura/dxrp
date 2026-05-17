using Dxura.RP.Game.UI;
using Sandbox;

namespace Dxura.RP.Game;

public sealed class Bin : Component, Component.ITriggerListener, IContextualObject
{
	public string DisplayText => string.Format(
		Language.GetPhrase( "context.bin.display" ),
		$"${Config.Current.Game.GarbageRubbishPaymentPrice:N0}"
	);
	public Vector3 ContextPosition => WorldPosition + Vector3.Up * 50f;

	public bool LookOpacity => false;
	public float ContextMaxDistance => 120f;

	public void OnTriggerEnter( GameObject other )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		// Pay player for rubbish and destroy it
		if ( other.Tags.Has( Constants.GarbageTag ) )
		{
			var player = GameUtils.GetPlayerByConnectionId( other.Network.OwnerId );

			if ( player.IsValid() )
			{
				player.Success( string.Format(
					Language.GetPhrase( "notify.bin.recycled" ),
					$"${Config.Current.Game.GarbageRubbishPaymentPrice:N0}"
				) );
				_ = player.PayHost( Config.Current.Game.GarbageRubbishPaymentPrice, Language.GetPhrase( "payment.rubbish" ) );

				player.IncrementStat( "garbage-collected", 1 );
			}

			other.Root.Destroy();
		}
	}
}
