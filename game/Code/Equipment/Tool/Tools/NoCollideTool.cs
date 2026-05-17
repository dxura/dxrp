namespace Dxura.RP.Game.Tools;

[Tool( "#tool.nocollide.name", "#tool.nocollide.description", "#tool.group.construction" )]
public class NoCollideTool : BaseTool
{
	public override string Attack1Control => "#tool.nocollide.attack1";

	public override void PrimaryUseStart()
	{
		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var rootGameObject = tr.GameObject.Root;

		if ( !GameManager.Instance.RequestOwnership( rootGameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "nocollide", Config.Current.Game.NoCollideCooldown, true ) )
		{
			return;
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );


		var prop = rootGameObject.GetComponent<Prop>();

		if ( !prop.IsValid() )
		{
			Notify.Error( "#generic.error" );
			return;
		}

		if ( prop.Data is PropData propData )
		{
			var newData = propData with
			{
				NoCollide = !propData.NoCollide
			};
			Construct.Current.UpdateConstructPlayer( prop.Type, newData, rootGameObject );
			Notify.Success( newData.NoCollide ? "#tool.nocollide.add" : "#tool.nocollide.remove" );
		}
	}
}
