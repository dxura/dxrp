using Dxura.RP.Game.PostProcess;
using System.Text;

namespace Dxura.RP.Game.Statuses;

public class DrunkStatus : BaseStatus
{
	public override string Id => Constants.DrunkStatus;
	public override string Name => "Drunk";
	public override string? MaterialIcon => "local_bar";
	public override Color Color => Color.FromRgb( 0xF4A460 );

	public override float? DefaultDuration => 180f;

	public override bool RemoveOnRespawn => true;

	// Stacking - 3 beers and you're fucked
	public override bool Stackable => true;
	public override int MaxStacks => 3;

	private DrunkPostProcess? _postProcess;

	private float _currentIntensity;
	private float _targetIntensity;

	private const float FadeInSpeed = 2f;

	public override void OnAddedOwner( Player player )
	{
		_postProcess = player.Scene.Camera?.GameObject.Components.GetOrCreate<DrunkPostProcess>();

		var stacks = player.GetStatusStacks( Constants.DrunkStatus );
		_targetIntensity = GetIntensity( stacks );
	}

	public override void OnRemovedOwner( Player player )
	{
		if ( _postProcess is not null )
		{
			_postProcess.Destroy();
			_postProcess = null;
		}

		player.HueRotateTarget = 0f;

		player.Controller.WalkSpeed = GameConfig.WalkSpeed;
		player.Controller.RunSpeed = GameConfig.RunSpeed;
		player.Controller.DuckedSpeed = GameConfig.DuckedSpeed;
	}

	public override void OnUpdateOwner( Player player )
	{
		var stacks = player.GetStatusStacks( Constants.DrunkStatus );
		_targetIntensity = GetIntensity( stacks );

		// Gradually ramp intensity toward target
		_currentIntensity = float.Lerp( _currentIntensity, _targetIntensity, FadeInSpeed * Time.Delta );

		if ( _postProcess is not null )
		{
			_postProcess.Intensity = _currentIntensity;
		}

		ApplyMovement( player, stacks );
	}

	public override string ModifyChat( Player player, string message, MessageType messageType )
	{
		if ( messageType != MessageType.LocalChat )
		{
			return message;
		}

		var scrambleChance = CurrentStacks switch
		{
			1 => 0.05f,
			2 => 0.15f,
			_ => 0.30f
		};

		var chars = message.ToCharArray();
		for ( var i = 0; i < chars.Length; i++ )
		{
			if ( char.IsWhiteSpace( chars[i] ) || Random.Shared.Float() > scrambleChance )
			{
				continue;
			}

			if ( char.IsLetter( chars[i] ) )
			{
				// Swap with a nearby character in the alphabet
				var offset = Random.Shared.Int( -3, 3 );
				var isUpper = char.IsUpper( chars[i] );
				var baseLetter = char.ToLower( chars[i] ) - 'a';
				var scrambled = (char)('a' + ((baseLetter + offset + 26) % 26));
				chars[i] = isUpper ? char.ToUpper( scrambled ) : scrambled;
			}
			else if ( char.IsDigit( chars[i] ) )
			{
				chars[i] = (char)('0' + Random.Shared.Int( 0, 9 ));
			}
		}

		// At high stacks, randomly double some letters (slurring)
		if ( CurrentStacks >= 2 )
		{
			var result = new StringBuilder();
			var slurChance = CurrentStacks >= 3 ? 0.12f : 0.05f;

			foreach ( var c in chars )
			{
				result.Append( c );
				if ( char.IsLetter( c ) && Random.Shared.Float() < slurChance )
				{
					result.Append( c );
				}
			}

			return result.ToString();
		}

		return new string( chars );
	}

	private static float GetIntensity( int stacks )
	{
		return stacks switch
		{
			1 => 1f,
			2 => 2.5f,
			_ => 5f
		};
	}

	// 20% / 35% / 50% damage reduction per stack
	public override float ModifyDamageTaken( Player player )
	{
		return CurrentStacks switch
		{
			1 => 0.80f,
			2 => 0.65f,
			_ => 0.50f
		};
	}

	private void ApplyMovement( Player player, int stacks )
	{
		// Hue rotation scales hard at max
		player.HueRotateTarget = stacks switch
		{
			1 => 10f,
			2 => 25f,
			_ => 50f
		};

		// 1 stack: 85% speed, 2 stacks: 50% speed, 3 stacks: 15% speed (can barely move)
		float speedMult = stacks switch
		{
			1 => 0.85f,
			2 => 0.50f,
			_ => 0.15f
		};

		player.Controller.WalkSpeed = GameConfig.WalkSpeed * speedMult;
		player.Controller.RunSpeed = GameConfig.RunSpeed * speedMult;
		player.Controller.DuckedSpeed = GameConfig.DuckedSpeed * speedMult;
	}
}
