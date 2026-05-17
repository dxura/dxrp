namespace Dxura.RP.Shared;

public class BroadcastMessageActionDto : BaseServerActionDto
{
	public required string Message { get; set; }

	/// <summary>
	/// Duration in seconds to display the message (if applicable)
	/// </summary>
	public int? DurationSeconds { get; set; }
}
