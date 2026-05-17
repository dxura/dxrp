using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public sealed class Undo : GameObjectSystem<Undo>
{
	private readonly Dictionary<long, PlayerUndoHistory> _playerHistories = new();

	public Undo( Scene scene ) : base( scene )
	{
		if ( Scene.IsEditor )
		{
			return;
		}

		Listen( Stage.FinishUpdate, 1, OnUpdateUndo, "OnUpdateUndo" );
	}

	private void OnUpdateUndo()
	{
		if ( !Input.Down( "Undo" ) )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "undo", Config.Current.Game.UndoCooldown ) )
		{
			return;
		}

		UndoLastAction();
	}

	public void AddUndo( long steamId, IUndoable action )
	{
		Assert.True( Networking.IsHost );

		if ( !_playerHistories.TryGetValue( steamId, out var history ) )
		{
			history = new PlayerUndoHistory();
			_playerHistories[steamId] = history;
		}

		history.Add( action );
	}

	public bool RemoveUndoById( long steamId, Guid undoId )
	{
		Assert.True( Networking.IsHost );

		if ( !_playerHistories.TryGetValue( steamId, out var history ) )
		{
			return false;
		}

		return history.RemoveById( undoId );
	}

	[Rpc.Host]
	private void UndoLastAction()
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:undo", Config.Current.Game.UndoCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !_playerHistories.TryGetValue( player.SteamId, out var history ) || history.Count == 0 )
		{
			player.Error( "#notify.undo.none" );
			return;
		}

		var lastAction = history.RemoveLast();
		if ( lastAction == null )
		{
			player.Error( "#notify.undo.failed" );
			return;
		}

		try
		{
			lastAction.Undo();

			if ( !string.IsNullOrEmpty( lastAction.UndoMessage ) )
			{
				player.Info( lastAction.UndoMessage );
			}

			// Player callback
			using ( Rpc.FilterInclude( x => x == player.Connection ) )
			{
				OnUndoActionSuccess();
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"Failed to undo action {lastAction.Id}: {ex.Message}" );
			player.Error( "#generic.error" );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnUndoActionSuccess()
	{
		Sound.Play( "pop" );
	}

	// Helper class to maintain O(1) operations
	private sealed class PlayerUndoHistory
	{
		private readonly LinkedList<IUndoable> _actions = new();
		private readonly Dictionary<Guid, LinkedListNode<IUndoable>> _nodes = new();

		public int Count => _actions.Count;

		public void Add( IUndoable action )
		{
			// Remove old reference if it exists (replace action)
			if ( _nodes.TryGetValue( action.Id, out var oldNode ) )
			{
				_actions.Remove( oldNode );
			}
			var node = _actions.AddLast( action );
			_nodes[action.Id] = node;
		}

		public bool RemoveById( Guid undoId )
		{
			if ( _nodes.TryGetValue( undoId, out var node ) )
			{
				_actions.Remove( node );
				_nodes.Remove( undoId );
				return true;
			}
			return false;
		}

		public IUndoable? RemoveLast()
		{
			if ( _actions.Last == null )
			{
				return null;
			}
			var last = _actions.Last.Value;
			_nodes.Remove( last.Id );
			_actions.RemoveLast();
			return last;
		}
	}
}
