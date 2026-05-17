namespace Dxura.RP.Shared;

public class UpdateServerDto
{
	public string? Name { get; init; }
	public string? Description { get; init; }
	public int? MaxPlayers { get; init; }
	public Guid? MapId { get; init; }
	public Guid? RulesetId { get; init; }
	public Guid? GameModeId { get; init; }
	public List<Guid>? WhitelistRankIds { get; init; }
	public string? OverrideConfig { get; init; }
}
