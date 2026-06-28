using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Statuses;

public class StunStatus : FreezeStatus
{
	public const float RagdollDuration = 5f;

	public override string Id => Constants.StunStatus;
	public override string Name => "#generic.stunned";
	public override string? MaterialIcon => "bolt";
	public override Color Color => Color.FromRgb( 0xFFC107 );
	public override float? DefaultDuration => 10f;
	public override bool RemoveOnDeath => true;
	public override bool ShowOnNameplate => true;

	public override void OnAddedServer( Player player )
	{
		base.OnAddedServer( player );
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
		base.OnAddedOwner( player );

		player.Holster();

		if ( EquipmentOverlay.Instance.IsValid() )
		{
			EquipmentOverlay.Instance.IsActive = false;
		}

		player.Controller.ThirdPerson = true;
	}

	public override void OnRemovedOwner( Player player )
	{
		base.OnRemovedOwner( player );
		player.UpdatePerspective();
	}

	public override void OnRemovedBroadcast( Player player )
	{
		if ( player.AnimationHelper.IsValid() )
		{
			player.AnimationHelper.HoldTypePose = 0;
		}
	}
}
