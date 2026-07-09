namespace Dxura.RP.Game;

public partial class Player
{
	private const float AfkInputReportInterval = 1f;
	private const float AfkLookInputThreshold = 0.01f;

	private static readonly string[] AfkActivityActions =
	[
		"Forward",
		"Backward",
		"Left",
		"Right",
		"Jump",
	];

	private TimeSince _timeSinceLastAfkInputReport = AfkInputReportInterval;
	private int _localAfkInputActivitySequence;

	/// <summary>
	///     When the player became AFK. Synced from host; null when not AFK.
	/// </summary>
	[Sync( SyncFlags.FromHost )]
	public TimeSince? AfkSince { get; set; }

	public int AfkInputActivitySequence { get; private set; }

	public float AfkDuration => AfkSince?.Relative ?? 0f;

	public string GetAfkDurationText() => TimeUtils.Format( AfkDuration, TimeDisplayFormat.Duration );

	private void OnUpdateAfkOwner()
	{
		if ( !HasAfkInputActivity() || _timeSinceLastAfkInputReport < AfkInputReportInterval )
		{
			return;
		}

		_timeSinceLastAfkInputReport = 0f;

		unchecked
		{
			_localAfkInputActivitySequence++;
		}

		ReportAfkInputActivityHost( _localAfkInputActivitySequence );
	}

	private static bool HasAfkInputActivity()
	{
		if ( MathF.Abs( Input.AnalogLook.pitch ) > AfkLookInputThreshold ||
		     MathF.Abs( Input.AnalogLook.yaw ) > AfkLookInputThreshold ||
		     Input.MouseWheel.y != 0f )
		{
			return true;
		}

		return AfkActivityActions.Any( action => Input.Down( action ) );
	}

	[Rpc.Host]
	private void ReportAfkInputActivityHost( int activitySequence )
	{
		if ( Rpc.Caller != Network.Owner )
		{
			return;
		}

		AfkInputActivitySequence = activitySequence;
	}
}
