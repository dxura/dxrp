namespace Dxura.RP.Shared;

public class BackupRestoredActionDto : BaseServerActionDto
{
	public required List<BackupRestoredPlayerDto> Players { get; set; }
}

public class BackupRestoredPlayerDto
{
	public required long PlayerId { get; set; }
	public required uint Balance { get; set; }
	public int? Level { get; set; }
}
