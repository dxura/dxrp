namespace Dxura.RP.Game;

public sealed partial class Chat
{

	private const int DisplayLimit = 80;

	private readonly LinkedList<ChatEntry> _entries = new();
	public LinkedList<ChatEntry> Entries => _entries;

	public Guid LatestMessageId { get; private set; } = Guid.Empty;

	private void AddEntry( ChatEntry entry )
	{
		_entries.AddFirst( entry );
		TrimToLimit();
		LatestMessageId = entry.MessageId;
	}

	private void TrimToLimit()
	{
		while ( _entries.Count > DisplayLimit )
		{
			_entries.RemoveLast();
		}
	}


}
