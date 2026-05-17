using Sandbox.Audio;
namespace Dxura.RP.Game.Wire;

[Title( "Speaker" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class SpeakerWire() : BaseWireConstruct( ConstructType.SpeakerWire ), IWireEvents
{
	private SpeakerWireData _data = new();
	private SoundEvent? _soundEvent;
	private SoundHandle? _soundHandle;
	private TimeUntil _timeUntilRepeat;

	private Mixer SoundMixer => Mixer.FindMixerByName( "Wire" ) ?? Mixer.Master;

	public override string Name => $"Speaker ({_data.Sound})";

	[WireInput( "emit" )]
	public bool Emit
	{
		set
		{
			if ( value )
			{
				BroadcastPlaySound();
			}
			else
			{
				BroadcastStopSound();
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	[WireInput( "play" )]
	public bool Play
	{
		set
		{
			if ( value )
			{
				BroadcastPlaySound();
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	[WireInput( "stop" )]
	public bool Stop
	{
		set
		{
			if ( value )
			{
				BroadcastStopSound();
			}
		}
		get => false; // This is just a trigger, no need to store state
	}

	[WireOutput( "playing" )]
	public bool Playing { get; private set; }

	void IWireEvents.OnWireTick()
	{
		if ( !Playing )
		{
			return;
		}

		if ( _soundHandle.IsValid() && _soundHandle.IsPlaying )
		{
			return;
		}

		switch ( _data.Loop )
		{
			case true when _timeUntilRepeat <= 0.0f:
				// Restart the sound for looping
				BroadcastPlaySound();
				break;
			case false:
				// Non-looping sound finished
				Playing = false;
				break;
		}
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as SpeakerWireData ?? new SpeakerWireData();

		_soundEvent = new SoundEvent( _data.Sound );
		if ( _soundEvent.IsValid() )
		{
			_soundEvent.Falloff = Curve.Linear.Reverse();
			_soundEvent.Distance = _data.Distance;
		}
		else
		{
			Log.Warning( $"Invalid sound event for wire speaker: {_data.Sound}" );
		}
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastPlaySound()
	{
		PlaySound();
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastStopSound()
	{
		StopSound();
	}

	private void PlaySound()
	{
		StopSound();

		_soundHandle = _soundEvent.Play( WorldPosition, GameObject );

		if ( !_soundHandle.IsValid() )
		{
			return;
		}

		_soundHandle.TargetMixer = SoundMixer;
		_soundHandle.Volume = _data.Volume;
		_soundHandle.Pitch = _data.Pitch;

		// Set up repeat timing for looping
		if ( _data.Loop )
		{
			_timeUntilRepeat = 0.1f; // Small delay before checking for repeat
		}

		Playing = true;
	}

	private void StopSound( bool isDestroying = false )
	{
		if ( _soundHandle.IsValid() && _soundHandle.IsPlaying )
		{
			_soundHandle.Stop();
		}

		_soundHandle = null;
		_timeUntilRepeat = 0.0f;

		if ( !isDestroying )
		{
			Playing = false;
		}
	}

	protected override void OnDestroy()
	{
		StopSound( true );
		base.OnDestroy();
	}
}
