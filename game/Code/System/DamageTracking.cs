namespace Dxura.RP.Game;

public class DamageTrackerSystem : Component, IDamageEvents, IGameEvents
{
	[Property] public bool ClearOnRespawn { get; set; } = false;

	public Dictionary<Player, List<DamageInfo>> Registry { get; set; } = new();
	public Dictionary<Player, List<DamageInfo>> MyInflictedDamage { get; set; } = new();

	void IDamageEvents.OnDamageTakenHost( Component victim, DamageInfo damageInfo )
	{
		var player = victim as Player;
		if ( !player.IsValid() )
		{
			return;
		}

		// Track damage for the victim
		if ( !Registry.TryGetValue( player, out var list ) )
		{
			Registry.Add( player, new List<DamageInfo>
			{
				damageInfo
			} );
		}
		else
		{
			list.Add( damageInfo );
		}

		// Track damage dealt by local player
		var attackerPlayer = damageInfo.Attacker as Player;
		if ( attackerPlayer == Player.Local )
		{
			if ( !MyInflictedDamage.TryGetValue( player, out var myList ) )
			{
				MyInflictedDamage.Add( player, new List<DamageInfo>
				{
					damageInfo
				} );
			}
			else
			{
				myList.Add( damageInfo );
			}
		}
	}

	public void OnPlayerSpawnedHost( Player player )
	{
		if ( !ClearOnRespawn )
		{
			return;
		}

		// Only include the owner
		using ( Rpc.FilterInclude( player.Network.Owner ) )
		{
			// Send the refresh
			BroadcastRefresh();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastRefresh()
	{
		Refresh();
	}

	public List<DamageInfo> GetDamageOnMe()
	{
		return GetDamageInflictedTo( Player.Local );
	}

	public List<DamageInfo> GetDamageInflictedTo( Player player )
	{
		if ( !Registry.TryGetValue( player, out var list ) )
		{
			return new List<DamageInfo>();
		}

		return list;
	}

	public List<DamageInfo> GetMyInflictedDamage( Player player )
	{
		if ( !MyInflictedDamage.TryGetValue( player, out var list ) )
		{
			return new List<DamageInfo>();
		}

		return list;
	}

	public List<GroupedDamage> GetGroupedDamage( Player player )
	{
		return GetDamageInflictedTo( player )
			.GroupBy( x => x.Attacker )
			.Select( group => new GroupedDamage
			{
				Attacker = group.First().Attacker is Player attackerPlayer ? attackerPlayer : null, Count = group.Count(), Damage = group.Sum( x => x.Damage )
			} )
			.ToList();
	}

	public List<GroupedDamage> GetGroupedInflictedDamage( Player player )
	{
		return GetMyInflictedDamage( player )
			.GroupBy( x => x.Attacker )
			.Select( group => new GroupedDamage
			{
				Attacker = group.First().Attacker is Player attackerPlayer ? attackerPlayer : null, Count = group.Count(), Damage = group.Sum( x => x.Damage )
			} )
			.ToList();
	}

	public void Refresh()
	{
		MyInflictedDamage.Clear();
		Registry.Clear();
	}

	public struct GroupedDamage
	{
		public Player? Attacker { get; set; }
		public int Count { get; set; }
		public float Damage { get; set; }
	}
}
