namespace Dxura.RP.Game.Throw;

public class ExcrementThrow : Component, Component.ICollisionListener
{
	[Property]
	private GameObject? ImpactGameObject { get; set; }

	[Property]
	private SoundEvent? ImpactSound { get; set; }

	private bool _didImpact;

	public void OnCollisionStart( Collision collision )
	{
		if ( IsProxy || _didImpact )
		{
			return;
		}

		var hitRoot = collision.Other.GameObject.Root;

		var hitPlayer = hitRoot.GetComponent<Player>();
		if ( hitPlayer.IsValid() && hitPlayer == Player.Local )
		{
			// Don't allow self hit
			return;
		}

		Splat( hitRoot, collision.Contact.Point, collision.Contact.Normal );
		_didImpact = true;
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void Splat( GameObject? hitObject, Vector3 hitPosition, Vector3 hitNormal )
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:excrement:throw", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		ImpactSound?.BroadcastHost( WorldPosition );

		var thrower = GameUtils.GetPlayerByConnectionId( Network.OwnerId );
		if ( thrower.IsValid() && thrower.CurrentEquipment.IsValid() && hitObject.IsValid() )
		{
			// Do a little damage
			hitObject.TakeDamageHost( new DamageInfo( thrower, 1, thrower.CurrentEquipment, hitPosition ) );
		}

		if ( ImpactGameObject.IsValid() )
		{
			var rotation = Rotation.LookAt( -hitNormal ) * Rotation.FromPitch( -90 );
			var impactGameObject = ImpactGameObject?.Clone( hitPosition, rotation );

			if ( impactGameObject.IsValid() )
			{
				impactGameObject.DestroyAsync( 30, true );
				impactGameObject.NetworkSpawn();
			}
		}

		GameObject.Destroy();
	}
}
