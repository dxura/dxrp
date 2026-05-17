using Sandbox.Rendering;
namespace Dxura.RP.Game.PostProcess;

[Title( "Drunk" )]
[Category( "Post Processing" )]
public sealed class DrunkPostProcess : BasePostProcess<DrunkPostProcess>
{
	[Range( 0, 1 ), Property] public float Intensity { get; set; } = 1.0f;

	private static readonly Material Shader = Material.FromShader( "shaders/drunk.shader" );

	public override void Render()
	{
		var size = GetWeighted( x => x.Intensity );
		if ( size <= 0f ) return;

		Attributes.Set( "intensity", size );

		var blit = BlitMode.WithBackbuffer( Shader, Stage.BeforePostProcess, 4000, true );
		Blit( blit, "Drunk" );
	}
}
