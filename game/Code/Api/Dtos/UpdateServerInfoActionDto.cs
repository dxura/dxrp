namespace Dxura.RP.Shared;

public class UpdateServerInfoActionDto : BaseServerActionDto
{
	public required string Name { get; init; }
	public required string Description { get; init; }

	public int MaxPlayers { get; init; }
	public Guid? RulesetId { get; init; }
	public Guid? GameModeId { get; init; }
	public IReadOnlyCollection<Guid> WhitelistRankIds { get; init; } = [];
	public string? OverrideConfig { get; init; }
}
