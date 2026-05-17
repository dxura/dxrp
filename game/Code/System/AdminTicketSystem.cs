using Dxura.RP.Shared;
using System.Linq;

namespace Dxura.RP.Game;

public struct AdminTicketState
{
	public Guid Id { get; set; }
	public int SortOrder { get; set; }
	public long CreatedAtUnixTime { get; set; }
	public long ReporterSteamId { get; set; }
	public string ReporterName { get; set; }
	public List<string> Messages { get; set; }
	public long? ClaimedBySteamId { get; set; }
	public string? ClaimedByName { get; set; }
}

public sealed class AdminTicketSystem : GameObjectSystem<AdminTicketSystem>, IGameEvents
{
	private const long TicketLifetimeSeconds = 30 * 60;

	private sealed class AdminTicketRecord
	{
		public required Guid Id { get; init; }
		public required int SortOrder { get; init; }
		public required long CreatedAtUnixTime { get; init; }
		public required long ReporterSteamId { get; init; }
		public required string ReporterName { get; set; }
		public List<string> Messages { get; } = [];
		public long? ClaimedBySteamId { get; set; }
		public string? ClaimedByName { get; set; }

		public AdminTicketState ToState()
		{
			return new AdminTicketState
			{
				Id = Id,
				SortOrder = SortOrder,
				CreatedAtUnixTime = CreatedAtUnixTime,
				ReporterSteamId = ReporterSteamId,
				ReporterName = ReporterName,
				Messages = [..Messages],
				ClaimedBySteamId = ClaimedBySteamId,
				ClaimedByName = ClaimedByName
			};
		}
	}

	private readonly Dictionary<Guid, AdminTicketRecord> _activeTickets = [];
	private readonly List<AdminTicketState> _visibleTickets = [];
	private TimeSince _timeSinceTicketValidation = 0f;
	private int _nextSortOrder = 1;

	public IReadOnlyList<AdminTicketState> VisibleTickets => _visibleTickets;
	public int ClientRevision { get; private set; }

	public AdminTicketSystem( Scene scene ) : base( scene )
	{
	}

	public bool CreateOrAppendTicketHost( Player reporter, string message )
	{
		if ( !Networking.IsHost || !reporter.IsValid() )
		{
			return false;
		}

		message = message.Trim();
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			return false;
		}

		var ticket = _activeTickets.Values.FirstOrDefault( x => x.ReporterSteamId == reporter.SteamId );
		var created = false;

		if ( ticket == null )
		{
			ticket = new AdminTicketRecord
			{
				Id = Guid.NewGuid(),
				SortOrder = _nextSortOrder++,
				CreatedAtUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				ReporterSteamId = reporter.SteamId,
				ReporterName = reporter.DisplayName
			};

			_activeTickets[ticket.Id] = ticket;
			created = true;
		}

		ticket.ReporterName = reporter.DisplayName;
		ticket.Messages.Add( message );

		SyncVisibleTicketsForAllStaff( created );

		var action = created ? "StaffRequestCreated" : "StaffRequestUpdated";
		_ = ServerApiClient.Audit( action, $"{reporter.SteamName} ({reporter.SteamId}): {message}", reporter.SteamId );

		return created;
	}

	public void ClearVisibleTicketsClient()
	{
		if ( _visibleTickets.Count == 0 )
		{
			return;
		}

		_visibleTickets.Clear();
		ClientRevision++;
	}

	[Rpc.Host]
	public void RequestTicketSyncHost()
	{
		var caller = GetValidStaffCaller();
		if ( caller == null )
		{
			return;
		}

		SyncVisibleTicketsForStaff( caller );
	}

	[Rpc.Host]
	public void ClaimTicketHost( Guid ticketId )
	{
		var caller = GetValidStaffCaller();
		if ( caller == null || !_activeTickets.TryGetValue( ticketId, out var ticket ) )
		{
			return;
		}

		if ( ticket.ClaimedBySteamId.HasValue && ticket.ClaimedBySteamId != caller.SteamId )
		{
			return;
		}

		var reporter = GameUtils.GetPlayerById( ticket.ReporterSteamId );
		ticket.ClaimedBySteamId = caller.SteamId;
		ticket.ClaimedByName = caller.DisplayName;

		caller.Success( "#admin.ticket.claimed" );
		if ( reporter.IsValid() )
		{
			reporter.SendMessage( string.Format( Language.GetPhrase( "command.staff.claimed" ), caller.DisplayName ) );
		}

		_ = ServerApiClient.Audit( "StaffTicketClaimed",
			$"{caller.SteamName} ({caller.SteamId}) claimed {ticket.ReporterName} ({ticket.ReporterSteamId})",
			caller.SteamId );

		SyncVisibleTicketsForAllStaff();
	}

	[Rpc.Host]
	public void ResolveTicketHost( Guid ticketId )
	{
		var caller = GetValidStaffCaller();
		if ( caller == null || !_activeTickets.TryGetValue( ticketId, out var ticket ) )
		{
			return;
		}

		if ( ticket.ClaimedBySteamId != caller.SteamId )
		{
			return;
		}

		var reporter = GameUtils.GetPlayerById( ticket.ReporterSteamId );
		_activeTickets.Remove( ticketId );

		caller.Success( "#admin.ticket.resolved" );
		if ( reporter.IsValid() )
		{
			reporter.SendMessage( Language.GetPhrase( "command.staff.resolved" ) );
		}

		_ = ServerApiClient.Audit( "StaffTicketResolved",
			$"{caller.SteamName} ({caller.SteamId}) resolved {ticket.ReporterName} ({ticket.ReporterSteamId})",
			caller.SteamId );

		SyncVisibleTicketsForAllStaff();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastVisibleTickets( AdminTicketState[] tickets )
	{
		if ( !RankSystem.HasLocalPermission( Permission.HandleTickets ) )
		{
			ClearVisibleTicketsClient();
			return;
		}

		_visibleTickets.Clear();
		_visibleTickets.AddRange( tickets.OrderBy( x => x.SortOrder ) );
		ClientRevision++;
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost || _timeSinceTicketValidation < 1f )
		{
			return;
		}

		_timeSinceTicketValidation = 0f;

		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var didChange = false;
		foreach ( var ticket in _activeTickets.Values.ToList() )
		{
			var reporter = GameUtils.GetPlayerById( ticket.ReporterSteamId );
			var isExpired = now - ticket.CreatedAtUnixTime > TicketLifetimeSeconds;
			var reporterLeft = !reporter.IsValid() || !reporter.IsConnected;
			if ( isExpired || reporterLeft )
			{
				_activeTickets.Remove( ticket.Id );
				didChange = true;
				continue;
			}

			if ( !ticket.ClaimedBySteamId.HasValue )
			{
				continue;
			}

			var claimedBy = GameUtils.GetPlayerById( ticket.ClaimedBySteamId.Value );
			if ( claimedBy.IsValid() && RankSystem.HasPermission( ticket.ClaimedBySteamId.Value, Permission.HandleTickets ) )
			{
				continue;
			}

			ticket.ClaimedBySteamId = null;
			ticket.ClaimedByName = null;
			didChange = true;
		}

		if ( didChange )
		{
			SyncVisibleTicketsForAllStaff();
		}
	}

	private void SyncVisibleTicketsForAllStaff( bool playTicketOpen = false )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		foreach ( var staffPlayer in GameUtils.Players.Where( CanReceiveTickets ).ToList() )
		{
			SyncVisibleTicketsForStaff( staffPlayer, playTicketOpen );
		}
	}

	private void SyncVisibleTicketsForStaff( Player staffPlayer, bool playTicketOpen = false )
	{
		if ( !staffPlayer.IsValid() || staffPlayer.Connection == null )
		{
			return;
		}

		var visibleTickets = _activeTickets.Values
			.Where( ticket => !ticket.ClaimedBySteamId.HasValue || ticket.ClaimedBySteamId == staffPlayer.SteamId )
			.OrderBy( ticket => ticket.SortOrder )
			.Select( ticket => ticket.ToState() )
			.ToArray();

		using ( Rpc.FilterInclude( c => c.Id == staffPlayer.ConnectionId ) )
		{
			BroadcastVisibleTickets( visibleTickets );
			if ( playTicketOpen )
			{
				PlayTicketOpenClient();
			}
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void PlayTicketOpenClient()
	{
		if ( !RankSystem.HasLocalPermission( Permission.HandleTickets ) )
		{
			return;
		}

		Sound.Play( "ticket-open" );
	}

	private static bool CanReceiveTickets( Player player )
	{
		return player.IsValid()
		       && player.Connection != null
		       && RankSystem.HasPermission( player.SteamId, Permission.HandleTickets );
	}

	private static Player? GetValidStaffCaller()
	{
		if ( !RankSystem.HasPermission( Rpc.Caller.SteamId, Permission.HandleTickets ) )
		{
			return null;
		}

		var caller = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		return caller.IsValid() ? caller : null;
	}
}
