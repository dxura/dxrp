namespace Dxura.RP.Game.Entities;

public class WeedHarvestEntity : BaseEntity
{

	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnDriedChanged ) )]
	public bool Dried { get; set; }

	[Property]
	public required ModelRenderer ModelRenderer { get; set; }

	private void OnDriedChanged( bool oldValue, bool newValue )
	{
		ModelRenderer.SetBodyGroup( "state", newValue ? 1 : 0 );
	}

	public override string DisplayName => Language.GetPhrase( Dried ? "entity.weed.dried" : "entity.weed.wet" );
}
