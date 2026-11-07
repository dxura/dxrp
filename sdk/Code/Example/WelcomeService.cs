namespace Dxura.RP.Game.Addons.Example;

/// <summary>
/// Code-only example: created automatically on the core systems object, no content entry needed.
/// </summary>
[AddonService]
public sealed class WelcomeService : Component, IGameEvents
{
	protected override void OnStart()
	{
		Log.Info( "[Example] Welcome service started" );
	}

	public void OnPlayerJoined( Player player )
	{
		if ( !player.IsValid() )
		{
			return;
		}

		player.SendMessage( "Welcome! Try /visits, the Example tab, and the Coin Flip phone app." );
	}
}
