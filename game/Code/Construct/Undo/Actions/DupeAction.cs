namespace Dxura.RP.Game;

/// <summary>
///     Represents an action related to dupes that can be undone.
/// </summary>
public class DupeAction( List<IConstruct> constructs ) : IUndoable
{
	public Guid Id => Guid.NewGuid();
	public string UndoMessage => "Undone Dupe";

	/// <summary>
	///     Undoes the dupe
	/// </summary>
	public void Undo()
	{
		foreach ( var construct in constructs )
		{

			if ( !construct.IsValid() )
			{
				continue;
			}

			construct.Destroy();
		}
	}
}
