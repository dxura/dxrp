namespace Dxura.RP.Game;

public partial class Player
{
	[Sync( SyncFlags.FromHost )]
	[Property]
	[Group( "Score" )]
	[ReadOnly]
	public int Kills { get; set; }

	[Sync( SyncFlags.FromHost )]
	[Property]
	[Group( "Score" )]
	[ReadOnly]
	public int Deaths { get; set; }


	private void OnKillScoreHost( Component victim, DamageInfo damage )
	{
		var damageInfo = damage;

		if ( !damageInfo.Attacker.IsValid() )
		{
			return;
		}

		if ( !damageInfo.Victim.IsValid() )
		{
			return;
		}

		if ( !IsValid )
		{
			return;
		}

		var killerPlayer = GameUtils.GetPlayerFromComponent( damageInfo.Attacker );
		var victimPlayer = GameUtils.GetPlayerFromComponent( damageInfo.Victim );

		if ( !victimPlayer.IsValid() )
		{
			return;
		}

		if ( !killerPlayer.IsValid() )
		{
			if ( victimPlayer == this )
			{
				Deaths++;
			}

			return;
		}

		if ( killerPlayer != this )
		{
			killerPlayer.Kills++;
		}

		if ( victimPlayer == this )
		{
			Deaths++;
		}
	}
}
