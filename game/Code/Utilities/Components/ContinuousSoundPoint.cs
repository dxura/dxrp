namespace Dxura.RP.Game;

public class ContinuousSoundPoint : Component, IOcclusionEvents
{
	[Property]
	[Group( "Sound" )]
	public required SoundEvent SoundEvent { get; set; }

	private SoundHandle? _soundHandle;

	protected override void OnStart()
	{
		UpdatePlaybackState( GameObject.Tags.Has( Constants.OccludeTag ) );
	}

	protected override void OnUpdate()
	{
		if ( _soundHandle.IsValid() )
		{
			_soundHandle.Position = WorldPosition;
		}
	}

	public void OnOcclusionChanged( bool occlude )
	{
		UpdatePlaybackState( occlude );
	}

	private void UpdatePlaybackState( bool occluded )
	{
		if ( GameManager.IsHeadless || occluded || !Enabled )
		{
			StopSound();
			return;
		}

		StartSound();
	}

	private void StartSound()
	{
		if ( _soundHandle.IsValid() || !SoundEvent.IsValid() )
		{
			return;
		}

		_soundHandle = Sound.Play( SoundEvent, WorldPosition );
	}

	private void StopSound()
	{
		_soundHandle?.Stop( 0.1f );
		_soundHandle = null;
	}

	protected override void OnDestroy()
	{
		_soundHandle?.Stop( 0.1f );
		_soundHandle = null;

		base.OnDestroy();
	}

	protected override void OnDisabled()
	{
		_soundHandle?.Stop( 0.1f );
		_soundHandle = null;

		base.OnDisabled();
	}
}
