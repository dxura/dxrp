using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.labeler.name", "#tool.wire.labeler.description", "#tool.group.core", Category = ToolCategory.Wire )]
public class WireLabelerTool : BaseTool
{
	public static bool IsDeployed { get; private set; }

	[Property]
	[Title( "Label" )]
	[Description( "Label to apply to the wire construct you are looking at" )]
	[Range( 0, WireLabelHelper.MaxWireLabelLength )]
	public string Label { get; set; } = string.Empty;

	public override string Attack1Control => "#tool.wire.labeler.attack1";
	public override string Attack2Control => "#tool.wire.labeler.attack2";
	public override string ReloadControl => "#tool.wire.labeler.reload";

	public override void OnToolFixedUpdate()
	{
		if ( !Player.Local.IsValid() )
		{
			return;
		}

		IsDeployed = true;
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		IsDeployed = false;
	}

	public override void PrimaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:wire:labeler:apply", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();
		if ( !TryGetWireTarget( tr, out var targetGo ) )
		{
			return;
		}

		if ( Construct.Current.UpdateWireLabelPlayer( targetGo, Label ) )
		{
			Notify.Success( "#tool.wire.labeler.applied" );
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		}
	}

	public override void SecondaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:wire:labeler:copy", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();
		if ( !TryGetWireTarget( tr, out var targetGo ) )
		{
			return;
		}

		var construct = targetGo.GetComponent<IConstruct>();
		if ( construct?.Data is not IWireLabelData labelData )
		{
			Notify.Error( "#tool.wire.labeler.invalid" );
			return;
		}

		Label = labelData.Label;
		Notify.Success( "#tool.wire.labeler.copied" );
		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void ReloadUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:wire:labeler:clear", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();
		if ( !TryGetWireTarget( tr, out var targetGo ) )
		{
			return;
		}

		if ( Construct.Current.UpdateWireLabelPlayer( targetGo, string.Empty ) )
		{
			Notify.Success( "#tool.wire.labeler.cleared" );
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		}
	}

	private bool TryGetWireTarget( SceneTraceResult tr, out GameObject targetGo )
	{
		targetGo = default!;

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			Notify.Error( "#tool.wire.labeler.invalid" );
			return false;
		}

		targetGo = tr.GameObject.Root;
		var wireComponent = targetGo.GetComponent<IWireComponent>();
		if ( wireComponent == null || !wireComponent.GameObject.IsValid() )
		{
			Notify.Error( "#tool.wire.labeler.invalid" );
			return false;
		}

		var construct = targetGo.GetComponent<IConstruct>();
		if ( construct?.Data is not IWireLabelData )
		{
			Notify.Error( "#tool.wire.labeler.invalid" );
			return false;
		}

		if ( !GameUtils.HasPermission( Connection.Local, wireComponent.GameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return false;
		}

		return true;
	}
}
