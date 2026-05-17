namespace Dxura.RP.Game.Minigame.Minigames;

[AssetType( Name = "Minigame", Extension = "minigame", Category = "DXRP" )]
public class MinigameResource : GameResource
{
	public static HashSet<MinigameResource> All { get; set; } = new();

	public string Identifier { get; set; } = "";

	public required GameObject? MainPrefab { get; set; }
	public List<GameObject>? SecondaryPrefabs { get; set; }

	public string Name { get; set; } = "";

	public string Description { get; set; } = "";

	[ImageAssetPath]
	public required string Icon { get; set; }

	public Color? Color { get; set; }

	[Category( "Lobby" )]
	public Vector3 LobbyOffset { get; set; } = new( 0, 0, 500 );

	[Category( "Players" )]
	public int MaxPlayers { get; set; } = 16;

	[Category( "Players" )]
	public int MinPlayers { get; set; } = 2;

	[Category( "Selection" )]
	public bool IsSelectable { get; set; } = true;

	[Category( "Selection" )]
	public int Weight { get; set; } = 50;

	[Category( "Gameplay" )]
	[Description( "Duration of the minigame in seconds" )]
	public int Duration { get; set; } = 120;

	[Category( "Gameplay" )]
	[Description( "Duration of the lobby in seconds" )]
	public int LobbyDuration { get; set; } = 15;

	[Category( "Equipment" )]
	[Description( "Duration of the lobby in seconds" )]
	public List<string> StartingEquipmentIdentifiers { get; set; } = new();

	[Category( "Equipment" )]
	[Description( "Give Max Ammo" )]
	public bool GiveMaxAmmo { get; set; } = true;

	[Category( "Spawning" )]
	[Description( "Spawn Ruleset" )]
	public MinigameSpawnRuleset SpawnRuleset { get; set; } = MinigameSpawnRuleset.OneLife;

	[Category( "Spawning" )]
	[Description( "Respawn Duration in seconds (when applicable)" )]
	public int RespawnDuration { get; set; } = 5;

	[Category( "Spawning" )]
	[Description( "Spawn Method" )]
	public MinigameSpawnMethod SpawnMethod { get; set; } = MinigameSpawnMethod.RoundRobin;

	[Category( "Spawning" )]
	[Description( "Spectate on death" )]
	public bool MakeDeadPlayersSpectators { get; set; } = true;

	[Category( "Win" )]
	[Description( "Duration of the minigame in seconds" )]
	public MinigameWinCondition WinCondition { get; set; } = MinigameWinCondition.LastManStanding;

	protected override void PostLoad()
	{
		All.Add( this );
	}
}

public enum MinigameWinCondition
{
	None,
	LastManStanding,
	MostKills
}

public enum MinigameSpawnMethod
{
	Random,
	RoundRobin
}

public enum MinigameSpawnRuleset
{
	OneLife,
	Respawn
}
