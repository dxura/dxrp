using Dxura.RP.Shared;
using Sandbox.Diagnostics;

namespace Dxura.RP.Game;

public class RankSystem : SingletonComponent<RankSystem>
{
	/// <summary>
	/// Rank definitions: RankId -> definition
	/// </summary>
	[Sync( SyncFlags.FromHost )] private NetDictionary<Guid, RankDto> Ranks { get; set; } = new();

	/// <summary>
	/// Player assignments: SteamId -> rank ID list
	/// </summary>
	[Sync( SyncFlags.FromHost )] private NetDictionary<long, List<Guid>> RankAssignments { get; set; } = new();

	protected override void OnStart()
	{
		// Set local player to owner if host and no server authorization key is provided.
		if ( !ServerApiLink.HasAuthorizationKey && Networking.IsHost && !Application.IsDedicatedServer )
		{
			var ownerRankId = Guid.NewGuid();
			Ranks[ownerRankId] = new RankDto
			{
				Id = ownerRankId,
				Name = "Owner",
				Color = 0xE74C3C,
				Order = 100,
				Permissions = ["*"]
			};
			RankAssignments[Sandbox.Game.SteamId] = [ownerRankId];
		}
	}

	/// <summary>
	/// Sets all rank definitions from the server (used by pulse and snapshot actions)
	/// </summary>
	public void SetRanks( IEnumerable<RankDto> definitions )
	{
		Assert.True( Networking.IsHost );

		Ranks.Clear();
		foreach ( var def in definitions )
		{
			Ranks[def.Id] = def;
		}

	}

	/// <summary>
	/// Sets all rank assignments from the pulse/init response.
	/// </summary>
	public void SetRankAssignments( IEnumerable<RankAssignmentDto> rankAssignments )
	{
		Assert.True( Networking.IsHost );

		RankAssignments.Clear();
		foreach ( var assignment in rankAssignments )
		{
			if ( assignment.RankIds.Count > 0 )
				RankAssignments[assignment.PlayerId] = assignment.RankIds;
		}
	}

	/// <summary>
	/// Sets a player's full rank list (used by rank assignment action).
	/// </summary>
	public void SetPlayerRanks( long playerId, List<Guid> rankIds )
	{
		Assert.True( Networking.IsHost );

		if ( rankIds.Count > 0 )
			RankAssignments[playerId] = rankIds;
		else
			RankAssignments.Remove( playerId );
	}

	/// <summary>
	/// Returns all rank IDs assigned to a player.
	/// </summary>
	public List<Guid> GetPlayerRankIds( long steamId )
	{
		if ( !RankAssignments.TryGetValue( steamId, out var rankIds ) )
			return [];

		return rankIds;
	}

	/// <summary>
	/// Returns the display rank for a player — the highest-order rank they hold.
	/// Falls back to the default rank if they have none.
	/// </summary>
	public RankDto? GetPlayerRank( long steamId )
	{
		var rankIds = GetPlayerRankIds( steamId );
		if ( rankIds.Count == 0 )
			return GetDefaultRank();

		RankDto? best = null;
		foreach ( var id in rankIds )
		{
			if ( !Ranks.TryGetValue( id, out var rank ) ) continue;
			if ( best == null || rank.Order > best.Order )
				best = rank;
		}
		return best ?? GetDefaultRank();
	}

	/// <summary>
	/// Returns the default rank (the fallback rank when a player has no assigned ranks).
	/// </summary>
	private RankDto? GetDefaultRank()
	{
		foreach ( var rank in Ranks.Values )
		{
			if ( rank.IsDefault )
				return rank;
		}
		return null;
	}

	/// <summary>
	/// Returns the display name of the player's highest-order rank.
	/// </summary>
	public string GetRankName( long steamId ) => GetPlayerRank( steamId )?.Name ?? "";

	/// <summary>
	/// Returns the color of the player's highest-order rank.
	/// </summary>
	public uint GetRankColor( long steamId ) => GetPlayerRank( steamId )?.Color ?? 0xFFFFFF;

	/// <summary>
	/// Returns the order value of the player's highest-order rank.
	/// </summary>
	public int GetRankOrder( long steamId ) => GetPlayerRank( steamId )?.Order ?? 0;

	// --- Permission-based API ---

	/// <summary>
	/// Returns true if <paramref name="pattern"/> matches <paramref name="permission"/>.
	/// Supports exact match, wildcard "*", and prefix "category.*".
	/// </summary>
	private static bool PatternMatches( string pattern, string permission )
	{
		if ( pattern == "*" ) return true;

		if ( pattern.EndsWith( ".*", StringComparison.OrdinalIgnoreCase ) )
		{
			var prefix = pattern[..^1];
			return permission.StartsWith( prefix, StringComparison.OrdinalIgnoreCase );
		}

		return string.Equals( pattern, permission, StringComparison.OrdinalIgnoreCase );
	}

	/// <summary>
	/// Returns true if the given rank's resolved permission list grants <paramref name="permission"/>.
	/// Deny entries (prefixed with '!') take precedence within the same rank.
	/// </summary>
	private static bool CheckRankPermission( RankDto rank, string permission )
	{
		var perms = rank.Permissions;

		if ( perms.Contains( "!" + permission, StringComparer.OrdinalIgnoreCase ) ) return false;
		if ( perms.Contains( permission, StringComparer.OrdinalIgnoreCase ) ) return true;

		foreach ( var p in perms )
		{
			if ( p.StartsWith( '!' ) && PatternMatches( p[1..], permission ) ) return false;
		}

		foreach ( var p in perms )
		{
			if ( !p.StartsWith( '!' ) && PatternMatches( p, permission ) ) return true;
		}

		return false;
	}

	/// <summary>
	/// Returns true if any of the player's assigned ranks grants the permission.
	/// Falls back to the default rank if the player has no assigned ranks.
	/// </summary>
	public static bool HasPermission( long steamId, string permission )
	{
		if ( !Instance.IsValid() ) return false;

		var rankIds = Instance.GetPlayerRankIds( steamId );
		if ( rankIds.Count == 0 )
		{
			var defaultRank = Instance.GetDefaultRank();
			return defaultRank != null && CheckRankPermission( defaultRank, permission );
		}

		foreach ( var id in rankIds )
		{
			if ( Instance.Ranks.TryGetValue( id, out var rank ) && CheckRankPermission( rank, permission ) )
				return true;
		}

		return false;
	}

	/// <summary>
	/// Returns true if any of the player's assigned ranks grants the permission.
	/// </summary>
	public static bool HasPermission( long steamId, Permission permission )
		=> HasPermission( steamId, permission.ToId() );

	/// <summary>
	/// Returns true if the local player's ranks grant the permission.
	/// </summary>
	public static bool HasLocalPermission( string permission )
		=> HasPermission( Sandbox.Game.SteamId, permission );

	/// <summary>
	/// Returns true if the local player's ranks grant the permission.
	/// </summary>
	public static bool HasLocalPermission( Permission permission )
		=> HasPermission( Sandbox.Game.SteamId, permission );

	/// <summary>
	/// Returns true if the player holds the specified rank.
	/// </summary>
	public static bool HasRank( long steamId, Guid rankId )
	{
		if ( !Instance.IsValid() ) return false;
		return Instance.GetPlayerRankIds( steamId ).Contains( rankId );
	}

	/// <summary>
	/// Returns true if the player's highest-order rank meets or exceeds <paramref name="minimumOrder"/>.
	/// </summary>
	public static bool IsRankOrAbove( long steamId, int minimumOrder )
	{
		if ( !Instance.IsValid() ) return false;
		return Instance.GetRankOrder( steamId ) >= minimumOrder;
	}

	/// <summary>
	/// Returns true if the caller's rank order is greater than or equal to the target's.
	/// Always returns true when targeting yourself.
	/// </summary>
	public static bool CanTarget( long callerSteamId, long targetSteamId )
	{
		if ( !Instance.IsValid() ) return false;
		if ( callerSteamId == targetSteamId ) return true;
		return Instance.GetRankOrder( callerSteamId ) >= Instance.GetRankOrder( targetSteamId );
	}

	/// <summary>
	/// Returns true if the local player can target <paramref name="targetSteamId"/>.
	/// </summary>
	public static bool CanLocalTarget( long targetSteamId )
		=> CanTarget( Sandbox.Game.SteamId, targetSteamId );

	/// <summary>
	/// Returns true if the player has at least one explicitly assigned rank.
	/// </summary>
	public static bool HasAnyRank( long steamId )
	{
		if ( !Instance.IsValid() ) return false;
		return Instance.RankAssignments.TryGetValue( steamId, out var ids ) && ids.Count > 0;
	}

	public static bool IsRankWhitelisted( long steamId, IReadOnlyCollection<Guid> whitelistRankIds )
	{
		if ( whitelistRankIds.Count == 0 )
			return true;

		if ( !Instance.IsValid() )
			return false;

		var rank = Instance.GetPlayerRank( steamId );
		return rank != null && whitelistRankIds.Contains( rank.Id );
	}
}
