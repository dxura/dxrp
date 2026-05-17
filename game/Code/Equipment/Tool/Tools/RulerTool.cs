using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.ruler.name", "#tool.ruler.description", "#tool.group.miscellaneous" )]
public class RulerTool : BaseTool
{
	private Vector3? _startPoint;
	private Vector3? _endPoint;
	private bool _measuring;

	public override string Attack1Control => "#tool.ruler.attack1";
	public override string Attack2Control => "#tool.ruler.attack2";

	public override void PrimaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:ruler:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit )
		{
			return;
		}

		if ( !_measuring )
		{
			_startPoint = tr.HitPosition;
			_measuring = true;

			Notify.Info( "#tool.ruler.start_measuring" );
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		}
		else
		{
			_endPoint = tr.HitPosition;
			_measuring = false;

			if ( _startPoint.HasValue && _endPoint.HasValue )
			{
				var distance = Vector3.DistanceBetween( _startPoint.Value, _endPoint.Value );

				Notify.Info( $"Distance: {distance:F2} units" );
			}

			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		}
	}

	public override void SecondaryUseStart()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "tool:ruler:use", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return;
		}

		_startPoint = null;
		_endPoint = null;
		_measuring = false;

		Notify.Info( "#tool.ruler.reset" );
	}
}
