namespace Dxura.RP.Game.UI;

public static class UiUtils
{
	/// <summary>
	/// Maps a health percentage (0–100) to a hex color string using the DXRP gradient:
	/// low red → mid yellow → high green. Matches the PlayerInfo vitals bar exactly.
	/// </summary>
	public static string HealthColorHex( float healthPercent )
	{
		var low = new Color( 0xf8 / 255f, 0x71 / 255f, 0x71 / 255f );
		var mid = new Color( 0xfa / 255f, 0xcc / 255f, 0x15 / 255f );
		var high = new Color( 0x22 / 255f, 0xc5 / 255f, 0x5e / 255f );

		if ( healthPercent <= 15f )
		{
			return low.Hex;
		}

		var t = (healthPercent - 15f) / 85f;
		var c = t < 0.5f
			? Color.Lerp( low, mid, t * 2f )
			: Color.Lerp( mid, high, (t - 0.5f) * 2f );
		return c.Hex;
	}
}
