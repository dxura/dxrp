using Dxura.RP.Game.UI;
using Dxura.RP.Game.Wire;
using Dxura.RP.Shared;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.wire.screen.name", "#tool.wire.screen.description", "#tool.group.notify", Category = ToolCategory.Wire, MinimumLevel = 1 )]
public class ScreenWireTool() : BaseConstructTool<ScreenWireData>( ConstructType.ScreenWire )
{
	protected override Rotation FlatSurfaceRotationOffset => Rotation.From( -90, 0, 0 );

	[Property]
	[Title( "Width" )]
	[Description( "Width of the screen in units" )]
	[Range( ScreenWireDefinition.MinScreenSize, ScreenWireDefinition.MaxScreenSize )]
	public int Width
	{
		get => Data.Width;
		set => Data = Data with
		{
			Width = value
		};
	}

	[Property]
	[Title( "Height" )]
	[Description( "Height of the screen in units" )]
	[Range( ScreenWireDefinition.MinScreenSize, ScreenWireDefinition.MaxScreenSize )]
	public int Height
	{
		get => Data.Height;
		set => Data = Data with
		{
			Height = value
		};
	}

	[Property]
	[Title( "Show Header" )]
	public bool ShowHeader
	{
		get => Data.ShowHeader;
		set => Data = Data with
		{
			ShowHeader = value
		};
	}

	[Property]
	[Title( "Header Label" )]
	[Description( "Text label to display on the header" )]
	public string Label
	{
		get => Data.Label;
		set => Data = Data with
		{
			Label = value
		};
	}

	[Property]
	[Title( "Header Color" )]
	public Color HeaderColor
	{
		get => Data.HeaderColor;
		set => Data = Data with
		{
			HeaderColor = value
		};
	}
}
