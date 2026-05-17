namespace Dxura.RP.Shared;

public class InitalizePlayerDto
{
	public long Id { get; set; }
	public required string Name { get; set; }
}

public class InitalizePlayerResponseDto
{
	public uint Balance { get; set; }
	public bool PrivacyConsent { get; set; }
	public int Playtime { get; set; }
	public int Level { get; set; }
	public uint Streak { get; set; }
	public string? RpName { get; set; }
	public List<SanctionActionDto> ActiveSanctions { get; set; } = [];
}
