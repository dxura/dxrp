using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public sealed class SpawnTriggerArea : Component, Component.ITriggerListener
{
	private const string PlayerIgnoreTag = "playerclip";

	[Property] public GameObject? TriggerGameObject { get; set; } = null;

	protected override void OnStart()
	{
		if ( TriggerGameObject.IsValid() )
		{
			TriggerGameObject.Enabled = false;
		}
	}

	public void OnTriggerEnter( GameObject other )
	{
		var player = other.Root.GetComponent<Player>();

		if ( player == Player.Local && TriggerGameObject.IsValid() )
		{
			TriggerGameObject.Enabled = true;
		}

		if ( !Networking.IsHost )
		{
			return;
		}

		var root = other.Root;

		// Destroy any constructs that are in the spawn area (except perma props)
		if ( root.Tags.Has( Constants.ConstructTag ) )
		{
			var construct = root.GetComponent<IConstruct>();
			var isPermanent = construct is { Owner: 0 };
			var canBypassSpawnBuild = construct != null && RankSystem.HasPermission( construct.Owner, Permission.SpawnBuildBypass );
			if ( !isPermanent && !canBypassSpawnBuild )
			{
				// Alert the owner of the prop
				var owner = construct != null ? GameUtils.GetPlayerById( construct.Owner ) : null;
				if ( owner.IsValid() )
				{
					owner.Error( "Cannot spawn props in the spawn area." );
				}

				root.Destroy();
			}
		}

		if ( player != null )
		{
			BroadcastPlayerIgnoreTag( root, true );

			// Give godmode in zone after respawn
			if ( player.TimeSinceLastRespawn < 5 )
			{
				player.AddStatus( Constants.GodStatus );
			}
		}
	}

	public void OnTriggerExit( GameObject other )
	{
		var player = other.Root.GetComponent<Player>();

		if ( player == Player.Local && TriggerGameObject.IsValid() )
		{
			TriggerGameObject.Enabled = false;
		}

		if ( Networking.IsHost && player != null )
		{
			BroadcastPlayerIgnoreTag( other.Root, false );
			player.RemoveStatus( Constants.GodStatus );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastPlayerIgnoreTag( GameObject gameObject, bool isAdd )
	{
		if ( isAdd )
		{
			gameObject.Tags.Add( PlayerIgnoreTag );
		}
		else
		{
			gameObject.Tags.Remove( PlayerIgnoreTag );
		}
	}
}
