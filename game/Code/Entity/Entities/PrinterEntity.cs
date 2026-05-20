using System.Threading;
using System.Threading.Tasks;

namespace Dxura.RP.Game.Entities;

public sealed class PrinterEntityConfig
{
	public uint BaseMoneyGeneration { get; init; } = 25;
	public uint BalanceCapacity { get; init; } = 8000;
	public float GenerationInterval { get; init; } = 60f;
	public string ColorHex{ get; init; } = "0xFFFFFF";
}

[Title( "Printer" )]
[Category( "Entities" )]
public sealed class PrinterEntity : BaseEntity, IAreaDamageReceiver, Component.IPressable, IGameEvents
{
	// References
	[Property] public required TextRenderer TextRender { get; set; }
	[Property] public required GameObject PrinterFan { get; set; }
	[Property] public required ModelRenderer ModelRenderer { get; set; }

	// Printer Timer Setup
	[Property]
	[Sync( SyncFlags.FromHost )]
	private uint CurrentBalance { get; set; }

	private TimeSince _lastGeneration = 0f;

	[Sync( SyncFlags.FromHost )]
	private TimeSince? TimeSinceOwnerDisconnect { get; set; }

	/// <summary>
	///     What to spawn when we explode?
	/// </summary>
	[Property]
	[Group( "Effects" )]
	public required GameObject Explosion { get; set; }
	[Property]
	[Group( "Effects" )]
	public GameObject? SmokeEffect { get; set; }

	[Property]
	[Group( "Effects" )]
	[Range( 3, 6 )]
	public float MinSmokeTime { get; set; } = 3f;

	[Property]
	[Group( "Effects" )]
	[Range( 3, 6 )]
	public float MaxSmokeTime { get; set; } = 6f;

	private GameObject? _activeSmoke;
	private bool _isExploding;

	[Property] [Group( "Effects" )] private SoundEvent? WithdrawSound { get; set; }
	[Property] [Group( "Effects" )] private SoundEvent? PrintSound { get; set; }
	[Property] [Group( "Effects" )] public required SoundPointComponent IdleSoundPoint { get; set; }

	public void ApplyAreaDamage( AreaDamage component )
	{
		var dmg = new DamageInfo( component.Attacker, component.Damage, component.Inflictor,
			component.WorldPosition,
			Flags: component.DamageFlags );

		HealthComponent?.TakeDamageHost( dmg );
	}

	public override string DisplayName => TimeSinceOwnerDisconnect.HasValue
			? string.Format(
				Language.GetPhrase( "entity.printer.disconnect_timer" ),
				base.DisplayName,
				MathF.Max( 0f, Config.Current.Game.PrinterDestroyAfterDisconnectTime - TimeSinceOwnerDisconnect.Value.Relative ) )
			: base.DisplayName ?? string.Empty;
	
	public override Color Color => Color.Parse( _printerConfig.ColorHex ) ?? Color.White;

	private static readonly SemaphoreSlim WithdrawSemaphore = new( 1, 1 );

	private PrinterEntityConfig _printerConfig = null!;
	private bool _occluded;
	private TimeSince _lastServerTick = 0f;

	protected override void OnStart()
	{
		base.OnStart();

		TimeSinceOwnerDisconnect = null;
		PrinterFan.Flags |= GameObjectFlags.NoInterpolation;
		
		_printerConfig = GetConfig(new PrinterEntityConfig());

		UpdateState();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// Don't update text if occluded (or headless)
		if ( !_occluded && !GameManager.IsHeadless && !Cooldown.Current.CheckAndStartCooldown( $"{GameObject.Id}:spin", 0.05f ) )
		{
			TextRender.Text = "$" + NumberUtils.FormatNumberWithSuffix( CurrentBalance );
			SpinFan();
		}

	}

	public void OnSecondlyUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		//
		// (SERVER CODE)
		//
		
		if (_lastServerTick.Relative < 2.5f)
		{
			return;
		}
		
		_lastServerTick = 0f;

		if ( Config.Current.Game.PrinterDecayEnabled )
		{
			if ( GameObject.Tags.Has( Constants.PocketTag ) )
			{
				TimeSinceOwnerDisconnect = null;
				return;
			}

			if ( Owner <= 0 || IsOwnerConnected() )
			{
				TimeSinceOwnerDisconnect = null;
			}
			else if ( !TimeSinceOwnerDisconnect.HasValue )
			{
				TimeSinceOwnerDisconnect = 0f;
			}
			else if ( TimeSinceOwnerDisconnect.Value > Config.Current.Game.PrinterDestroyAfterDisconnectTime )
			{
				OnDestroyed();
				return;
			}
		}

		// If the timer has passed, add money
		if ( _lastGeneration >= _printerConfig.GenerationInterval )
		{
			if ( CurrentBalance < _printerConfig.BalanceCapacity )
			{
				CurrentBalance += _printerConfig.BaseMoneyGeneration;
				PrintSound.Broadcast( WorldPosition );
			}

			_lastGeneration = 0; // Reset the timer
		}
	}

	public override void OnOcclusionChanged( bool occlude )
	{
		base.OnOcclusionChanged( occlude );

		_occluded = occlude;
	}

	private bool IsOwnerConnected()
	{
		return Connection.All.Any( connection =>
			connection.SteamId == Owner &&
			(connection.IsActive || connection.IsHost) );
	}

	private void UpdateState()
	{
		GameObject.Name = DisplayName;
		ModelRenderer.Tint = Color.TryParse(_printerConfig.ColorHex, out var color) ? color : Color.White;
	}
	
	public bool Press( IPressable.Event e )
	{
		if ( CurrentBalance <= 0 )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "action:quick", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return false;
		}

		WithdrawHost();

		return true;
	}

	[Rpc.Host]
	private void WithdrawHost()
	{
		var caller = Rpc.Caller;
		var callerId = Rpc.CallerId;
		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:action:quick", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		if ( !GameUtils.HasPermission( caller, GameObject ) )
		{
			return;
		}

		_ = Withdraw( callerId );
	}

	private async Task Withdraw( Guid callerId )
	{
		await WithdrawSemaphore.WaitAsync();

		try
		{
			var player = GameUtils.GetPlayerByConnectionId( callerId );

			if ( !player.IsValid() )
			{
				return;
			}

			if ( CurrentBalance == 0 )
			{
				return;
			}

			if ( !await player.PayHost( CurrentBalance, "Withdrew money from printer" ) )
			{
				return;
			}

			CurrentBalance = 0;
			WithdrawSound?.BroadcastHost( WorldPosition );
		}
		finally
		{
			WithdrawSemaphore.Release();
		}
	}

	private void SpinFan()
	{
		// Calculate the rotation amount based on PrinterFanSpeed and Time.Delta
		var rotationAmount = 1000 * Time.Delta;

		// Apply the rotation relative to the GameObject's current rotation
		PrinterFan.WorldRotation *= Rotation.FromAxis( Vector3.Left, -rotationAmount );
	}

	protected override void OnDestroyed()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( _isExploding )
		{
			return;
		}

		GameObject.Tags.Remove( Constants.PocketItemTag );
		_isExploding = true;

		_ = TriggerSmokeAndExplosion();
	}

	private async Task TriggerSmokeAndExplosion()
	{
		if ( SmokeEffect.IsValid() )
		{
			_activeSmoke = SmokeEffect.Clone();
			_activeSmoke.Parent = GameObject;
			_activeSmoke.NetworkSpawn();

			var smokeTime = Random.Shared.Float( MinSmokeTime, MaxSmokeTime );
			var elapsed = 0f;
			var interval = 0.75f; // sound repeat interval (adjust as needed)

			// Start smoke malfunction sound loop
			while ( elapsed < smokeTime )
			{
				if ( !GameObject.IsValid() )
				{
					return;
				}

				PrintSound?.Broadcast( WorldPosition ); // Emulate printer glitch noise
				await GameTask.DelaySeconds( interval );
				elapsed += interval;
			}
		}

		if ( !GameObject.IsValid() )
		{
			return;
		}

		_activeSmoke?.Destroy();
		_activeSmoke = null;

		var explosion = Explosion.Clone( WorldPosition );
		explosion.NetworkSpawn();

		var areaDamage = explosion.AddComponent<AreaDamage>();
		areaDamage.Damage = 60;
		areaDamage.Attacker = this;
		areaDamage.TimeLimit = 0.1f;
		areaDamage.DamageFlags = DamageFlags.Explosion;

		base.OnDestroyed();
	}

}
