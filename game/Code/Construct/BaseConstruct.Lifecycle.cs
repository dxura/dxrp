using System.Threading;
using System.Threading.Tasks;

public abstract partial class BaseConstruct
{
	public void Freeze( Vector3 position, Rotation rotation )
	{
		if ( !Networking.IsHost )
		{
			RequestFreeze( true, position, rotation );
			return;
		}

		BroadcastFreeze( position, rotation );
	}

	public void Unfreeze()
	{
		if ( !Networking.IsHost )
		{
			RequestFreeze( false );
			return;
		}

		BroadcastUnfreeze();
	}

	[Rpc.Host]
	private void RequestFreeze( bool freeze, Vector3 position = default, Rotation rotation = default )
	{
		var caller = Rpc.Caller;

		if ( !GameUtils.HasPermission( caller, GameObject, false ) )
		{
			return;
		}

		if ( !GameObject.IsValid() )
		{
			return;
		}

		if ( IsPreview )
		{
			return;
		}

		if ( freeze )
		{
			BroadcastFreeze( position, rotation );
		}
		else
		{
			BroadcastUnfreeze();
		}
	}

	private async Task FreezeCollider( CancellationToken cancellationToken )
	{
		if ( Collider.IsValid() )
		{
			Collider.Static = false;
		}

		await GameTask.DelayRealtimeSeconds( 5, cancellationToken );
		await GameTask.MainThread();

		if ( cancellationToken.IsCancellationRequested )
		{
			return;
		}

		if ( !IsFrozen )
		{
			return;
		}

		if ( Collider.IsValid() )
		{
			Collider.Static = true;
		}
	}
}
