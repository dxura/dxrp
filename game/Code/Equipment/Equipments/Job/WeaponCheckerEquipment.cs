using Dxura.RP.Game.UI;
using System.Text;

namespace Dxura.RP.Game.Equipments;

public class WeaponCheckerEquipment : InputWeaponComponent, IEquipmentEvents
{
	[Property] [Group( "Effects" )]
	public required SoundEvent SearchingSoundEvent { get; set; }

	[Property] [Group( "Effects" )]
	public required SoundEvent SearchedSoundEvent { get; set; }

	private bool _isSearching;
	private Player? _targetPlayer;
	private TimeSince _searchStartTime;
	private TimeSince _lastSoundTime;

	private const float SearchDuration = 5f;
	private const float SoundInterval = 1f;

	protected override void OnInputDown()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "search:action", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		if ( !_isSearching )
		{
			StartSearch();
		}
	}

	protected override void OnInput()
	{
		if ( _isSearching )
		{
			UpdateSearch();
		}
	}

	protected override void OnInputUp()
	{
		if ( Input.Released( "Attack1" ) && _isSearching )
		{
			StopSearch( "Search cancelled" );
		}
	}

	protected override void OnDisabled()
	{
		StopSearch();
	}

	public new void OnEquipmentHolstered( Equipment equipment )
	{
		StopSearch();
	}

	private void StartSearch()
	{
		var trace = GetTrace( Config.Current.Game.ReachDistance * 0.5f );

		if ( trace is not { Hit: true } || !trace.Value.GameObject.IsValid() )
		{
			return;
		}

		var target = trace.Value.GameObject.Root;
		if ( !target.IsValid() || !target.Tags.Has( Constants.PlayerTag ) )
		{
			return;
		}

		var targetPlayer = target.GetComponent<Player>();
		if ( !targetPlayer.IsValid() || targetPlayer == Player.Local )
		{
			return;
		}

		_isSearching = true;
		_targetPlayer = targetPlayer;
		_searchStartTime = 0;
		_lastSoundTime = 0;

		ShowMessage( $"Searching {_targetPlayer.DisplayName}", 0f );
		SearchingSoundEvent.Broadcast( Player.Local.WorldPosition, Player.Local.GameObject );
	}

	private void UpdateSearch()
	{
		if ( !_targetPlayer.IsValid() )
		{
			StopSearch( "Target is no longer valid" );
			return;
		}

		// Check if target is still in range
		var distance = Vector3.DistanceBetween( Player.Local.WorldPosition, _targetPlayer.WorldPosition );
		if ( distance > Config.Current.Game.ReachDistance * 0.5f )
		{
			StopSearch( "Target moved out of range" );
			return;
		}

		// Update progress
		var progress = Math.Clamp( _searchStartTime / SearchDuration, 0f, 1f );
		ShowMessage( $"Searching {_targetPlayer.DisplayName}", progress );

		// Play sound every second
		if ( _lastSoundTime >= SoundInterval && _searchStartTime < SearchDuration )
		{
			SearchingSoundEvent.Broadcast( Player.Local.WorldPosition, Player.Local.GameObject );
			_lastSoundTime = 0;
		}

		// Check if search is complete
		if ( _searchStartTime >= SearchDuration )
		{
			CompleteSearch();
		}
	}

	private void CompleteSearch()
	{
		if ( !_targetPlayer.IsValid() )
		{
			StopSearch( "Target is no longer valid" );
			return;
		}

		// Request search results from server
		RequestSearchResultsHost( _targetPlayer.SteamId );
		StopSearch();
		SearchedSoundEvent.Broadcast( Player.Local.WorldPosition, Player.Local.GameObject );
	}

	private void StopSearch( string? message = null )
	{
		_isSearching = false;
		_targetPlayer = null;

		if ( EquipmentOverlay.Instance.IsValid() )
		{
			EquipmentOverlay.Instance.IsActive = false;
		}

		if ( message != null )
		{
			Notify.Warn( message );
		}
	}

	private void ShowMessage( string text, float progress = 0f )
	{
		EquipmentOverlay.Instance.Status = text;
		EquipmentOverlay.Instance.Progress = progress;
		EquipmentOverlay.Instance.IsActive = true;
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	private void RequestSearchResultsHost( long targetSteamId )
	{
		var callerId = Rpc.CallerId;

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:search:complete", Config.Current.Game.ActionCooldown ) )
		{
			return;
		}

		var callerPlayer = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !callerPlayer.IsValid() )
		{
			return;
		}

		var targetPlayer = GameUtils.GetPlayerById( targetSteamId );
		if ( !targetPlayer.IsValid() )
		{
			return;
		}

		// Check if caller is still close enough to target
		var distance = Vector3.DistanceBetween( callerPlayer.WorldPosition, targetPlayer.WorldPosition );
		if ( distance > Config.Current.Game.ReachDistance * 0.5f )
		{
			callerPlayer.Error( "#equipment.weapon_checker.target_moved" );
			return;
		}

		// Build search results
		var results = new StringBuilder();
		results.AppendLine( string.Format( Language.GetPhrase( "equipment.weapon_checker.results_title" ), targetPlayer.DisplayName ) );

		// Weapons
		var weaponNames = targetPlayer.Equipment
			.Where( e => e.IsValid() && e.CanDrop )
			.Select( eq => eq.Resource.DisplayName() )
			.ToList();
		results.AppendLine( weaponNames.Count > 0
			? string.Format( Language.GetPhrase( "equipment.weapon_checker.results.weapons" ), string.Join( ", ", weaponNames ) )
			: string.Format( Language.GetPhrase( "equipment.weapon_checker.results.weapons" ), Language.GetPhrase( "equipment.weapon_checker.none" ) ) );

		// Statuses
		results.AppendLine( targetPlayer.Statuses.Count > 0
			? string.Format(
				Language.GetPhrase( "equipment.weapon_checker.results.statuses" ),
				string.Join( ", ", targetPlayer.Statuses.Keys.Select( x => Status.Current.GetCachedInstance( x )?.Name ) ) )
			: string.Format( Language.GetPhrase( "equipment.weapon_checker.results.statuses" ), Language.GetPhrase( "equipment.weapon_checker.none" ) ) );

		// Pockets
		var pocketItems = PocketSystem.Instance.ListPocketItems( targetSteamId );
		results.AppendLine( pocketItems.Count > 0
			? string.Format( Language.GetPhrase( "equipment.weapon_checker.results.pockets" ), string.Join( ", ", pocketItems ) )
			: string.Format( Language.GetPhrase( "equipment.weapon_checker.results.pockets" ), Language.GetPhrase( "equipment.weapon_checker.empty" ) ) );

		// Send results to caller
		foreach ( var line in results.ToString().Split( '\n' ) )
		{
			var trimmed = line.TrimEnd( '\r' );
			if ( string.IsNullOrWhiteSpace( trimmed ) )
			{
				continue;
			}
			callerPlayer.SendMessage( trimmed );
		}

		callerPlayer.Success( "#equipment.weapon_checker.complete" );
	}
}
