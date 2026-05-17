using System.Threading;

namespace Dxura.RP.Game;

[Title( "Status On Press" )]
[Category( "DXRP" )]
public class StatusOnPress : Component, Component.IPressable
{
	private static readonly SemaphoreSlim PickupSemaphore = new( 1, 1 );

	[Property]
	public string StatusToApply { get; set; } = Constants.SatiatedStatus;

	[Property]
	public SoundEvent? UseSound { get; set; }

	public bool CanPress( IPressable.Event e )
	{
		return true;
	}

	public bool Press( IPressable.Event e )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "action", Config.Current.Game.ActionCooldown, true ) )
		{
			return false;
		}

		OnUseHost();

		return true;
	}



	[Rpc.Host]
	private void OnUseHost()
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:action", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		// Proximity and LOS check before processing
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}
		var tr = Scene.Trace.Ray( player.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( player.GameObject )
			.UseHitboxes()
			.Run();

		if ( !tr.Hit || tr.GameObject.Root != GameObject.Root )
		{
			return;
		}

		PickupSemaphore.Wait();

		try
		{
			// player validated earlier
			if ( GameObject.IsDestroyed )
			{
				return;
			}
			if ( !player.ArmorComponent.IsValid() )
			{
				return;
			}

			player.AddStatus( StatusToApply );

			UseSound.Broadcast( player.WorldPosition, player.GameObject );

			GameObject.Destroy();
		}
		finally
		{
			PickupSemaphore.Release();
		}
	}
}
