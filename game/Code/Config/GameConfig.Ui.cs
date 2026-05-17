namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// UI
	//

	public virtual int DefaultTextEntryMaxLength { get; set; } = 96;

	// TabMenu
	public virtual bool ShowDashboardMenu { get; set; } = true;
	public virtual bool ShowMarketMenu { get; set; } = true;
	public virtual bool ShowPlayerInfoMenu { get; set; } = true;

	// TabMenu - RP
	public virtual bool ShowJobsMenu { get; set; } = true;
	public virtual bool ShowPlayersRpColumnsMenu { get; set; } = true;

	// Hud
	public virtual bool ShowJobPlayerInfoHud { get; set; } = true;
	public virtual bool ShowTimeDisplayHud { get; set; } = true;

	// Titles
	public virtual bool ShowTitleOnNameplate { get; set; } = true;
	public virtual bool ShowTitleOnHud { get; set; } = true;
	public virtual bool ShowTitleOnPlayerList { get; set; } = true;

	// Camera
	public virtual float MaxFov { get; set; } = 130f;
	public virtual float MinFov { get; set; } = 90f;

	// Command Wheel — 8 entries ordered by wheel position (top, top-right, right, bottom-right, bottom, bottom-left, left, top-left)
	public virtual CommandWheelDefault[] CommandWheelDefaults { get; set; } =
	[
		new( "/dance",  "music_note",  "#commandwheel.slot.dance"      ),  // top
		new(),                                                                    // top-right
		new( "/panic",        "warning",    "#commandwheel.slot.panic"      ),  // right
		new(),                                                                    // bottom-right
		new( "/surrender",    "pan_tool",    "#commandwheel.slot.surrender"  ),  // bottom
		new(),                                                                    // bottom-left
		new( "/wave",   "waving_hand", "#commandwheel.slot.wave"       ),  // left
		new(),                                                                    // top-left
	];
}

public record CommandWheelDefault( string Command = "", string Icon = "", string Label = "" );

