namespace Dxura.RP.Game;

public struct TextData() : IConstructData
{
	public readonly uint SchemaVersion => 1;

	public string Text { get; set; } = "";
	public float FontSize { get; set; } = 100f;
	public Color Color { get; set; } = Color.White;

	public bool Italic { get; set; } = false;

	public int FontWeight { get; set; } = 400;

	public bool Outline { get; set; } = false;
	public Color OutlineColor { get; set; } = Color.Black;
	public float OutlineSize { get; set; } = 5f;
}
