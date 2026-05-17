namespace Dxura.RP.Game.Wire;

public record struct WireConnection( IWireComponent Source, string OutputId, IWireComponent Target, string InputId )
{
	public Guid Id { get; init; }
	public IWireComponent Source { get; init; } = Source;
	public string OutputId { get; init; } = OutputId;
	public IWireComponent Target { get; init; } = Target;
	public string InputId { get; init; } = InputId;

	// Customization
	public Color Color { get; set; } = Color.Red;
	public float Thickness { get; set; } = 1f;
	public float Opacity { get; set; } = 1f;
	public IEnumerable<Vector3>? Anchors { get; set; } = null;
}
