namespace Dxura.RP.Game;

public record ChatEntry( Guid MessageId,
	long SteamId,
	string Author,
	string Message,
	RealTimeSince TimeSinceAdded,
	MessageType Type,
	Color Color,
	string? Role,
	Color? RoleColor );
