using Dxura.RP.Shared;

namespace Dxura.RP.Game.Minigame;

public partial class MinigameSystem
{
	// Storage for player state before minigame
	private readonly Dictionary<long, PlayerPreviousState> _playerStashedState = new();

	private class PlayerPreviousState
	{
		public float Health { get; set; }
		public List<Tuple<string, Tuple<int, int>>> StashedEquipment { get; set; } = new();
		public required Transform ReturnPoint { get; set; }
	}

	private void PreparePlayerBaseline( Player player )
	{
		// Respawn if dead
		if ( player.IsDead )
		{
			player.SpawnHost( true );
		}

		player.RemoveStatus( Constants.WeedHighStatus );
		player.RemoveStatus( Constants.SatiatedStatus );
		player.HealthComponent.Health = player.HealthComponent.MaxHealth;
		player.Restricted = true;
		player.ClearLoadoutHost();

		// Ensure the player is not sitting
		player.SetSit( null );

		Log.Info( $"[Minigame] Baseline preparation complete for {player.DisplayName}" );
	}

	private void PreparePlayerForMinigame( Player player )
	{
		// Give starting equipment for minigame
		if ( CurrentMinigame?.StartingEquipmentIdentifiers != null )
		{
			var isFirst = true;
			foreach ( var equipmentIdentifier in CurrentMinigame.StartingEquipmentIdentifiers )
			{
				var startingEquipment = GameModeEquipments.FindByIdentifier( equipmentIdentifier );
				if ( startingEquipment == null )
				{
					continue;
				}
				var equipment = player.GiveHost( startingEquipment, isFirst, false );
				isFirst = false;

				if ( !equipment.IsValid() )
				{
					continue;
				}

				if ( CurrentMinigame.GiveMaxAmmo )
				{
					var ammoComponent = equipment.GameObject.GetComponentInChildren<AmmoComponent>( true );
					if ( ammoComponent.IsValid() )
					{
						ammoComponent.UpdateAmmoValues( ammoComponent.MaxAmmo, 1000 ); // Arbitrary large number to simulate infinite reserve ammo
						Log.Info( $"[Minigame] Gave max ammo for '{startingEquipment.DisplayName()}' to {player.DisplayName}" );
					}
				}
			}
		}

		Log.Info( $"[Minigame] Player playing preparation complete for {player.DisplayName}" );
	}

	private void StoreAllPlayersState()
	{
		foreach ( var player in _players )
		{
			StorePlayerState( player );
		}
	}

	private void StorePlayerState( Player player )
	{
		// Create state storage for this player
		var state = new PlayerPreviousState
		{
			Health = player.HealthComponent.Health, ReturnPoint = new Transform( player.WorldPosition, player.WorldRotation )
		};

		StashPlayerEquipment( player, ref state );

		// Store the state
		_playerStashedState[player.SteamId] = state;
	}

	private void RestoreAllPlayersState()
	{
		foreach ( var player in _players )
		{
			RestorePlayerState( player );
		}

		foreach ( var spectator in _spectators )
		{
			RestorePlayerState( spectator );
		}
	}

	private void RestorePlayerState( Player player )
	{
		if ( !_playerStashedState.TryGetValue( player.SteamId, out var state ) )
		{
			Log.Warning( $"No stashed state found for {player.DisplayName}" );
			return;
		}

		Log.Info( $"[Minigame] RESTORING {player.DisplayName} to position {state.ReturnPoint.Position} (current state: {CurrentState})" );

		player.Restricted = false;

		player.SpawnHost( false, state.ReturnPoint );

		player.HealthComponent.Health = player.HealthComponent.MaxHealth;

		RestorePlayerEquipment( player, ref state );

		// Remove from storage
		_playerStashedState.Remove( player.SteamId );

		Log.Info( $"[Minigame] Player state restored for {player.DisplayName}" );
	}

	private void StashPlayerEquipment( Player player, ref PlayerPreviousState state )
	{
		foreach ( var equipment in player.Equipment )
		{
			if ( !equipment.CanDrop )
			{
				continue;
			}

			var ammoComponent = equipment.GameObject.GetComponentInChildren<AmmoComponent>( true );
			var ammoCount = ammoComponent.IsValid() ? ammoComponent.Ammo : 0;
			var reserveAmmoCount = ammoComponent.IsValid() ? ammoComponent.ReserveAmmo : 0;

			state.StashedEquipment.Add( new Tuple<string, Tuple<int, int>>( equipment.Identifier, new Tuple<int, int>( ammoCount, reserveAmmoCount ) ) );
		}

		Log.Info( $"[Minigame] Stashed equipment for {player.DisplayName}" );
	}

	private void RestorePlayerEquipment( Player player, ref PlayerPreviousState state )
	{
		foreach ( var equipmentTuple in state.StashedEquipment )
		{
			var identifier = equipmentTuple.Item1;
			var ammoCounts = equipmentTuple.Item2;
			var equipmentResource = GameModeEquipments.FindByIdentifier( identifier );
			if ( equipmentResource == null )
			{
				Log.Warning( $"[Minigame] Failed to find equipment '{identifier}' for {player.DisplayName}" );
				continue;
			}

			var equipment = player.GiveHost( equipmentResource, false );

			if ( !equipment.IsValid() )
			{
				Log.Warning( $"[Minigame] Failed to restore equipment '{equipmentResource.DisplayName()}' for {player.DisplayName}" );
				continue;
			}

			Log.Info( $"[Minigame] Restored equipment '{equipmentResource.DisplayName()}' for {player.DisplayName}" );

			// Restore ammo if applicable
			var ammoComponent = equipment.GameObject.GetComponentInChildren<AmmoComponent>( true );
			if ( ammoComponent.IsValid() )
			{
				ammoComponent.Ammo = ammoCounts.Item1;
				ammoComponent.ReserveAmmo = ammoCounts.Item2;

				Log.Info( $"[Minigame] Restored {ammoCounts.Item1}/{ammoCounts.Item2} ammo for '{equipmentResource.DisplayName()}'" );
			}
		}

		Log.Info( $"[Minigame] Equipment restoration complete for {player.DisplayName}" );
	}
}
