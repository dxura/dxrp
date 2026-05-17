namespace Dxura.RP.Game.Wire;

public interface IWireComponent
{
	public string Name { get; }

	public GameObject GameObject { get; }

	IEnumerable<WirePort> GetInputPorts();
	IEnumerable<WirePort> GetOutputPorts();
	void OnWireInput( string inputId, WireValue value );
	Vector3 GetPortPosition();

	/// <summary>
	/// Called when a wire connection to this component's input is removed.
	/// Default implementation does nothing - override to handle disconnections.
	/// </summary>
	void OnWireInputDisconnected( string inputId ) {}
}
