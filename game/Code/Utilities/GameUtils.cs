using Dxura.RP.Shared;

namespace Dxura.RP.Game;

/// <summary>
///     A list of game utilities that'll help us achieve common goals with less code... I guess?
/// </summary>
public static class GameUtils
{
	/// <summary>
	///     Every Player currently in the world.
	/// </summary>
	public static IEnumerable<Player> Players =>
		GameNetworkManager.Instance.Players.Values;

	/// <summary>
	///     Get Player currently in the world, on the given job.
	/// </summary>
	public static IEnumerable<Player> GetPlayersByJob( GameModeJobDto job )
	{
		return Players.Where( x => x.Job == job );
	}

	public static IEnumerable<Player> GetPlayersByJobTag( JobTag tag )
	{
		return Players.Where( x => x.Job.HasTag( tag ) );
	}

	/// <summary>
	///     Get Player currently in the world, by given steamId.
	/// </summary>
	public static Player? GetPlayerById( long steamId )
	{
		return GameNetworkManager.Instance.Players.GetValueOrDefault( steamId );
	}

	/// <summary>
	///     Get Player currently in the world, with the given connection.
	/// </summary>
	public static Player? GetPlayerByConnectionId( Guid id )
	{
		return GameNetworkManager.Instance.PlayersByConnectionIdCache.GetValueOrDefault( id );
	}

	public static IDescription? GetDescription( GameObject go )
	{
		return go.IsValid() ? go.Components.Get<IDescription?>( FindMode.EverythingInSelfAndDescendants ) : null;
	}

	public static IDescription? GetDescription( Component? component )
	{
		return component == null ? null : GetDescription( component.GameObject );
	}

	/// <summary>
	///     Get a player from a component that belongs to a player or their descendants.
	/// </summary>
	public static Player? GetPlayerFromComponent( Component component )
	{
		if ( component is Player player )
		{
			return player;
		}

		if ( !component.IsValid() )
		{
			return null;
		}

		return !component.GameObject.IsValid()
			? null
			: component.GameObject.Root.Components.Get<Player>( FindMode.EnabledInSelfAndDescendants );
	}

	/// <summary>
	///     Get a player from a component that belongs to a player or their descendants.
	/// </summary>
	public static Player? GetPlayer( Component component )
	{
		if ( component is Player player )
		{
			return player;
		}

		if ( !component.IsValid() )
		{
			return null;
		}

		return !component.GameObject.IsValid()
			? null
			: component.GameObject.Root.Components.Get<Player>( FindMode.EnabledInSelfAndDescendants );
	}

	public static int GetActivePlayerCount()
	{
		return Players.Count( p => p.IsValid() && p.IsConnected && !p.HasStatus( Constants.AfkStatus ) );
	}

	public static Equipment? FindEquipment( Component inflictor )
	{
		if ( inflictor is Equipment equipment )
		{
			return equipment;
		}

		return null;
	}

	public static List<Player> GetPlayersByName( string name )
	{
		var allPlayers = Players.Where( p => p.IsValid() ).ToList();

		// Exact match first
		var exactMatches = allPlayers.Where( p =>
				string.Equals( p.DisplayName, name, StringComparison.OrdinalIgnoreCase ) )
			.ToList();

		if ( exactMatches.Count != 0 )
		{
			return exactMatches;
		}

		// Partial match - players whose names start with the target name
		var partialMatches = allPlayers.Where( p =>
				p.DisplayName.StartsWith( name, StringComparison.OrdinalIgnoreCase ) )
			.ToList();

		return partialMatches;
	}

	public static bool HasPermission( Connection connection, GameObject go, bool checkDistance = true )
	{
		var player = GetPlayerByConnectionId( connection.Id );

		return player.IsValid() && HasPermission( player.SteamId, go, checkDistance );
	}

	public static bool HasPermission( long steamId, GameObject gameObject, bool checkDistance = true )
	{
		var player = GetPlayerById( steamId );

		// Basic validation
		if ( !player.IsValid() || !gameObject.IsValid() )
		{
			return false;
		}

		// Distance check
		if ( checkDistance && !IsWithinReach( player, gameObject ) )
		{
			return false;
		}

		// Public entities (garbage, money, etc.) - always accessible
		if ( gameObject.Tags.Has( Constants.EntityTag ) && !gameObject.Tags.Has( Constants.RestrictedEntity ) )
		{
			return true;
		}

		// Check ownership permissions
		return HasOwnershipPermission( steamId, gameObject );
	}

	private static bool IsWithinReach( Player player, GameObject gameObject )
	{
		var maxDistance = Config.Current.Game.ReachDistance * Config.Current.Game.ReachDistance * 1.25f;

		// Quick distance check first
		if ( player.WorldPosition.DistanceSquared( gameObject.WorldPosition ) <= maxDistance )
		{
			return true;
		}

		// Fallback: check distance to object bounds
		var modelRenderer = gameObject.GetComponent<ModelRenderer>();
		if ( !modelRenderer.IsValid() || !modelRenderer.Model.IsValid() )
		{
			return false;
		}

		var closestPoint = modelRenderer.Bounds.ClosestPoint( player.WorldPosition );

		return player.WorldPosition.DistanceSquared( closestPoint ) < maxDistance;
	}

	private static bool HasOwnershipPermission( long steamId, GameObject gameObject )
	{
		// Handle doors specifically
		var door = gameObject.GetComponent<Door>();
		if ( door != null )
		{
			// Player owns the door
			if ( door.Owner == steamId )
			{
				return true;
			}

			// Check friend permissions for owned doors
			if ( FriendSystem.Instance.IsValid() )
			{
				return FriendSystem.Instance.HasDoorPermission( door.Owner, steamId );
			}

			return false; // No owner = no access
		}

		// Handle other owned objects (constructs, entities)
		if ( gameObject.Tags.HasAny( Constants.ConstructTag, Constants.EntityTag ) )
		{
			var owned = gameObject.GetComponent<IOwned>();
			if ( owned != null )
			{
				// Permanent objects (owner 0) can be interacted with by anyone with the Permanent permission
				if ( owned.Owner == 0 )
				{
					return RankSystem.HasPermission( steamId, Permission.Permanent );
				}

				if ( FriendSystem.Instance.IsValid() )
				{
					// Restricted entities use door permissions; constructs use construct permissions
					if ( gameObject.Tags.Has( Constants.EntityTag ) )
					{
						return FriendSystem.Instance.HasDoorPermission( owned.Owner, steamId );
					}

					return FriendSystem.Instance.HasConstructPermission( owned.Owner, steamId );
				}
			}
		}

		return false;
	}

	/// <summary>
	///     Calculates the spawn position for a game object spawn based on the player's aim ray.
	/// </summary>
	/// <param name="playerRay"></param>
	/// <param name="bufferDistance">Distance from surface hit</param>
	/// <returns></returns>
	public static Vector3 GetSpawnPosition( Ray playerRay, int? bufferDistance = null )
	{
		var trace = Sandbox.Game.ActiveScene.Trace.Ray( playerRay, Config.Current.Game.ReachDistance )
			.WithoutTags( Constants.RagdollTag, "movement", Constants.PlayerTag )
			.UseHitboxes()
			.Run();

		return trace.EndPosition + trace.Normal * (bufferDistance ?? 30);
	}
}
