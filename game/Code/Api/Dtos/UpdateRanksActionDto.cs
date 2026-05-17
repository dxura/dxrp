namespace Dxura.RP.Shared;

public class UpdateRanksActionDto : BaseServerActionDto
{
	public required List<RankDto> Ranks { get; set; }
}
