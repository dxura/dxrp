namespace Dxura.RP.Game.Wire;

public record NotifierWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public string Message { get; set; } = "Value Changed";
	public bool IncludeValue { get; set; } = false;
	public bool IgnoreFalsyValue { get; set; } = true;
}
