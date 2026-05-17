using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
namespace Dxura.RP.Game;

public class SanctionActionHandler : ActionHandler<SanctionActionDto>
{
	protected override void Execute( SanctionActionDto action )
	{
		PlayerSanctionHistorySystem.Current?.InvalidateCachedSanctions( action.PlayerId );

		var player = GameUtils.GetPlayerById( action.PlayerId );
		if ( !player.IsValid() )
		{
			return;
		}

		Log.Info( $"Processing sanction action {action.Type} for player {player.DisplayName} ({player.SteamId}) Reason: {action.Reason}" );

		// Process any modifiers
		ProcessSanctionModifiers( player, action.Modifiers );

		// Process main sanction action
		switch ( action.Type )
		{
			case SanctionType.Warning:
				if ( !action.Silent )
				{
					Chat.Current?.BroadcastSystemText( $"{player.DisplayName} has been warned for: {action.Reason}" );
				}
				
				WarningOverlayHelper.ShowWarning( player, "You have been warned!", action.Reason );
				break;
			case SanctionType.Kick:
				GameNetworkManager.Instance.KickPlayer( player.Connection, action.Reason );
				break;
			case SanctionType.Jail when action.Duration is {} jailDuration:
				Governance.Current.ArrestPolitical( player.SteamId, jailDuration, action.Reason, action.Silent );
				if ( !action.Silent )
				{
					Chat.Current?.BroadcastSystemText( $"{player.DisplayName} has been politically jailed for: {action.Reason} ({jailDuration.TotalMinutes}m)" );
				}
				
				WarningOverlayHelper.ShowWarning( player, "You have been jailed!", action.Reason );
				break;
			case SanctionType.Ban:
				GameNetworkManager.Instance.KickPlayer( player.Connection, action.Reason, true );
				break;
			case SanctionType.Jail when action.Duration == null: // Release from jail
				Governance.Current.Release( player.SteamId );
				break;
			case SanctionType.Gag when action.Duration != null: // Gag
				{
					var durationSeconds = (float?)action.Duration?.TotalSeconds;
					player.AddStatus( Constants.GaggedStatus, durationSeconds );
					if ( !action.Silent )
					{
						Chat.Current?.BroadcastSystemText( $"{player.DisplayName} has been gagged for: {action.Reason} ({action.Duration?.TotalMinutes}m)" );
					}
					
					WarningOverlayHelper.ShowWarning( player, "You have been gagged!", action.Reason );
					break;
				}
			case SanctionType.Gag when action.Duration == null: // UnGag
				player.RemoveStatus( Constants.GaggedStatus );
				break;
			case SanctionType.Automatic:
			case SanctionType.Note:
				// No immediate action needed
				break;
		}
	}

	/// <summary>
	/// Process sanction modifiers as immediate actions
	/// </summary>
	private void ProcessSanctionModifiers( Player player, SanctionModifier modifiers )
	{
		if ( modifiers == SanctionModifier.None )
		{
			return;
		}

		if ( modifiers.HasFlag( SanctionModifier.ClearConstructs ) )
		{
			CleanupSystem.Current.CleanupConstructs( player.SteamId );
			Log.Info( $"Cleared constructs for sanctioned player {player.DisplayName}" );
		}

		if ( modifiers.HasFlag( SanctionModifier.Kick ) )
		{
			GameNetworkManager.Instance.KickPlayer( player.Connection, "Sanctioned" );
			Log.Info( $"Kicked sanctioned player {player.DisplayName}" );
		}

		if ( modifiers.HasFlag( SanctionModifier.Kill ) )
		{
			player.HealthComponent.TakeDamageHost( new DamageInfo( GameManager.Instance, float.MaxValue ) );
			Log.Info( $"Killed sanctioned player {player.DisplayName}" );
		}
	}
}
