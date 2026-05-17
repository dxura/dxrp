using Dxura.RP.Game.Entities;
using System.Threading.Tasks;

namespace Dxura.RP.Game.UI;

/// <summary>
/// Base class for slot machine game logic
/// Handles common functionality like player management, money operations, and general state
/// </summary>
public abstract class BaseSlotMachineGame : PanelComponent
{
	
	[Property]
	public required SlotMachineEntity SlotMachineEntity { get; set; }
	
	/// <summary>
	/// The current player using the slot machine
	/// </summary>
	[Property]
	[Sync( SyncFlags.FromHost )]
	public Player? CurrentPlayer { get; protected set; }

	/// <summary>
	/// The bet amount for this game
	/// </summary>
	[Property]
	[Sync( SyncFlags.FromHost )]
	public uint BetAmount { get; set; } = 500;

	/// <summary>
	/// Check if machine is currently in use
	/// </summary>
	public bool IsInUse => CurrentPlayer.IsValid();

	/// <summary>
	/// Check if the local player is the owner of the slot machine
	/// </summary>
	public bool IsOwner => Network.IsOwner;

	/// <summary>
	/// Check if the local player is the current player
	/// </summary>
	protected bool IsLocalPlayerPlaying => CurrentPlayer.IsValid() && CurrentPlayer == Player.Local;
	
	protected override void OnStart()
	{
		SlotMachineEntity = GameObject.Root.GetComponent<SlotMachineEntity>();
	}

	/// <summary>
	/// Handle settings button click - override in derived class
	/// </summary>
	public virtual void HandleSettingsClicked()
	{
		Log.Info( "Settings clicked" );
	}

	/// <summary>
	/// Handle end game button click - resets the game
	/// </summary>
	public virtual void HandleEndGameClicked()
	{
		EndGameHost();
	}

	/// <summary>
	/// Called to end the current game session
	/// </summary>
	[Rpc.Host]
	protected virtual void EndGameHost()
	{
		// Only owner can force end game
		if ( Rpc.Caller != Network.Owner )
		{
			return;
		}

		CurrentPlayer = null;
		OnGameReset();
	}

	/// <summary>
	/// Called when the game is reset - override in derived class to reset game-specific state
	/// </summary>
	protected virtual void OnGameReset()
	{
	}

	/// <summary>
	/// Charge the player money 
	/// </summary>
	protected async Task<bool> ChargePlayer( Player player, uint amount, string reason )
	{
		if ( !Networking.IsHost )
		{
			return false;
		}

		return await player.ChargeHost( amount, reason, true );
	}

	/// <summary>
	/// Pay the player money 
	/// </summary>
	protected async Task<bool> PayPlayer( Player player, uint amount, string reason )
	{
		if ( !Networking.IsHost )
		{
			return false;
		}

		return await player.PayHost( amount, reason, true );
	}

	/// <summary>
	/// Set the current player (server-side only)
	/// </summary>
	protected void SetCurrentPlayer( Player? player )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		CurrentPlayer = player;
	}

	/// <summary>
	/// Validate that a caller is the current player
	/// </summary>
	protected bool ValidateCurrentPlayer( Guid callerId )
	{
		return CurrentPlayer.IsValid() && CurrentPlayer.ConnectionId == callerId;
	}

	[Rpc.Broadcast ( NetFlags.HostOnly | NetFlags.Unreliable )]
	protected void BroadcastProcessEffects(float seconds)
	{
		_ =  DoProcessEffects(seconds);
	}

	private async Task DoProcessEffects(float seconds)
	{
		if(!SlotMachineEntity.IsValid()) return;
		
		var handle = SlotMachineEntity.ProcessSound?.Play( WorldPosition, GameObject );
		await GameTask.DelayRealtimeSeconds( seconds );

		if ( handle.IsValid() )
		{
			handle.Stop();
		}
	}
	
	[Rpc.Broadcast ( NetFlags.HostOnly | NetFlags.Unreliable )]
	protected void BroadcastWinEffects()
	{
		if(!SlotMachineEntity.IsValid()) return;

		SlotMachineEntity.WinSound?.Play( WorldPosition, GameObject );
	}
	
	[Rpc.Broadcast ( NetFlags.HostOnly | NetFlags.Unreliable )]
	protected void BroadcastLoseEffects()
	{
		if(!SlotMachineEntity.IsValid()) return;

		SlotMachineEntity.LoseSound?.BroadcastHost( WorldPosition, GameObject );
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( CurrentPlayer, BetAmount );
	}
}
