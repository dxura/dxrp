using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace Dxura.RP.Game;

public class WaitingRoom : Component, IGameEvents
{
	/// <summary>
	/// The name of the server the player was previously on, set before ejecting to the waiting room.
	/// </summary>
	public static string? PreviousServerName { get; set; }

	private TimeSince LastJoinAttempt { get; set; } = 0;
	private TimeSince TimeInWaitingRoom { get; set; } = 0;
	
	protected override void OnUpdate()
	{
		if ( LastJoinAttempt < 10 )
		{
			return;
		}

		LastJoinAttempt = 0;

		Log.Info( "Attempting to join..." );

		_ = AttemptJoin();
	}

	private async Task AttemptJoin()
	{
		using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 5 ) );

		try
		{
			var lobbies = await Networking.QueryLobbies( cts.Token );

			var hasBeenWaitingTenMinutes = TimeInWaitingRoom >= 600;

			// Filter available lobbies
			var availableLobbies = lobbies.Where( x => !x.IsFull );

			// For the first X minutes, only join the previous server
			if ( !hasBeenWaitingTenMinutes )
			{
				availableLobbies = availableLobbies.Where( lobby =>
					!string.IsNullOrEmpty( lobby.Name ) && IsPreviousServer( lobby.Name ) );
			}

			// Sort lobbies: prefer previous server, then "DXRP Official X" servers by number
			var orderedLobbies = availableLobbies
				.OrderByDescending( lobby => IsPreviousServer( lobby.Name ) )
				.ThenByDescending( lobby => IsOfficialServer( lobby.Name ) )
				.ThenBy( lobby => GetOfficialServerNumber( lobby.Name ) )
				.ToList();

			if ( orderedLobbies.Count == 0 )
			{
				Log.Info( "No available lobbies found, retrying later..." );
				return;
			}

			Log.Info( $"Found {orderedLobbies.Count} lobbies, joining {orderedLobbies.FirstOrDefault().Name}..." );

			if ( await Networking.TryConnectSteamId( orderedLobbies.FirstOrDefault().OwnerId ) )
			{
				Log.Info( "Connected to lobby" );
			}
			else
			{
				Log.Warning( "Failed to connect to lobby" );
			}
		}
		catch ( Exception e )
		{
			Log.Warning( e );
		}
	}

	private bool IsPreviousServer( string lobbyName )
	{
		return !string.IsNullOrEmpty( PreviousServerName ) &&
		       !string.IsNullOrEmpty( lobbyName ) &&
		       lobbyName.StartsWith( PreviousServerName );
	}

	private bool IsOfficialServer( string lobbyName )
	{
		return !string.IsNullOrEmpty( lobbyName ) &&
		       Regex.IsMatch( lobbyName, @"^DXRP Official \d+$" );
	}

	private int GetOfficialServerNumber( string lobbyName )
	{
		if ( IsOfficialServer( lobbyName ) )
		{
			return int.Parse( Regex.Match( lobbyName, @"\d+$" ).Value );
		}
		return int.MaxValue;
	}
}
