using Dxura.RP.Game.UI;
using Dxura.RP.Game.Utilities;
using System.Text.Json;

namespace Dxura.RP.Game.Tools;

public abstract class BaseConstructTool<TData>( ConstructType type ) : BaseTool
	where TData : IConstructData, new()
{
	public override string Attack1Control => string.Format( Language.GetPhrase( "tool.construct.attack1" ), type );
	public override string Attack2Control => string.Format( Language.GetPhrase( "tool.construct.attack2" ), type );

	protected TData Data { get; set; } = new();

	protected virtual bool FlatSurface { get; set; } = true;
	protected virtual Rotation FlatSurfaceRotationOffset => Rotation.Identity;

	private IConstruct? _preview;
	private string? _lastPreviewDataJson;

	public override void OnEquip()
	{
		base.OnEquip();
		_lastPreviewDataJson = null; // Reset data tracking
	}

	public override void OnToolFixedUpdate()
	{
		base.OnToolFixedUpdate();
		UpdatePreviewFixed();
	}

	public override void OnToolUpdate()
	{
		base.OnToolUpdate();
		UpdatePreviewUpdate();
	}

	public override void OnUnequip()
	{
		base.OnUnequip();

		ClearPreview();
	}

	public override void PrimaryUseStart()
	{
		var tr = PerformEyeTrace();

		if ( !IsValidTrace( tr ) )
		{
			Notify.Warn( "#notify.construct.invalid_surface" );
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "construct", Config.Current.Game.ConstructCooldown, true ) )
		{
			return;
		}

		var definition = Construct.Current.GetDefinition( type );

		if ( definition == null )
		{
			Notify.Error( "#notify.construct.invalid" );
			return;
		}

		// Update existing construct if possible (prefer root for nested hits)
		var targetGo = tr.GameObject.IsValid() ? tr.GameObject.Root : null;
		var existingConstruct = targetGo?.GetComponent<IConstruct>() ?? tr.GameObject.GetComponent<IConstruct>();
		if ( existingConstruct != null && GameUtils.HasPermission( Player.Local.SteamId, existingConstruct.GameObject ) && existingConstruct.Type == type )
		{
			var didUpdate = Construct.Current.UpdateConstructPlayer( existingConstruct.Type, Data, existingConstruct.GameObject );
			if ( didUpdate )
			{
				Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
			}

			return;
		}

		// Spawn new construct
		var toPlayer = (Player.Local.WorldPosition - tr.HitPosition).Normal;

		var positon = GameUtils.GetSpawnPosition( Player.Local.AimRay, FlatSurface ? 1 : 30 );
		var rotation = FlatSurface ? MathUtils.CalculateSurfaceFlatRotation( tr.Normal, toPlayer, FlatSurfaceRotationOffset ) : Rotation.Identity;

		var didSpawn = Construct.Current.SpawnConstructPlayer( type, Data, positon, rotation );

		if ( didSpawn )
		{
			Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
		}
	}

	public override void SecondaryUseStart()
	{
		var tr = PerformEyeTrace();

		if ( !IsValidTrace( tr ) )
		{
			Notify.Warn( "#notify.construct.invalid_target" );
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "construct:update", Config.Current.Game.ConstructUpdateCooldown, true ) )
		{
			return;
		}

		var definition = Construct.Current.GetDefinition( type );
		if ( definition == null )
		{
			Notify.Error( "#notify.construct.invalid" );
			return;
		}

		// Only copy from existing construct of same type (prefer root for nested hits)
		var targetGo = tr.GameObject.IsValid() ? tr.GameObject.Root : null;
		var existingConstruct = targetGo?.GetComponent<IConstruct>() ?? tr.GameObject.GetComponent<IConstruct>();

		if ( existingConstruct == null )
		{
			Notify.Warn( "#notify.construct.not_found" );
			return;
		}

		if ( existingConstruct.Type != type )
		{
			Notify.Warn( "#tool.construct.cannot_copy_type" );
			return;
		}

		if ( !GameUtils.HasPermission( Player.Local.SteamId, existingConstruct.GameObject ) )
		{
			Notify.Error( "#generic.permission" );
			return;
		}

		// Copy data from existing construct
		if ( existingConstruct.Type != type )
		{
			Notify.Error( "#tool.construct.cannot_copy_type" );
			return;
		}

		Data = Construct.Current.GetData<TData>( existingConstruct );
		Notify.Success( "#tool.construct.copied" );
	}

	protected virtual bool IsValidTrace( SceneTraceResult tr )
	{
		if ( !tr.Hit )
		{
			return false;
		}
		if ( !tr.GameObject.IsValid() )
		{
			return false;
		}

		// Don't allow placing on players or grabbed objects
		if ( tr.GameObject.Tags.HasAny( Constants.PlayerTag, Constants.GrabbedTag ) )
		{
			return false;
		}

		// Check for reasonable distance from hit position
		var distance = Vector3.DistanceBetween( Player.Local.WorldPosition, tr.HitPosition );
		if ( distance > 1000.0f ) // Reasonable build distance limit
		{
			return false;
		}

		return true;
	}

	private void UpdatePreviewUpdate()
	{
		if ( _preview == null || !_preview.IsValid() )
		{
			return;
		}

		var tr = PerformEyeTrace();

		// Don't update preview if already very close to the target position
		if ( tr.HitPosition.Distance( _preview.GameObject.WorldPosition ) < 0.5f )
		{
			return;
		}

		// Update position and rotation
		var toPlayer = (Player.Local.WorldPosition - tr.HitPosition).Normal;
		var rotation = FlatSurface ? MathUtils.CalculateSurfaceFlatRotation( tr.Normal, toPlayer, FlatSurfaceRotationOffset ) : Rotation.Identity;

		var bufferDistance = FlatSurface ? 1f : 30f;
		_preview.GameObject.WorldPosition = tr.HitPosition + tr.Normal * bufferDistance;
		_preview.GameObject.WorldRotation = rotation;
	}

	private void UpdatePreviewFixed()
	{
		var tr = PerformEyeTrace();
		var definition = Construct.Current.GetDefinition( type );
		var hasValidTarget = tr.Hit && IsValidTrace( tr ) && definition != null;

		// Check if targeting existing construct for update
		if ( hasValidTarget )
		{
			var targetGo = tr.GameObject.IsValid() ? tr.GameObject.Root : null;
			var existingConstruct = targetGo?.GetComponent<IConstruct>() ?? tr.GameObject.GetComponent<IConstruct>();
			if ( existingConstruct != null && existingConstruct.Type == type &&
			     GameUtils.HasPermission( Player.Local.SteamId, existingConstruct.GameObject ) )
			{
				hasValidTarget = false; // Don't show preview for updates
			}
		}

		// Hide preview if no valid target
		if ( !hasValidTarget )
		{
			ClearPreview();
			return;
		}

		// Create preview object if it doesn't exist
		if ( _preview == null || !_preview.IsValid() )
		{
			// Clean up old reference if it exists but is invalid
			ClearPreview();

			_preview = CreatePreviewObject();
			UpdatePreviewData( Data );
		}

		// Only update preview data if it has changed
		var serializationResult = Construct.Current.Serializer.Serialize( type, Data );
		if ( serializationResult.IsSuccess && !serializationResult.Value.Equals( _lastPreviewDataJson ) )
		{
			UpdatePreviewData( Data, true );
		}
	}

	private IConstruct? CreatePreviewObject()
	{
		var definition = Construct.Current.GetDefinition( type );

		// Create the actual construct using the definition
		var tr = PerformEyeTrace();

		if ( !tr.Hit )
		{
			return null;
		}

		var construct = definition?.CreateConstruct( Player.Local.SteamId, Data, tr.EndPosition, Rotation.Identity, true );
		if ( construct?.GameObject == null )
		{
			return null;
		}

		var previewObject = construct.GameObject;

		// Make it client-side only and rename it
		previewObject.NetworkMode = NetworkMode.Never;
		previewObject.Name = $"{type}Preview";
		previewObject.Tags.Remove( Constants.OccludeTag );

		// Apply preview effects (semi-transparent)
		ApplyPreviewEffects( previewObject );

		return construct;
	}

	private void UpdatePreviewData( TData data, bool hideUi = false )
	{
		// If we have a preview, update its data
		if ( _preview == null || !_preview.IsValid() )
		{
			return;
		}

		var definition = Construct.Current.GetDefinition( type );

		var isValid = definition?.Validate( data );

		if ( !isValid?.IsValid ?? false )
		{
			return;
		}

		var serializationResult = Construct.Current.Serializer.Serialize( type, Data );
		if ( !serializationResult.IsSuccess )
		{
			return;
		}

		_preview.SetData( serializationResult.Value );
		_lastPreviewDataJson = serializationResult.Value;

		if ( hideUi )
		{
			BuildMenu.Instance?.RequestLeftPanelHide();
		}
	}

	private void ClearPreview()
	{
		if ( _preview != null && _preview.IsValid() )
		{
			_preview.GameObject.Destroy();
			_preview = null;
		}

		_lastPreviewDataJson = null; // Reset data tracking
	}

	private void ApplyPreviewEffects( GameObject previewObject )
	{
		// Remove all colliders to prevent interaction
		var colliders = previewObject.GetComponentsInChildren<Collider>();
		foreach ( var collider in colliders )
		{
			collider.Destroy();
		}

		// Apply semi-transparent effect to all renderers
		var renderers = previewObject.GetComponentsInChildren<ModelRenderer>();
		foreach ( var renderer in renderers )
		{
			if ( renderer.IsValid() )
			{
				var originalTint = renderer.Tint;
				renderer.Tint = new Color( originalTint.r, originalTint.g, originalTint.b, 0.5f );
			}
		}
	}
}
