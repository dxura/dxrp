namespace Dxura.RP.Game;

/// <summary>
/// A component that destroys its GameObject after a specified date.
/// </summary>
public class DateExpiry : Component
{
	[Property]
	public DateTimeOffset ExpiryDate { get; set; }

	protected override void OnStart()
	{
		if ( IsExpired() )
		{
			GameObject.Destroy();
		}
	}

	private bool IsExpired()
	{
		return DateTime.UtcNow > ExpiryDate;
	}
}
