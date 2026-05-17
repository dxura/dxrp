using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Entities;

[Title( "Dice" )]
[Category( "Entities" )]
public class DiceEntity : BaseEntity, IContextualObject, Component.IPressable
{

	[Sync( SyncFlags.FromHost )]
	private int RollValue { get; set; } = 0;

	[Sync( SyncFlags.FromHost )]
	private TimeSince LastRoll { get; set; } = 1000f;

	[Property] private int DiceValueShowDuration { get; set; } = 3;

	[Property] private SoundEvent? RollSound { get; set; }

	private string _displayText => LastRoll.Relative <= DiceValueShowDuration ? $"{RollValue}!" : "#entity.dice.roll";

	public bool Press( IPressable.Event e )
	{
		if ( Cooldown.Current.CheckAndStartCooldown( "action:quick", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return false;
		}

		OnUseHost();

		return true;
	}

	[Rpc.Host]
	private void OnUseHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:action:quick", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{GameObject.Id}:dice:roll", Config.Current.Game.DiceCooldown ) )
		{
			var player = GameUtils.GetPlayerByConnectionId( callerId );
			player?.Error( "#notify.dice.cooldown" );
			return;
		}

		// Set specific rotation values for dice faces (1-6)
		var roll = Random.Shared.Next( 1, 7 );
		Rotation faceRotation;

		switch ( roll )
		{
			case 1:
				faceRotation = Rotation.FromAxis( Vector3.Right, 180 );
				break;
			case 2:
				faceRotation = Rotation.FromAxis( Vector3.Forward, -90 );
				break;
			case 3:
				faceRotation = Rotation.FromAxis( Vector3.Right, -90 );
				break;
			case 4:
				faceRotation = Rotation.FromAxis( Vector3.Right, 90 );
				break;
			case 5:
				faceRotation = Rotation.FromAxis( Vector3.Forward, 90 );
				break;
			case 6:
				faceRotation = Rotation.Identity;
				break;
			default:
				faceRotation = Rotation.Identity;
				break;
		}


		RollValue = roll;
		LastRoll = 0;
		RollSound.Broadcast( WorldPosition );

		OnUseOwner( faceRotation );
	}


	[Rpc.Owner( NetFlags.HostOnly | NetFlags.Reliable )]
	private void OnUseOwner( Rotation rotation )
	{
		WorldRotation = rotation;
	}

	public string DisplayText => _displayText;
	public Vector3 ContextPosition => WorldPosition + Vector3.Up * 10f;

	public bool LookOpacity => false;
	public float ContextMaxDistance => 120f;

	public override bool CanScale( Player player )
	{
		if ( !this.IsValid() || !player.IsValid() )
		{
			return false;
		}

		return GameUtils.HasPermission( player.SteamId, GameObject );
	}
}
