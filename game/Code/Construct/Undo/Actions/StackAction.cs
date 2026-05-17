namespace Dxura.RP.Game;

/// <summary>
///     Represents an action related to stacking that can be undone.
/// </summary>
public class StackAction( List<IConstruct> constructs ) : IUndoable
{
	public Guid Id => Guid.NewGuid();
	public string UndoMessage => "Undone Stack";

	/// <summary>
	///     Undoes the stack
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
