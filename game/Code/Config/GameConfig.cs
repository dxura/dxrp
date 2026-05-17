namespace Dxura.RP.Game;

/// <summary>
/// Base config of DarkRP game 2014 (standard roleplay mode)
/// </summary>
public abstract partial class GameConfig
{
	//
	// Game
	//
	public abstract string Identifier { get; }
	
	public virtual int RespawnTime { get; set; } = 30;

	public virtual bool IsLobbySupported { get; set; } = false;
	
	// Steam
	public virtual bool AllowFamilySharePlayers { get; set; } = false;

	// Branding
	public virtual string DashboardName { get; set; } = "DXRP";
	public virtual string DashboardDescription { get; set; } = "#tabmenu.dashboard.description";
	
	public virtual string DiscordUrl { get; set; } = "https://discord.gg/uBwQ2QHP2D";
	
	public virtual bool MotdEnabled { get; set; } = true;
	public virtual string? MotdUrlOverride { get; set; } = null;
	
	// Cloud
	public virtual string[] CloudPackages { get; set; } = [
		"facepunch.watermelon",
	];
	
	// Rules
	public virtual bool RulesEnabled { get; set; } = true;
	public virtual string? RulesUrlOverride { get; set; } = null;
	public virtual bool UseLocalizedRulesUrls { get; set; } = false;
	public virtual string DefaultRulesLanguage { get; set; } = "en";
	public virtual string[] SupportedRulesLanguages { get; set; } = ["en", "fr", "ru"];

	// Map
	public virtual bool MapFittingEnabled { get; set; } = true;

	// Building / Props
	public virtual bool BuildingEnabled { get; set; } = true;
	public virtual bool NoClip { get; set; } = false;
	public virtual bool PreventPropExploits { get; set; } = true;

	public virtual int? MaxPropSize { get; set; } = 800000;
	public virtual string? RestrictCloudOrg { get; set; } = "facepunch";

	// Text moderation
	public virtual bool ModerateText { get; set; } = true;
	public virtual bool TextSteamFilter { get; set; } = true;
	public virtual string[] TextWordBlacklist { get; set; } =
	[
		"nigger",
		"nigga",
		"faggot",
		"fag"
	];

	public virtual string[] MaterialWhitelist => new[]
	{
		"facepunch.glass_a",
		"materials/glass_blur.vmat",
		"materials/glass.vmat",
		"facepunch.wired_glass_a",
		"facepunch.floor_steel_grid_a",
		"facepunch.wire_cage_b_painted",
		"materials/default/construction.vmat",
		"materials/default/white.vmat",
		"facepunch.metal_painted_blue_a_hs",
		"facepunch.metal_trim_01",
		"facepunch.office_ceiling_b",
		"facepunch.concrete_damaged",
		"facepunch.floor_steel_diamond_plate_a",
		"facepunch.wall_panel_grey_b",
		"facepunch.wall_brick_older",
		"facepunch.wall_brick_old",
		"facepunch.wall_brick_d",
		"facepunch.woodtrimpolishedblend04",
		"facepunch.woodenflooringblight",
		"facepunch.floor_wooden_deck_a",
		"facepunch.plywood_panels",
		"facepunch.wood_painted_black_b_hs",
		"facepunch.wood_varnished_a_hs",
		"facepunch.trash_a",
		"facepunch.soil_a",
		"facepunch.roof_tile_a",
		"facepunch.floor_slick_asphalt",
		"facepunch.floor_rubber",
		"facepunch.floor_rough_asphalt",
		"facepunch.metal_trim_clean",
		"facepunch.granite_simple",
		"facepunch.concrete_polished_02_blend",
		"facepunch.floor_grass_a",
		"facepunch.grass_mud_blend_01a",
		"facepunch.rubbertilesa",
		"facepunch.woodenflooringa",
		"facepunch.metal_01a",
		"facepunch.carpet_b",
		"facepunch.floor_tile_01",
		"facepunch.floor_tile_02",
		"facepunch.metal_board_01c",
		"facepunch.dirt_ground_02_blend",
		"facepunch.woodenceilingapainted",
		"facepunch.floor_wood_laminated",
		"facepunch.ceiling_plaster_01",
		"facepunch.wall_cinderblock_a",
		"facepunch.barriertapea",
		"facepunch.barriermesh",
		"facepunch.rust_trim",
		"facepunch.floor_steel_dia_plate_rusty_a",
		"facepunch.woodenflooringbblend",
		"facepunch.wired_glass_b",
		"facepunch.glass_dotted_a"
	};
}

