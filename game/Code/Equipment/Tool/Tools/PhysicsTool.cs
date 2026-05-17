namespace Dxura.RP.Game.Tools;

[Tool( "#tool.physics.name", "#tool.physics.description", "#tool.group.interaction" )]
public class PhysicsTool : BaseTool
{

	[Property]
	[Title( "Apply Friction" )]
	private bool ApplyFriction { get; set; } = true;

	[Property]
	[Title( "Friction" )]
	[Range( PropDefinition.MinPropFriction, PropDefinition.MaxPropFriction )]
	private float Friction { get; set; } = PropDefinition.MinPropFriction;

	[Property]
	[Title( "Apply Elasticity" )]
	private bool ApplyElasticity { get; set; } = false;

	[Property]
	[Title( "Elasticity" )]
	[Range( PropDefinition.MinPropElasticity, PropDefinition.MaxPropElasticity )]
	private float Elasticity { get; set; } = PropDefinition.MaxPropElasticity;

	public override string Attack1Control => "#tool.physics.attack1";
	public override string ReloadControl => "#tool.physics.reload";

	public override void PrimaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:physics:use", Config.Current.Game.ActionQuickCooldown, true ) )
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
				Friction = ApplyFriction ? Friction : null, Elasticity = ApplyElasticity ? Elasticity : null
			};

			Construct.Current.UpdateConstructPlayer( prop.Type, newData, go );
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void ReloadUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:physics:use", Config.Current.Game.ActionQuickCooldown, true ) )
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
				Friction = null, Elasticity = null
			};
			Construct.Current.UpdateConstructPlayer( prop.Type, newData, root );
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		Notify.Success( "#tool.physics.reset" );
	}
}
