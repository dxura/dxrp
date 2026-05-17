namespace Dxura.RP.Shared;

[Flags]
public enum SanctionFlags
{
	None = 0,
	Appealed = 1 << 0,
	Privileged = 1 << 1,
	AppealAccepted = 1 << 2,
	AppealDenied = 1 << 3
}
