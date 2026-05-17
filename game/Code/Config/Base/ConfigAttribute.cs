namespace Dxura.RP.Game.Meta;

[AttributeUsage( AttributeTargets.Property )]
public class ConfigAttribute( string label, string description, string section = "miscellaneous" ) : Attribute
{
	// Basic Info
	public string Label { get; set; } = label;
	public string? Description { get; set; }
	public string Section { get; set; } = section; // e.g., "Economy", "Legal"
	public string? Group { get; set; } // e.g., "Printer" within "Entities"

	// UI Logic
	public ConfigType ControlType { get; set; } = ConfigType.Unknown;
	public string? Placeholder { get; set; }

	// Validation
	public double Min { get; set; } = double.MinValue;
	public double Max { get; set; } = double.MaxValue;
	public int Step { get; set; } = 1;

	// Select Options (for Enums or fixed lists)
	public string[]? Options { get; set; }

}
