namespace Dxura.RP.Game.Tools;

[Tool( "#tool.fadingdoor.name", "#tool.fadingdoor.description", "#tool.group.interaction" )]
public class FadingDoorTool : BaseTool
{
	[Property]
	[Title( "#tool.fadingdoor.duration" )]
	[Range( 0, PropDefinition.MaxFadingDoorDuration )] [Step( 1f )]
	[Description( "#tool.fadingdoor.duration.description" )]
	private float FadeDuration { get; set; } = 2f;

	[Property] [Title( "#tool.fadingdoor.reverse" )]
	private bool IsReversed { get; set; } = false;

	public override string Attack1Control => "#tool.fadingdoor.attack1";
	public override string ReloadControl => "#tool.fadingdoor.reload";

	public override void PrimaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:fadingdoor:use", Config.Current.Game.FadingDoorCreateCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit )
		{
			return;
		}

		var rootGameObject = tr.GameObject.Root;

		if ( !GameUtils.HasPermission( Connection.Local, rootGameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var prop = rootGameObject.GetComponent<Prop>();

		if ( !prop.IsValid() )
		{
			return;
		}

		if ( FadeDuration != 0f && FadeDuration < PropDefinition.MinFadingDoorDuration )
		{
			Notify.Warn( $"Duration must be {PropDefinition.MinFadingDoorDuration}s or greater, or 0 for switch state mode" );
			return;
		}

		ApplyFadingDoorHost( prop, FadeDuration, false, IsReversed );

		Notify.Success( "#tool.fadingdoor.add" );

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void ReloadUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:fadingdoor:use", Config.Current.Game.FadingDoorCreateCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit )
		{
			return;
		}

		var rootGameObject = tr.GameObject.Root;

		if ( !GameUtils.HasPermission( Connection.Local, rootGameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		var prop = rootGameObject.GetComponent<Prop>();

		if ( !prop.IsValid() )
		{
			return;
		}

		ApplyFadingDoorHost( prop, FadeDuration, true );

		Notify.Success( "#tool.fadingdoor.remove" );

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	private void ApplyFadingDoorHost( Prop prop, float duration, bool remove = false, bool reverse = false )
	{

		// Get current PropData
		var data = Construct.Current.GetData<PropData>( prop );

		// Update fading door properties
		if ( remove )
		{
			data.FadingDoor = false;
			data.FadingDoorDuration = null;
			data.FadingDoorIsReversed = false;
		}
		else
		{
			data.FadingDoor = true;
			data.FadingDoorDuration = duration;
			data.FadingDoorIsReversed = reverse;
		}

		Construct.Current.UpdateConstructPlayer( prop.Type, data, prop.GameObject );
	}
}
