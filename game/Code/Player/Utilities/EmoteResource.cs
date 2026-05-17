namespace Dxura.RP.Game;

[GameResource( "Emote", "emote", "An emote animation that can be played by a player.", Icon = "emoji_people" )]
public class EmoteResource : GameResource
{
	public static HashSet<EmoteResource> All { get; set; } = new();
	
	[Property] [Category( "Animation" )]
	public string SequenceName { get; set; } = "";

	[Property] [Category( "Animation" )]
	public bool Repeat { get; set; } = false;

	[Property] [Category( "Animation" )]
	public float Duration { get; set; } = 3f;

	[Property] [Category( "Behaviour" )]
	public bool CancelOnMove { get; set; } = true;

	protected override void PostLoad()
	{
		All.Add( this );
	}

	protected override void PostReload()
	{
		All.Add( this );
	}
}
