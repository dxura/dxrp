using Dxura.RP.Game.Tools;
using Dxura.RP.Shared;
using Sandbox.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace Dxura.RP.Game;

/// <summary>
/// Handles requests from clients to do construct-related actions, such as spawning or clearing, etc.
/// </summary>
public partial class Construct
{
	private readonly Dictionary<long, CancellationTokenSource> _activeDupeSpawns = new();

	public IConstruct? SpawnConstructFromDataJson( ConstructType type, long owner, string dataJson, Vector3 position,
		Rotation rotation, bool freeze, bool enforceLimits, bool applyPropExploitGuard, bool addUndo )
	{
		Assert.True( Networking.IsHost );

		var definition = GetDefinition( type );
		if ( definition == null )
		{
			Log.Warning( $"Failed to spawn construct, missing definition for type {type}" );
			return null;
		}

		// Deserialize using central serializer with migration support
		var deserializationResult = Serializer.DeserializeWithMigration( dataJson, definition );
		if ( !deserializationResult.IsSuccess )
		{
			Log.Error( $"Failed to deserialize construct data for type {type}: {deserializationResult.Error}" );
			return null;
		}

		var data = deserializationResult.Value;

		var validationResult = definition.Validate( data );
		if ( !validationResult.IsValid )
		{
			Log.Warning( $"Failed to spawn construct ({type}), validation failed: {validationResult.ErrorMessage}" );
			return null;
		}

		var player = GameUtils.GetPlayerById( owner );

		if ( enforceLimits && !HasLimit( type, owner ) )
		{
			player?.Error( "#notify.construct.limit" );
			return null;
		}

		var construct = definition.CreateConstruct( owner, data, position, rotation );
		if ( construct == null || !construct.IsValid() )
		{
			Log.Warning( $"Failed to spawn construct ({type}) for owner {owner}, CreateConstruct returned null/invalid" );
			return null;
		}

		// Set Data (canonicalize json after migration)
		var serializationResult = Serializer.Serialize( type, data );
		if ( !serializationResult.IsSuccess )
		{
			Log.Error( $"Failed to serialize construct data for type {type}: {serializationResult.Error}" );
			construct.Destroy();
			return null;
		}

		construct.SetData( serializationResult.Value );

		using ( Rpc.FilterExclude( x => x.IsHost ) )
		{
			BroadcastSpawn( construct.GameObject.Serialize().ToJsonString() );
		}

		if ( applyPropExploitGuard && Config.Current.Game.PreventPropExploits && player.IsValid() && player.Level <= 1 )
		{
			construct.GameObject.AddComponent<CollideGuard>();
		}

		// Count only after successful spawn
		IncrementCount( owner, type );

		if ( !freeze )
		{
			construct.Unfreeze();
		}

		if ( addUndo )
		{
			Undo.Current.AddUndo( owner, new ConstructAction( construct ) );

		}

		return construct;
	}

	[Rpc.Host]
	private void SpawnConstructHost( ConstructType type, string dataJson, Vector3 position, Rotation rotation, bool freeze = true )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:construct", Config.Current.Game.ConstructCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		// Distance check to prevent exploits
		var distance = Vector3.DistanceBetween( player.GameObject.WorldPosition, position );
		if ( distance > Config.Current.Game.ReachDistance * 1.5f )
		{
			return;
		}

		var construct = SpawnConstructFromDataJson(
			type,
			player.SteamId,
			dataJson,
			position,
			rotation,
			freeze,
			true,
			true,
			true
		);

		if ( construct == null )
		{
			return;
		}

		// Notify player of successful spawn with counts
		var current = GetCurrentCount( type, player.SteamId );
		var limit = GetLimit( type, player.SteamId );

		player.Info( $"{type} spawned ({current}/{limit})" );
		player.IncrementStat( "construct", 1 );
		player.IncrementStat( $"construct:{type.ToString().ToLowerInvariant()}", 1 );
	}

	private IConstruct? SpawnConstruct( Player player, ConstructType type, IConstructData data, Vector3 position, Rotation rotation )
	{
		Assert.True( Networking.IsHost );

		var definition = GetDefinition( type );
		if ( definition == null )
		{
			return null;
		}

		var validationResult = definition.Validate( data );
		if ( !validationResult.IsValid )
		{
			return null;
		}

		// Enforce server-side construct limit per type
		if ( !HasLimit( type, player.SteamId ) )
		{
			player.Error( "#notify.construct.limit" );
			return null;
		}

		Log.Info( $"Player {player.SteamId} spawned construct ({type})" );

		var construct = definition.CreateConstruct( player.SteamId, data, position, rotation );

		if ( construct == null )
		{
			return null;
		}

		// Set Data
		var serializationResult = Serializer.Serialize( type, data );
		if ( !serializationResult.IsSuccess )
		{
			Log.Error( $"Failed to serialize construct data for type {type}: {serializationResult.Error}" );
			return null;
		}

		construct.SetData( serializationResult.Value );

		using ( Rpc.FilterExclude( x => x.IsHost ) )
		{
			BroadcastSpawn( construct.GameObject.Serialize().ToJsonString() );
		}

		construct.GameObject.AddComponent<CollideGuard>();

		// Count only after successful spawn
		IncrementCount( player.SteamId, type );

		return construct;
	}

	[Rpc.Host]
	private void UpdateConstructHost( GameObject target, string dataJson )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:construct:update", Config.Current.Game.ConstructUpdateCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			Log.Warning( "Invalid player tried to update construct" );
			return;
		}

		// Validate target exists and is a construct
		if ( !target.IsValid() || !target.Root.Tags.Has( Constants.ConstructTag ) )
		{
			Log.Warning( $"Player ({player.SteamId}) tried to update construct but target is invalid" );
			return;
		}

		// Distance check to prevent exploits
		var distance = Vector3.DistanceBetween( player.GameObject.WorldPosition, target.WorldPosition );
		if ( distance > Config.Current.Game.ReachDistance * 1.25f )
		{
			Log.Warning( $"Player ({player.SteamId}) tried to update construct outside reach distance ({distance} > {Config.Current.Game.ReachDistance})" );
			player.Error( "#generic.error" );
			return;
		}

		var construct = target.Root.GetComponent<IConstruct>();

		if ( !construct.IsValid() || !GameUtils.HasPermission( player.SteamId, construct.GameObject ) )
		{
			player.Error( "You do not have permission to update this construct." );
			return;
		}

		var definition = GetDefinition( construct.Type );
		if ( definition == null )
		{
			return;
		}

		// Deserialize using central serializer with migration support
		var deserializationResult = Serializer.DeserializeWithMigration( dataJson, definition );
		if ( !deserializationResult.IsSuccess )
		{
			Log.Warning( $"Failed to deserialize construct data for type {construct.Type}: {deserializationResult.Error}" );
			return;
		}

		var data = deserializationResult.Value;

		// Use centralized update method
		if ( !UpdateConstruct( construct, data ) )
		{
			player.Error( "Failed to update construct data" );
			return;
		}

		Log.Info( $"Player {player.SteamId} updated construct ({construct.Type})" );

		player.Success( "Applied" );
	}

	[Rpc.Host]
	public void StackConstructHost( GameObject target, int count, float offset, StackerToolDirection direction,
		bool rotate, float rotationAmount, Vector3 viewDirection )
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:stacker", Config.Current.Game.StackerCooldown ) )
		{
			return;
		}

		if ( !GameUtils.HasPermission( caller, target ) )
		{
			return;
		}

		var targetConstruct = target.GetComponent<IConstruct>();

		if ( !targetConstruct.IsValid() )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		if ( !Current.HasLimit( targetConstruct.Type, player.SteamId ) )
		{
			return;
		}

		if ( count is <= 0 or >= 10 )
		{
			player.Warn( "#notify.stacker.invalid_count" );
			return;
		}

		if ( offset < StackerTool.MinOffset || offset > StackerTool.MaxOffset )
		{
			player.Warn( "#notify.stacker.invalid_offset" );
			return;
		}

		Log.Info( $"Player {player.SteamId} stacked construct ({targetConstruct.Type}, {count})" );

		// Calculate normalized direction based on the tool direction and player view
		var normalizedDirection = direction.GetRelativeNormal( targetConstruct.GameObject.WorldRotation, viewDirection );
		var stepDistance = targetConstruct.GameObject.GetStackStepDistance( normalizedDirection, offset );

		// Stack constructs
		var stackedConstructs = new List<IConstruct>();

		for ( var i = 0; i < count; i++ )
		{
			// Calculate position offset directly using the normalized direction
			var posOffset = normalizedDirection * stepDistance * (i + 1);
			var newPosition = targetConstruct.GameObject.WorldPosition + posOffset;

			// Calculate rotation if enabled
			var newRotation = targetConstruct.GameObject.WorldRotation;
			if ( rotate )
			{
				var additionalRotation = Rotation.FromAxis( normalizedDirection, rotationAmount * (i + 1) );
				newRotation *= additionalRotation;
			}

			var definition = GetDefinition( targetConstruct.Type );
			if ( definition == null )
			{
				continue;
			}

			// Create a copy of the construct data
			var constructData = targetConstruct.Data;

			var construct = SpawnConstruct( player, targetConstruct.Type, constructData, newPosition, newRotation );

			if ( construct?.IsValid() ?? false )
			{
				stackedConstructs.Add( construct );
			}
		}

		if ( stackedConstructs.Count <= 0 )
		{
			return;
		}

		// Create undo action for all stacked constructs
		Undo.Current.AddUndo( player.SteamId, new StackAction( stackedConstructs ) );

		player.Success( $"Stacked {stackedConstructs.Count} {targetConstruct.Type}s" );
	}

	[Rpc.Host]
	public void SpawnDupeHost( ConstructDupe constructDupe, Vector3? position, float rotationOffset = 0, float xOffset = 0f, float yOffset = 0f, float zOffset = 0f )
	{
		var callerId = Rpc.CallerId;
		var callerSteamId = Rpc.Caller.SteamId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:dupe", constructDupe.GetCooldown() ) && !RankSystem.HasPermission( callerSteamId, Permission.DuplicateBypass ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() )
		{
			return;
		}

		if ( !TryBeginDupeSpawn( player.SteamId, out var cancellationTokenSource ) )
		{
			player.Error( "#notify.dupe.spawn_in_progress" );
			return;
		}

		_ = SpawnDupe( player, constructDupe, cancellationTokenSource, position, rotationOffset, xOffset, yOffset, zOffset );
	}

	[Rpc.Host]
	public void CancelDupeSpawnHost()
	{
		var callerId = Rpc.CallerId;
		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( !TryCancelDupeSpawn( player.SteamId ) )
		{
			player.Error( "#notify.dupe.cancel.none" );
			return;
		}

		Cooldown.Current.CancelCooldown( $"{callerId}:dupe" );

		using ( Rpc.FilterInclude( x => x == player.Connection ) )
		{
			OnDupeSpawnCanceled();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnDupeSpawnCanceled()
	{
		Cooldown.Current.CancelCooldown( "dupe" );
	}


	public async Task SpawnDupe( Player player, ConstructDupe constructDupe, CancellationTokenSource? cancellationTokenSource, Vector3? position, float rotationOffset = 0, float xOffset = 0f, float yOffset = 0f, float zOffset = 0f )
	{
		Assert.True( Networking.IsHost );

		var cancellationToken = cancellationTokenSource?.Token ?? default;
		var dupeItemCount = constructDupe.Items.Count();
		var spawnedItems = new List<IConstruct>();

		try
		{
			// Check if spawning all constructs would exceed any type limits (server-side)
			var constructTypeCounts = constructDupe.Items.GroupBy( item => item.Type )
				.ToDictionary( g => g.Key, g => g.Count() );

			foreach ( var (constructType, count) in constructTypeCounts )
			{
				var currentCount = GetCurrentCount( constructType, player.SteamId );
				var maxCount = GetLimit( constructType, player.SteamId );

				if ( currentCount + count <= maxCount )
				{
					continue;
				}

				player.Error( $"Failed to paste. Exceeds {constructType} limit!" );
				return;
			}

			xOffset = Math.Clamp( xOffset, DuplicatorTool.MinDirectionOffset, DuplicatorTool.MaxDirectionOffset );
			yOffset = Math.Clamp( yOffset, DuplicatorTool.MinDirectionOffset, DuplicatorTool.MaxDirectionOffset );
			zOffset = Math.Clamp( zOffset, DuplicatorTool.MinDirectionOffset, DuplicatorTool.MaxDirectionOffset );
			rotationOffset = Math.Clamp( rotationOffset, DuplicatorTool.MinRotationOffset, DuplicatorTool.MaxRotationOffset );

			Log.Info( $"Player {player.SteamId} spawned a dupe with {dupeItemCount} items (Rotation: {rotationOffset}°, Offsets: X={xOffset}, Y={yOffset}, Z={zOffset})" );

			// Delay spawning
			var delayBetweenItems =
				!RankSystem.HasPermission( player.SteamId, Permission.DuplicateBypass ) ? 500 : 200;

			// No delay for host
			if ( player.Connection?.IsHost ?? false )
			{
				delayBetweenItems = 0;
			}

			player.Info( "Spawning dupe (~" + dupeItemCount * delayBetweenItems / 1000 + " seconds)" );

			spawnedItems = await SpawnDupeItems(
				constructDupe,
				position,
				rotationOffset,
				xOffset,
				yOffset,
				zOffset,
				player.SteamId,
				true,
				true,
				false,
				delayBetweenItems,
				cancellationToken: cancellationToken
			);

			if ( cancellationToken.IsCancellationRequested )
			{
				RollbackDupeSpawn( spawnedItems );
				player.Info( "#notify.dupe.cancelled" );
				return;
			}

			if ( spawnedItems.Count != dupeItemCount )
			{
				RollbackDupeSpawn( spawnedItems );
				player.Error( "#generic.error" );
				Log.Warning( $"Player {player.SteamId} dupe spawn rolled back after partial spawn ({spawnedItems.Count}/{dupeItemCount})" );
				return;
			}

			if ( spawnedItems.Count > 0 )
			{
				Undo.Current.AddUndo( player.SteamId, new DupeAction( spawnedItems ) );
			}

			player.Success( "#notify.dupe.spawned" );
		}
		catch ( Exception ex )
		{
			RollbackDupeSpawn( spawnedItems );
			Log.Error( $"Failed to spawn dupe for player {player.SteamId}: {ex}" );
			player.Error( "#generic.error" );
		}
		finally
		{
			EndDupeSpawn( player.SteamId, cancellationTokenSource );
		}
	}

	/// <summary>
	/// Core method that spawns all items from a dupe and restores wire connections.
	/// Used by both player dupe pasting and server recovery.
	/// When ownerOverride is 0, uses each item's Owner field.
	/// </summary>
	public async Task<List<IConstruct>> SpawnDupeItems(
		ConstructDupe dupe,
		Vector3? position,
		float rotationOffset = 0,
		float xOffset = 0f,
		float yOffset = 0f,
		float zOffset = 0f,
		long ownerOverride = 0,
		bool enforceLimits = false,
		bool applyPropExploitGuard = false,
		bool addUndo = false,
		int delayBetweenItems = 0,
		int delayBetweenWires = 50,
		CancellationToken cancellationToken = default )
	{
		Assert.True( Networking.IsHost );

		var spawnedItems = new List<IConstruct>();
		var idToConstructMap = new Dictionary<Guid, IConstruct>();
		var axisOffset = new Vector3( xOffset, yOffset, zOffset );

		foreach ( var dupeItem in dupe.Items )
		{
			if ( cancellationToken.IsCancellationRequested )
			{
				break;
			}

			// Calculate base rotation with offset
			var baseRotation = Rotation.FromAxis( Vector3.Up, rotationOffset );

			// Calculate spawn position relative to reference point
			var rotatedPosition = baseRotation * dupeItem.Position;
			var rotatedAxisOffset = baseRotation * axisOffset;
			var offsetPosition = rotatedPosition + rotatedAxisOffset;

			var spawnPosition = position != null
				? position.Value + offsetPosition
				: offsetPosition + dupe.ReferencePoint;

			// Combine base rotation with item's rotation
			var spawnRotation = baseRotation * dupeItem.Rotation;

			var owner = ownerOverride != 0 ? ownerOverride : dupeItem.Owner;

			var construct = SpawnConstructFromDataJson(
				dupeItem.Type,
				owner,
				dupeItem.DataJson,
				spawnPosition,
				spawnRotation,
				true,
				enforceLimits,
				applyPropExploitGuard,
				addUndo
			);

			if ( construct != null && construct.IsValid() )
			{
				spawnedItems.Add( construct );
				idToConstructMap[dupeItem.Id] = construct;

			}

			if ( delayBetweenItems > 0 )
			{
				await GameTask.Delay( delayBetweenItems );
			}
		}

		if ( cancellationToken.IsCancellationRequested )
		{
			return spawnedItems;
		}

		// Restore wire connections after all constructs are spawned
		await RestoreWireConnections( dupe.WireConnections, idToConstructMap, dupe.ReferencePoint, position, axisOffset, rotationOffset, delayBetweenWires, cancellationToken );

		return spawnedItems;
	}


	private static async Task RestoreWireConnections( IEnumerable<ConstructDupeWireConnection> dupeWireConnections,
		Dictionary<Guid, IConstruct> idToConstructMap,
		Vector3 referencePoint, Vector3? spawnPosition, Vector3 axisOffset, float rotationOffset,
		int delayBetweenWires = 80, CancellationToken cancellationToken = default )
	{
		// Small delay to ensure all constructs are fully initialized
		if ( delayBetweenWires > 0 )
		{
			await GameTask.Delay( 100 );
		}

		// Restore each wire connection
		foreach ( var dupeWireConnection in dupeWireConnections )
		{
			if ( cancellationToken.IsCancellationRequested )
			{
				break;
			}

			// Find the source and target constructs
			if ( !idToConstructMap.TryGetValue( dupeWireConnection.SourceId, out var sourceConstruct ) ||
			     !idToConstructMap.TryGetValue( dupeWireConnection.TargetId, out var targetConstruct ) )
			{
				Log.Warning( $"Could not find constructs for wire connection: {dupeWireConnection.SourceId} -> {dupeWireConnection.TargetId}" );
				continue;
			}

			// Validate constructs and their GameObjects are not null
			if ( !sourceConstruct.IsValid() || !targetConstruct.IsValid() ||
			     !sourceConstruct.GameObject.IsValid() || !targetConstruct.GameObject.IsValid() )
			{
				continue;
			}

			// Get wire components from the constructs
			var sourceWireComponent = sourceConstruct.GameObject.GetComponent<Wire.IWireComponent>();
			var targetWireComponent = targetConstruct.GameObject.GetComponent<Wire.IWireComponent>();

			if ( sourceWireComponent == null || !sourceWireComponent.GameObject.IsValid() ||
			     targetWireComponent == null || !targetWireComponent.GameObject.IsValid() )
			{
				Log.Warning( $"Wire components not found on constructs for connection restoration" );
				continue;
			}

			var inputId = !string.IsNullOrWhiteSpace( dupeWireConnection.InputName )
				? dupeWireConnection.InputName.ToLowerInvariant().Replace( " ", "_" )
				: dupeWireConnection.InputId;
			var outputId = !string.IsNullOrWhiteSpace( dupeWireConnection.OutputName )
				? dupeWireConnection.OutputName.ToLowerInvariant().Replace( " ", "_" )
				: dupeWireConnection.OutputId;

			// Validate that the ports exist
			var sourceHasOutput = sourceWireComponent.GetOutputPorts().Any( p => p.Id == outputId );
			var targetHasInput = targetWireComponent.GetInputPorts().Any( p => p.Id == inputId );

			if ( !sourceHasOutput || !targetHasInput )
			{
				Log.Warning( $"Port mismatch during wire connection restoration: {outputId} -> {inputId}" );
				continue;
			}

			if ( dupeWireConnection.Anchors?.Length > 100 )
			{
				Log.Warning( $"Too many anchors, skipping" );
				continue;
			}

			// Transform anchors based on rotation and position offsets
			IEnumerable<Vector3>? transformedAnchors = null;
			if ( dupeWireConnection.Anchors != null && dupeWireConnection.Anchors.Length > 0 )
			{
				var baseRotation = Rotation.FromAxis( Vector3.Up, rotationOffset ); // Use the actual rotation offset
				transformedAnchors = dupeWireConnection.Anchors.Select( anchor =>
				{
					var relativeAnchor = anchor - referencePoint;
					var rotatedAnchor = baseRotation * relativeAnchor;
					var offsetAnchor = rotatedAnchor + baseRotation * axisOffset;
					return (spawnPosition ?? referencePoint) + offsetAnchor;
				} );
			}

			// Restore the connection using the wire system
			Wire.Wire.Current.Connect(
				sourceWireComponent,
				outputId,
				targetWireComponent,
				inputId,
				transformedAnchors,
				dupeWireConnection.Color,
				dupeWireConnection.Thickness,
				dupeWireConnection.Opacity
			);

			// Small delay between connections to prevent overload
			if ( delayBetweenWires > 0 )
			{
				await GameTask.Delay( delayBetweenWires );
			}
		}
	}

	private bool TryBeginDupeSpawn( long steamId, out CancellationTokenSource cancellationTokenSource )
	{
		if ( _activeDupeSpawns.TryGetValue( steamId, out var activeSpawn ) && !activeSpawn.IsCancellationRequested )
		{
			cancellationTokenSource = activeSpawn;
			return false;
		}

		cancellationTokenSource = new CancellationTokenSource();
		_activeDupeSpawns[steamId] = cancellationTokenSource;
		return true;
	}

	private bool TryCancelDupeSpawn( long steamId )
	{
		if ( !_activeDupeSpawns.TryGetValue( steamId, out var activeSpawn ) || activeSpawn.IsCancellationRequested )
		{
			return false;
		}

		activeSpawn.Cancel();
		return true;
	}

	public void OnPlayerDisconnectHost( long steamId )
	{
		TryCancelDupeSpawn( steamId );
	}

	private void EndDupeSpawn( long steamId, CancellationTokenSource? cancellationTokenSource )
	{
		if ( cancellationTokenSource == null )
		{
			return;
		}

		if ( _activeDupeSpawns.TryGetValue( steamId, out var activeSpawn ) && ReferenceEquals( activeSpawn, cancellationTokenSource ) )
		{
			_activeDupeSpawns.Remove( steamId );
		}

		cancellationTokenSource.Dispose();
	}

	private static void RollbackDupeSpawn( IEnumerable<IConstruct> spawnedItems )
	{
		foreach ( var construct in spawnedItems.Reverse() )
		{
			if ( !construct.IsValid() )
			{
				continue;
			}

			construct.Destroy();
		}
	}

}
