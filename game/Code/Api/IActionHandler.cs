using Dxura.RP.Shared;
namespace Dxura.RP.Game;

/// <summary>
/// Extensible server-action handler contract. Implement and it will be discovered via TypeLibrary.
/// </summary>
public interface IActionHandler
{
	bool CanHandle( BaseServerActionDto action );
	void Execute( BaseServerActionDto action );
}

public abstract class ActionHandler<T> : IActionHandler where T : BaseServerActionDto
{
	public bool CanHandle( BaseServerActionDto action ) => action is T;

	public void Execute( BaseServerActionDto action )
	{
		if ( action is T typed )
		{
			Execute( typed );
		}
	}

	protected abstract void Execute( T action );
}
