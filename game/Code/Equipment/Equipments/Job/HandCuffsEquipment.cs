namespace Dxura.RP.Game.Equipments;

public class HandCuffsEquipment : InputWeaponComponent, IInputHints
{
	[Property] [Group( "Effects" )] private SoundEvent? UseSound { get; set; }

	[Property] [Group( "Effects" )] private SoundEvent? ArrestSound { get; set; }

	[Property] [Group( "Effects" )] private SoundEvent? ReleaseSound { get; set; }

	IEnumerable<(string Action, string Label)> IInputHints.GetInputHints()
	{
		yield return ("attack1", "#input.handcuffs.arrest");
		yield return ("attack2", "#input.handcuffs.unarrest");
	}

	protected override void OnInputDown()
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "hand_cuffs:use", Config.Current.Game.EquipmentHandCuffUseCooldown, true ) )
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
		if ( !Player.IsValid() || !Governance.Current.ValidateArrest( player, Player ) )
		{
			return;
		}

		Governance.Current.ArrestHost( player.SteamId );
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
	}

	internal static HandCuffsEquipment? FromPlayer( Player player )
	{
		return player.CurrentEquipment?.Components.Get<HandCuffsEquipment>( FindMode.EverythingInSelfAndDescendants );
	}

	internal void PlayArrestSoundFromHost()
	{
		ArrestSound?.Broadcast( WorldPosition, GameObject );
	}

	internal void PlayReleaseSoundFromHost()
	{
		ReleaseSound?.Broadcast( WorldPosition, GameObject );
	}

	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Unreliable )]
	private void DoSwingEffectsHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:hand_cuffs:use",
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
