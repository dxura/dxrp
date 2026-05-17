using System.Threading.Tasks;

namespace Dxura.RP.Game;

/// <summary>
/// Preloads materials when players join the game
/// </summary>
public class MaterialPreloader : Component
{
	protected override void OnStart()
	{
		_ = PreloadMaterialsAsync();
	}
	private async Task PreloadMaterialsAsync()
	{

		try
		{
			foreach ( var materialPath in Config.Current.Game.MaterialWhitelist )
			{
				if ( string.IsNullOrEmpty( materialPath ) )
				{
					continue;
				}

				if ( !materialPath.EndsWith( ".vmat" ) )
				{
					if ( Config.Current.Game.RestrictCloudOrg != null &&
					     !materialPath.StartsWith( Config.Current.Game.RestrictCloudOrg ) )
					{
						continue;
					}

					var package = await Package.FetchAsync( materialPath, true, true );

					if ( package == null )
					{
						continue;
					}

					await package.MountAsync();

					if ( !package.IsMounted() )
					{
						return;
					}

					var primaryAsset = package.GetMeta( "PrimaryAsset", "" );

					if ( string.IsNullOrEmpty( primaryAsset ) )
					{
						continue;
					}

					await Material.LoadAsync( primaryAsset );
				}
				else
				{
					await Material.LoadAsync( materialPath );
				}
			}
		}
		catch ( Exception )
		{
			Log.Info( "Unable to preload materials" );
		}
	}
}
