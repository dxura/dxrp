namespace Dxura.RP.Shared;

public class SentinelDto
{
	public required long SteamId { get; set; }
	public required string SteamName { get; set; }

	public string Exploit { get; set; } = null!;
	public string Detail { get; set; } = null!;
}
