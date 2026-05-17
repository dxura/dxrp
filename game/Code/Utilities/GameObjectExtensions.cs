using Scene=Sandbox.Scene;

namespace Dxura.RP.Game;

public static partial class GameObjectExtensions
{
	/// <summary>
	///     Take damage. Only the host can call this.
	/// </summary>
	/// <param name="go"></param>
	/// <param name="damageInfo"></param>
	public static void TakeDamageHost( this GameObject go, DamageInfo damageInfo )
	{
		if ( !Networking.IsHost )
		{
			Log.Warning( $"Tried to run TakeDamage on {go}, but we're not the host." );
			return;
		}

		foreach ( var damageable in go.Root.Components.GetAll<Component.IDamageable>() )
		{
			var sandboxDamage = new Sandbox.DamageInfo
			{
				Damage = damageInfo.Damage, Position = damageInfo.Position
			};
			damageable.OnDamage( sandboxDamage );
		}

		foreach ( var damageable in go.Root.Components.GetAll<HealthComponent>() )
		{
			damageable.TakeDamageHost( damageInfo );
		}
	}

	public static void OnPlayerInteractHost( this GameObject? go, Player? player )
	{
		if ( !Networking.IsHost )
		{
			Log.Warning( $"Tried to run OnPlayerInteract on {go}, but we're not the host." );
			return;
		}

		if ( !go.IsValid() || !player.IsValid() )
		{
			return;
		}

		// For entities, try and take ownership if possible.
		if ( go.Tags.Contains( Constants.EntityTag ) )
		{
			var baseEntity = go.GetComponent<BaseEntity>();
			if ( baseEntity.IsValid() && baseEntity.AllowOwnershipTransfer )
			{
				baseEntity.Owner = player.SteamId;
			}
		}
	}

	public static void CopyPropertiesTo( this Component src, Component dst )
	{
		var json = src.Serialize().AsObject();
		json.Remove( "__guid" );
		dst.DeserializeImmediately( json );
	}

	public static string GetScenePath( this GameObject go )
	{
		return go is Scene ? "" : $"{go.Parent.GetScenePath()}/{go.Name}";
	}
}
