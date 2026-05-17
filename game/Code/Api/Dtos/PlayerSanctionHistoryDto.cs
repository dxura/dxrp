namespace Dxura.RP.Shared;

public class PlayerSanctionHistoryDto
{
	public DateTimeOffset Created { get; set; }
	public SanctionType Type { get; set; }
	public bool IsGlobal { get; set; }
	public SanctionState State { get; set; }
	public SanctionFlags Flags { get; set; }
	public TimeSpan? Duration { get; set; }
	public string Reason { get; set; } = null!;
	public string? Notes { get; set; }

#if ASPNETCORE
	public static PlayerSanctionHistoryDto FromEntity( Domain.Entities.Sanction entity ) => new()
	{
		Created = entity.Created,
		Type = entity.Type,
		IsGlobal = entity.IsGlobal,
		State = entity.State,
		Flags = entity.Flags,
		Duration = entity.Duration,
		Reason = entity.Reason,
		Notes = entity.Notes
	};
#endif
}
