namespace Dxura.RP.Game.Wire;

/// <summary>
/// Interface for wire components that need to handle wire system events
/// </summary>
public interface IWireEvents : ISceneEvent<IWireEvents>
{
	/// <summary>
	/// Called before wire propagation process
	/// </summary>
	void OnPreWirePropagate() {}

	/// <summary>
	/// Called after all wire propagation process
	/// </summary>
	void OnPostWirePropagate() {}

	/// <summary>
	/// Called periodically for heavy computations (every second or configurable interval)
	/// </summary>
	void OnWireTick() {}
}
