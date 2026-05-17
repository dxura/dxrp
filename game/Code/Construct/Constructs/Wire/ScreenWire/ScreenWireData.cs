namespace Dxura.RP.Game.Wire;

public record ScreenWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public int Width { get; set; } = ScreenWireDefinition.DefaultScreenWidth;
	public int Height { get; set; } = ScreenWireDefinition.DefaultScreenHeight;

	public bool ShowHeader { get; set; } = true;
	public string Label { get; set; } = "Screen";
	public Color HeaderColor { get; set; } = Color.FromRgb( 0x4A90E2 );
}
