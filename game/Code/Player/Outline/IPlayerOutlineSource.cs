namespace Dxura.RP.Game;

/// <summary>
/// Full outline configuration returned by an <see cref="IPlayerOutlineSource"/>.
/// </summary>
public readonly struct PlayerOutlineRequest
{
	public float Width { get; init; }
	public Color Color { get; init; }
	public Color ObscuredColor { get; init; }
	public Color InsideColor { get; init; }
	public Color InsideObscuredColor { get; init; }

	/// <summary>
	/// When true the outline is restricted to the renderers returned by the player's
	/// <c>GetOutlineTargets()</c> (body + clothing only). When false the outline applies
	/// to all renderers on the GameObject (engine default).
	/// </summary>
	public bool OverrideTargets { get; init; }
}

/// <summary>
/// Implemented by any system that wants to draw a <see cref="HighlightOutline"/> on a player
/// for a specific local viewer (e.g. party membership, death ESP, job team).
/// Concrete types are discovered automatically via TypeLibrary — no registration needed.
/// The first source that returns a non-null request wins.
/// </summary>
public interface IPlayerOutlineSource
{
	/// <summary>
	/// Returns the outline configuration to apply to <paramref name="target"/> as seen by
	/// <paramref name="viewer"/>, or <c>null</c> if this source has no opinion for this pair.
	/// </summary>
	PlayerOutlineRequest? GetOutlineRequest( Player viewer, Player target );
}
