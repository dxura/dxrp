using System.Threading;

public abstract partial class BaseConstruct
{
	[Property]
	[Group( "Construct" )]
	// ReSharper disable once MemberCanBePrivate.Global
	public Guid? OverrideNetworkOwner { get; set; }

	public Guid NetworkOwner
	{
		get
		{
			if ( OverrideNetworkOwner.HasValue)
			{
				var overrideOwner = GameUtils.GetPlayerByConnectionId( OverrideNetworkOwner.Value );
				if ( overrideOwner.IsValid() && overrideOwner.IsConnected )
				{
					return OverrideNetworkOwner.Value;
				}
			}

			var owner = GameUtils.GetPlayerById( Owner );
			return owner.IsValid() && owner.IsConnected ? owner.ConnectionId : Guid.Empty;
		}
	}

	protected bool IsNetworkOwner => NetworkOwner == Connection.Local.Id;

	private TimeSince _lastTransformChange = 1000f;
	private TimeSince _lastUnreliableSync = 1000f;
	private TimeSince _lastReliableSync = 1000f;

	private static TimeSince _globalSyncWindow = 1000f;
	private static int _globalSyncCounter;
	private static readonly int MaxSyncsPerSecond = 200;

	private void OnUpdateNetworking()
	{
		if ( IsPreview || IsFrozen || !IsNetworkOwner || NetworkOwner == Guid.Empty )
		{
			return;
		}

		// Local rate limit
		if ( _lastUnreliableSync < 0.1f )
		{
			return;
		}

		// Reset global sync rate limiting
		if ( _globalSyncWindow >= 1.0f )
		{
			_globalSyncCounter = 0;
			_globalSyncWindow = 0f;
		}

		// Global rate limit (only apply when not grabbed)
		if ( !GameObject.Tags.Has( Constants.GrabbedTag ) && _globalSyncCounter >= MaxSyncsPerSecond )
		{
			return;
		}

		// Check if moved significantly since last sync
		if ( WorldPosition.DistanceSquared( _targetPosition ) > 0.01f || WorldRotation.Distance( _targetRotation ) > 1f )
		{
			_lastTransformChange = 0f;
		}

		// Don't send unreliable updates if haven't moved recently 
		if ( _lastTransformChange > 1.5f )
		{
			return;
		}

		using ( Rpc.FilterExclude( x => x.Id == NetworkOwner ) )
		{
			BroadcastUnreliableTransform( WorldPosition, WorldRotation );
		}

		_targetPosition = WorldPosition;
		_targetRotation = WorldRotation;

		_lastUnreliableSync = 0f;
		_globalSyncCounter++;
	}

	[Rpc.Broadcast( NetFlags.UnreliableNoDelay )]
	private void BroadcastUnreliableTransform( Vector3 position, Rotation rotation )
	{
		// Only accept updates from the network owner
		if ( Rpc.CallerId != NetworkOwner )
		{
			return;
		}

		// Ignore unreliable updates if a reliable sync was done recently
		if ( _lastReliableSync < 1.0f )
		{
			return;
		}

		// No need to interpolate if already at target
		if ( position == _targetPosition && rotation == _targetRotation )
		{
			return;
		}

		_targetPosition = position;
		_targetRotation = rotation;

		_interpolating = true;
	}


	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastData( string jsonData )
	{
		SetDataInternal( jsonData );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastSetOwner( long owner )
	{
		Owner = owner;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	protected void BroadcastFreeze( Vector3 position, Rotation rotation )
	{
		_lastReliableSync = 0;

		WorldPosition = position;
		WorldRotation = rotation;

		ResetInterpolation();

		IsFrozen = true;

		_freezeCancellationTokenSource?.Cancel();
		_freezeCancellationTokenSource = new CancellationTokenSource();
		_ = FreezeCollider( _freezeCancellationTokenSource.Token );

		ClearNetworkState();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	protected void BroadcastUnfreeze()
	{
		_freezeCancellationTokenSource?.Cancel();

		ResetInterpolation();

		if ( Collider.IsValid() )
		{
			Collider.Static = false;
		}

		IsFrozen = false;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void BroadcastSetNetworkOwner( Guid networkOwnerId )
	{
		var owner = GameUtils.GetPlayerById( Owner );
		OverrideNetworkOwner = owner.IsValid() && owner.ConnectionId == networkOwnerId
			? null
			: networkOwnerId;

		ResetInterpolation();

		ClearNetworkState();
	}

	private void ClearNetworkState()
	{
		if ( IsNetworkOwner )
		{
			return;
		}

		var rigidbody = GameObject.GetComponent<Rigidbody>();
		if ( rigidbody.IsValid() )
		{
			rigidbody.Destroy();
		}
	}

	public bool RequestNetworkOwnership()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "request:construct:ownership", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return false;
		}

		if ( !CanTakeNetworkOwnership( Connection.Local ) )
		{
			return false;
		}

		RequestNetworkOwnershipHost();

		return true;
	}

	[Rpc.Host]
	private void RequestNetworkOwnershipHost()
	{
		var caller = Rpc.Caller;
		var callerId = caller.Id;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:request:construct:ownership", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		if ( !CanTakeNetworkOwnership( caller ) )
		{
			return;
		}

		BroadcastSetNetworkOwner( callerId );
	}

	private bool CanTakeNetworkOwnership( Connection connection )
	{
		if ( IsPreview )
		{
			return false;
		}

		if ( IsNetworkOwner )
		{
			return true;
		}

		// Prevent ownership change while grabbed
		if ( GameObject.Tags.Has( Constants.GrabbedTag ) )
		{
			return false;
		}

		var hasGeneralPermission = GameUtils.HasPermission( connection, GameObject );

		if ( IsFrozen && !hasGeneralPermission )
		{
			return false;
		}

		return true;
	}
}
