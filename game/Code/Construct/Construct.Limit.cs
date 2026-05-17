using Dxura.RP.Shared;
using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

/// <summary>
/// The limit for constructs in DXRP
/// </summary>
public partial class Construct
{
	private readonly Dictionary<long, Dictionary<ConstructType, int>> _ownerToTypeCounts = new();

	private int GetCurrentCount( ConstructType type, long owner )
	{
		return !_ownerToTypeCounts.TryGetValue( owner, out var map ) ? 0 : map.GetValueOrDefault( type, 0 );
	}

	public uint GetLimit( ConstructType type, long owner )
	{
		// Prefer definition-provided limit if available
		var baseLimit = _definitions.TryGetValue( type, out var def ) ? def.Limit : type switch
		{
			ConstructType.Prop => Config.Current.Game.PropLimit,
			ConstructType.Text => Config.Current.Game.TextLimit,
			ConstructType.Frame => Config.Current.Game.FrameLimit,
			_ => 0
		};

		// Rank modifiers (mirror previous behavior)
		var player = GameUtils.GetPlayerById( owner );
		if ( !player.IsValid() )
		{
			return baseLimit;
		}

		if ( RankSystem.HasPermission( player.SteamId, Permission.BuildUnlimited ) )
		{
			return 99999999;
		}

		// Level modifiers
		var levelMultiplier = (uint)Math.Max( 1, player.Level * 2 );
		baseLimit *= levelMultiplier;

		return baseLimit;
	}

	private bool HasLimit( ConstructType type, long owner, int amount = 1 )
	{
		Assert.True( Networking.IsHost );

		var current = GetCurrentCount( type, owner );
		var limit = GetLimit( type, owner );
		return current + amount <= limit;
	}

	private void IncrementCount( long owner, ConstructType type )
	{
		Assert.True( Networking.IsHost );

		if ( !_ownerToTypeCounts.TryGetValue( owner, out var map ) )
		{
			map = new Dictionary<ConstructType, int>();
			_ownerToTypeCounts[owner] = map;
		}
		map[type] = map.GetValueOrDefault( type, 0 ) + 1;
	}

	public void DecrementCount( long owner, ConstructType type )
	{
		if ( !Networking.IsHost )
		{
			return;
		}
		if ( !_ownerToTypeCounts.TryGetValue( owner, out var map ) )
		{
			return;
		}
		if ( !map.TryGetValue( type, out var value ) )
		{
			return;
		}
		if ( value <= 1 )
		{
			map.Remove( type );
			if ( map.Count == 0 )
			{
				_ownerToTypeCounts.Remove( owner );
			}
		}
		else
		{
			map[type] = value - 1;
		}
	}
}
