namespace Dxura.RP.Game;

/// <summary>
///     A simple component that destroys its GameObject.
/// </summary>
public sealed class TimedDestroyComponent : Component
{
	/// <summary>
	///     How long until we destroy the GameObject.
	/// </summary>
	[Property]
	public float Time { get; set; } = 1f;

	/// <summary>
	///     The real time until we destroy the GameObject.
	/// </summary>
	[Property]
	[ReadOnly]
	private TimeUntil TimeUntilDestroy { get; set; } = 0;

	[Property]
	public bool ServerSideOnly { get; set; }

	protected override void OnStart()
	{
		TimeUntilDestroy = Time;
	}

	public void ResetTimer()
	{
		TimeUntilDestroy = Time;
	}

	protected override void OnUpdate()
	{
		if ( ServerSideOnly && !Networking.IsHost )
		{
			return;
		}

		if ( TimeUntilDestroy )
		{
			GameObject.Destroy();
		}
	}
}

public static partial class GameObjectExtensions
{
	/// <summary>
	///     Creates a <see cref="TimedDestroyComponent" /> which will deferred delete the <see cref="GameObject" />.
	/// </summary>
	/// <param name="self"></param>
	/// <param name="serverSide">Should this only be destroyed by the host</param>
	/// <param name="seconds"></param>
	public static void DestroyAsync( this GameObject self, float seconds = 1.0f, bool serverSide = false )
	{
		var component = self.Components.Create<TimedDestroyComponent>();
		component.ServerSideOnly = serverSide;
		component.Time = seconds;
	}
}
