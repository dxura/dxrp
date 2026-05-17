namespace Dxura.RP.Game.Wire;

[Title( "Forcer" )]
[Category( "Wire" )]
[Icon( "push_pin" )]
public class ForcerWire() : BaseWireConstruct( ConstructType.ForcerWire ), IWireEvents
{
	private ForcerWireData _data = new();

	[WireInput( "force" )]
	public bool Force { get; set; }

	[WireOutput( "forcing" )]
	public bool Forcing { get; set; }

	[Property] public GameObject EndLaserTarget { get; set; } = null!;
	[Property] public LineRenderer LineRenderer { get; set; } = null!;

	public override string Name => $"Forcer ({_data.ForceAmount})";

	private TimeSince _lastForceTime = 1000f;

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as ForcerWireData ?? new ForcerWireData();

		if ( EndLaserTarget.IsValid() )
		{
			EndLaserTarget.WorldPosition = WorldPosition + WorldRotation.Up * _data.Range;
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		UpdateLaserVisibility();
	}

	public override void OnOcclusionChanged( bool occlude )
	{
		base.OnOcclusionChanged( occlude );
		UpdateLaserVisibility( occlude );
	}

	private void UpdateLaserVisibility( bool? occluded = null )
	{
		// Hide the laser to save performance if we're occluded or in headless mode
		LineRenderer.Enabled = !(occluded ?? Tags.Contains( Constants.OccludeTag )) && !GameManager.IsHeadless;
	}

	protected override void OnFixedUpdate()
	{
		// Run force logic on owner only
		if ( !IsOwner )
		{
			return;
		}

		TryApplyForce();
	}

	private void TryApplyForce()
	{
		var trace = Scene.Trace.Ray( WorldPosition, EndLaserTarget.WorldPosition )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( Config.Current.Game.ForcerExcludeTags )
			.Run();

		if ( !trace.Hit || !trace.GameObject.IsValid() )
		{
			return;
		}

		var rigidbody = trace.GameObject?.GetComponent<Rigidbody>();
		if ( !rigidbody.IsValid() )
		{
			return;
		}

		var forceDirection = (EndLaserTarget.WorldPosition - WorldPosition).Normal;
		var distance = Vector3.DistanceBetween( WorldPosition, trace.HitPosition );
		var distanceFactor = 1.0f - distance / _data.Range;
		var adjustedForce = _data.ForceAmount * distanceFactor;

		// Send to server for validation
		HostApplyForce( rigidbody, trace.HitPosition, adjustedForce * forceDirection );
	}

	[Rpc.Host( NetFlags.Unreliable )]
	private void HostApplyForce( Rigidbody? rigidbody, Vector3 impactPoint, Vector3 force )
	{
		var callerId = Rpc.CallerId;

		if ( callerId != NetworkOwner )
		{
			return;
		}

		if ( !rigidbody.IsValid() || !Force )
		{
			return;
		}

		if ( Config.Current.Game.ForcerExcludeTags.Any( tag => rigidbody.Tags.Has( tag ) ) )
		{
			return;
		}

		var distance = Vector3.DistanceBetween( WorldPosition, rigidbody.WorldPosition );
		if ( distance > _data.Range + 10f ) // Extra buffer to avoid false positives
		{
			return;
		}

		using ( Rpc.FilterInclude( rigidbody.Network.Owner ) )
		{
			BroadcastApplyForce( rigidbody, impactPoint, force );
		}

		_lastForceTime = 0f;
	}

	public void OnWireTick()
	{
		Forcing = _lastForceTime < 1f;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastApplyForce( Rigidbody rigidbody, Vector3 impactPoint, Vector3 force )
	{
		if ( !rigidbody.IsValid() )
		{
			return;
		}

		// Apply the force
		if ( _data.Uniform )
		{
			rigidbody.ApplyForce( force * rigidbody.Mass );
		}
		else
		{
			rigidbody.ApplyForceAt( impactPoint, force * rigidbody.Mass );
		}
	}
}
