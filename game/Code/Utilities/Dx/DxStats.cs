namespace Dxura.RP.Game;

public static class DxStats
{
	public static string GetStatKey( string stat )
	{
		var tenantId = ServerApiLink.Current?.TenantId;
		var isOfficial = string.IsNullOrEmpty( tenantId ) || tenantId.Equals( Constants.OfficialTenantId, StringComparison.InvariantCultureIgnoreCase );

		// Increment network-specific stats (if possible), otherwise set global for official
		return isOfficial ? stat : $"{stat}-{tenantId}";
	}
}
