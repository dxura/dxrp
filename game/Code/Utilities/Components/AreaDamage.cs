namespace Dxura.RP.Game;

public interface IAreaDamageReceiver : IValid
{
	Guid Id { get; }
	GameObject GameObject { get; }
	void ApplyAreaDamage( AreaDamage component );
}

[Title( "Area Damage" )]
public class AreaDamage : Component
{
	public Component Attacker { get; set; } = null!;
	public Component Inflictor { get; set; } = null!;

	private Dictionary<Guid, AreaDamageTarget> Targets { get; } = new();
	private HashSet<Guid> TargetsToRemove { get; } = new();

	/// <summary>
	///     Radius of the area damage.
	/// </summary>
	[Property]
	public float Radius { get; set; } = 150f;

	/// <summary>
	///     How much damage to deal each interval.
	/// </summary>
	[Property]
	public float Damage { get; set; } = 10f;

	/// <summary>
	///     How often to deal damage while in range (in seconds.)
	/// </summary>
	[Property]
	public float Interval { get; set; } = 0.5f;

	/// <summary>
	///     Limit to duration of area damage
	/// </summary>
	[Property]
	public float TimeLimit { get; set; } = 120f;

	/// <summary>
	///     Ignore any <see cref="IAreaDamageReceiver" /> targets with any of these tags.
	/// </summary>
	[Property]
	public TagSet? IgnoreTags { get; set; }

	/// <summary>
	///     Whether to require line of sight to apply damage
	/// </summary>
	[Property]
	public bool RequireLineOfSight { get; set; } = true;

	private TimeSince TimeSinceCreation { get; } = 0;

	[Property] public DamageFlags DamageFlags { get; set; } = DamageFlags.None;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !Networking.IsHost )
		{
			return;
		}

		if ( TimeSinceCreation >= TimeLimit )
		{
			return;
		}

		foreach ( var receiver in FindReceiversInRadius() )
		{
			if ( IgnoreTags is not null && receiver.GameObject.Tags.HasAny( IgnoreTags ) )
			{
				continue;
			}

			if ( RequireLineOfSight && !HasLineOfSight( receiver.GameObject ) )
			{
				continue;
			}

			if ( !Targets.TryGetValue( receiver.Id, out var target ) )
			{
				target = new AreaDamageTarget
				{
					Receiver = receiver, NextDamageTime = 0f
				};
				Targets[receiver.Id] = target;
			}

			if ( !target.NextDamageTime )
			{
				continue;
			}

			target.LastDamageTime = 0f;
			target.NextDamageTime = Interval;
			target.Receiver.ApplyAreaDamage( this );
		}

		// Clean up stale targets
		foreach ( var (id, target) in Targets )
		{
			if ( target.Receiver.IsValid() && target.LastDamageTime <= Interval * 2f )
			{
				continue;
			}

			TargetsToRemove.Add( id );
		}

		foreach ( var id in TargetsToRemove )
		{
			Targets.Remove( id );
		}

		TargetsToRemove.Clear();
	}

	private IEnumerable<IAreaDamageReceiver> FindReceiversInRadius()
	{
		var seen = new HashSet<Guid>();

		var overlapping = Scene.FindInPhysics( new Sphere( WorldPosition, Radius ) );
		foreach ( var go in overlapping )
		{
			var receiver = go.Root.Components.GetInDescendantsOrSelf<IAreaDamageReceiver>();
			if ( !receiver.IsValid() || !seen.Add( receiver.Id ) )
			{
				continue;
			}

			yield return receiver;
		}

		foreach ( var receiver in Scene.GetAll<IAreaDamageReceiver>() )
		{
			if ( !receiver.IsValid() || !seen.Add( receiver.Id ) )
			{
				continue;
			}

			if ( receiver.GameObject.WorldPosition.Distance( WorldPosition ) > Radius )
			{
				continue;
			}

			yield return receiver;
		}
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Red.WithAlpha( 0.3f );
		Gizmo.Draw.LineSphere( Vector3.Zero, Radius );
	}

	private bool HasLineOfSight( GameObject targetGameObject )
	{
		var targetPosition = targetGameObject.WorldPosition;
		var toTarget = targetPosition - WorldPosition;
		var distance = toTarget.Length;
		if ( distance <= 0f )
		{
			return true;
		}

		var traces = Scene.Trace.Ray( WorldPosition, targetPosition )
			.IgnoreGameObject( GameObject )
			.IgnoreGameObjectHierarchy( targetGameObject )
			.WithoutTags( Constants.TraceIgnoreTags )
			.RunAll();

		foreach ( var trace in traces )
		{
			if ( trace.GameObject.Tags.Has( Constants.MapTag ) || trace.GameObject.Tags.Has( Constants.ConstructTag ) )
			{
				return false;
			}
		}

		return true;
	}

	private class AreaDamageTarget
	{
		public required IAreaDamageReceiver Receiver { get; init; }
		public TimeUntil NextDamageTime { get; set; }
		public TimeSince LastDamageTime { get; set; }
	}
}
