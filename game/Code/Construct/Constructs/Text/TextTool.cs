namespace Dxura.RP.Game.Tools;

[Tool( "#tool.text.name", "#tool.text.description", "#tool.group.construction" )]
public class TextTool() : BaseConstructTool<TextData>( ConstructType.Text )
{
	protected override bool FlatSurface => true;

	[Property]
	[Title( "Text" )]
	[Range( TextDefinition.MinTextLength, TextDefinition.MaxTextLength )]
	public string Text
	{
		get => Data.Text;
		set => Data = Data with
		{
			Text = value
		};
	}

	[Property]
	[Title( "Size" )]
	[Range( TextDefinition.MinFontSize, TextDefinition.MaxFontSize )]
	public float Size
	{
		get => Data.FontSize;
		set => Data = Data with
		{
			FontSize = value
		};
	}

	[Property]
	[Title( "Color" )]
	public Color Color
	{
		get => Data.Color;
		set => Data = Data with
		{
			Color = value
		};
	}

	[Property]
	[Title( "Italic" )]
	public bool Italic
	{
		get => Data.Italic;
		set => Data = Data with
		{
			Italic = value
		};
	}

	[Property]
	[Title( "Font Weight" )]
	[Range( TextDefinition.MinFontWeight, TextDefinition.MaxFontWeight )]
	public int FontWeight
	{
		get => Data.FontWeight;
		set => Data = Data with
		{
			FontWeight = value
		};
	}

	[Property]
	[Title( "Outline" )]
	public bool Outline
	{
		get => Data.Outline;
		set => Data = Data with
		{
			Outline = value
		};
	}

	[Property]
	[Title( "Outline Color" )]
	public Color OutlineColor
	{
		get => Data.OutlineColor;
		set => Data = Data with
		{
			OutlineColor = value
		};
	}

	[Property]
	[Title( "Outline Size" )]
	[Range( TextDefinition.MinOutlineSize, TextDefinition.MaxOutlineSize )]
	[Step( 1 )]
	public float OutlineSize
	{
		get => Data.OutlineSize;
		set => Data = Data with
		{
			OutlineSize = value
		};
	}
}
