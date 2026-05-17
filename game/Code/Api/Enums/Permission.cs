#if ASPNETCORE
using System.Reflection;
#endif

namespace Dxura.RP.Shared;

// WARNING: ADD TO BACKEND NOT GAME, THIS GETS MIRRORED.
public enum Permission
{
	// Portal / Web
	[PermissionMeta( "portal.access", "Portal Access", "Access the web portal", "Portal" )]
	PortalAccess,

	[PermissionMeta( "portal.server.view", "View Servers", "View server list and details", "Portal" )]
	ViewServer,

	[PermissionMeta( "portal.server.edit", "Edit Servers", "Manage server settings", "Portal" )]
	EditServer,

	[PermissionMeta( "portal.rank.view", "View Ranks", "View rank list and details", "Portal" )]
	ViewRanks,

	[PermissionMeta( "portal.rank.edit", "Edit Ranks", "Create and edit ranks", "Portal" )]
	EditRank,

	[PermissionMeta( "portal.map.view", "View Maps", "View map rotation", "Portal" )]
	ViewMap,

	[PermissionMeta( "portal.map.edit", "Edit Maps", "Manage map rotation", "Portal" )]
	EditMap,

	[PermissionMeta( "portal.ruleset.view", "View Rulesets", "View server rulesets", "Portal" )]
	ViewRuleset,

	[PermissionMeta( "portal.ruleset.edit", "Edit Rulesets", "Manage server rulesets", "Portal" )]
	EditRuleset,

	[PermissionMeta( "portal.faction.view", "View Factions", "View faction list and details", "Portal" )]
	ViewFaction,

	[PermissionMeta( "portal.sanction.view", "View Sanctions", "View sanctions", "Portal" )]
	ViewSanction,

	[PermissionMeta( "portal.sanction.issue", "Issue Sanctions", "Issue sanctions", "Portal" )]
	IssueSanction,

	[PermissionMeta( "portal.sanction.pardon", "Pardon Sanctions", "Pardon or expire sanctions", "Portal" )]
	PardonSanction,

	[PermissionMeta( "portal.audit.view", "View Audit", "View audit events", "Portal" )]
	ViewAudit,

	[PermissionMeta( "portal.network.edit", "Edit Network Settings", "Manage tenant network settings", "Portal" )]
	EditNetworkSettings,

	[PermissionMeta( "portal.announcement.edit", "Edit Announcement", "Manage announcement", "Portal" )]
	EditAnnouncement,

	[PermissionMeta( "portal.items.view", "View Items", "View items and inventory", "Portal" )]
	ViewItems,

	[PermissionMeta( "portal.items.manage", "Manage Items", "Create, edit, and delete items", "Portal" )]
	ManageItems,

	[PermissionMeta( "portal.addon.view", "View Addons", "View addon list and details", "Portal" )]
	ViewAddon,

	[PermissionMeta( "portal.addon.edit", "Edit Addons", "Manage addons and revisions", "Portal" )]
	EditAddon,

	[PermissionMeta( "portal.gamemode.view", "View Game Modes", "View game mode list and details", "Portal" )]
	ViewGameMode,

	[PermissionMeta( "portal.gamemode.edit", "Edit Game Modes", "Manage game modes", "Portal" )]
	EditGameMode,

	[PermissionMeta( "portal.server.snapshot", "Manage Snapshots", "Download, upload, and clear server snapshots", "Portal" )]
	ManageSnapshot,

	[PermissionMeta( "portal.players.view", "View Players", "View the player list", "Portal" )]
	ViewPlayers,

	[PermissionMeta( "portal.players.view.incognito", "View Incognito Players", "View incognito players in the in-game player list", "Portal" )]
	ViewIncognitoPlayers,

	[PermissionMeta( "portal.player.view", "View Player", "View a player details", "Portal" )]
	ViewPlayer,

	[PermissionMeta( "portal.player.view.alt", "View Player Alts", "View a player alts", "Portal" )]
	ViewPlayerAlts,

	[PermissionMeta( "portal.player.notes.manage", "Manage Player Notes", "Create and edit internal staff notes on players", "Portal" )]
	ManagePlayerNotes,

	[PermissionMeta( "portal.rank.assign", "Assign Ranks", "Assign ranks to players", "Portal" )]
	AssignRanks,

	[PermissionMeta( "portal.store.manage", "Manage Store", "Read, write, and delete entries in the data store", "Portal" )]
	ManageStore,

	[PermissionMeta( "portal.backups.manage", "Manage Backups", "View/Restore/Save the network's backups", "Portal" )]
	ManageBackups,

	[PermissionMeta( "portal.apikeys.manage", "Manage API Keys", "Create, list, and revoke API keys", "Portal" )]
	ManageApiKeys,

	// Moderation
	[PermissionMeta( "player.kick", "Kick Player", "Kick players from the server", "Moderation" )]
	PlayerKick,

	[PermissionMeta( "player.ban", "Ban Player", "Ban players from the server", "Moderation" )]
	PlayerBan,

	[PermissionMeta( "player.jail", "Jail Player", "Jail players", "Moderation" )]
	PlayerJail,

	[PermissionMeta( "player.gag", "Gag Player", "Prevent a player from chatting and using voice", "Moderation" )]
	PlayerGag,

	[PermissionMeta( "player.warn", "Warn Player", "Warn players", "Moderation" )]
	PlayerWarn,

	[PermissionMeta( "player.sanctions.view.self", "View Own Sanctions", "View your own in-game sanction history", "Moderation" )]
	ViewOwnSanctions,

	[PermissionMeta( "player.sanctions.view.other", "View Other Sanctions", "View other players' in-game sanction history", "Moderation" )]
	ViewOtherSanctions,

	[PermissionMeta( "player.sanctions.view.notes", "View Sanction Notes", "View sanction notes in the in-game sanction history", "Moderation" )]
	ViewSanctionNotes,

	[PermissionMeta( "player.screenshot", "Force Screenshot", "Force a player screenshot", "Moderation" )]
	ForceScreenshot,

	[PermissionMeta( "player.spectate", "Spectate", "Spectate another player's perspective", "Moderation" )]
	PlayerSpectate,

	[PermissionMeta( "player.tickets.handle", "Handle Tickets", "Able to claim/resolve in-game tickets (with given permissions)", "Moderation" )]
	HandleTickets,

	// Server Management
	[PermissionMeta( "server.restart", "Server Restart", "Restart the server", "Server Management" )]
	ServerRestart,

	[PermissionMeta( "server.broadcast", "Server Broadcast", "Broadcast messages to all players", "Server Management" )]
	ServerBroadcast,

	[PermissionMeta( "economy.manage", "Manage Economy", "Manage the economy", "Server Management" )]
	ManageEconomy,

	[PermissionMeta( "level.manage", "Manage Level", "Set player levels via the portal", "Server Management" )]
	ManageLevel,

	[PermissionMeta( "recovery.manage", "Manage Recovery", "Manage recovery system", "Server Management" )]
	ManageRecovery,

	// Commands
	[PermissionMeta( "command.god", "God Mode", "Become invincible", "Commands" )]
	CommandGodMode,

	[PermissionMeta( "command.clearprops", "Clear Props", "Clear all props on the server", "Commands" )]
	CommandClearProps,

	[PermissionMeta( "command.clearallprops", "Clear All Props", "Clear all props owned by all players", "Commands" )]
	CommandClearAllProps,

	[PermissionMeta( "command.clearentities", "Clear Entities", "Clear all entities owned by a player", "Commands" )]
	CommandClearEntities,

	[PermissionMeta( "command.clearallentities", "Clear All Entities", "Clear all entities owned by all players", "Commands" )]
	CommandClearAllEntities,

	[PermissionMeta( "command.incognito", "Incognito", "Hide from player list", "Commands" )]
	CommandIncognito,

	[PermissionMeta( "command.fakedisconnect", "Fake Disconnect", "Broadcast a decoy disconnect message for yourself", "Commands" )]
	CommandFakeDisconnect,

	[PermissionMeta( "command.sethealth", "Set Health", "Set a health of a player (and/or revive)", "Commands" )]
	CommandSetHealth,

	[PermissionMeta( "command.cloak", "Cloak", "Become invisible", "Commands" )]
	CommandCloak,

	[PermissionMeta( "command.votebet.participate", "Vote Bets Participate", "Participate with vote bets via command", "Commands" )]
	CommandVoteBetParticipate,

	[PermissionMeta( "command.votebet.manage", "Vote Bets Manage", "Create/End vote bets via command", "Commands" )]
	CommandVoteBetManage,

	[PermissionMeta( "command.minigame.participate", "Minigame Participate", "Participate with minigames via command", "Commands" )]
	CommandMinigameParticipate,

	[PermissionMeta( "command.minigame.manage", "Minigame Manage", "Start/Skip/End minigames", "Commands" )]
	CommandMinigameManage,

	[PermissionMeta( "command.rpname", "RP Name", "Set a custom RP name", "Commands" )]
	CommandRpName,

	[PermissionMeta( "command.forcerpname", "Force RP Name", "Force set a player's RP name", "Commands" )]
	CommandForceRpName,

	[PermissionMeta( "command.freeze", "Freeze Player", "Freeze a player in place", "Commands" )]
	CommandFreeze,

	[PermissionMeta( "command.arrest", "Arrest Player", "Arrest a player using the staff command", "Commands" )]
	CommandArrest,

	[PermissionMeta( "command.unarrest", "Unarrest Player", "Release a player from arrest using the staff command", "Commands" )]
	CommandUnarrest,

	[PermissionMeta( "command.forceselldoor", "Force Sell Door", "Force sell the door the player is looking at", "Commands" )]
	CommandForceSellDoor,

	[PermissionMeta( "command.job.manage", "Manage Jobs", "Force set jobs for other players", "Commands" )]
	CommandJobManage,

	[PermissionMeta( "command.canceldemote", "Cancel Demote", "Cancel active demote votes against a player", "Commands" )]
	CommandCancelDemote,

	[PermissionMeta( "command.dropitem", "Drop Inventory Item", "Drop inventory items into the world via command", "Commands" )]
	CommandDropItem,

	[PermissionMeta( "command.spawnitem", "Spawn Inventory Item", "Spawn inventory items into the world via command", "Commands" )]
	CommandSpawnItem,

	[PermissionMeta( "command.title", "Use Title", "Equip inventory titles", "Commands" )]
	CommandTitle,

	[PermissionMeta( "command.waypoint.use", "Use Waypoints", "Teleport to saved waypoints", "Commands" )]
	CommandWaypointUse,

	[PermissionMeta( "command.waypoint.edit", "Edit Waypoints", "Create and clear saved waypoints", "Commands" )]
	CommandWaypointEdit,

	[PermissionMeta( "command.xray", "XRay", "View an overlay of a player's owned entities and their positions", "Commands" )]
	CommandXray,

	// Ability
	[PermissionMeta( "ability.teleport", "Teleport", "Teleport to, bring, and return players", "Ability" )]
	PlayerTeleport,

	[PermissionMeta( "ability.teleportall", "Teleport All", "Teleport all players to you", "Ability" )]
	PlayerTeleportAll,

	[PermissionMeta( "ability.noclip", "Noclip", "Allows flying through walls", "Ability" )]
	Noclip,

	[PermissionMeta( "ability.playergrab", "Player Grab", "Allows grabbing player with build tool", "Ability" )]
	PlayerGrab,

	[PermissionMeta( "ability.bypass.maxplayers", "Bypass Max Players", "Bypass the max players limit to join a full server", "Ability" )]
	BypassMaxPlayers,

	// Building / Constructs
	[PermissionMeta( "construct.unlimited", "Unlimited Building", "Bypass build limits", "Building" )]
	BuildUnlimited,

	[PermissionMeta( "construct.dupe.bypass", "Duplicate Bypass", "Bypass duplicator cooldown", "Building" )]
	DuplicateBypass,

	[PermissionMeta( "construct.remover.bypass", "Remover Tool Bypass", "Allows player to remove anyone's constructs/entities", "Building" )]
	RemoverBypass,

	[PermissionMeta( "construct.propsize.bypass", "Prop Size Bypass", "Bypass prop size limits", "Building" )]
	PropSizeBypass,

	[PermissionMeta( "construct.spawn.bypass", "Spawn Build Bypass", "Allows spawning and moving constructs inside spawn areas", "Building" )]
	SpawnBuildBypass,

	[PermissionMeta( "construct.permanent", "Permanent", "Make constructs and entities permanent and interact with them", "Building" )]
	Permanent,

	// Misc
	[PermissionMeta( "chat.staff", "Staff Chat", "Access the staff chat", "Misc" )]
	StaffChat,

	[PermissionMeta( "debug.access", "Debug Access", "Access debug tools", "Misc" )]
	DebugAccess,

	[PermissionMeta( "debug.full", "Debug Full", "Full debug access", "Misc" )]
	DebugFull,

	// Inventory
	[PermissionMeta( "inventory.view", "View Inventory", "View player inventories", "Server Management" )]
	ViewInventory,

	[PermissionMeta( "inventory.manage", "Manage Inventory", "Give and take inventory items", "Server Management" )]
	ManageInventory,

	[PermissionMeta( "inventory.manage.bulk", "Manage Inventory Bulk", "Run bulk inventory operations across multiple players", "Server Management" )]
	ManageInventoryBulk

}

//
// Grunt work below
//

[AttributeUsage( AttributeTargets.Field )]
public sealed class PermissionMetaAttribute( string id,
	string name,
	string description,
	string category )
	: Attribute
{
	public string Id { get; } = id;
	public string Name { get; } = name;
	public string Description { get; } = description;
	public string Category { get; } = category;
}

public static class PermissionExtensions
{
	private static readonly Lazy<Dictionary<Permission, PermissionMetaAttribute>> _metaCache =
		new( BuildCache );

	// Accessing .Value triggers the BuildCache function if it hasn't run yet
	private static Dictionary<Permission, PermissionMetaAttribute> MetaCache => _metaCache.Value;

	private static Dictionary<Permission, PermissionMetaAttribute> BuildCache()
	{
		var dict = new Dictionary<Permission, PermissionMetaAttribute>();

#if ASPNETCORE
		foreach ( var field in typeof( Permission ).GetFields( BindingFlags.Public | BindingFlags.Static ) )
		{
			if ( !field.IsLiteral ) continue;
			var attr = field.GetCustomAttribute<PermissionMetaAttribute>();
			if ( attr == null ) continue;
			var value = (Permission)field.GetValue( null )!;
			dict[value] = attr;
		}
#else
		// Sandbox Reflection
		// We get the TypeDescription for the Permission enum
		var typeDesc = TypeLibrary.GetType( typeof( Permission ) );

		if ( typeDesc != null )
		{
			// In TypeLibrary, Enum values are treated as Members
			foreach ( var member in typeDesc.Members )
			{
				// Check if this member has our attribute
				var attr = member.Attributes.OfType<PermissionMetaAttribute>().FirstOrDefault();
				
				if ( attr != null )
				{
					// Parse the member name back to the Enum
					if ( Enum.TryParse<Permission>( member.Name, out var value ) )
					{
						dict[value] = attr;
					}
				}
			}
		}
#endif
		return dict;
	}

	public static string ToId( this Permission permission ) => (MetaCache.TryGetValue( permission, out var meta ) ? meta.Id : null)!;

	public static IReadOnlyList<(Permission Value, PermissionMetaAttribute Meta)> All =>
		MetaCache.Select( kvp => (kvp.Key, kvp.Value) ).ToList().AsReadOnly();
}
