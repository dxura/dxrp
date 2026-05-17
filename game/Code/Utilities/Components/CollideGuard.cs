namespace Dxura.RP.Game;

public class CollideGuard : Component, Component.ICollisionListener, IGameEvents
{
	public bool Immediate = false;

	private TimeSince _lastPlayerCollisionTime = 0;

	private Collider? _collider;

	protected override void OnStart()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		_collider = GameObject.GetComponentInChildren<Collider>();

		if ( !_collider.IsValid() )
		{
			Destroy();
			return;
		}

		GameManager.Instance.BroadcastTagHost( GameObject, true, Constants.PlayerClip );
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( !_collider.IsValid() )
		{
			GameManager.Instance.BroadcastTagHost( GameObject, false, Constants.PlayerClip );
			Destroy();
			return;
		}

		var bounds = _collider.GetWorldBounds();
		bounds = bounds.Grow( 10f );

		var objectsInBounds = Scene.FindInPhysics( bounds );
		var playersInBounds = objectsInBounds
			.Where( x => x.Tags.Has( Constants.PlayerTag ) )
			.Any();

		if ( playersInBounds )
		{
			_lastPlayerCollisionTime = 0;
		}

		if ( _lastPlayerCollisionTime > 1.5f || !playersInBounds && Immediate )
		{
			GameManager.Instance.BroadcastTagHost( GameObject, false, Constants.PlayerClip );
			Destroy();
		}
	}

	public void ResetTimer()
	{
		_lastPlayerCollisionTime = 0;
	}
}
