namespace Dxura.RP.Shared;

/// <summary>
/// Modifiers that can be applied to sanctions like jail and warnings
/// </summary>
[Flags]
public enum SanctionModifier
{
	None = 0,
	ClearConstructs = 1 << 0,
	Kill = 1 << 1,
	Kick = 1 << 2
}
