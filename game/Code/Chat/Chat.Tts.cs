using Sandbox.Audio;
using Sandbox.Speech;
using System.Text;
namespace Dxura.RP.Game;

public sealed partial class Chat
{
	private const int TtsMaxLength = 300;

	[ConVar( "dx_chat_tts", ConVarFlags.Saved )]
	private static bool IsTtsEnabled { get; set; } = true;

	private SoundHandle? _activeTtsHandle;

	private readonly Dictionary<string, string> _abbreviations = new()
	{
		{
			"idc", "I don't care"
		},
		{
			"ily", "I love you"
		},
		{
			"bruh", "bro"
		},
		{
			"bai", "bye"
		},
		{
			"bc", "because"
		},
		{
			"cmon", "come on"
		},
		{
			"ty", "thank you"
		},
		{
			"wsp", "whats up"
		},
		{
			"smth", "something"
		},
		{
			"thx", "thanks"
		},
		{
			"yw", "you're welcome"
		},
		{
			"np", "no problem"
		},
		{
			"gtg", "gotta go"
		},
		{
			"dw", "don't worry"
		},
		{
			"rn", "right now"
		},
		{
			"ttyl", "talk to you later"
		},
		{
			"ppl", "people"
		},
		{
			"ngl", "not gonna lie"
		},
		{
			"tbh", "to be honest"
		},
		{
			"tbf", "to be fair"
		},
		{
			"wdym", "what do you mean"
		},
		{
			"wdyt", "what do you think"
		},
		{
			"wya", "where you at"
		},
		{
			"imo", "in my opinion"
		},
		{
			"imho", "in my humble opinion"
		},
		{
			"idek", "I don't even know"
		},
		{
			"idk", "I don't know"
		},
		{
			"cya", "see you"
		},
		{
			"wtf", "what the fuck"
		},
		{
			"wth", "what the hell"
		},
		{
			"smh", "shaking my head"
		},
		{
			"afaik", "as far as I know"
		},
		{
			"iirc", "if I recall correctly"
		},
		{
			"brb", "be right back"
		},
		{
			"rofl", "rolling on the floor laughing"
		},
		{
			"lmao", "laughing my ass off"
		},
		{
			"btw", "by the way"
		},
		{
			"omg", "oh my god"
		},
		{
			"omw", "on my way"
		},
		{
			"fyi", "for your information"
		},
		{
			"asap", "as soon as possible"
		},
		{
			"dm", "direct message"
		},
		{
			"irl", "in real life"
		},
		{
			"jk", "just kidding"
		},
		{
			"ikr", "I know right"
		},
		{
			"lmk", "let me know"
		},
		{
			"fomo", "fear of missing out"
		},
		{
			"tldr", "too long didn't read"
		},
		{
			"eta", "estimated time of arrival"
		},
		{
			"pov", "point of view"
		},
		{
			"rip", "rest in peace"
		},
		{
			"bff", "best friends forever"
		},
		{
			"tmr", "tomorrow"
		},
		{
			"tmrw", "tomorrow"
		},
		{
			"pls", "please"
		},
		{
			"msg", "message"
		},
		{
			"ofc", "of course"
		},
		{
			"nvm", "never mind"
		},
		{
			"tbd", "to be determined"
		},
		{
			"gl", "good luck"
		},
		{
			"hmu", "hit me up"
		},
		{
			"def", "definitely"
		},
		{
			"aka", "also known as"
		},
		{
			"ftw", "for the win"
		}
	};
	
	private readonly MixerHandle _ttsMixer = Mixer.FindMixerByName( "TTS" );

	private void DoTts( string message, Player author )
	{
		if ( !Config.Current.Game.LocalChatTts || !IsTtsEnabled )
		{
			return;
		}

		try
		{
			if ( _activeTtsHandle.IsValid() )
			{
				_activeTtsHandle.Stop();
			}

			var synthMessage = SanitizeTtsText( DeAbbreviateText( message ), TtsMaxLength );
			if ( string.IsNullOrWhiteSpace( synthMessage ) )
			{
				return;
			}

			Synthesizer synthesizer = new();
			synthesizer.TrySetVoice( "Microsoft David Desktop" );
			synthesizer.WithText( synthMessage );

			var sound = synthesizer.Play();

			if ( author.GameObject.IsValid() )
			{
				sound.Parent = author.GameObject;
				sound.TargetMixer = _ttsMixer.GetOrDefault();
				sound.FollowParent = true;
			}

			sound.Volume = DxSound.TtsVolume;

			_activeTtsHandle = sound;
		}
		catch
		{
			// ignored
		}
	}

	private void StopTts()
	{
		_activeTtsHandle?.Stop();
		_activeTtsHandle = null;
	}

	private string DeAbbreviateText( string message )
	{
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			return string.Empty;
		}

		var tokens = message.Split( ' ', StringSplitOptions.RemoveEmptyEntries );

		for ( var i = 0; i < tokens.Length; i++ )
		{
			var lower = tokens[i].ToLowerInvariant();
			if ( _abbreviations.TryGetValue( lower, out var full ) )
			{
				tokens[i] = full.Trim();
			}
		}

		return string.Join( ' ', tokens );
	}

	private static string SanitizeTtsText( string message, int maxLength )
	{
		if ( string.IsNullOrWhiteSpace( message ) || maxLength <= 0 )
		{
			return string.Empty;
		}

		var sanitized = new StringBuilder( Math.Min( message.Length, maxLength ) );
		var lastWasSpace = true;

		foreach ( var c in message )
		{
			if ( char.IsWhiteSpace( c ) )
			{
				if ( lastWasSpace )
				{
					continue;
				}

				sanitized.Append( ' ' );
				lastWasSpace = true;
			}
			else if ( c is >= ' ' and <= '~' )
			{
				sanitized.Append( c );
				lastWasSpace = false;
			}
			else
			{
				continue;
			}

			if ( sanitized.Length >= maxLength )
			{
				break;
			}
		}

		return sanitized.ToString().Trim();
	}
}
