namespace Dxura.RP.Game.Addons.Official;

[Title( "Soccer Ball" )]
[Category( "Entities" )]
public sealed class SoccerBallEntity : BaseEntity, Component.ITriggerListener
{
	[Property] [Range( 100f, 1500f )] public float KickForce { get; set; } = 600f;
	[Property] [Range( 0f, 1f )] public float KickUpwardBias { get; set; } = 0.3f;
	[Property] [Range( 0.1f, 1f )] public float KickCooldown { get; set; } = 0.2f;
	[Property] public SoundEvent? KickSound { get; set; }

	private TimeSince _lastKick = 10f;

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if (Application.IsHeadless || other.Tags.Has( Constants.OccludeTag )) return;
		
		if ( _lastKick < KickCooldown || !other.Tags.Has( Constants.PlayerTag ) )
		{
			return;
		}
		
		var player = other.GameObject?.Root.GetComponent<Player>();
		if ( !player.IsValid() || player != Player.Local )
		{
			return;
		}

		var playerVelocity = player.Controller?.Velocity ?? Vector3.Zero;
		var kickDir = (WorldPosition - player.WorldPosition).Normal;

		if ( playerVelocity.Length > 10f )
		{
			kickDir = Vector3.Lerp( kickDir, playerVelocity.Normal, 0.5f ).Normal;
		}

		kickDir = (kickDir + Vector3.Up * KickUpwardBias).Normal;
		var speedFactor = Math.Clamp( playerVelocity.Length / 300f, 0.4f, 1.5f );

		if ( !Network.IsOwner )
		{
			var didTakeOver = GameObject.Network.TakeOwnership();
			if (!didTakeOver) return;
		}

		Rigidbody?.Velocity = kickDir * KickForce * speedFactor;
		_lastKick = 0f;

		KickSound.Broadcast( WorldPosition );
	}
	
	public override bool CanScale( Player player )
	{
		if ( !this.IsValid() || !player.IsValid() )
		{
			return false;
		}
		
		return player.SteamId == Owner && GameUtils.HasPermission( player.SteamId, GameObject );
	}
}
