namespace Dxura.RP.Game.Statuses;

public class FreezeStatus : BaseStatus
{
	public override string Id => Constants.FreezeStatus;
	public override string Name => "#generic.frozen";
	public override string? MaterialIcon => "ac_unit";
	public override Color Color => Color.FromRgb( 0x00BCD4 );

	public override bool RemoveOnDeath => true;
	public override bool ShowOnNameplate => true;

	public override void OnAddedServer( Player player )
	{
		// Holster current weapon
		player.Holster();
	}

	public override void OnAddedOwner( Player player )
	{
		player.CantSwitch = true;

		// Block all movement
		player.Controller.WalkSpeed = 0f;
		player.Controller.RunSpeed = 0f;
		player.Controller.DuckedSpeed = 0f;
		player.Controller.JumpSpeed = 0f;
	}

	public override void OnRemovedOwner( Player player )
	{
		player.CantSwitch = false;

		// Restore base speeds
		player.Controller.WalkSpeed = GameConfig.WalkSpeed;
		player.Controller.RunSpeed = GameConfig.RunSpeed;
		player.Controller.DuckedSpeed = GameConfig.DuckedSpeed;
		player.Controller.JumpSpeed = GameConfig.JumpSpeed;
	}

	public override void OnAddedBroadcast( Player player )
	{
		if ( player.AnimationHelper.IsValid() )
		{
			player.AnimationHelper.HoldTypePose = 3;
		}
	}

	public override void OnRemovedBroadcast( Player player )
	{
		if ( player.AnimationHelper.IsValid() )
		{
			player.AnimationHelper.HoldTypePose = 0;
		}
	}
}
