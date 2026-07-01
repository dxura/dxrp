namespace Dxura.RP.Game;

public static class PackedColorExtensions
{
	public static Color ToColor( this int packedColor )
	{
		return unchecked( (uint)packedColor ).ToColor();
	}

	public static Color ToColor( this uint packedColor )
	{
		return Color.FromBytes(
			(byte)(packedColor >> 16),
			(byte)(packedColor >> 8),
			(byte)packedColor,
			255 );
	}

	/// <summary>Packs a <see cref="Color"/> back into the 0xRRGGBB form the party system stores/syncs.</summary>
	public static uint ToPacked( this Color color )
	{
		var c = color.ToColor32();
		return (uint)((c.r << 16) | (c.g << 8) | c.b);
	}

	public static string Hex( this uint packedColor )
	{
		return packedColor.ToColor().Hex;
	}

	public static string Hex( this int packedColor )
	{
		return packedColor.ToColor().Hex;
	}
}
