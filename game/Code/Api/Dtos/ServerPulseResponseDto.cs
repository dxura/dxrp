namespace Dxura.RP.Shared;

public class ServerPulseResponseDto
{
	public required IEnumerable<RankAssignmentDto> RankAssignments { get; set; }

	public required IEnumerable<BanDto> Bans { get; set; }
	public required IEnumerable<ServerActionDto> PendingActions { get; set; }
}
