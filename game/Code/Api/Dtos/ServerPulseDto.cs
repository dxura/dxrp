namespace Dxura.RP.Shared;

public class ServerPulseDto
{
	public required long[] PlayerIds { get; set; }
	public ServerHostStatsDto? HostStats { get; set; }
}
