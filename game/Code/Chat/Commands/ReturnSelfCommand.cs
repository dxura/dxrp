using Dxura.RP.Shared;
namespace Dxura.RP.Game.Commands;

public class ReturnSelfCommand : ICommand
{
	public string Command => "returnself";
	public string Help => "/returnself - Return to your previous position after teleporting";
	public Permission[] RequiredPermissions => [Permission.PlayerTeleport];

	private static readonly ReturnCommand ReturnCommand = new();

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		return ReturnCommand.ExecuteHost( caller, [caller.SteamId.ToString()], $"/{ReturnCommand.Command} {caller.SteamId}" );
	}
}
