using Dxura.RP.Shared;
using System.Threading.Tasks;
namespace Dxura.RP.Game;

public partial class ServerApiLink
{
	private readonly List<IActionHandler> _actionHandlers = new();

	private void RegisterActionHandlers()
	{
		_actionHandlers.Clear();

		var handlerTypes = TypeLibrary.GetTypes<IActionHandler>();
		foreach ( var type in handlerTypes.Where( t => !t.IsAbstract && t.TargetType != null ) )
		{
			if ( TypeLibrary.Create<IActionHandler>( type.TargetType ) is {} instance )
			{
				_actionHandlers.Add( instance );
			}
		}
	}

	private async Task HandleServerActions( IEnumerable<ServerActionDto> pendingActions )
	{
		foreach ( var pendingAction in pendingActions )
		{
			await GameTask.WorkerThread();
			// Acknowledge action first to avoid re-processing if something fails
			await ServerApiClient.AcknowledgeServerAction( pendingAction.Id );
			await GameTask.MainThread();
			
			var handler = _actionHandlers.FirstOrDefault( h => h.CanHandle( pendingAction.Payload ) );
			handler?.Execute( pendingAction.Payload );
		}
	}

	public void ExecuteAction( BaseServerActionDto action )
	{
		var handler = _actionHandlers.FirstOrDefault( h => h.CanHandle( action ) );
		handler?.Execute( action );
	}
}
