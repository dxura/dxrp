/// <summary>
///     Represents a resource of anything.
/// </summary>
[GameResource( "Resource", "resource", "A growable of sorts." )]
public class Resource : GameResource
{
	[Property]
	public required string Identifier { get; set; }

	[Property]
	public required string Name { get; set; }

	[Property]
	public Texture? Icon { get; set; }

	[Property]
	public Color Color { get; set; } = Color.White;
}
