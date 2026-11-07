namespace Dxura.RP.Game.Addons.Example;

/// <summary>
/// Entity content: a ball that bounces up when a player walks into it. The local player that touches it takes
/// ownership and simulates it; the network distributes the result.
/// </summary>
[Title( "Bouncy Ball" )]
[Category( "Entities" )]
public sealed class BouncyBallEntity : BaseEntity, Component.ITriggerListener
{
	[Property] [Range( 100f, 1500f )]
	public float BounceForce { get; set; } = 500f;

	[Property] [Range( 0.1f, 1f )]
	public float Cooldown { get; set; } = 0.3f;

	private TimeSince _lastBounce = 10f;

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( Application.IsHeadless || _lastBounce < Cooldown || !other.Tags.Has( Constants.PlayerTag ) )
		{
			return;
		}

		var player = other.GameObject?.Root.GetComponent<Player>();
		if ( !player.IsValid() || player != Player.Local || !Rigidbody.IsValid() )
		{
			return;
		}

		if ( !Network.IsOwner && !GameObject.Network.TakeOwnership() )
		{
			return;
		}

		var away = (WorldPosition - player.WorldPosition).WithZ( 0 ).Normal;
		Rigidbody.Velocity = (away * 0.3f + Vector3.Up).Normal * BounceForce;
		_lastBounce = 0f;
	}
}
