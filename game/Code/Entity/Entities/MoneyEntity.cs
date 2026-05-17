using System.Threading;
using System.Threading.Tasks;


namespace Dxura.RP.Game.Entities;

[Title( "Money" )]
[Category( "Entities" )]
public class MoneyEntity : BaseEntity, Component.ICollisionListener, Component.IPressable
{
	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnValueChanged ) )]
	public uint Value { get; set; }

	[Property] public required TextRenderer TextRenderer { get; set; }

	[Property] public required SoundEvent UseSound { get; set; }

	public static readonly SemaphoreSlim PickupSemaphore = new( 1, 1 );

	protected override void OnStart()
	{
		base.OnStart();

		OnValueChanged( 0, Value );
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

		// LOS check
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

		_ = DoPickupHost( player );

	}

	private async Task DoPickupHost( Player player )
	{
		await PickupSemaphore.WaitAsync();

		try
		{
			if ( GameObject.IsDestroyed )
			{
				return;
			}

			if ( !await player.PayHost( Value, "Picked up money" ) )
			{
				return;
			}

			UseSound.Broadcast( WorldPosition );

			GameObject.Destroy();
		}
		finally
		{
			PickupSemaphore.Release();
		}
	}

	public void OnCollisionStart( Collision collision )
	{
		if ( !Networking.IsHost || !collision.Other.GameObject.IsValid() || !collision.Other.GameObject.Tags.Has( Constants.EntityTag ) )
		{
			return;
		}

		var otherMoney = collision.Other.GameObject.GetComponent<MoneyEntity>();
		if ( otherMoney == null || otherMoney == this )
		{
			return;
		}

		_ = TryMergeWith( otherMoney );
	}

	private async Task TryMergeWith( MoneyEntity other )
	{
		await PickupSemaphore.WaitAsync();

		try
		{
			if ( GameObject.IsDestroyed || other.GameObject.IsDestroyed )
			{
				return;
			}

			var newValue = Value + other.Value;
			Value = newValue;

			UseSound.Broadcast( WorldPosition );

			var timeDestroy = GameObject.GetComponent<TimedDestroyComponent>();
			if ( timeDestroy.IsValid() )
			{
				timeDestroy.ResetTimer();
			}

			other.GameObject.Destroy();
		}
		finally
		{
			PickupSemaphore.Release();
		}
	}
	private void OnValueChanged( uint oldValue, uint newValue )
	{
		if(!TextRenderer.IsValid()) return;
		
		TextRenderer.Text = '$' + NumberUtils.FormatNumberWithSuffix( newValue );
	}

}
