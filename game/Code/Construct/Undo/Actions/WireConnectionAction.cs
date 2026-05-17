using Dxura.RP.Game.Wire;

namespace Dxura.RP.Game;

/// <summary>
///     Represents a wire connection that can be undone.
/// </summary>
public class WireConnectionAction( WireConnection connection ) : IUndoable
{
	public Guid Id { get; } = Guid.NewGuid();
	public string? UndoMessage => "Undone wire connection";

	/// <summary>
	///     Undoes the wire connection by disconnecting it
	/// </summary>
	public void Undo()
	{
		if ( !connection.Source.GameObject.IsValid() || !connection.Target.GameObject.IsValid() )
		{
			return;
		}

		var wireSystem = Wire.Wire.Current;
		if ( wireSystem == null )
		{
			return;
		}

		// Find the connection and disconnect it
		var existingConnection = wireSystem.GetConnections().FirstOrDefault( c =>
			c.Source == connection.Source &&
			c.OutputId == connection.OutputId &&
			c.Target == connection.Target &&
			c.InputId == connection.InputId );

		if ( existingConnection.Source != null && existingConnection.Source.GameObject.IsValid() )
		{
			wireSystem.Disconnect( existingConnection );
		}
	}
}
