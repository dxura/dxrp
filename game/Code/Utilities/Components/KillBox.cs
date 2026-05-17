namespace Dxura.RP.Game;

public class KillBox : Component, Component.ITriggerListener
{
	[Property]
	private bool UseRootGameObject { get; set; } = true;

	[Property]
	private string[]? FilterTags { get; set; } = null;

	public void OnTriggerEnter( Collider other )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		var target = UseRootGameObject ? other.GameObject.Root : other.GameObject;

		// Tag filtering: Only allow objects with matching tags (When set)
		if ( FilterTags is { Length: > 0 } && !FilterTags.Any( t => target.Tags.Has( t ) ) )
		{
			return;
		}

		// Kill player
		if ( target.Tags.Has( Constants.PlayerTag ) )
		{
			var player = target.GetComponent<Player>();
			player.HealthComponent.IsGodMode = false;
			player.HealthComponent.TakeDamageHost( new DamageInfo( player, float.MaxValue ) );

			player.UnlockAchievement( "fall_map" );

			return;
		}

		// Destroy anything else
		target.Destroy();
	}
}
