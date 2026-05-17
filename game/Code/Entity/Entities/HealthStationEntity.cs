using System.Threading.Tasks;
using Dxura.RP.Game.UI;

namespace Dxura.RP.Game.Entities;

public class HealthStationEntity : BaseEntity, Component.IPressable, IContextualObject
{
	private const float MinHealPrice = 10f;
	private const float MaxHealPrice = 500f;
	private const float HealPriceStep = 10f;

	[Property] public float HealAmount { get; set; } = 25f;
	[Property] private float HealDelay { get; set; } = 1f;

	[Property] 
	[Sync( SyncFlags.FromHost )]
	public float HealPrice { get; set; } = 100f;
	
	[Property]
	public SoundEvent? HealSound { get; set; }
	
	public bool IsOwner => Player.Local.SteamId == Owner;

	protected override void OnStart()
	{
		base.OnStart();
		HealPrice = GetClampedHealPrice();
	}

	public Vector3 ContextPosition => WorldPosition + Vector3.Up * 18f;
	public string InputHint => "use";
	public float ContextMaxDistance => Config.Current.Game.ReachDistance;
	public bool LookOpacity => false;
	public string DisplayText => IsOwner
		? "#entity.healthstation.owner"
		: string.Format( Language.GetPhrase( "entity.healthstation.context" ), $"{GetClampedHealPrice():C0}", HealAmount );


	public bool CanPress( IPressable.Event e )
	{
		if ( IsOwner )
		{
			return false;
		}
		
		return true;
	}

	public bool Press( IPressable.Event e )
	{
		if ( IsOwner )
		{
			Notify.Error( "#entity.healthstation.owner" );
			return false;
		}

		if ( !Player.Local.IsValid() || Player.Local.IsDead )
		{
			Notify.Error( "#command.dead" );
			return false;
		}

		if ( !Player.Local.HealthComponent.IsValid() || Player.Local.HealthComponent.Health >= Player.Local.HealthComponent.MaxHealth )
		{
			Notify.Error( "#entity.healthstation.full" );
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"healthstation:{GameObject.Id}", HealDelay, true ) )
		{
			return false;
		}

		TryHealHost();
		return true;
	}
	
	[Rpc.Host]
	private async void TryHealHost()
	{
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:healthstation:{GameObject.Id}", HealDelay ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() )
		{
			return;
		}

		if ( player.SteamId == Owner )
		{
			player.Error( "#entity.healthstation.owner" );
			return;
		}

		if ( !HasInteractionLineOfSight( player ) )
		{
			return;
		}

		var health = player.HealthComponent;
		if ( !health.IsValid() || health.State != LifeState.Alive )
		{
			player.Error( "#command.dead" );
			return;
		}

		if ( health.Health >= health.MaxHealth )
		{
			player.Error( "#entity.healthstation.full" );
			return;
		}

		var amountHealed = MathF.Min( HealAmount, health.MaxHealth - health.Health );
		if ( amountHealed <= 0f )
		{
			player.Error( "#entity.healthstation.full" );
			return;
		}

		var price = (uint)MathF.Ceiling( GetClampedHealPrice() );
		if ( price > 0 && !await player.ChargeHost( price, "Used health station" ) )
		{
			return;
		}

		if ( !player.IsValid() || !GameObject.IsValid() || !health.IsValid() || health.State != LifeState.Alive )
		{
			return;
		}

		health.Health = MathF.Min( health.MaxHealth, health.Health + amountHealed );
		HealSound?.Broadcast( WorldPosition );

		if ( price > 0 && !await PayOwnerAsync( price ) )
		{
			Log.Warning( $"Health station payout failed for owner {Owner} after charging player {player.SteamId} ${price}." );
		}
	}

	[Rpc.Host]
	public void TryUpdatePrice( bool increase )
	{
		var callerId = Rpc.CallerId;
		
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:heal_price_update", 0.3f ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !player.IsValid() || player.SteamId != Owner )
		{
			return;
		}
		
		if ( increase )
		{
			HealPrice += HealPriceStep;
		}
		else
		{
			HealPrice -= HealPriceStep;
		}

		HealPrice = GetClampedHealPrice();
		Sound.Play( "pop" );
	}

	private float GetClampedHealPrice()
	{
		return Math.Clamp( HealPrice, MinHealPrice, MaxHealPrice );
	}

	private bool HasInteractionLineOfSight( Player player )
	{
		var tr = Scene.Trace.Ray( player.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( player.GameObject )
			.UseHitboxes()
			.Run();

		return tr.Hit && tr.GameObject.Root == GameObject.Root;
	}

	private async Task<bool> PayOwnerAsync( uint amount )
	{
		if ( amount == 0 || Owner == 0 )
		{
			return true;
		}

		var ownerPlayer = GameUtils.GetPlayerById( Owner );
		if ( ownerPlayer.IsValid() )
		{
			return await ownerPlayer.PayHost( amount, "Health station sale" );
		}

		return await ServerApiClient.ModifyPlayerBalance( Owner, (int)amount, "Health station sale" );
	}

}
