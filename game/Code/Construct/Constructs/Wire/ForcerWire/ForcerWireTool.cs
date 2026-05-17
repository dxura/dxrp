using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.forcer.name", "#tool.wire.forcer.description", "#tool.group.interaction", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class ForcerWireTool() : BaseConstructTool<ForcerWireData>( ConstructType.ForcerWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Force Amount" )]
	[Description( "Multiplier for the force applied to the rigidbody" )]
	[Range( ForcerWireDefinition.MinForcerWireForce, ForcerWireDefinition.MaxForcerWireForce )]
	public float ForceAmount
	{
		get => Data.ForceAmount;
		set => Data = Data with
		{
			ForceAmount = Math.Clamp( value, ForcerWireDefinition.MinForcerWireForce, ForcerWireDefinition.MaxForcerWireForce )
		};
	}

	[Property]
	[Title( "Range" )]
	[Description( "Maximum range of the laser in units" )]
	[Range( ForcerWireDefinition.MinForcerLaserWireRange, ForcerWireDefinition.MaxForcerLaserWireRange )]
	public float Range
	{
		get => Data.Range;
		set => Data = Data with
		{
			Range = Math.Clamp( value, ForcerWireDefinition.MinForcerLaserWireRange, ForcerWireDefinition.MaxForcerLaserWireRange )
		};
	}

	[Property]
	[Title( "Uniform Force" )]
	[Description( "If true, applies the force to entire object instead of just the impact point" )]
	public bool Uniform
	{
		get => Data.Uniform;
		set => Data = Data with
		{
			Uniform = value
		};
	}
}
