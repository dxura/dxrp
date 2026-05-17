using Editor.GraphicsItems;
using static Editor.GraphicsItems.ChartBackground;

namespace Dxura.RP.Game.Editor;

/// <summary>
/// A widget which contains an editable curve
/// </summary>
public class RecoilPatternEditor : GraphicsView
{
	readonly ChartBackground _background;

	public RecoilPatternEditor( Widget? parent ) : base( parent )
	{
		SceneRect = new( 0, Size );
		HorizontalScrollbar = ScrollbarMode.Off;
		VerticalScrollbar = ScrollbarMode.Off;
		Scale = 1;
		CenterOn( new Vector2( 100, 10 ) );

		_background = new ChartBackground
		{
			Size = SceneRect.Size,
			RangeX = new Vector2( -5, 5 ),
			RangeY = new Vector2( 0, 5 ),
			AxisX = new AxisConfig { LineColor = Color.White.WithAlpha( 0.2f ), Ticks = 11, Width = 30.0f, LabelColor = Color.White.WithAlpha( 0.5f ) },
			AxisY = new AxisConfig { LineColor = Color.White.WithAlpha( 0.2f ), Ticks = 11, Width = 20.0f, LabelColor = Color.White.WithAlpha( 0.5f ) }
		};
		Add( _background );
	}

	public void SetProperty( SerializedProperty property )
	{
		var instance = new RecoilPatternInstance( this )
		{
			RangeX = _background.RangeX,
			RangeY = _background.RangeY,
			Property = property
		};

		Add( instance );
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		SceneRect = new( 0, Size );
		_background.Size = SceneRect.Size;
		
		foreach ( var i in Items )
		{
			if ( i is RecoilPatternInstance instance )
			{
				if ( instance.SceneRect == _background.ChartRect ) continue;
				instance.SceneRect = _background.ChartRect;
				instance.Refresh();
			}
		}
	}
}
