using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Tools;

[Tool( "#tool.duplicator.name", "#tool.duplicator.description", "#tool.group.miscellaneous" )]
public class DuplicatorTool : BaseTool
{
	public const int MinDupeRange = 25;
	public const int MaxDupeRange = 5000;
	public const float MinDirectionOffset = -200.0f;
	public const float MaxDirectionOffset = 200.0f;
	public const float MinRotationOffset = 0.0f;
	public const float MaxRotationOffset = 360.0f;

	[Property]
	[DupeProperty]
	public object DupeFileManager { get; set; } = new();

	[Property]
	[Title( "Duplicate Range" )]
	[Range( MinDupeRange, MaxDupeRange )]
	[Step( 5 )]
	public int Radius { get; set; } = 500;

	[Property]
	[Title( "Original Position" )]
	public bool OriginalPosition { get; set; }

	[Property]
	[Title( "Preserve Position" )]
	public bool PreservePosition { get; set; }

	[Property]
	[Title( "Rotation Offset" )]
	[Range( MinRotationOffset, MaxRotationOffset )]
	[Step( 1 )]
	public float RotationOffset { get; set; } = 0;

	[Property]
	[Title( "Left/Right Offset" )]
	[Range( MinDirectionOffset, MaxDirectionOffset )]
	[Step( 1 )]
	public float XOffset { get; set; } = 0f;

	[Property]
	[Title( "Forward/Back Offset" )]
	[Range( MinDirectionOffset, MaxDirectionOffset )]
	[Step( 1 )]
	public float YOffset { get; set; } = 0f;

	[Property]
	[Title( "Up/Down Offset" )]
	[Range( MinDirectionOffset, MaxDirectionOffset )]
	public float ZOffset { get; set; } = 0f;

	public override string Attack1Control => "#tool.duplicator.attack1";
	public override string Attack2Control => "#tool.duplicator.attack2";
	public override string ReloadControl => "#tool.duplicator.reload";

	public static ConstructDupe? SelectedDupe { get; set; }
	public static bool SelectedDupeSaved { get; set; }

	private string? _previewDupeName;
	private GameObject? _previewGameObject;
	private Vector3? _lockedPosition;
	private bool _lastPreservePositionState;

	private float _lastXOffset;
	private float _lastYOffset;
	private float _lastZOffset;
	private float _lastRotationOffset;

	private readonly Dictionary<Guid, GameObject> _previewConstructMap = new();
	private readonly List<SceneLineObject> _previewWireLines = new();

	public override void OnEquip()
	{
		base.OnEquip();

		_lastXOffset = XOffset;
		_lastYOffset = YOffset;
		_lastZOffset = ZOffset;
		_lastRotationOffset = RotationOffset;
		_lastPreservePositionState = PreservePosition;

		CleanupPreview();
	}

	public override void OnToolUpdate()
	{
		base.OnToolUpdate();

		// Draw range preview sphere when aiming
		var tr = PerformEyeTrace();
		if ( tr.Hit && tr.GameObject.IsValid() && tr.GameObject.Tags.Has( Constants.ConstructTag ) )
		{
			// Draw the duplicate range sphere
			Gizmo.Draw.Color = Color.Cyan;
			Gizmo.Draw.LineSphere( tr.GameObject.WorldPosition, Radius );
		}
	}

	public override void OnToolFixedUpdate()
	{
		if ( SelectedDupe == null )
		{
			CleanupPreview();
			return;
		}

		if ( SelectedDupe.Name != _previewDupeName )
		{
			CleanupPreview();
		}

		var slidersChanged = XOffset != _lastXOffset || YOffset != _lastYOffset ||
		                     ZOffset != _lastZOffset || RotationOffset != _lastRotationOffset;

		if ( slidersChanged )
		{
			_lastXOffset = XOffset;
			_lastYOffset = YOffset;
			_lastZOffset = ZOffset;
			_lastRotationOffset = RotationOffset;

			BuildMenu.Instance?.RequestLeftPanelHide();
		}

		if ( _lastPreservePositionState && !PreservePosition )
		{
			_lockedPosition = null;
		}
		_lastPreservePositionState = PreservePosition;

		// Create preview if it doesn't exist
		if ( _previewGameObject.IsValid() )
		{
			UpdatePreviewTransforms();
			UpdateWirePreviewPositions();
		}
		else
		{
			_previewGameObject = new GameObject
			{
				Name = "DuplicatorPreview", NetworkMode = NetworkMode.Never
			};

			_previewGameObject.Tags.Add( "preview" );
			_previewDupeName = SelectedDupe.Name;
			_ = AddConstructsToPreview( _previewGameObject, SelectedDupe );
		}
	}

	public override void PrimaryUseStart()
	{
		if ( SelectedDupe == null )
		{
			Notify.Warn( "#tool.duplicator.no_dupe" );
			return;
		}

		if ( !RankSystem.HasLocalPermission( Permission.DuplicateBypass ) && Cooldown.Current.CheckAndStartCooldown( $"dupe", SelectedDupe.GetCooldown(), true ) )
		{
			return;
		}

		var tr = PerformEyeTrace();

		Vector3 spawnPosition;
		if ( OriginalPosition )
		{
			spawnPosition = SelectedDupe.ReferencePoint;
		}
		else if ( PreservePosition && _lockedPosition.HasValue )
		{
			spawnPosition = _lockedPosition.Value;
		}
		else
		{
			spawnPosition = GameUtils.GetSpawnPosition( Player.Local.AimRay, 0 );
		}

		Construct.Current.SpawnDupeHost( SelectedDupe, spawnPosition, RotationOffset, XOffset, YOffset, ZOffset );

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void SecondaryUseStart()
	{
		SelectedDupe = null;
		CleanupPreview();

		var tr = PerformEyeTrace();

		if ( !tr.Hit )
		{
			return;
		}

		if ( !tr.GameObject.Tags.Has( Constants.ConstructTag ) )
		{
			return;
		}

		if ( !tr.Body.IsValid() )
		{
			return;
		}

		var nearbyGameObjects = Sandbox.Game.ActiveScene.FindInPhysics( new Sphere( tr.GameObject.WorldPosition, Radius ) );
		if ( nearbyGameObjects == null )
		{
			return;
		}

		var dupe = Duplicate( nearbyGameObjects, tr.HitPosition );
		SelectedDupe = dupe;
		SelectedDupeSaved = false;

		CleanupPreview();

		Tool.DoUseEffects( true, tr.HitPosition, tr.Normal );
	}

	public override void ReloadUseStart()
	{
		Construct.Current.CancelDupeSpawnHost();
	}

	private async Task AddConstructsToPreview( GameObject parent, ConstructDupe constructDupe )
	{
		foreach ( var dupeItem in constructDupe.Items )
		{
			if ( !parent.IsValid() )
			{
				return;
			}

			// Get the definition for this construct type
			var definition = Construct.Current.GetDefinition( dupeItem.Type );
			if ( definition == null )
			{
				continue;
			}

			// Deserialize the construct data
			var deserializationResult = Construct.Current.Serializer.DeserializeWithMigration( dupeItem.DataJson, definition );
			if ( !deserializationResult.IsSuccess )
			{
				continue;
			}

			// Create a preview construct
			var previewConstruct = definition.CreateConstruct( Player.Local.SteamId, deserializationResult.Value, dupeItem.Position, dupeItem.Rotation, true );
			if ( previewConstruct == null || !previewConstruct.IsValid() )
			{
				continue;
			}
			previewConstruct.SetData( dupeItem.DataJson );

			// Make it client-side only and apply preview effects
			var previewObject = previewConstruct.GameObject;
			if ( !previewObject.IsValid() )
			{
				continue;
			}
			previewObject.NetworkMode = NetworkMode.Never;
			previewObject.Name = $"{dupeItem.Type}Preview";
			previewObject.Parent = parent;
			previewObject.LocalPosition = dupeItem.Position;
			previewObject.LocalRotation = dupeItem.Rotation;

			previewObject.Tags.Remove( Constants.OccludeTag );
			previewObject.Tags.Remove( Constants.OccludableTag );

			// Store mapping for wire connections
			_previewConstructMap[dupeItem.Id] = previewObject;

			ApplyPreviewEffects( previewObject );

			await GameTask.Delay( 50 ); // Prevent your pc from dying from too many objects being created at once
		}

		// Create wire previews after all constructs are loaded
		CreateWirePreviews( constructDupe );
	}

	private static void ApplyPreviewEffects( GameObject previewObject )
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

	private void UpdatePreviewTransforms()
	{
		if ( !_previewGameObject.IsValid() || SelectedDupe == null )
		{
			return;
		}

		var previewPosition = OriginalPosition
			? SelectedDupe.ReferencePoint
			: PreservePosition
				? _lockedPosition ??= GameUtils.GetSpawnPosition( Player.Local.AimRay, 1 )
				: GameUtils.GetSpawnPosition( Player.Local.AimRay, 1 );


		var baseRotation = Rotation.FromAxis( Vector3.Up, RotationOffset );
		var axisOffset = new Vector3( XOffset, YOffset, ZOffset );
		var rotatedAxisOffset = baseRotation * axisOffset;

		_previewGameObject.WorldPosition = previewPosition + rotatedAxisOffset;
		_previewGameObject.WorldRotation = Rotation.Identity;

		var children = _previewGameObject.Children.ToArray();
		var dupeItems = SelectedDupe.Items.ToArray();

		for ( var i = 0; i < children.Length && i < dupeItems.Length; i++ )
		{
			var child = children[i];
			var dupeItem = dupeItems[i];

			var rotatedPosition = baseRotation * dupeItem.Position;
			var combinedRotation = baseRotation * dupeItem.Rotation;

			child.LocalPosition = rotatedPosition;
			child.LocalRotation = combinedRotation;
		}
	}


	public static ConstructDupe? Duplicate( IEnumerable<GameObject> gameObjects, Vector3 hitPosition, bool includeUnowned = false )
	{
		var dupeItems = new List<ConstructDupeItem>();
		var wireConnections = new List<ConstructDupeWireConnection>();
		var constructIdMap = new Dictionary<IConstruct, Guid>();

		foreach ( var gameObject in gameObjects )
		{
			if ( !gameObject.IsValid() )
			{
				continue;
			}

			var root = gameObject.Root;

			if ( !root.IsValid() )
			{
				continue;
			}

			var owned = root.GetComponent<IOwned>();
			if ( !includeUnowned && ( owned?.Owner == 0 || !GameUtils.HasPermission( Connection.Local, root, false ) ) )
			{
				continue;
			}

			if ( !root.Tags.Has( Constants.ConstructTag ) )
			{
				continue;
			}

			var construct = root.GetComponent<IConstruct>();
			if ( !construct.IsValid() )
			{
				continue;
			}

			var serializationResult = Construct.Current.Serializer.Serialize( construct.Type, construct.Data );

			if ( !serializationResult.IsSuccess )
			{
				Log.Error( $"Failed to serialize construct {construct.Type} for duplication: {serializationResult.Error}" );
				continue;
			}

			var dupeId = construct.Id;
			var dupeItem = new ConstructDupeItem
			{
				Id = dupeId,
				Type = construct.Type,
				Owner = construct.Owner,
				Position = root.WorldPosition - hitPosition,
				Rotation = root.WorldRotation,
				DataJson = serializationResult.Value
			};

			dupeItems.Add( dupeItem );
			constructIdMap[construct] = dupeId;
		}

		if ( dupeItems.Count == 0 )
		{
			return null;
		}

		// Collect wire connections between the selected constructs
		foreach ( var (sourceConstruct, sourceId) in constructIdMap )
		{
			var sourceWireComponent = sourceConstruct.GameObject.GetComponent<Wire.IWireComponent>();
			if ( sourceWireComponent == null || !sourceWireComponent.GameObject.IsValid() )
			{
				continue;
			}

			var sourceConnections = Wire.Wire.Current.GetSourceConnections( sourceWireComponent );

			foreach ( var connection in sourceConnections )
			{
				if ( connection == null || connection.Target == null || !connection.Target.GameObject.IsValid() )
				{
					continue;
				}

				var targetConstruct = connection.Target.GameObject.GetComponent<IConstruct>();
				if ( targetConstruct.IsValid() && constructIdMap.TryGetValue( targetConstruct, out var targetId ) )
				{
					wireConnections.Add( new ConstructDupeWireConnection
					{
						SourceId = sourceId,
						OutputId = connection.OutputId,
						TargetId = targetId,
						InputId = connection.InputId,
						Color = connection.Color,
						Thickness = connection.Thickness,
						Opacity = connection.Opacity,
						Anchors = connection.Anchors?.ToArray()
					} );
				}
			}
		}

		return new ConstructDupe
		{
			Game = Sandbox.Game.Ident,
			Author = Sandbox.Game.SteamId.ToString(),
			Items = dupeItems,
			WireConnections = wireConnections,
			ReferencePoint = hitPosition
		};
	}

	private void CreateWirePreviews( ConstructDupe constructDupe )
	{
		foreach ( var wireConnection in constructDupe.WireConnections )
		{
			// Find the source and target preview objects
			if ( !_previewConstructMap.TryGetValue( wireConnection.SourceId, out var sourceObject ) ||
			     !_previewConstructMap.TryGetValue( wireConnection.TargetId, out var targetObject ) )
			{
				continue; // Skip if either construct is missing
			}

			// Get wire components from the preview objects
			var sourceWireComponent = sourceObject.GetComponent<Wire.IWireComponent>();
			var targetWireComponent = targetObject.GetComponent<Wire.IWireComponent>();

			if ( sourceWireComponent == null || targetWireComponent == null )
			{
				continue; // Skip if either component is missing
			}

			// Create the wire line
			var lineObject = new SceneLineObject( Sandbox.Game.ActiveScene.SceneWorld );
			_previewWireLines.Add( lineObject );

			var fromPosition = sourceWireComponent.GetPortPosition();
			var toPosition = targetWireComponent.GetPortPosition();

			// Make wire preview semi-transparent like the constructs
			var previewColor = new Color( wireConnection.Color.r, wireConnection.Color.g, wireConnection.Color.b, wireConnection.Opacity );

			// Use static wire rendering method
			Wire.Wire.RenderWireLine( lineObject, fromPosition, toPosition, wireConnection.Anchors, previewColor, wireConnection.Thickness, wireConnection.Opacity );
		}
	}

	private void CleanupPreview()
	{
		_previewGameObject?.Destroy();
		_previewGameObject = null;
		_lockedPosition = null;
		_previewDupeName = null;
		_lastPreservePositionState = false;

		// Clean up wire previews
		foreach ( var wirePreview in _previewWireLines )
		{
			wirePreview?.Delete();
		}
		_previewWireLines.Clear();
		_previewConstructMap.Clear();
	}

	private void UpdateWirePreviewPositions()
	{
		if ( SelectedDupe == null || !_previewGameObject.IsValid() )
		{
			return;
		}

		// Update each wire preview line
		var wireConnectionsList = SelectedDupe.WireConnections.ToArray();
		for ( var i = 0; i < _previewWireLines.Count && i < wireConnectionsList.Length; i++ )
		{
			var lineObject = _previewWireLines[i];
			var wireConnection = wireConnectionsList[i];

			// Find the source and target preview objects
			if ( !_previewConstructMap.TryGetValue( wireConnection.SourceId, out var sourceObject ) ||
			     !_previewConstructMap.TryGetValue( wireConnection.TargetId, out var targetObject ) )
			{
				continue;
			}

			var fromPosition = sourceObject.WorldPosition;
			var toPosition = targetObject.WorldPosition;

			var previewColor = new Color( wireConnection.Color.r, wireConnection.Color.g, wireConnection.Color.b, 0.5f );

			// Transform anchors if they exist
			IEnumerable<Vector3>? transformedAnchors = null;
			if ( wireConnection.Anchors != null && SelectedDupe != null )
			{
				var baseRotation = Rotation.FromAxis( Vector3.Up, RotationOffset );
				transformedAnchors = wireConnection.Anchors.Select( anchor =>
				{
					// Anchors are stored in world coordinates from when the dupe was created
					// Convert them to be relative to the dupe reference point, then transform to preview position
					var relativeAnchor = anchor - SelectedDupe.ReferencePoint;
					var rotatedAnchor = baseRotation * relativeAnchor;
					return _previewGameObject.WorldPosition + rotatedAnchor;
				} );
			}

			// Use static wire rendering method
			Wire.Wire.RenderWireLine( lineObject, fromPosition, toPosition, transformedAnchors, previewColor, wireConnection.Thickness, wireConnection.Opacity );
		}
	}

	public override void OnUnequip()
	{
		base.OnUnequip(); // Save settings

		CleanupPreview();
	}
}
