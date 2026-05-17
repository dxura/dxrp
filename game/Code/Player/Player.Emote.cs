using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public partial class Player
{
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnEmoteChanged ) )]
	public EmoteResource? CurrentEmote { get; set; }

	private TimeUntil _emoteTimeRemaining;
	private TimeUntil _emotePositionCheck;
	private Vector3 _emoteLastPosition;

	private bool _wasFirstPersonPriorEmote;

	public void PlayEmoteHost( EmoteResource emote )
	{
		Assert.True( Networking.IsHost );

		if ( !emote.IsValid() || IsDead || HasStatus( Constants.FreezeStatus ) )
		{
			return;
		}

		// Cancel any existing emote first
		if ( CurrentEmote.IsValid() )
		{
			StopEmoteHost();
		}

		Holster();

		_emoteLastPosition = WorldPosition;
		_emotePositionCheck = 1f;
		_emoteTimeRemaining = emote.Duration;
		CurrentEmote = emote;
	}

	public void StopEmoteHost()
	{
		Assert.True( Networking.IsHost );

		if ( !CurrentEmote.IsValid() )
		{
			return;
		}

		CurrentEmote = null;
	}

	private void OnEmoteChanged( EmoteResource? oldEmote, EmoteResource? newEmote )
	{
		if ( !EmoteRenderer.IsValid() || !Renderer.IsValid() )
		{
			return;
		}

		if ( newEmote.IsValid() )
		{
			StartEmoteVisual( newEmote );
		}
		else
		{
			StopEmoteVisual();
		}
	}

	private void StartEmoteVisual( EmoteResource emote )
	{
		if ( !EmoteRenderer.IsValid() )
		{
			return;
		}

		EmoteRenderer.Enabled = true;

		Renderer.BoneMergeTarget = EmoteRenderer;
		
		EmoteRenderer.Sequence.Name = emote.SequenceName;
		EmoteRenderer.Sequence.Looping = emote.Repeat;

		if ( IsLocalPlayer )
		{
			_wasFirstPersonPriorEmote = !IsThirdPersonPreferred;
			IsThirdPersonPreferred = true;
			EnterThirdPerson();
		}
	}

	private void StopEmoteVisual()
	{
		if ( !EmoteRenderer.IsValid() )
		{
			return;
		}

		// Restore body renderer
		Renderer.BoneMergeTarget = null;

		EmoteRenderer.Enabled = false;

		if ( IsLocalPlayer && _wasFirstPersonPriorEmote )
		{
			IsThirdPersonPreferred = false;
			EnterFirstPerson();
		}
	}

	private void OnUpdateEmote()
	{
		if ( !CurrentEmote.IsValid() )
		{
			return;
		}

		if ( Networking.IsHost )
		{
			if ( CurrentEmote.CancelOnMove && _emotePositionCheck <= 0 )
			{
				if ( WorldPosition.Distance( _emoteLastPosition ) > 5f )
				{
					StopEmoteHost();
					return;
				}

				_emoteLastPosition = WorldPosition;
				_emotePositionCheck = 1f;
			}

			if ( !CurrentEmote.Repeat && _emoteTimeRemaining <= 0 )
			{
				StopEmoteHost();
			}
		}
	}
}
