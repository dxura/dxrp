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

	public static string Hex( this uint packedColor )
	{
		return packedColor.ToColor().Hex;
	}

	public static string Hex( this int packedColor )
	{
		return packedColor.ToColor().Hex;
	}
}
