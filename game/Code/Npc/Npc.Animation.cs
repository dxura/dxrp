using Sandbox.Citizen;
namespace Dxura.RP.Game;

public partial class Npc
{
	/// <summary>
	/// Whether the NPC is currently moving or not
	/// </summary>
	protected bool IsMoving { get; set; }

	[RequireComponent] public required CitizenAnimationHelper AnimationHelper { get; set; }

	/// <summary>
	/// Initialize animation state
	/// </summary>
	protected virtual void OnStartAnimation()
	{
	}

	/// <summary>
	/// Update animations based on movement
	/// </summary>
	protected virtual void OnUpdateAnimation()
	{
		if ( !Body.IsValid() )
		{
			return;
		}

		AnimationHelper.WithVelocity( Agent.Velocity );

		IsMoving = Agent.Velocity.Length > 1.0f;
	}

	/// <summary>
	/// Play attack animation
	/// </summary>
	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Unreliable )]
	protected virtual void PlayAttackAnimation()
	{
		if ( Body.IsValid() )
		{
			Body.Set( "b_attack", true );
		}
	}

	/// <summary>
	/// Play hit reaction animation when taking damage
	/// </summary>
	protected virtual void PlayHitReactionAnimation( DamageInfo damageInfo )
	{
		if ( Body.IsValid() )
		{
			// Trigger hit animation
			Body.Set( "hit", true );
			Body.Set( "hit_direction", (damageInfo.Position - WorldPosition).Normal );
		}
	}
}
