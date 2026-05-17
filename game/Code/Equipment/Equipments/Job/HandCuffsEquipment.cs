namespace Dxura.RP.Game.Equipments;

public class HandCuffsEquipment : InputWeaponComponent
{
	[Property] [Group( "Effects" )] private SoundEvent? UseSound { get; set; }

	[Property] [Group( "Effects" )] private SoundEvent? ArrestSound { get; set; }

	[Property] [Group( "Effects" )] private SoundEvent? ReleaseSound { get; set; }

	protected override void OnInputDown()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "handcuff:use", Config.Current.Game.EquipmentHandCuffUseCooldown, true ) )
		{
			return;
		}

		DoSwingEffectsHost();

		var trace = GetTrace();

		if ( trace is not { Hit: true } || !trace.Value.GameObject.IsValid() )
		{
			return;
		}

		var player = trace.Value.GameObject.Root.GetComponentInParent<Player>();
		if ( player == null || !player.IsValid() )
		{
			return;
		}

		// Arrest
		if ( Input.Down( "attack1" ) )
		{
			TryArrest( player );
		}
		else
		{
			TryRelease( player );
		}
	}

	private void TryArrest( Player player )
	{
		var validArrest = Governance.Current.ValidateArrest( player, Player.Local );

		if ( !validArrest )
		{
			return;
		}

		Governance.Current.ArrestHost( player.SteamId );
		ArrestSound.Broadcast( WorldPosition );
	}

	private void TryRelease( Player player )
	{

		if ( player.Job.IsPoliticalPrisonerRole() )
		{
			Notify.Warn( "#equipment.handcuffs.political" );
			return;
		}

		if ( !player.HasStatus( Constants.PrisonerStatus ) )
		{
			Notify.Warn( "#equipment.handcuffs.not_prisoner" );
			return;
		}

		Governance.Current.ReleaseHost( player.SteamId );
		ReleaseSound.Broadcast( WorldPosition );
		Notify.Success( string.Format( Language.GetPhrase( "equipment.handcuffs.released" ), player.DisplayName ) );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Unreliable )]
	private void DoSwingEffectsHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:handcuff:use",
			Config.Current.Game.EquipmentHandCuffUseCooldown ) )
		{
			return;
		}

		BroadcastSwingEffects();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	private void BroadcastSwingEffects()
	{
		if ( UseSound.IsValid() )
		{
			UseSound.Play( WorldPosition, GameObject );
		}

		// Third person
		Equipment.Owner?.Renderer?.Set( "b_attack", true );

		// First person
		Equipment?.ViewModel?.ModelRenderer.Set( "b_attack", true );
	}
}
