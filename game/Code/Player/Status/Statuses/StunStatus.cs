namespace Dxura.RP.Game.Statuses;

/// <summary>
///     Stun lasts <see cref="GameConfig.StunDuration" /> seconds total.
///     The target ragdolls for <see cref="GameConfig.StunRagdollDuration" /> seconds, then stands immobile until the status expires.
/// </summary>
public class StunStatus : BaseStatus
{
	public override string Id => Constants.StunStatus;
	public override string Name => "#generic.stunned";
	public override string? MaterialIcon => "bolt";
	public override Color Color => Color.FromRgb( 0xFFC107 );
	public override float? DefaultDuration => Config.Current.Game.StunDuration;
	public override bool RemoveOnDeath => true;
	public override bool RemoveOnJobChange => true;
	public override bool ShowOnNameplate => true;

	public override void OnAddedServer( Player player )
	{
		player.BeginStunRagdollHost();
	}

	public override void OnRefreshedServer( Player player )
	{
		player.BeginStunRagdollHost();
	}

	public override void OnRemovedServer( Player player )
	{
		player.ClearStunRagdollHost();
	}

	public override void OnAddedOwner( Player player )
	{
		player.ApplyStunOwnerEffects();
		ApplyImmobilization( player );
	}

	public override void OnRemovedOwner( Player player )
	{
		player.CantSwitch = false;
		RestoreMovement( player );
		player.UpdatePerspective();
	}

	public override void OnUpdateOwner( Player player )
	{
		ApplyImmobilization( player );
	}

	public override void OnRemovedBroadcast( Player player )
	{
		if ( player.AnimationHelper.IsValid() )
		{
			player.AnimationHelper.HoldTypePose = 0;
		}
	}

	private static void ApplyImmobilization( Player player )
	{
		player.CantSwitch = true;

		player.Controller.WalkSpeed = 0f;
		player.Controller.RunSpeed = 0f;
		player.Controller.DuckedSpeed = 0f;
		player.Controller.JumpSpeed = 0f;
		player.Controller.WishVelocity = Vector3.Zero;
		player.Controller.GroundVelocity = Vector3.Zero;

		if ( player.Controller.Body.IsValid() )
		{
			player.Controller.Body.Velocity = Vector3.Zero;
		}
	}

	private static void RestoreMovement( Player player )
	{
		player.Controller.WalkSpeed = GameConfig.WalkSpeed;
		player.Controller.RunSpeed = GameConfig.RunSpeed;
		player.Controller.DuckedSpeed = GameConfig.DuckedSpeed;
		player.Controller.JumpSpeed = GameConfig.JumpSpeed;
	}
}
