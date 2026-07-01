namespace Dxura.RP.Game;

/// <summary>
/// A single party member entry for display. <see cref="Id"/> is the member's Steam ID, matching
/// the existing player-list group-icon check (<c>PartyRoom.Current?.Members.Any( x => x.Id == ... )</c>).
/// </summary>
public readonly record struct PartyMember( long Id, string Name, bool IsLeader );

/// <summary>
/// A read-only, client-facing snapshot of a party, built from the host-synced <see cref="PartySystem"/>
/// state. This is display/helper state only — the authority always lives in <see cref="PartySystem"/>.
/// </summary>
public sealed class PartyRoom
{
	public Guid Id { get; init; }
	public long LeaderSteamId { get; init; }
	public IReadOnlyList<PartyMember> Members { get; init; } = Array.Empty<PartyMember>();

	public int Count => Members.Count;
	public bool IsLeader( long steamId ) => steamId == LeaderSteamId;

	/// <summary>The local player's current party, or <c>null</c> if they are not in one.</summary>
	public static PartyRoom? Current
	{
		get
		{
			var system = PartySystem.Instance;
			if ( system is null )
			{
				return null;
			}

			var local = Player.Local;
			if ( !local.IsValid() )
			{
				return null;
			}

			var partyId = system.GetPartyId( local.SteamId );
			return partyId.HasValue ? system.GetRoomView( partyId.Value ) : null;
		}
	}
}
