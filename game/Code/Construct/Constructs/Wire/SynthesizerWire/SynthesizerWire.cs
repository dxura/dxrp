using Sandbox.Audio;
using Sandbox.Speech;

namespace Dxura.RP.Game.Wire;

[Title( "Synthesizer" )]
[Category( "Wire" )]
[Icon( "cable" )]
public class SynthesizerWire() : BaseWireConstruct( ConstructType.SynthesizerWire )
{
	private SynthesizerWireData _data = new();
	private SoundHandle? _activeTtsHandle;
	private string _lastText = string.Empty;

	private TimeSince _timeSinceLastTts;

	private Mixer SoundMixer => Mixer.FindMixerByName( "Wire" ) ?? Mixer.Master;

	public override string Name => "Synthesizer";

	[WireInput( "text" )]
	public string Text
	{
		set
		{
			if ( string.IsNullOrWhiteSpace( value ) )
			{
				return;
			}

			// Store the text for later playback
			_lastText = value;

			// Autoplay if enabled
			if ( _data.AutoPlay )
			{
				if ( Cooldown.Current.CheckAndStartCooldown( $"{Id}:synthesize", Config.Current.Game.WireSynthesizeCooldown ) )
				{
					return;
				}

				BroadcastSynthesizeText( value );
			}
		}
		get => string.Empty;
	}

	[WireInput( "play" )]
	public bool Play
	{
		set
		{
			if ( !value || string.IsNullOrWhiteSpace( _lastText ) )
			{
				return;
			}

			if ( Cooldown.Current.CheckAndStartCooldown( $"{Id}:synthesize", Config.Current.Game.WireSynthesizeCooldown ) )
			{
				return;
			}

			BroadcastSynthesizeText( _lastText );
		}
		get => false;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void BroadcastSynthesizeText( string text )
	{
		SynthesizeText( text );
	}

	public override void OnSecondlyUpdate()
	{
		base.OnSecondlyUpdate();

		// Auto-stop after 10 seconds
		if ( _activeTtsHandle.IsValid() && _timeSinceLastTts > 10f )
		{
			_activeTtsHandle.Stop();
			_activeTtsHandle = null;
		}
	}

	public override void OnOcclusionChanged( bool occlude )
	{
		base.OnOcclusionChanged( occlude );

		if ( !occlude || !_activeTtsHandle.IsValid() )
		{
			return;
		}

		_activeTtsHandle.Stop();
		_activeTtsHandle = null;
	}

	private void SynthesizeText( string text )
	{
		// Don't play if occluded
		if ( GameObject.Tags.Has( Constants.OccludeTag ) )
		{
			return;
		}

		// Stop previous TTS, one at a time
		if ( _activeTtsHandle.IsValid() )
		{
			_activeTtsHandle.Stop();
			_activeTtsHandle = null;
		}

		try
		{
			var synth = new Synthesizer();
			synth.WithText( text );

			var sound = synth.Play();

			if ( !sound.IsValid() )
			{
				return;
			}

			sound.Parent = GameObject;
			sound.FollowParent = true;
			sound.Volume = _data.Volume;
			sound.Pitch = _data.Pitch;
			sound.TargetMixer = SoundMixer;

			_activeTtsHandle = sound;
			_timeSinceLastTts = 0f;
		}
		catch
		{
			// Ignore TTS errors
		}
	}

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		_data = newData as SynthesizerWireData ?? new SynthesizerWireData();
	}

	protected override void OnDestroy()
	{
		if ( _activeTtsHandle.IsValid() )
		{
			_activeTtsHandle.Stop();
		}
		_activeTtsHandle = null;

		base.OnDestroy();
	}
}
