using Dxura.RP.Shared;
namespace Dxura.RP.Game.Tools;

[Tool( "#tool.remover.name", "#tool.remover.description", "#tool.group.construction" )]
public class RemoverTool : BaseTool
{
	public override string Attack1Control => "#tool.remover.attack1";

	public override void PrimaryUseStart()
	{
		var ownerBypass = RankSystem.HasLocalPermission( Permission.RemoverBypass );

		// Staff bypass cooldown
		if ( !ownerBypass && Cooldown.Current.CheckAndStartCooldown( "tool:remover:use", Config.Current.Game.RemoverCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.Tags.HasAny( Constants.ConstructTag, Constants.EntityTag ) )
		{
			return;
		}

		var rootGameObject = tr.GameObject.Root;

		if ( !rootGameObject.IsValid() )
		{
			return;
		}

		// Entity restriction: players can only destroy removable entities
		if ( rootGameObject.Tags.Has( Constants.EntityTag ) && !ownerBypass && !rootGameObject.Tags.Has( Constants.RestrictedEntity ) )
		{
			return;
		}

		// Permission check: staff bypass, others need permission
		if ( !ownerBypass && !GameUtils.HasPermission( Player.Local.SteamId, rootGameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		GameManager.Instance.DestroyHost( rootGameObject );

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}
}
