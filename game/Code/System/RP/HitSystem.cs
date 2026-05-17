namespace Dxura.RP.Game;

public struct ActiveHit
{
	public Guid Id;
	public long HitmanSteamId;
	public long TargetSteamId;
	public long RequesterSteamId; // Added to track refunds
	public TimeSince TimeSinceRequested;
	public uint Price;
}

public struct HitRequest
{
	public Guid Id;
	public long HitmanSteamId;
	public long TargetSteamId;
	public long RequesterSteamId;
	public TimeSince TimeSinceRequested;
	public string Reason;
	public uint Price;
}

public class HitSystem : SingletonComponent<HitSystem>, IGameEvents
{
	[Sync( SyncFlags.FromHost )]
	public NetDictionary<long, uint> HitPrices { get; set; } = new();

	// Server State
	private readonly Dictionary<long, ActiveHit> _globalActiveHits = new();
	private readonly Dictionary<long, List<HitRequest>> _globalHitRequests = new();
	private readonly HashSet<long> _hitmenAcceptingRequests = new();

	// Local Client State
	public readonly List<HitRequest> PendingHitRequests = new();
	public ActiveHit? ActiveHit;


	protected override void OnStart()
	{
		if ( !Config.Current.Game.HitmanEnabled )
		{
			Destroy();
		}
	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost)
		{
			return;
		}

		// 1. Process Expired/Invalid Hits
		foreach ( var hit in _globalActiveHits.Values.ToList() )
		{
			if ( hit.TimeSinceRequested > Config.Current.Game.HitmanActiveHitDuration )
			{
				TerminateHit( hit.HitmanSteamId, false, "#hit.expired" );
			}

			// Check for invalid hits
			var hitman = GameUtils.GetPlayerById( hit.HitmanSteamId );
			var target = GameUtils.GetPlayerById( hit.TargetSteamId );

			if ( !hitman.IsValid() || !target.IsValid() || !hitman.IsConnected || !target.IsConnected )
			{
				TerminateHit( hit.HitmanSteamId, false, "#hit.terminated_disconnect" );
			}

			if ( hitman?.Restricted ?? false )
			{
				TerminateHit( hit.HitmanSteamId, false, "#hit.terminated_restrictions" );
			}

			// If target is dead, terminate the hit as failed
			if ( target.IsValid() && target.IsDead )
			{
				if ( target.GetLastKiller()?.SteamId == hit.HitmanSteamId )
				{
					TerminateHit( hit.HitmanSteamId, true );
				}
				else
				{
					TerminateHit( hit.HitmanSteamId, false, "#hit.failed_target_died" );
				}
			}
		}

		// 2. Process Expired Requests
		foreach ( var (hitmanId, requests) in _globalHitRequests.ToList() )
		{
			var expired = requests.Where( r => r.TimeSinceRequested > Config.Current.Game.HitmanRequestTimeout ).ToList();
			foreach ( var req in expired )
			{
				requests.Remove( req );
				NotifyRequestRemoved( hitmanId, req.Id );
			}
			if ( requests.Count == 0 )
			{
				_globalHitRequests.Remove( hitmanId );
			}
		}
	}

	public void OnPlayerKillHost( Player player )
	{
		if ( !Networking.IsHost || !player.IsValid() )
		{
			return;
		}

		// If a hitman dies, they fail their active hit
		if ( _globalActiveHits.ContainsKey( player.SteamId ) )
		{
			TerminateHit( player.SteamId, false, "#hit.failed_you_died" );
			return;
		}

		// If a target dies, check if it was the assigned hitman who killed them
		var activeHit = _globalActiveHits.Values.FirstOrDefault( x => x.TargetSteamId == player.SteamId );
		if ( activeHit.Id != Guid.Empty )
		{
			var attacker = player.LastDamageInfo?.Attacker?.GameObject?.Root?.GetComponent<Player>();
			var killedByHitman = attacker.IsValid() && attacker.SteamId == activeHit.HitmanSteamId;

			TerminateHit( activeHit.HitmanSteamId, killedByHitman, killedByHitman ? "" : "#hit.failed_target_died" );
		}
	}

	/// <summary>
	/// Handles the end of a hit, including payouts, refunds, and cleanups.
	/// </summary>
	private void TerminateHit( long hitmanId, bool success, string reason = "" )
	{
		if ( !_globalActiveHits.Remove( hitmanId, out var hit ) )
		{
			return;
		}

		var hitman = GameUtils.GetPlayerById( hitmanId );
		var target = GameUtils.GetPlayerById( hit.TargetSteamId );
		var requester = GameUtils.GetPlayerById( hit.RequesterSteamId );

		if ( success )
		{
			// Payout Hitman
			if ( hitman.IsValid() )
			{
				_ = hitman.PayHost( hit.Price, "Hitman Service Fee" );
				hitman.Success( string.Format( Language.GetPhrase( "hit.completed" ), hit.Price ) );
			}
			Chat.Current?.BroadcastSystemText( string.Format( Language.GetPhrase( "hit.completed_broadcast" ), hitman?.DisplayName, target?.DisplayName ) );
			Cooldown.Current.StartCooldown( $"hitman:{hit.TargetSteamId}:targeted", Config.Current.Game.HitPlayerCooldown );
		}
		else
		{
			// Refund Requester
			if ( requester.IsValid() )
			{
				_ = requester.PayHost( hit.Price, "Hitman Service Refund" );
				requester.Info( string.Format( Language.GetPhrase( "hit.refunded" ), hit.Price ) );
			}
			if ( hitman.IsValid() && !string.IsNullOrEmpty( reason ) )
			{
				hitman.Warn( reason );
			}
		}

		// Cleanup Hitman Status
		if ( hitman.IsValid() )
		{
			hitman.RemoveStatus( Constants.HitAcceptedStatus );
			using ( Rpc.FilterInclude( c => c.Id == hitman.ConnectionId ) )
			{
				SyncActiveHitClient( null );
			}
		}
	}

	#region RPCS

	[Rpc.Host]
	public void SetHitPrice( uint price )
	{
		var callerId = Rpc.CallerId;
		var player = GameUtils.GetPlayerByConnectionId( callerId );

		if ( !player.IsValid() || !player.Job.IsHitmanRole() )
		{
			return;
		}
		if ( Cooldown.Current.CheckAndStartCooldown( $"hitman:{player.SteamId}:price", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		HitPrices[player.SteamId] = price.Clamp( Config.Current.Game.MinHitPrice, Config.Current.Game.MaxHitPrice );
	}

	[Rpc.Host]
	public void RequestHit( long hitmanId, long targetId, string reason )
	{
		var requester = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		var hitman = GameUtils.GetPlayerById( hitmanId );

		if ( !requester.IsValid() || !hitman.IsValid() || !hitman.Job.IsHitmanRole() )
		{
			return;
		}
		if ( Cooldown.Current.CheckAndStartCooldown( $"hitman:{requester.SteamId}:request", Config.Current.Game.HitmanRequestCooldown ) )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"hitman:{targetId}:targeted", Config.Current.Game.HitPlayerCooldown ) )
		{
			requester.Error( "#hit.cannot_request" );
			return;
		}

		var dist = (requester.WorldPosition - hitman.WorldPosition).LengthSquared;
		if ( dist > MathF.Pow( Config.Current.Game.PlayerInteractDistance, 2 ) )
		{
			return;
		}

		var request = new HitRequest
		{
			Id = Guid.NewGuid(),
			HitmanSteamId = hitmanId,
			TargetSteamId = targetId,
			RequesterSteamId = requester.SteamId,
			Reason = reason.Truncate( 64 ),
			Price = GetHitPrice( hitmanId ),
			TimeSinceRequested = 0
		};

		_globalHitRequests.TryAdd( hitmanId, new List<HitRequest>() );
		_globalHitRequests[hitmanId].Add( request );

		using ( Rpc.FilterInclude( c => c.Id == hitman.ConnectionId ) )
		{
			NotifyHitRequestClient( request );
		}
	}

	[Rpc.Host]
	public async void AcceptHitRequestHost( Guid requestId )
	{
		var hitman = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( !hitman.IsValid() || _globalActiveHits.ContainsKey( hitman.SteamId ) || !_hitmenAcceptingRequests.Add( hitman.SteamId ) )
		{
			return;
		}

		if ( !_globalHitRequests.TryGetValue( hitman.SteamId, out var requests ) )
		{
			_hitmenAcceptingRequests.Remove( hitman.SteamId );
			return;
		}

		var req = requests.FirstOrDefault( x => x.Id == requestId );
		if ( req.Id == Guid.Empty )
		{
			_hitmenAcceptingRequests.Remove( hitman.SteamId );
			return;
		}

		requests.RemoveAll( x => x.Id == requestId );
		NotifyRequestRemoved( hitman.SteamId, req.Id );

		var requester = GameUtils.GetPlayerById( req.RequesterSteamId );
		if ( !requester.IsValid() || requester.WalletBalance + requester.BankBalance < req.Price )
		{
			_hitmenAcceptingRequests.Remove( hitman.SteamId );
			return;
		}

		try
		{
			// Transaction
			var charged = await requester.ChargeHost( req.Price, "Hitman Contract", true );
			await GameTask.MainThread();

			if ( !charged )
			{
				if ( !_globalActiveHits.ContainsKey( hitman.SteamId ) )
				{
					_globalHitRequests.TryAdd( hitman.SteamId, new List<HitRequest>() );
					_globalHitRequests[hitman.SteamId].Add( req );
					using ( Rpc.FilterInclude( c => c.Id == hitman.ConnectionId ) )
					{
						NotifyHitRequestClient( req );
					}
				}

				return;
			}

			var activeHit = new ActiveHit
			{
				Id = Guid.NewGuid(),
				HitmanSteamId = hitman.SteamId,
				TargetSteamId = req.TargetSteamId,
				RequesterSteamId = req.RequesterSteamId,
				Price = req.Price,
				TimeSinceRequested = 0
			};

			_globalActiveHits[hitman.SteamId] = activeHit;
			_globalHitRequests.Remove( hitman.SteamId );

			hitman.AddStatus( Constants.HitAcceptedStatus, Config.Current.Game.HitmanActiveHitDuration );

			using ( Rpc.FilterInclude( c => c.Id == hitman.ConnectionId ) )
			{
				SyncActiveHitClient( activeHit );
			}

			Chat.Current?.BroadcastSystemText( $"{hitman?.DisplayName} accepted a hit" );
			var target = GameUtils.GetPlayerById( activeHit.TargetSteamId );
			var requesterPlayer = GameUtils.GetPlayerById( activeHit.RequesterSteamId );
			_ = ServerApiClient.Audit( "Hit", $"{hitman?.SteamName} ({hitman?.SteamId}) accepted a ${activeHit.Price} hit from {requesterPlayer?.SteamName} ({requesterPlayer?.SteamId}) on {target?.SteamName} ({target?.SteamId}) for {req.Reason}", hitman?.SteamId );
		}
		finally
		{
			_hitmenAcceptingRequests.Remove( hitman.SteamId );
		}
	}

	[Rpc.Host]
	public void DenyHitRequestHost( Guid requestId )
	{
		var hitman = GameUtils.GetPlayerByConnectionId( Rpc.CallerId );
		if ( !hitman.IsValid() || !_globalHitRequests.TryGetValue( hitman.SteamId, out var requests ) )
		{
			return;
		}

		requests.RemoveAll( r => r.Id == requestId );
		NotifyRequestRemoved( hitman.SteamId, requestId );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void NotifyHitRequestClient( HitRequest req )
	{
		PendingHitRequests.Add( req );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void SyncActiveHitClient( ActiveHit? hit )
	{
		ActiveHit = hit;
		if ( hit != null )
		{
			PendingHitRequests.Clear();
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void NotifyHitRequestRemovedClient( Guid id )
	{
		PendingHitRequests.RemoveAll( x => x.Id == id );
	}

	private void NotifyRequestRemoved( long hitmanId, Guid reqId )
	{
		var hitman = GameUtils.GetPlayerById( hitmanId );
		if ( !hitman.IsValid() )
		{
			return;
		}
		using ( Rpc.FilterInclude( c => c.Id == hitman.ConnectionId ) )
		{
			NotifyHitRequestRemovedClient( reqId );
		}
	}

	#endregion

	public uint GetHitPrice( long hitmanId )
	{
		return HitPrices.GetValueOrDefault( hitmanId, Config.Current.Game.MinHitPrice );
	}
	public ActiveHit? GetActiveHitForHitman( long hitmanId )
	{
		return _globalActiveHits.GetValueOrDefault( hitmanId );
	}
}
