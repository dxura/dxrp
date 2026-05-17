namespace Dxura.RP.Shared;

public class ServerHostStatsDto
{
	public float OutBytesPerSecond { get; set; }
	public float InBytesPerSecond { get; set; }

	public ushort Fps { get; set; }
	public ushort Avg30SecFps { get; set; }
	public ushort Min30SecFps { get; set; }
}
