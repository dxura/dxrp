namespace Dxura.RP.Game;

public interface IUndoable
{
	public Guid Id { get; }

	public void Undo();

	public string? UndoMessage => null;
}
