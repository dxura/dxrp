namespace Dxura.RP.Game.Tools;

[Tool( "#tool.color.name", "#tool.color.description", "#tool.group.render" )]
public class ColorTool : BaseTool
{

	[Property]
	[Title( "Override Materials" )]
	[Category( "Advanced" )]
	[Description( "Replaces materials with a white material that can be fully colored. Disable to preserve textures." )]
	public bool OverrideMaterials { get; set; } = false;

	[Property]
	[Title( "Color" )]
	public Color Color { get; set; } = Color.White;

	public override string Attack1Control => "#tool.color.attack1";
	public override string Attack2Control => "#tool.color.attack2";
	public override string ReloadControl => "#tool.color.reload";

	public override void PrimaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:color:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var go = tr.GameObject.Root;

		var prop = go.GetComponent<Prop>();

		if ( !prop.IsValid() )
		{
			return;
		}

		if ( !GameUtils.HasPermission( Connection.Local, go ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		if ( prop.Data is PropData propData )
		{
			var newData = propData with
			{
				Material = OverrideMaterials ? "materials/default/white.vmat" : propData.Material, Tint = Color
			};

			Construct.Current.UpdateConstructPlayer( prop.Type, newData, go );
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void SecondaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:color:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var go = tr.GameObject.Root;

		var prop = go.GetComponent<Prop>();

		if ( !prop.IsValid() )
		{
			return;
		}

		if ( prop.Data is PropData propData && propData.Tint.HasValue )
		{
			Color = propData.Tint.Value;
			Notify.Success( "#tool.color.copied" );
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		}
	}

	public override void ReloadUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:color:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();
		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		var root = tr.GameObject.Root;

		if ( !GameUtils.HasPermission( Connection.Local, root ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var prop = root.GetComponent<Prop>();
		if ( !prop.IsValid() )
		{
			return;
		}

		if ( prop.Data is PropData propData )
		{
			var newData = propData with
			{
				Tint = Color.White
			};
			Construct.Current.UpdateConstructPlayer( prop.Type, newData, root );
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		Notify.Success( "#tool.color.reset" );
	}
}
