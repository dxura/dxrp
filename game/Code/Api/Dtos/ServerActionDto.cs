namespace Dxura.RP.Shared;

/// <summary>
/// Represents a server action that needs to be processed.
/// The Payload contains a polymorphic action DTO (RestartServerAction, etc.)
/// </summary>
public class ServerActionDto
{
	public required Guid Id { get; init; }
	public required BaseServerActionDto Payload { get; init; }
	public required int Priority { get; init; }
	public required DateTimeOffset Created { get; init; }
}
