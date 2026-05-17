using Dxura.RP.Shared;

namespace Dxura.RP.Game;

/// <summary>
/// Extensible chat/game command contract. Implement and it will be discovered via TypeLibrary.
/// </summary>
public interface ICommand
{
	/// <summary>
	/// Command used after '/'. Example: 'pm' for '/pm'.
	/// </summary>
	string Command { get; }

	/// <summary>
	/// Alternative command names that also trigger this command. Example: ['pm'] for '/msg' command.
	/// </summary>
	string[] Aliases => Array.Empty<string>();

	/// <summary>
	/// Short description for help UIs.
	/// </summary>
	string Help { get; }

	/// <summary>
	/// Whether this command can be used while dead. Defaults to true.
	/// </summary>
	bool IsUsableWhileDead => true;

	/// <summary>
	/// Whether this command can be used while prisoner. Defaults to false.
	/// </summary>
	bool IsUsableWhileRestricted => false;

	/// <summary>
	/// Whether this command can be used while frozen. Defaults to true.
	/// </summary>
	bool IsUsableWhileFrozen => true;

	/// <summary>
	/// Override the default command cooldown for this command (in seconds).
	/// Return null to use the default <see cref="GameConfig.CommandCooldown"/>.
	/// </summary>
	float? CooldownOverride => null;

	/// <summary>
	/// Required enum-based permissions for this command.
	/// </summary>
	Permission[] RequiredPermissions => Array.Empty<Permission>();

	/// <summary>
	/// Required string-based permission IDs for this command.
	/// </summary>
	string[] RequiredPermissionIds => Array.Empty<string>();

	/// <summary>
	/// Execute on host. Return true if consumed.
	/// </summary>
	bool ExecuteHost( Player caller, string[] args, string raw );

	/// <summary>
	/// Execute on the caller's client before sending to host. Return true to consume
	/// the command and skip host execution. Useful for commands that only drive local UI.
	/// Defaults to no local handling.
	/// </summary>
	bool ExecuteLocal( string[] args, string raw ) => false;

	/// <summary>
	/// Called every frame on the client. Opt in for commands that need ongoing rendering or state.
	/// </summary>
	void OnFrame() { }
}
