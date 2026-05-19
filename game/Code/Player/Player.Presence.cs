namespace Dxura.RP.Game;

public partial class Player
{
	[Property] [Feature( "Misc" )] [Group( "Torch" )]
	private GameObject Torch { get; set; } = null!;

	[Property] [Feature( "Misc" )] [Group( "Torch" )]
	private readonly SoundEvent _torchToggleSound = null!;

	[Property] [Feature( "Misc" )] [Group( "Voice" )]
	public required PlayerVoiceComponent Voice { get; set; }

	/// <summary>
	///     When set, voice/chat distance is measured from this object instead of the player's body.
	/// </summary>
	[Sync( SyncFlags.FromHost )] public GameObject? ListenerTarget { get; set; }

	private void OnUpdatePresence()
	{
		// Torch logic
		if ( Input.Pressed( "Flashlight" ) )
		{
			Torch.Enabled = !Torch.Enabled;
			_torchToggleSound.Play( WorldPosition );
		}

		if ( !Torch.Enabled )
		{
			return;
		}

		// Update the torch position and direction 
		if ( IsThirdPersonPreferred )
		{
			var forwardOffset = BodyForward.WorldRotation.Forward * 8f;
			Torch.WorldPosition = BodyForward.WorldPosition + forwardOffset;
			Torch.WorldRotation = BodyForward.WorldRotation;
		}
		else
		{
			Torch.WorldPosition = Scene.Camera.WorldPosition;
			Torch.WorldRotation = Scene.Camera.WorldRotation;
		}
	}
	
	public Vector3 GetListenerPosition()
	{
		return ListenerTarget.IsValid() ? ListenerTarget.WorldPosition : WorldPosition;
	}
}
