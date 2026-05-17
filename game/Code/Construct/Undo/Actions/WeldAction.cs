namespace Dxura.RP.Game;

/// <summary>
///     Represents an action related to welds that can be undone.
/// </summary>
public class WeldAction( FixedJoint joint ) : IUndoable
{
	public Guid Id => Guid.NewGuid();
	public string UndoMessage => "Undone Weld";

	/// <summary>
	///     Undoes the weld by destroy joint
	/// </summary>
	public void Undo()
	{
		if ( joint.IsValid() )
		{
			joint.Destroy();
		}
	}
}
