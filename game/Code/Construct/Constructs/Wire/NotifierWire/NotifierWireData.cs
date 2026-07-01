namespace Dxura.RP.Game.Wire;

public record NotifierWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public string Message { get; set; } = "Value Changed";
	public bool IncludeValue { get; set; } = false;
	public bool IgnoreFalsyValue { get; set; } = true;
	public string Label { get; set; } = string.Empty;
	public NotifierWireChime Chime { get; set; } = NotifierWireChime.None;
}
