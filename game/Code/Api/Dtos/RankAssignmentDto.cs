namespace Dxura.RP.Shared;

public class RankAssignmentDto
{
	public required long PlayerId { get; set; }
	public List<Guid> RankIds { get; set; } = [];
	// Backward compat for game clients that only read a single rank
	public Guid? RankId => RankIds.Count > 0 ? RankIds[0] : null;
}
