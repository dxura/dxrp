namespace Dxura.RP.Game;

public struct FrameData() : IConstructData
{
	public readonly uint SchemaVersion => 1;
	public string ImgurUrl { get; set; } = "https://i.imgur.com/Joi6qfR.png";
	public Vector2 Size { get; set; } = 1;
	public Color FrameColor { get; set; } = Color.White;
	public bool FrameEnabled { get; set; } = true;

}
