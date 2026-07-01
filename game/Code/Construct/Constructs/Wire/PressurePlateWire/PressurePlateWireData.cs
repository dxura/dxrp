namespace Dxura.RP.Game.Wire;

public record PressurePlateWireData : IConstructData, IWireLabelData
{
	public uint SchemaVersion => 1;
	public int Width { get; set; } = PressurePlateWireDefinition.DefaultWidth;
	public int Length { get; set; } = PressurePlateWireDefinition.DefaultLength;
	public int Depth { get; set; } = PressurePlateWireDefinition.DefaultDepth;
	public TriggerFilterType FilterType { get; set; } = TriggerFilterType.Everything;
	public string Label { get; set; } = string.Empty;
}
