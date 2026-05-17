namespace Dxura.RP.Game;

public partial class Npc
{
	[Property]
	public required Collider Collider { get; set; }

	[Property]
	public required SkinnedModelRenderer Body { get; set; }

	[Property]
	public required ModelPhysics BodyPhysics { get; set; }

	[Property]
	public required Dresser Dresser { get; set; }

	[Property]
	[Title( "Random Clothing" )]
	public bool RandomClothing { get; set; } = true;


	private void OnStartBody()
	{
		if ( RandomClothing )
		{
			Dresser?.Randomize();
		}

		Body.SetBodyGroup( "Head", 1 );
		Body.MaterialGroup = "skin_light";
	}
}
