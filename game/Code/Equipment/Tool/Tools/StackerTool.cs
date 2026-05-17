using System.Threading.Tasks;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Tools;

public enum StackerToolDirection
{
	Left,
	Right,
	Up,
	Down,
	Forward,
	Backward
}

public static class StackerToolDirectionExtensions
{
	public static float GetStackStepDistance( this GameObject sourceObject, Vector3 direction, float surfaceGap )
	{
		var normalizedDirection = direction.Normal;
		var sourceSizeAlongDirection = sourceObject.GetProjectedSizeAlongDirection( normalizedDirection );
		return Math.Max( 0f, sourceSizeAlongDirection + surfaceGap );
	}

	private static float GetProjectedSizeAlongDirection( this GameObject sourceObject, Vector3 direction )
	{
		var (halfExtents, rotation, valid) = sourceObject.GetStackerBounds();
		if ( !valid )
		{
			return 1f;
		}

		var ax = rotation * new Vector3( 1, 0, 0 );
		var ay = rotation * new Vector3( 0, 1, 0 );
		var az = rotation * new Vector3( 0, 0, 1 );

		var projectedHalfExtent =
			MathF.Abs( halfExtents.x * Vector3.Dot( ax, direction ) ) +
			MathF.Abs( halfExtents.y * Vector3.Dot( ay, direction ) ) +
			MathF.Abs( halfExtents.z * Vector3.Dot( az, direction ) );

		return projectedHalfExtent * 2f;
	}

	private static (Vector3 HalfExtents, Rotation Rotation, bool Valid) GetStackerBounds( this GameObject sourceObject )
	{
		var worldTransform = sourceObject.WorldTransform;
		var modelRenderer = sourceObject.GetComponentInChildren<ModelRenderer>( false );
		if ( modelRenderer?.Model != null )
		{
			var modelBounds = modelRenderer.Model.Bounds;
			if ( modelBounds.Size.Length > 0.1f )
			{
				return (modelBounds.Size * 0.5f * worldTransform.Scale, worldTransform.Rotation, true);
			}
		}

		var skinnedModelRenderer = sourceObject.GetComponentInChildren<SkinnedModelRenderer>( false );
		if ( skinnedModelRenderer?.Model != null )
		{
			var modelBounds = skinnedModelRenderer.Model.Bounds;
			if ( modelBounds.Size.Length > 0.1f )
			{
				return (modelBounds.Size * 0.5f * worldTransform.Scale, worldTransform.Rotation, true);
			}
		}

		var colliders = sourceObject.GetComponentsInChildren<Collider>( false, true ).Where( x => !x.IsTrigger ).ToArray();
		if ( colliders.Length <= 0 )
		{
			return (Vector3.Zero, Rotation.Identity, false);
		}

		var bounds = colliders[0].GetWorldBounds();
		for ( var i = 1; i < colliders.Length; i++ )
		{
			bounds = bounds.AddBBox( colliders[i].GetWorldBounds() );
		}

		return bounds.Size.Length > 0.1f
			? (bounds.Size * 0.5f, Rotation.Identity, true)
			: (Vector3.Zero, Rotation.Identity, false);
	}

	public static Vector3 GetRelativeNormal( this StackerToolDirection direction, Rotation propRotation, Vector3 playerViewDirection )
	{
		// Get prop's orientation vectors
		var propForward = propRotation.Forward;
		var propRight = propRotation.Right;
		var propUp = propRotation.Up;

		// Project player view onto the prop's local XZ plane (forward-right plane)
		var viewNormal = playerViewDirection.Normal;

		// Calculate the angle between view direction and prop forward
		var forwardDot = viewNormal.Dot( propForward );
		var rightDot = viewNormal.Dot( propRight );

		// Calculate local view angle in prop's coordinate system
		var viewAngle = Math.Atan2( rightDot, forwardDot );

		// Convert to degrees and normalize to 0-360
		var angleDegrees = viewAngle * (180 / Math.PI);
		if ( angleDegrees < 0 )
		{
			angleDegrees += 360;
		}

		// Divide the viewing space into 4 quadrants (45° offset to align with cardinal directions)
		// 0-90° = Front-Right, 90-180° = Right-Back, 180-270° = Back-Left, 270-360° = Left-Front
		var quadrant = (int)((angleDegrees + 45) / 90) % 4;

		// Based on quadrant, determine which local directions to use
		Vector3 localForward, localRight;

		switch ( quadrant )
		{
			case 0: // Front quadrant
				localForward = propForward;
				localRight = propRight;
				break;
			case 1: // Right quadrant
				localForward = propRight;
				localRight = -propForward;
				break;
			case 2: // Back quadrant
				localForward = -propForward;
				localRight = -propRight;
				break;
			case 3: // Left quadrant
				localForward = -propRight;
				localRight = propForward;
				break;
			default:
				localForward = propForward;
				localRight = propRight;
				break;
		}

		// Map the requested direction to the appropriate local direction
		return direction switch
		{
			StackerToolDirection.Forward => localForward,
			StackerToolDirection.Backward => -localForward,
			StackerToolDirection.Right => localRight,
			StackerToolDirection.Left => -localRight,
			StackerToolDirection.Up => propUp,
			StackerToolDirection.Down => -propUp,
			_ => Vector3.Zero
		};
	}
}

[Tool( "#tool.stacker.name", "#tool.stacker.description", "#tool.group.construction" )]
public class StackerTool : BaseTool
{
	public const float MinOffset = -320f;
	public const float MaxOffset = 320f;

	[Property]
	[Title( "Count" )]
	[Range( 1, 3 )] [Step( 1 )]
	public int Count { get; set; } = 1;

	[Property]
	[Title( "Offset" )]
	[Range( MinOffset, MaxOffset )]
	public float Offset { get; set; } = 75;

	[Property] [Title( "Direction" )] public StackerToolDirection Direction { get; set; } = StackerToolDirection.Right;

	[Property] [Title( "Rotate" )] public bool Rotate { get; set; } = false;

	[Property]
	[Title( "Rotation Amount" )]
	[Range( 0, 360 )]
	public float RotationAmount { get; set; } = 15;

	public override string Attack1Control => "#tool.stacker.attack1";

	private readonly List<IConstruct> _previewObjects = new();

	private int _lastCount;
	private float _lastOffset;
	private StackerToolDirection _lastDirection;
	private bool _lastRotate;
	private float _lastRotationAmount;

	public override void OnEquip()
	{
		base.OnEquip();

		_lastCount = Count;
		_lastOffset = Offset;
		_lastDirection = Direction;
		_lastRotate = Rotate;
		_lastRotationAmount = RotationAmount;

		ClearPreview();
	}

	public override void OnToolFixedUpdate()
	{
		var slidersChanged = Count != _lastCount ||
		                     Offset != _lastOffset ||
		                     Direction != _lastDirection ||
		                     Rotate != _lastRotate ||
		                     RotationAmount != _lastRotationAmount;

		if ( slidersChanged )
		{
			_lastCount = Count;
			_lastOffset = Offset;
			_lastDirection = Direction;
			_lastRotate = Rotate;
			_lastRotationAmount = RotationAmount;

			BuildMenu.Instance?.RequestLeftPanelHide();
		}

		UpdatePreview();
	}

	private void UpdatePreview()
	{
		var tr = PerformEyeTrace();
		var construct = tr.Hit && tr.GameObject.IsValid() ? tr.GameObject.Root.GetComponent<IConstruct>() : null;

		var hasValidTarget = construct != null && construct.IsValid() && GameUtils.HasPermission( Connection.Local, construct.GameObject );

		// If we don't have a valid target now, just clear and return
		if ( !hasValidTarget || !(construct?.IsValid() ?? false) )
		{
			ClearPreview();
			return;
		}

		var sourceObject = tr.GameObject;
		var normalizedDirection = Direction.GetRelativeNormal( tr.GameObject.Root.WorldRotation, Player.Local.Controller.EyeAngles.Forward );

		// Create or remove preview objects to match count
		while ( _previewObjects.Count > Count )
		{
			if ( _previewObjects[^1].IsValid() )
			{
				_previewObjects[^1].GameObject.Destroy();
			}

			_previewObjects.RemoveAt( _previewObjects.Count - 1 );
		}

		while ( _previewObjects.Count < Count )
		{
			var previewConstruct = CreatePreviewConstruct( construct );
			if ( previewConstruct != null )
			{
				_previewObjects.Add( previewConstruct );
			}
		}

		// Update all preview objects
		var stepDistance = sourceObject.GetStackStepDistance( normalizedDirection, Offset );

		for ( var i = 0; i < _previewObjects.Count; i++ )
		{
			var previewConstruct = _previewObjects[i];

			// Skip if preview object became invalid
			if ( !previewConstruct.IsValid() )
			{
				continue;
			}

			// Calculate position offset directly using the normalized direction
			var offset = normalizedDirection * stepDistance * (i + 1);
			var newPosition = sourceObject.WorldPosition + offset;

			// Calculate rotation if enabled
			var newRotation = sourceObject.WorldRotation;
			if ( Rotate )
			{
				var additionalRotation = Rotation.FromAxis( normalizedDirection, RotationAmount * (i + 1) );
				newRotation *= additionalRotation;
			}

			// Update transform
			previewConstruct.GameObject.WorldPosition = newPosition;
			previewConstruct.GameObject.WorldRotation = newRotation;
			previewConstruct.GameObject.WorldScale = sourceObject.WorldScale;
		}
	}

	private IConstruct? CreatePreviewConstruct( IConstruct sourceConstruct )
	{
		var definition = Construct.Current.GetDefinition( sourceConstruct.Type );

		// Create the actual construct using the definition as a preview
		var previewConstruct = definition?.CreateConstruct( Player.Local.SteamId, sourceConstruct.Data, Vector3.Zero, Rotation.Identity, true );
		if ( previewConstruct == null || !previewConstruct.GameObject.IsValid() )
		{
			return null;
		}

		previewConstruct.SetData( Construct.Current.Serializer.Serialize( sourceConstruct.Type, sourceConstruct.Data ).Value );

		var previewObject = previewConstruct.GameObject;

		// Make it client-side only and rename it
		previewObject.NetworkMode = NetworkMode.Never;
		previewObject.Name = $"{sourceConstruct.Type}StackerPreview";

		// Apply preview effects (semi-transparent)
		ApplyPreviewEffects( previewObject );

		return previewConstruct;
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
				renderer.Tint = new Color( originalTint.r, originalTint.g, originalTint.b, 0.35f );
			}
		}
	}

	private void ClearPreview()
	{
		foreach ( var construct in _previewObjects.ToList().Where( c => c.IsValid() ) )
		{
			construct.Destroy();
		}

		_previewObjects.Clear();
	}

	public override void PrimaryUseStart()
	{
		if ( Count <= 0 )
		{
			Notify.Warn( "#notify.stacker.invalid_count" );
			return;
		}

		var tr = PerformEyeTrace();

		if ( !tr.Hit || !tr.GameObject.IsValid() )
		{
			return;
		}

		// Check if we hit a valid construct
		var targetGameObject = tr.GameObject.Root;
		if ( !targetGameObject.Tags.Has( Constants.ConstructTag ) || !targetGameObject.GetComponent<IConstruct>().IsValid() )
		{
			Notify.Warn( "#notify.stacker.invalid_target" );
			return;
		}

		if ( !GameUtils.HasPermission( Connection.Local, targetGameObject ) )
		{
			Notify.Error( "#generic.forbidden" );
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "stacker", Config.Current.Game.StackerCooldown, true ) )
		{
			return;
		}

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );

		// Clear preview before placing real objects
		ClearPreview();

		// Call the server-side stack method
		Construct.Current.StackConstructHost(
			targetGameObject,
			Count,
			Offset,
			Direction,
			Rotate,
			RotationAmount,
			Player.Local.Controller.EyeAngles.Forward
		);
	}

	public override void OnUnequip()
	{
		base.OnUnequip();

		ClearPreview();
	}
}
