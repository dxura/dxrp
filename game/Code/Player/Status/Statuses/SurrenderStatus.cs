namespace Dxura.RP.Game.Statuses;

public class SurrenderStatus : BaseStatus
{
	public override string Id => Constants.SurrenderStatus;
	public override string Name => "Surrender";
	public override string? MaterialIcon => "back_hand";
	public override Color Color => Color.FromRgb( 0xFFC107 );

	public override bool RemoveOnDeath => true;
	public override bool ShowOnNameplate => false;

	public override void OnAddedServer( Player player )
	{
		// Holster current weapon
		player.Holster();
	}

	public override void OnAddedOwner( Player player )
	{
		player.CantSwitch = true;

		// Force slow walk speed on both walk and run
		var slowSpeed = Config.Current.Game.SlowWalkSpeed;
		player.Controller.WalkSpeed = slowSpeed;
		player.Controller.RunSpeed = slowSpeed;
		player.Controller.DuckedSpeed = MathF.Min( GameConfig.DuckedSpeed, slowSpeed );
	}

	public override void OnRemovedOwner( Player player )
	{
		player.CantSwitch = false;

		// Restore base speeds
		player.Controller.WalkSpeed = GameConfig.WalkSpeed;
		player.Controller.RunSpeed = GameConfig.RunSpeed;
		player.Controller.DuckedSpeed = GameConfig.DuckedSpeed;
	}

	public override void OnAddedBroadcast( Player player )
	{
		SetHandsUp( player, true );
	}

	public override void OnRemovedBroadcast( Player player )
	{
		SetHandsUp( player, false );
	}

	private void SetHandsUp( Player player, bool handsUp )
	{
		var animationHelper = player.AnimationHelper;
		if ( animationHelper == null )
		{
			return;
		}

		if ( handsUp )
		{
			var chest = player.ChestBone ?? player.BodyRoot ?? player.GameObject;

			var leftHand = new GameObject
			{
				Name = "IK_LeftHand_Surrender",
				Parent = chest,
				LocalPosition = new Vector3( 20, 0, 10 ),
				LocalRotation = Rotation.From( 0, 0, -90 )
			};

			var rightHand = new GameObject
			{
				Name = "IK_RightHand_Surrender",
				Parent = chest,
				LocalPosition = new Vector3( 20, 0, -10 ),
				LocalRotation = Rotation.From( 0, 0, 90 )
			};

			animationHelper.IkLeftHand = leftHand;
			animationHelper.IkRightHand = rightHand;
		}
		else
		{
			if ( animationHelper.IkLeftHand.IsValid() && animationHelper.IkLeftHand.Name == "IK_LeftHand_Surrender" )
			{
				animationHelper.IkLeftHand.Destroy();
				animationHelper.IkLeftHand = null;
			}

			if ( animationHelper.IkRightHand.IsValid() && animationHelper.IkRightHand.Name == "IK_RightHand_Surrender" )
			{
				animationHelper.IkRightHand.Destroy();
				animationHelper.IkRightHand = null;
			}
		}
	}
}
