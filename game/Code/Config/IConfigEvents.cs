namespace Dxura.RP.Game;

public interface IConfigEvents : ISceneEvent<IConfigEvents>
{
	/// <summary>
	///     Called on the host after config overrides have been applied and the config is ready.
	/// </summary>
	void OnConfigAppliedHost() {}

	/// <summary>
	///     Called for everyone when the config has been overridden (from default)
	/// </summary>
	void OnConfigOverride() {}
}
