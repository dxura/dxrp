using Dxura.RP.Shared;
using System.Threading.Tasks;
namespace Dxura.RP.Game;

public partial class Prop
{
	private Material? _overrideMaterial;

	/// <summary>
	///     Initializes the cloud model for the prop by fetching and loading it from the cloud.
	/// </summary>
	private async Task SetModel( string modelPath )
	{
		if ( string.IsNullOrWhiteSpace( modelPath ) )
		{
			return;
		}

		// Cloud, mount and set correct path
		if ( !modelPath.EndsWith( ".vmdl" ) && !modelPath.EndsWith( ".vmdl_c" ) )
		{
			if ( Config.Current.Game.RestrictCloudOrg != null && !modelPath.StartsWith( Config.Current.Game.RestrictCloudOrg ) )
			{
				return;
			}

			var package = await Package.Fetch( modelPath, false );
			if ( package == null )
			{
				return;
			}

			if ( !package.IsMounted() )
			{
				await package.MountAsync();
			}

			modelPath = package.GetMeta( "PrimaryAsset", "" );
		}

		var model = await Model.LoadAsync( modelPath );

		// Ensure we're on the main thread for model assignment
		await GameTask.MainThread();

		// Check if the component is still valid after async operations
		if ( !this.IsValid() || !GameObject.IsValid() )
		{
			return;
		}

		// Size check
		if ( Networking.IsHost )
		{
			if ( Config.Current.Game.MaxPropSize.HasValue && !RankSystem.HasPermission( Owner, Permission.PropSizeBypass ) )
			{
				// Don't allow too big
				if ( model.Bounds.Volume > Config.Current.Game.MaxPropSize.Value )
				{
					GameObject.Destroy();

					var player = GameUtils.GetPlayerById( Owner );
					if ( player.IsValid() )
					{
						player.Error( "#notify.prop.large" );
					}

					return;
				}
			}
		}

		if ( !this.IsValid() || !model.IsValid() || !ModelRenderer.IsValid() )
		{
			return;
		}

		if ( ModelRenderer.IsValid() )
		{
			ModelRenderer.Model = model;
		}

		if ( ModelCollider.IsValid() )
		{
			var isMesh = model.Physics.Parts.Any( physicsPart => physicsPart.Meshes.Count > 0 );
			ModelCollider.Model = isMesh ? null : model;

			// Model doesn't have any colliders, let's add a simple box collider
			if ( isMesh || model.Physics.Parts.Count == 0 )
			{
				var boxCollider = AddComponent<BoxCollider>();
				boxCollider.Scale = model.Bounds.Size;
				boxCollider.Center = model.Bounds.Center;

				Collider = boxCollider;
			}
		}

		if ( !IsFrozen && NetworkOwner == Connection.Local.Id )
		{
			GetOrAddComponent<Rigidbody>();
		}
	}

	private async Task SetMaterial( string? material )
	{
		if ( material == null )
		{
			return;
		}

		if ( string.IsNullOrWhiteSpace( material ) || !Config.Current.Game.MaterialWhitelist.Contains( material ) )
		{
			ModelRenderer.ClearMaterialOverrides();
			ModelRenderer.SetMaterialOverride( null, "" );
			ModelRenderer.MaterialGroup = "default";
			return;
		}

		// Cloud (FP), mount and set correct path
		if ( !material.EndsWith( ".vmat" ) )
		{
			if ( Config.Current.Game.RestrictCloudOrg != null && !material.StartsWith( Config.Current.Game.RestrictCloudOrg ) )
			{
				return;
			}

			var package = await Package.FetchAsync( material, true, true );
			if ( package == null )
			{
				return;
			}

			if ( !package.IsMounted() )
			{
				await package.MountAsync();
			}

			material = package.GetMeta( "PrimaryAsset", "" );
		}

		var materialRef = await Material.LoadAsync( material );

		if ( materialRef.IsValid() )
		{
			_overrideMaterial = materialRef;
			ModelRenderer.SetMaterialOverride( _overrideMaterial, "" );
		}
	}

	private void SetTint( Color? color )
	{
		ModelRenderer.Tint = color ?? Color.White;
	}

	private void SetPhysics( float? friction, float? elasticity )
	{
		ModelCollider.Friction = friction;
		ModelCollider.Elasticity = elasticity;
	}

	private void SetScale( Vector3 scale )
	{
		GameObject.WorldScale = scale;
	}

	private void SetNoCollide( bool noCollide, bool change )
	{
		Tags.Set( Constants.NoCollideTag, noCollide );

		// Add this so people can't exploit no collide props
		if ( Networking.IsHost && !noCollide && change )
		{
			var collideGuard = GetOrAddComponent<CollideGuard>();
			collideGuard.ResetTimer();
		}
	}

}
