namespace Dxura.RP.Shared;

[Flags]
public enum FactionPermission
{
	None = 0,
	InviteMember = 1 << 0,
	KickMember = 1 << 1,
	ManageFaction = 1 << 2,
	SetRanks = 1 << 3,
	WithdrawMoney = 1 << 4
}
