using System.Threading.Tasks;
using Dxura.RP.Shared;

namespace Dxura.RP.Game;

/// <summary>
/// Operator-tunable settings for the Party system. Exposed as a "System" content type
/// config override (e.g. MaxPartySize / PreventPartyDamage) in the web portal.
/// </summary>
public class PartySystemConfig
{
	/// <summary>Maximum members allowed in a single party, leader included.</summary>
	public int MaxPartySize { get; init; } = 4;

	/// <summary>When true, party members cannot damage each other (friendly fire bypass).</summary>
	public bool PreventPartyDamage { get; init; } = true;

	/// <summary>Seconds a pending /party invite stays valid before it auto-expires. Mirrors CoinFlipDuration (30s).</summary>
	public int InviteExpireSeconds { get; init; } = 30;

	/// <summary>
	/// When true, party members can see each other through geometry via <see cref="HighlightOutline"/>
	/// (requires the camera's <see cref="Highlight"/> post-process). Operators can disable server-wide.
	/// </summary>
	public bool AllowMemberOutline { get; init; } = true;
}

/// <summary>
/// All replicated state for a single party, keyed by its party id. Holding everything in one type
/// lets the whole system replicate through a single <see cref="SyncFlags.FromHost"/> dictionary
/// (<see cref="PartySystem.Parties"/>) instead of several parallel maps.
///
/// This is a reference type (class) rather than a struct on purpose: its nested <see cref="Members"/> /
/// <see cref="Invites"/> lists only round-trip to clients when the dictionary value is a class —
/// the same shape DXRP's <c>RankSystem</c> uses (<c>NetDictionary&lt;Guid, RankDto&gt;</c>).
/// </summary>
public class PartyData
{
	/// <summary>Steam id of the current party leader.</summary>
	public long Leader { get; set; }

	/// <summary>Member steam ids in join order. Index 0 is the oldest member (used for leader transfer).</summary>
	public List<long> Members { get; set; } = new();

	/// <summary>Steam ids with a pending invite to this party.</summary>
	public List<long> Invites { get; set; } = new();

	/// <summary>Packed 0xRRGGBB highlight color (leader-customizable; drives nameplate + future party HUD).</summary>
	public uint Color { get; set; }

	/// <summary>
	/// When true, members of this party see each other's silhouettes through geometry using the party color.
	/// Leader-controlled party option; still gated by <see cref="PartySystemConfig.AllowMemberOutline"/>.
	/// </summary>
	public bool MemberOutlineEnabled { get; set; } = true;
}

/// <summary>
/// Standalone, host-authoritative, session-only party system. Parties are on-the-fly squads
/// and are intentionally separate from the persistent <see cref="FactionSystem"/>: nothing here
/// reads, writes, or depends on factions.
///
/// All party state lives in a single <see cref="SyncFlags.FromHost"/> map of partyId → <see cref="PartyData"/>.
/// Clients read this synced state; only the host mutates it. Every host mutation reads the entry, mutates
/// it, and writes it back into <see cref="Parties"/> so the dictionary marks the key dirty and replicates
/// (mirrors how <c>RankSystem</c> reassigns <c>Ranks[id] = def</c>).
/// </summary>
public sealed class PartySystem : SingletonComponent<PartySystem>, Component.INetworkListener
{
	/// <summary>
	/// Runtime config. Populated with defaults today; portal "System" config-override plumbing
	/// is wired separately. Read-only at runtime.
	/// </summary>
	public PartySystemConfig Settings { get; private set; } = new();

	/// <summary>
	/// Default party highlight color (packed 0xRRGGBB) until the leader customizes it. Green mirrors
	/// the reference "party halo" color and reads clearly as a friendly/ally marker.
	/// </summary>
	public const uint DefaultPartyColor = 0x2ECC40;

	// ── Synced authoritative state (host → clients) ───────────────────────────────────────────
	// partyId → all data for that party (members, leader, invites, color)
	[Sync( SyncFlags.FromHost )] public NetDictionary<Guid, PartyData> Parties { get; set; } = new();

	// Host-only: targetSteamId → token of their current pending invite. A delayed expiry timer carries the
	// token it was scheduled with and only fires if it still matches, so a re-invite (or accept/clear)
	// supersedes the old timer. Purely host-side scheduling state — never synced.
	private readonly Dictionary<long, Guid> _inviteTokens = new();

	// ── Read helpers (synced data; safe on host and client) ───────────────────────────────────
	public bool IsInParty( long steamId ) => GetPartyId( steamId ).HasValue;

	public Guid? GetPartyId( long steamId )
	{
		foreach ( var kv in Parties )
		{
			if ( kv.Value.Members is { } members && members.Contains( steamId ) )
			{
				return kv.Key;
			}
		}

		return null;
	}

	public IEnumerable<long> GetMembers( Guid partyId ) =>
		Parties.TryGetValue( partyId, out var data ) && data.Members is { } members
			? members
			: Enumerable.Empty<long>();

	public int GetPartySize( Guid partyId ) =>
		Parties.TryGetValue( partyId, out var data ) && data.Members is { } members ? members.Count : 0;

	public long GetLeader( Guid partyId ) =>
		Parties.TryGetValue( partyId, out var data ) ? data.Leader : 0;

	public bool IsLeader( long steamId )
	{
		var partyId = GetPartyId( steamId );
		return partyId.HasValue && GetLeader( partyId.Value ) == steamId;
	}

	public bool AreInSameParty( long a, long b )
	{
		var pa = GetPartyId( a );
		var pb = GetPartyId( b );
		return pa.HasValue && pb.HasValue && pa.Value == pb.Value;
	}

	/// <summary>Highlight color (packed 0xRRGGBB) for a party, falling back to <see cref="DefaultPartyColor"/>.</summary>
	public uint GetPartyColor( Guid partyId ) =>
		Parties.TryGetValue( partyId, out var data ) ? data.Color : DefaultPartyColor;

	/// <summary>Highlight color for the party a player belongs to (default color if they are partyless).</summary>
	public uint GetPartyColorForMember( long steamId )
	{
		var partyId = GetPartyId( steamId );
		return partyId.HasValue ? GetPartyColor( partyId.Value ) : DefaultPartyColor;
	}

	/// <summary>
	/// Whether <paramref name="partyId"/> should render member outlines for its roster (system + party option).
	/// </summary>
	public bool IsMemberOutlineEnabled( Guid partyId ) =>
		Settings.AllowMemberOutline
		&& Parties.TryGetValue( partyId, out var data )
		&& data.MemberOutlineEnabled;

	/// <summary>
	/// Builds a client-readable view of a party for UI/HUD. This is display state only —
	/// <see cref="PartySystem"/> remains the single authority.
	/// </summary>
	public PartyRoom? GetRoomView( Guid partyId )
	{
		if ( !Parties.TryGetValue( partyId, out var data ) || data.Members is null )
		{
			return null;
		}

		var members = data.Members
			.Select( id =>
			{
				var player = GameUtils.GetPlayerById( id );
				var name = player.IsValid() ? player.DisplayName : id.ToString();
				return new PartyMember( id, name, id == data.Leader );
			} )
			.ToList();

		return new PartyRoom { Id = partyId, LeaderSteamId = data.Leader, Members = members };
	}

	/// <summary>
	/// Client-readable list of parties that currently hold a pending invite out to <paramref name="steamId"/>.
	/// Drives the /party menu "Join Party" tab (our invite-based equivalent of "request to join").
	/// </summary>
	public IEnumerable<PartyRoom> GetInvitesFor( long steamId )
	{
		foreach ( var kv in Parties )
		{
			if ( kv.Value.Invites is { } invites && invites.Contains( steamId ) )
			{
				var room = GetRoomView( kv.Key );
				if ( room is not null )
				{
					yield return room;
				}
			}
		}
	}

	// ── Host mutations (invoked from PartyCommand.ExecuteHost, which already runs on the host) ──

	/// <summary>Invite <paramref name="target"/> to the caller's party, auto-creating one if needed.</summary>
	public void HostInvite( Player caller, Player target )
	{
		if ( !Networking.IsHost || caller is null || target is null )
		{
			return;
		}

		if ( caller == target )
		{
			caller.Error( Language.GetPhrase( "party.invite_self_error" ) );
			return;
		}

		if ( IsInParty( target.SteamId ) )
		{
			caller.Error( string.Format( Language.GetPhrase( "party.target_in_party" ), target.DisplayName ) );
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		if ( !partyId.HasValue )
		{
			// A new party starts with 1 member; if MaxPartySize is already at capacity, bail before creating
			// so the caller isn't orphaned as the leader of a party they can never invite anyone into.
			if ( Settings.MaxPartySize <= 1 )
			{
				caller.Error( Language.GetPhrase( "party.party_full" ) );
				return;
			}

			partyId = CreatePartyForLeader( caller );
		}
		else
		{
			if ( !IsLeader( caller.SteamId ) )
			{
				caller.Error( Language.GetPhrase( "party.not_leader" ) );
				return;
			}

			if ( GetPartySize( partyId.Value ) >= Settings.MaxPartySize )
			{
				caller.Error( Language.GetPhrase( "party.party_full" ) );
				return;
			}
		}

		// One pending invite per player: clear any prior invite before recording this one.
		ClearInvite( target.SteamId );

		var data = Clone( Parties[partyId.Value] );
		data.Invites.Add( target.SteamId );
		Parties[partyId.Value] = data;

		// Invites are delivered as private, client-side party-chat lines (only the two players see them),
		// never a public chat broadcast — and auto-expire after Settings.InviteExpireSeconds.
		SendPartyChat( caller, string.Format( Language.GetPhrase( "party.invite_sent" ), target.DisplayName ) );
		SendPartyChat( target, string.Format( Language.GetPhrase( "party.invite_received" ), caller.DisplayName ) );

		if ( target.IsDebugPlayer )
		{
			HostAccept( target, partyId.Value );
			return;
		}

		ScheduleInviteExpiry( partyId.Value, target.SteamId );
	}

	/// <summary>Accept a pending invite for the caller. When <paramref name="specificPartyId"/> is provided
	/// (UI path), only accept that party's invite; otherwise search all parties (command path).</summary>
	public void HostAccept( Player caller, Guid? specificPartyId = null )
	{
		if ( !Networking.IsHost || caller is null )
		{
			return;
		}

		if ( IsInParty( caller.SteamId ) )
		{
			caller.Error( Language.GetPhrase( "party.already_in_party" ) );
			return;
		}

		// If a specific party was requested, honour it only when the invite actually exists there;
		// otherwise fall back to searching all parties (single-invite command path).
		var partyId = specificPartyId.HasValue &&
		              Parties.TryGetValue( specificPartyId.Value, out var pCheck ) &&
		              pCheck.Invites?.Contains( caller.SteamId ) == true
			? specificPartyId
			: FindInviteParty( caller.SteamId );

		if ( !partyId.HasValue )
		{
			caller.Error( Language.GetPhrase( "party.invite_none" ) );
			return;
		}

		if ( GetPartySize( partyId.Value ) >= Settings.MaxPartySize )
		{
			caller.Error( Language.GetPhrase( "party.party_full" ) );
			return;
		}

		_inviteTokens.Remove( caller.SteamId );

		var data = Clone( Parties[partyId.Value] );
		data.Invites.Remove( caller.SteamId );
		data.Members.Add( caller.SteamId );
		Parties[partyId.Value] = data;

		NotifyParty( partyId.Value, string.Format( Language.GetPhrase( "party.joined" ), caller.DisplayName ) );
	}

	/// <summary>Leader kicks <paramref name="target"/> from the party.</summary>
	public void HostKick( Player caller, Player target )
	{
		if ( !Networking.IsHost || caller is null || target is null )
		{
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		if ( !partyId.HasValue )
		{
			caller.Error( Language.GetPhrase( "party.no_party" ) );
			return;
		}

		if ( !IsLeader( caller.SteamId ) )
		{
			caller.Error( Language.GetPhrase( "party.not_leader" ) );
			return;
		}

		if ( caller == target )
		{
			caller.Error( Language.GetPhrase( "party.cannot_kick_self" ) );
			return;
		}

		if ( GetPartyId( target.SteamId ) != partyId )
		{
			caller.Error( string.Format( Language.GetPhrase( "party.target_not_found" ), target.DisplayName ) );
			return;
		}

		RemovePlayerInternal( target.SteamId );
		target.Warn( Language.GetPhrase( "party.kicked" ) );
		NotifyParty( partyId.Value, string.Format( Language.GetPhrase( "party.member_kicked" ), target.DisplayName ) );
	}

	/// <summary>Caller leaves their party (disbands it if they were the last member).</summary>
	public void HostLeave( Player caller )
	{
		if ( !Networking.IsHost || caller is null )
		{
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		if ( !partyId.HasValue )
		{
			caller.Error( Language.GetPhrase( "party.no_party" ) );
			return;
		}

		RemovePlayerInternal( caller.SteamId );
		caller.Info( Language.GetPhrase( "party.left" ) );

		// Notify whoever remains (no-op if the party disbanded).
		if ( Parties.ContainsKey( partyId.Value ) )
		{
			NotifyParty( partyId.Value, string.Format( Language.GetPhrase( "party.member_left" ), caller.DisplayName ) );
		}
	}

	/// <summary>Leader disbands the whole party.</summary>
	public void HostDisband( Player caller )
	{
		if ( !Networking.IsHost || caller is null )
		{
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		if ( !partyId.HasValue )
		{
			caller.Error( Language.GetPhrase( "party.no_party" ) );
			return;
		}

		if ( !IsLeader( caller.SteamId ) )
		{
			caller.Error( Language.GetPhrase( "party.not_leader" ) );
			return;
		}

		DisbandParty( partyId.Value );
	}

	/// <summary>Sends the caller a summary of their current party.</summary>
	public void HostInfo( Player caller )
	{
		if ( caller is null )
		{
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		var room = partyId.HasValue ? GetRoomView( partyId.Value ) : null;
		if ( room is null )
		{
			caller.SendMessage( Language.GetPhrase( "party.no_party" ) );
			return;
		}

		var names = string.Join( ", ", room.Members.Select( m =>
			m.IsLeader ? string.Format( Language.GetPhrase( "party.leader_tag" ), m.Name ) : m.Name ) );

		caller.SendMessage( string.Format( Language.GetPhrase( "party.info" ), room.Members.Count, Settings.MaxPartySize, names ) );
	}

	/// <summary>
	/// Leader sets the party's highlight color (packed 0xRRGGBB). This is the command-driven equivalent
	/// of the future party Settings color picker; both paths funnel through this single host mutation.
	/// </summary>
	public void HostSetColor( Player caller, uint color )
	{
		if ( !Networking.IsHost || caller is null )
		{
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		if ( !partyId.HasValue )
		{
			caller.Error( Language.GetPhrase( "party.no_party" ) );
			return;
		}

		if ( !IsLeader( caller.SteamId ) )
		{
			caller.Error( Language.GetPhrase( "party.not_leader" ) );
			return;
		}

		var data = Clone( Parties[partyId.Value] );
		data.Color = color;
		Parties[partyId.Value] = data;

		NotifyParty( partyId.Value, Language.GetPhrase( "party.color_changed" ) );
	}

	/// <summary>
	/// Leader toggles whether this party draws member silhouettes through geometry
	/// (<see cref="HighlightOutline.ObscuredColor"/> + camera <see cref="Highlight"/>).
	/// </summary>
	public void HostSetMemberOutline( Player caller, bool enabled )
	{
		if ( !Networking.IsHost || caller is null )
		{
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		if ( !partyId.HasValue )
		{
			caller.Error( Language.GetPhrase( "party.no_party" ) );
			return;
		}

		if ( !IsLeader( caller.SteamId ) )
		{
			caller.Error( Language.GetPhrase( "party.not_leader" ) );
			return;
		}

		if ( !Settings.AllowMemberOutline )
		{
			caller.Error( Language.GetPhrase( "party.outline_disabled_server" ) );
			return;
		}

		var data = Clone( Parties[partyId.Value] );
		if ( data.MemberOutlineEnabled == enabled )
		{
			return;
		}

		data.MemberOutlineEnabled = enabled;
		Parties[partyId.Value] = data;

		var phrase = enabled ? "party.outline_enabled" : "party.outline_disabled";
		NotifyParty( partyId.Value, Language.GetPhrase( phrase ) );
	}

	/// <summary>
	/// Leader hands leadership to <paramref name="target"/>, a fellow party member. This is the manual
	/// counterpart to the automatic oldest-member transfer in <see cref="RemovePlayerInternal"/> when a
	/// leader leaves; both just reassign <see cref="PartyData.Leader"/> and replicate.
	/// </summary>
	public void HostPromote( Player caller, Player target )
	{
		if ( !Networking.IsHost || caller is null || target is null )
		{
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		if ( !partyId.HasValue )
		{
			caller.Error( Language.GetPhrase( "party.no_party" ) );
			return;
		}

		if ( !IsLeader( caller.SteamId ) )
		{
			caller.Error( Language.GetPhrase( "party.not_leader" ) );
			return;
		}

		if ( caller == target )
		{
			caller.Error( Language.GetPhrase( "party.already_leader" ) );
			return;
		}

		if ( GetPartyId( target.SteamId ) != partyId )
		{
			caller.Error( string.Format( Language.GetPhrase( "party.target_not_found" ), target.DisplayName ) );
			return;
		}

		var data = Clone( Parties[partyId.Value] );
		data.Leader = target.SteamId;
		Parties[partyId.Value] = data;

		NotifyParty( partyId.Value, string.Format( Language.GetPhrase( "party.member_promoted" ), target.DisplayName ) );
	}

	/// <summary>
	/// Broadcasts <paramref name="message"/> to every member of the caller's party as a
	/// <see cref="MessageType.PartyChat"/> line. Shared by <c>/partychat</c> (<c>/pchat</c>) and the
	/// <c>/party &lt;message&gt;</c> shorthand so both run the same cooldown-checked, moderated,
	/// party-filtered path instead of duplicating it.
	/// </summary>
	public void HostSendPartyChat( Player caller, string message )
	{
		if ( !Networking.IsHost || !caller.IsValid() || Chat.Current is null )
		{
			return;
		}

		var partyId = GetPartyId( caller.SteamId );
		if ( !partyId.HasValue )
		{
			caller.SendMessage( Language.GetPhrase( "party.chat_not_in_party" ) );
			return;
		}

		if ( string.IsNullOrWhiteSpace( message ) )
		{
			caller.SendMessage( Language.GetPhrase( "party.chat_usage" ) );
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{caller.SteamId}:chat", Config.Current.Game.ChatCooldown ) )
		{
			caller.Error( "#generic.wait" );
			return;
		}

		message = message.Truncate( Config.Current.Game.ChatMaxLength );
		message = GameManager.ModerateText( caller.SteamId, $"CHAT {MessageType.PartyChat}", message, true );

		var members = GetMembers( partyId.Value ).ToHashSet();
		var partyConnections = GameUtils.Players
			.Where( p => p.IsValid() && members.Contains( p.SteamId ) )
			.Select( p => p.Connection )
			.ToHashSet();

		using ( Rpc.FilterInclude( c => partyConnections.Contains( c ) ) )
		{
			Chat.Current.BroadcastPlayerChat( Guid.NewGuid(), caller.ConnectionId, message, MessageType.PartyChat );
		}
	}

	// ── Client → host requests (invoked from the /party menu UI on the caller's client) ──────────
	// The menu can't run host mutations directly, so each button routes through one of these [Rpc.Host]
	// wrappers. They resolve the caller from the RPC and delegate to the same Host* mutation the
	// /party chat command uses — so menu and command share one authoritative, validated path.

	[Rpc.Host]
	public void RequestKick( long targetSteamId )
	{
		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		var target = GameUtils.GetPlayerById( targetSteamId );
		if ( caller.IsValid() && target.IsValid() )
		{
			HostKick( caller, target );
		}
	}

	[Rpc.Host]
	public void RequestPromote( long targetSteamId )
	{
		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		var target = GameUtils.GetPlayerById( targetSteamId );
		if ( caller.IsValid() && target.IsValid() )
		{
			HostPromote( caller, target );
		}
	}

	[Rpc.Host]
	public void RequestLeave()
	{
		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( caller.IsValid() )
		{
			HostLeave( caller );
		}
	}

	[Rpc.Host]
	public void RequestDisband()
	{
		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( caller.IsValid() )
		{
			HostDisband( caller );
		}
	}

	[Rpc.Host]
	public void RequestAccept( Guid partyId )
	{
		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( caller.IsValid() )
		{
			HostAccept( caller, partyId );
		}
	}

	[Rpc.Host]
	public void RequestInvite( string name )
	{
		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( !caller.IsValid() || string.IsNullOrWhiteSpace( name ) )
		{
			return;
		}

		// ResolvePlayer already messages the caller on a miss/ambiguous name.
		var target = CommandHelper.ResolvePlayer( caller, name );
		if ( target.IsValid() )
		{
			HostInvite( caller, target! );
		}
	}

	[Rpc.Host]
	public void RequestSetColor( uint color )
	{
		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( caller.IsValid() )
		{
			HostSetColor( caller, color );
		}
	}

	[Rpc.Host]
	public void RequestSetMemberOutline( bool enabled )
	{
		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( caller.IsValid() )
		{
			HostSetMemberOutline( caller, enabled );
		}
	}

	// ── Internal helpers (host-side) ────────────────────────────────────────────────────────

	/// <summary>
	/// Returns a deep copy of <paramref name="src"/> with fresh member/invite lists. Every host mutation
	/// works on a clone and assigns it back into <see cref="Parties"/> so the <see cref="NetDictionary{TKey,TValue}"/>
	/// sees a new value and replicates to clients. Mutating the stored instance in place and re-assigning the
	/// same reference would not mark the key dirty, so the change would never reach clients — this mirrors how
	/// <c>RankSystem</c> always assigns fresh <c>RankDto</c> objects.
	/// </summary>
	private static PartyData Clone( PartyData src ) => new()
	{
		Leader = src.Leader,
		Members = new List<long>( src.Members ),
		Invites = new List<long>( src.Invites ),
		Color = src.Color,
		MemberOutlineEnabled = src.MemberOutlineEnabled
	};

	private Guid CreatePartyForLeader( Player leader )
	{
		var partyId = Guid.NewGuid();
		Parties[partyId] = new PartyData
		{
			Leader = leader.SteamId,
			Members = new List<long> { leader.SteamId },
			Invites = new List<long>(),
			Color = DefaultPartyColor,
			MemberOutlineEnabled = Settings.AllowMemberOutline
		};
		SendPartyChat( leader, Language.GetPhrase( "party.created" ) );
		return partyId;
	}

	/// <summary>Returns the party that has a pending invite out to <paramref name="steamId"/>, if any.</summary>
	private Guid? FindInviteParty( long steamId )
	{
		foreach ( var kv in Parties )
		{
			if ( kv.Value.Invites is { } invites && invites.Contains( steamId ) )
			{
				return kv.Key;
			}
		}

		return null;
	}

	/// <summary>Clears any pending invite out to <paramref name="steamId"/> across all parties.</summary>
	private void ClearInvite( long steamId )
	{
		_inviteTokens.Remove( steamId );

		foreach ( var partyId in Parties.Keys.ToList() )
		{
			var existing = Parties[partyId];
			if ( existing.Invites is { } invites && invites.Contains( steamId ) )
			{
				var data = Clone( existing );
				data.Invites.Remove( steamId );
				Parties[partyId] = data;
			}
		}
	}

	/// <summary>
	/// Records a fresh expiry token for <paramref name="targetSteamId"/> and schedules the invite to
	/// auto-expire after <see cref="PartySystemConfig.InviteExpireSeconds"/>. Mirrors the /coinflip
	/// auto-cancel pattern (delay + token guard so a re-invite/accept supersedes an in-flight timer).
	/// </summary>
	private void ScheduleInviteExpiry( Guid partyId, long targetSteamId )
	{
		var token = Guid.NewGuid();
		_inviteTokens[targetSteamId] = token;
		_ = ExpireInviteAfter( partyId, targetSteamId, token, Settings.InviteExpireSeconds );
	}

	private async Task ExpireInviteAfter( Guid partyId, long targetSteamId, Guid token, int seconds )
	{
		await GameTask.DelayRealtimeSeconds( seconds );
		await GameTask.MainThread();

		// Bail if play stopped or we are no longer the host.
		if ( !this.IsValid() || !Networking.IsHost )
		{
			return;
		}

		// Superseded by a re-invite, accept, or clear — this timer is stale, do nothing.
		if ( !_inviteTokens.TryGetValue( targetSteamId, out var current ) || current != token )
		{
			return;
		}

		_inviteTokens.Remove( targetSteamId );

		// Only act if the invite is still pending in that party.
		if ( !Parties.TryGetValue( partyId, out var existing ) || existing.Invites is not { } invites || !invites.Contains( targetSteamId ) )
		{
			return;
		}

		var data = Clone( existing );
		data.Invites.Remove( targetSteamId );
		Parties[partyId] = data;

		var target = GameUtils.GetPlayerById( targetSteamId );
		if ( target.IsValid() )
		{
			SendPartyChat( target, Language.GetPhrase( "party.invite_expired" ) );
		}

		var leader = GameUtils.GetPlayerById( GetLeader( partyId ) );
		if ( leader.IsValid() )
		{
			var name = target.IsValid() ? target.DisplayName : targetSteamId.ToString();
			SendPartyChat( leader, string.Format( Language.GetPhrase( "party.invite_expired_leader" ), name ) );
		}
	}

	/// <summary>
	/// Sends one private, client-side party-chat line to a single player — only they see it, never public.
	/// Mirrors <c>MsgCommand</c>'s <c>Rpc.FilterInclude</c> delivery and is the foundation for the future
	/// /party chat channel (same <see cref="MessageType.PartyChat"/> styling).
	/// </summary>
	private static void SendPartyChat( Player recipient, string message )
	{
		if ( !recipient.IsValid() || Chat.Current is null )
		{
			return;
		}

		using ( Rpc.FilterInclude( c => c.Id == recipient.ConnectionId ) )
		{
			Chat.Current.BroadcastChat( message, MessageType.PartyChat );
		}
	}

	/// <summary>
	/// Removes a player from their party, disbanding it if empty or transferring leadership
	/// to the oldest remaining member when the leader leaves.
	/// </summary>
	private void RemovePlayerInternal( long steamId )
	{
		var partyId = GetPartyId( steamId );
		if ( !partyId.HasValue )
		{
			return;
		}

		var data = Clone( Parties[partyId.Value] );
		data.Members.Remove( steamId );
		data.Invites.Remove( steamId );

		if ( data.Members.Count == 0 )
		{
			Parties.Remove( partyId.Value );
			return;
		}

		// Leader left: promote the oldest remaining member (Members is kept in join order).
		if ( data.Leader == steamId )
		{
			data.Leader = data.Members[0];
			Parties[partyId.Value] = data;

			var promoted = GameUtils.GetPlayerById( data.Leader );
			if ( promoted.IsValid() )
			{
				promoted.Info( Language.GetPhrase( "party.leader_changed" ) );
			}
		}
		else
		{
			Parties[partyId.Value] = data;
		}
	}

	private void DisbandParty( Guid partyId )
	{
		if ( !Parties.TryGetValue( partyId, out var data ) || data.Members is null )
		{
			return;
		}

		foreach ( var id in data.Members.ToList() )
		{
			var player = GameUtils.GetPlayerById( id );
			if ( player.IsValid() )
			{
				player.Info( Language.GetPhrase( "party.disbanded" ) );
			}
		}

		Parties.Remove( partyId );
	}

	private void NotifyParty( Guid partyId, string message )
	{
		foreach ( var id in GetMembers( partyId ).ToList() )
		{
			var player = GameUtils.GetPlayerById( id );
			if ( player.IsValid() )
			{
				SendPartyChat( player, message );
			}
		}
	}

	// ── INetworkListener: clean up parties when a player disconnects ──────────────────────────
	public void OnDisconnected( Connection channel )
	{
		if ( !Networking.IsHost || channel is null )
		{
			return;
		}

		RemovePlayerInternal( channel.SteamId );
		ClearInvite( channel.SteamId );
	}
}
