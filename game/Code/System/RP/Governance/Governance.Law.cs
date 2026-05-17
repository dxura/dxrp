using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

public partial class Governance
{
	[Sync( SyncFlags.FromHost )]
	private NetList<string> Laws { get; set; } = new();

	private bool _lawsDefault = true;

	private void OnStartLaw()
	{
		SetDefaultLawsHost();
	}

	private void OnMayorAbsentLaw()
	{
		if ( !_lawsDefault )
		{
			SetDefaultLawsHost( announce: true );
		}

		// Also disable lockdown if active
		var lockdownSystem = LockdownSystem.Instance;
		if ( lockdownSystem.IsValid() && lockdownSystem.Lockdown )
		{
			lockdownSystem.Lockdown = false;
		}
	}


	[Rpc.Host]
	public void AddLawHost( string lawDescription )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( "global:law:change", Config.Current.Game.LawChange ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );

		if ( caller == null || !caller.Job.IsMayoralRole() )
		{
			return;
		}

		if ( lawDescription.Length is < 5 or > 60 )
		{
			caller.Error( "#notify.law.length" );
			return;
		}

		if ( Laws.Count >= Config.Current.Game.MaxLaws )
		{
			caller.Error( "#notify.law.max" );
			return;
		}

		lawDescription = GameManager.ModerateText( caller.SteamId, "ADD LAW", lawDescription );

		_lawsDefault = false;
		Laws.Add( lawDescription );
		BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.law.added.announcement" ), Language.GetPhrase( lawDescription ) == lawDescription && lawDescription.StartsWith( "#" ) ? Language.GetPhrase( lawDescription[1..] ) : Language.GetPhrase( lawDescription ) ) );
		caller.Success( "#notify.law.added" );
		Log.Info( $"Mayor {caller.DisplayName} added law: {lawDescription}" );
		_ = ServerApiClient.Audit( "AddLaw", $"{caller.SteamName} ({caller.SteamId}) added law: {lawDescription}", caller.SteamId);
	}

	[Rpc.Host]
	public void RemoveLawHost( int lawIndex )
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( "global:law:change", Config.Current.Game.LawChange ) )
		{
			return;
		}

		var caller = GameUtils.GetPlayerByConnectionId( callerId );

		if ( caller == null || !caller.Job.IsMayoralRole() )
		{
			return;
		}

		var lawCount = Laws.Count;
		if ( lawCount == 0 )
		{
			caller.Error( "#notify.law.remove.none" );
			return;
		}

		if ( lawIndex < 0 || lawIndex >= lawCount )
		{
			caller.Error( "#notify.law.remove.none" );
			return;
		}

		var removedLaw = Laws[lawIndex];
		_lawsDefault = false;
		Laws.RemoveAt( lawIndex );
		BroadcastGovernanceAnnouncementHost( string.Format( Language.GetPhrase( "governance.law.removed.announcement" ), Language.GetPhrase( removedLaw ) == removedLaw && removedLaw.StartsWith( "#" ) ? Language.GetPhrase( removedLaw[1..] ) : Language.GetPhrase( removedLaw ) ) );
		caller.Success( "#notify.law.removed" );
		Log.Info( $"Mayor {caller.DisplayName} removed law #{lawIndex + 1}" );
		_ = ServerApiClient.Audit( "RemoveLaw", $"{caller.SteamName} ({caller.SteamId}) removed law: {removedLaw}", caller.SteamId);
	}

	private void SetDefaultLawsHost( bool announce = false )
	{
		Assert.True( Networking.IsHost );

		Laws.Clear();

		// Add default laws
		foreach ( var law in Config.Current.Game.DefaultLaws )
		{
			Laws.Add( law );
		}

		_lawsDefault = true;
		if ( announce )
		{
			Log.Info( "Mayor missing, laws reset to default." );
			BroadcastGovernanceAnnouncementHost( Language.GetPhrase( "governance.law.reset.announcement" ) );
		}
	}

	public IEnumerable<string> GetAllLaws()
	{
		return Laws;
	}
}
