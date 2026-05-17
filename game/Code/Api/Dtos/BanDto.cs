#if ASPNETCORE
#endif

namespace Dxura.RP.Shared;

public class BanDto
{
	public required long PlayerId { get; set; }
	public string Reason { get; set; } = null!;
	public bool IsGlobal { get; set; }
	public DateTimeOffset Created { get; set; }
	public TimeSpan? Duration { get; set; }

#if ASPNETCORE
	public static BanDto FromEntity( Sanction entity ) => new()
	{
		PlayerId = entity.PlayerId,
		Reason = entity.Reason,
		IsGlobal = entity.IsGlobal,
		Created = entity.Created,
		Duration = entity.Duration
	};
#endif
}
