using Sandbox.Audio;
namespace Dxura.RP.Game;

public class Notify : GameObjectSystem<Notify>, IGameEvents
{
	public enum NotificationType
	{
		Generic,
		Money,
		Error,
		Success,
		Warning,
		Inventory,
		Cooldown
	}

	public IReadOnlyList<NotificationEntry> ActiveNotifications => _notifications;

	// List of active notifications
	private readonly List<NotificationEntry> _notifications = new();

	public Notify( Scene scene ) : base( scene )
	{
	}

	public void OnSecondlyUpdate()
	{
		for ( var i = _notifications.Count - 1; i >= 0; i-- )
		{
			if ( _notifications[i].Expiry <= 0f )
			{
				_notifications.RemoveAt( i );
			}
		}
	}

	// Add a new notification locally
	private void Add( string message, NotificationType type = NotificationType.Generic, float duration = 5f )
	{
		message = ResolvePhrase( message );
		Log.Info( $"[{type}] {message}" );

		var notification = new NotificationEntry( message, type, duration );

		if ( type is NotificationType.Error or NotificationType.Success )
		{
			Sound.Play( $"notify-{type.ToString().ToLower()}" );
		}

		_notifications.Add( notification );
	}

	private static string ResolvePhrase( string message )
	{
		return message.StartsWith( '#' ) ? Language.GetPhrase( message[1..] ) : message;
	}

	// Local notification methods
	public static void Money( int amount, bool bank = false, float duration = 3f )
	{
		if ( amount == 0 )
		{
			return;
		}

		var isPositive = amount >= 0;

		Current?.Add( $"{(isPositive ? "+" : "-")}${Math.Abs( amount ):N0} {(bank ? "(Bank)" : "(Wallet)")}", NotificationType.Money, duration );
	}

	public static void Info( string message, float duration = 5f )
	{
		Current?.Add( message, NotificationType.Generic, duration );
	}

	public static void Success( string message, float duration = 5f )
	{
		Current?.Add( message, NotificationType.Success, duration );
	}

	public static void Warn( string message, float duration = 5f )
	{
		Current?.Add( message, NotificationType.Warning, duration );
	}

	public static void Inventory( string message, float duration = 4f )
	{
		Current?.Add( message, NotificationType.Inventory, duration );
	}

	public static void Error( string message, float duration = 5f )
	{
		Current?.Add( message, NotificationType.Error, duration );
	}

	public static void Cooldown( string cooldownId )
	{
		CooldownSeconds( Game.Cooldown.Current.GetRemainingTime( cooldownId ) + 1 );
	}

	public static void CooldownSeconds( int remainingSeconds )
	{
		Current?.Add( remainingSeconds.ToString(), NotificationType.Cooldown );
	}

	// RPC methods to broadcast notifications
	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public static void BroadcastInfo( string message, float duration = 5f )
	{
		Info( message, duration );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public static void BroadcastSuccess( string message, float duration = 5f )
	{
		Success( message, duration );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public static void BroadcastWarn( string message, float duration = 5f )
	{
		Warn( message, duration );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public static void BroadcastInventory( string message, float duration = 4f )
	{
		Inventory( message, duration );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public static void BroadcastError( string message, float duration = 5f )
	{
		Error( message, duration );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public static void BroadcastMoney( int amount, bool bank = false )
	{
		Money( amount, bank );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public static void BroadcastCooldown( string cooldownId )
	{
		Cooldown( cooldownId );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public static void BroadcastCooldownSeconds( int remainingSeconds )
	{
		CooldownSeconds( remainingSeconds );
	}

	public record NotificationEntry( string Message, NotificationType Type, RealTimeUntil Expiry );
}

public static partial class PlayerExtensions
{
	public static void Info( this Player player, string message, float duration = 5f )
	{
		// Using RPC filtering to only send to a specific player
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			Notify.BroadcastInfo( message, duration );
		}
	}

	public static void Success( this Player player, string message, float duration = 5f )
	{
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			Notify.BroadcastSuccess( message, duration );
		}
	}

	public static void Warn( this Player player, string message, float duration = 5f )
	{
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			Notify.BroadcastWarn( message, duration );
		}
	}

	public static void Inventory( this Player player, string message, float duration = 4f )
	{
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			Notify.BroadcastInventory( message, duration );
		}
	}

	public static void Error( this Player player, string message, float duration = 5f )
	{
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			Notify.BroadcastError( message, duration );
		}
	}

	public static void Money( this Player player, int money, bool bank = false )
	{
		// Using RPC filtering to only send to a specific player
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			Notify.BroadcastMoney( money, bank );
		}
	}

	public static void Cooldown( this Player player, string cooldownId )
	{
		var remainingSeconds = Game.Cooldown.Current.GetRemainingTime( cooldownId ) + 1;
		using ( Rpc.FilterInclude( c => c.Id == player.ConnectionId ) )
		{
			Notify.BroadcastCooldownSeconds( remainingSeconds );
		}
	}
}
