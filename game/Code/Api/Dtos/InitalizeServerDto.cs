namespace Dxura.RP.Shared;

public class InitalizeServerDto
{
	public required string Version { get; set; }

	public required string DefaultConfig { get; set; }
}

public class InitalizeServerResponseDto
{
	public Guid TenantId { get; set; }

	public Guid Id { get; set; }

	public required string Name { get; set; }
	public required int MaxPlayers { get; set; }

	public required string OverrideConfig { get; set; }

	public string? MapSboxIdentifier { get; set; }
	public string? MapPrefabJson { get; set; }

	public Guid? RulesetId { get; set; }
	public Guid? GameModeId { get; set; }
	public required IReadOnlyCollection<Guid> WhitelistRankIds { get; set; }
	public GameModeDto? GameMode { get; set; }

	public required IEnumerable<RankDto> Ranks { get; set; }
	public required IEnumerable<RankAssignmentDto> RankAssignments { get; set; }
}
