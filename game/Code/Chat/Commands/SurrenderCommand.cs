namespace Dxura.RP.Game.Commands;

public class SurrenderCommand : ICommand
{
	public string Command => "surrender";
	public string[] Aliases => ["handsup", "hu"];
	public string Help => "Put your hands up and surrender";
	public bool IsUsableWhileDead => false;
	public bool IsUsableWhileFrozen => false;

	public bool ExecuteHost( Player caller, string[] args, string raw )
	{
		if ( !caller.IsValid() )
		{
			return false;
		}

		if ( caller.HasStatus( Constants.SurrenderStatus ) )
		{
			var cooldownKey = $"{caller.SteamId}:surrender:undo";
			if ( Cooldown.Current.CheckAndStartCooldown( cooldownKey, 30f ) )
			{
				var remaining = Cooldown.Current.GetRemainingTime( cooldownKey );
				caller.SendMessage( string.Format( Language.GetPhrase( "surrender.cooldown" ), remaining ) );
				return true;
			}

			caller.RemoveStatus( Constants.SurrenderStatus );
		}
		else
		{
			if ( caller.Sitting )
			{
				caller.SendMessage( Language.GetPhrase( "surrender.sitting" ) );
				return true;
			}

			caller.AddStatus( Constants.SurrenderStatus );
		}

		return true;
	}
}
