namespace Dxura.RP.Game;

/// <summary>
///     Represents an undo construct  that can be undone.
/// </summary>
public class ConstructAction( IConstruct construct ) : IUndoable
{
	public Guid Id => construct.Id;
	public string? UndoMessage => $"Undone {construct.Type}";

	/// <summary>
	///     Undoes the construct creation by destroying it
	/// </summary>
	public void Undo()
	{
		if ( !construct.IsValid() )
		{
			return;
		}

		construct.Destroy();
	}
}
