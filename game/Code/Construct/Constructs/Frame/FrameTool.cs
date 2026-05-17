namespace Dxura.RP.Game.Tools;

[Tool( "#tool.frame.name", "#tool.frame.description", "#tool.group.interaction", MinimumLevel = 1 )]
public class FrameTool() : BaseConstructTool<FrameData>( ConstructType.Frame )
{
	protected override bool FlatSurface => true;

	[Property]
	[Title( "Imgur (Direct Link)" )]
	[Range( 0, 500 )]
	private string ImgurUrl
	{
		get => Data.ImgurUrl;
		set => Data = Data with
		{
			ImgurUrl = value
		};
	}

	[Property]
	[Title( "Size (Height)" )]
	[Range( 0.25f, 3f )] [Step( 0.1f )]
	private float Height
	{
		get => Data.Size.x;
		set => Data = Data with
		{
			Size = new Vector2( value, Data.Size.y )
		};
	}

	[Property]
	[Title( "Size (Width)" )]
	[Range( 0.25f, 3f )] [Step( 0.1f )]
	private float Width
	{
		get => Data.Size.y;
		set => Data = Data with
		{
			Size = new Vector2( Data.Size.x, value )
		};
	}

	[Property]
	[Title( "Render Frame" )]
	private bool FrameEnabled
	{
		get => Data.FrameEnabled;
		set => Data = Data with
		{
			FrameEnabled = value
		};
	}

	[Property]
	[Title( "Frame Color" )]
	private Color FrameColor
	{
		get => Data.FrameColor;
		set => Data = Data with
		{
			FrameColor = value
		};
	}
}
