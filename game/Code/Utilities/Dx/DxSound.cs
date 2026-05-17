namespace Dxura.RP.Game;

public static partial class DxSound
{
	[Rpc.Host]
	public static void Broadcast( this SoundEvent? resource, Vector3 position, GameObject? parent = null )
	{
		if ( !resource.IsValid() )
		{
			return;
		}

		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:sound:{resource.ResourceName}",
			Config.Current.Game.SoundCooldown ) )
		{
			return;
		}

		resource.BroadcastHost( position, parent );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	public static void BroadcastHost( this SoundEvent? resource, Vector3 position, GameObject? parent = null )
	{
		if ( !resource.IsValid() )
		{
			return;
		}

		Play( resource, position, parent );
	}

	public static SoundHandle? Play( this SoundEvent? resource, Vector3 position, GameObject? parent = null )
	{
		if ( !resource.IsValid() )
		{
			return null;
		}

		if ( Player.Local.IsValid() )
		{
			var playerDistance = Player.Local.WorldPosition.Distance( position );
			if ( playerDistance > Math.Min( OcclusionSystem.OcclusionDistance, resource.Distance ) )
			{
				return null;
			}
		}

		var handle = Sound.Play( resource, position );

		if ( !handle.IsValid() )
		{
			return null;
		}

		if ( parent.IsValid() )
		{
			handle.Parent = parent;
			handle.FollowParent = true;
		}

		return handle;
	}

	public static SoundHandle? Play( this SoundEvent? resource )
	{
		if ( !resource.IsValid() )
		{
			return null;
		}

		return Sound.Play( resource );
	}
}
