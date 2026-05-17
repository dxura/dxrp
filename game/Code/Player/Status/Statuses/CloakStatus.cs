namespace Dxura.RP.Game.Statuses;

public class CloakStatus : BaseStatus
{
	public override string Id => Constants.CloakStatus;
	public override string Name => "Cloaked";
	public override string? MaterialIcon => "visibility_off";
	public override Color Color => Color.FromRgb( 0x9E9E9E );

	public override bool RemoveOnDeath => true;

	public override void OnAddedBroadcast( Player player )
	{
		ToggleInvisibility( player, true );
	}

	public override void OnRemovedBroadcast( Player player )
	{
		ToggleInvisibility( player, false );
	}

	private void ToggleInvisibility( Player player, bool invisible )
	{
		player.ModelHitboxes.Enabled = !invisible && !GameManager.IsHeadless;
		player.Controller.EnableFootstepSounds = !invisible;

		if ( !player.IsLocalPlayer )
		{
			player.Controller.Enabled = !invisible && !GameManager.IsHeadless;
			player.Controller.BodyCollider.Enabled = !invisible;
			player.Controller.FeetCollider.Enabled = !invisible;
			player.Renderer.Enabled = !invisible && !GameManager.IsHeadless;
		}

		player.GameObject.Tags.Set( "invisible", invisible );
		player.GameObject.Tags.Set( "playerclip", invisible );

		if ( !invisible )
		{
			player.ModelHitboxes.Rebuild();
		}

		player.Transform.ClearInterpolation();
	}
}
