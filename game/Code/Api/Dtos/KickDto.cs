namespace Dxura.RP.Shared;

public class KickDto
{
	public required long KickedSteamId { get; set; }

	public required string KickerSteamName { get; set; }
	public required long KickerSteamId { get; set; }
	public string Reason { get; set; } = null!;
}
