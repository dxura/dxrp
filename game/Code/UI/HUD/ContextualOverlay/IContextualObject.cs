namespace Dxura.RP.Game.UI;

public interface IContextualObject
{
	/// <summary>
	///     Raw access to the context object's <see cref="GameObject" />
	/// </summary>
	GameObject GameObject { get; }

	/// <summary>
	///     Where is this context?
	/// </summary>
	Vector3 ContextPosition { get; }

	/// <summary>
	///     Are we overriding the type here?
	/// </summary>
	Type? ContextPanelTypeOverride => null;

	/// <summary>
	///     What styles should we apply to the context?
	/// </summary>
	string? ContextStyles => null;

	/// <summary>
	///     What text should we show?
	/// </summary>
	string? DisplayText => null;

	/// <summary>
	///     How far can we see this context?
	/// </summary>
	float ContextMaxDistance => 0;

	/// <summary>
	///     How big's the context?
	/// </summary>
	int IconSize => 32;

	/// <summary>
	///     Should we show a chevron when we're off-screen?
	/// </summary>
	bool ShowChevron => true;

	/// <summary>
	///     Input hint?
	/// </summary>
	string? InputHint => null;

	/// <summary>
	///     Should we dim the context when looking at it?
	/// </summary>
	bool LookOpacity => true;

	/// <summary>
	///     Should we even show this context?
	/// </summary>
	/// <returns></returns>
	bool ShouldShow()
	{
		return true;
	}
}
