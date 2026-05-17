using System.Text.Json;
using System.Text.Json.Nodes;
namespace Dxura.RP.Game;

public partial class Config : GameObjectSystem<Config>
{
	public required GameConfig Game { get; init; }

	/// <summary>
	/// A snapshot of the default config values taken at startup, before any overrides are applied.
	/// Used to reset back to defaults before re-applying overrides.
	/// </summary>
	private string DefaultConfigJson { get; set; }

	/// <summary>
	/// The override config JSON, synced to clients so they can apply it locally.
	/// </summary>
	[Property]
	[Sync( SyncFlags.FromHost )]
	[Change( nameof( OnOverrideConfigJsonChanged ) )]
	public string OverrideConfigJson { get; private set; } = "";

	/// <summary>
	/// Whether the config has been fully initialized (overrides applied).
	/// </summary>
	[Property]
	[Sync( SyncFlags.FromHost )]
	public bool IsReady { get; private set; }
	
	[Property]
	[Sync( SyncFlags.FromHost )]
	public GameModeDto GameMode { get; private set; }

	public Config( Scene scene ) : base( scene )
	{
		Game = Sandbox.Game.Ident switch
		{
			"dxura.sandbox" => new SandboxGameConfig(),
			_ => new RpGameConfig()
		};

		DefaultConfigJson = Json.Serialize( Game );
		
		Log.Info( $"Configured game({Sandbox.Game.Ident}) with {Game.GetType().Name}" );
		
		Listen( Stage.SceneLoaded, 5, OnStartConfig, "Config Start" );
	}

	private void OnStartConfig()
	{
		// Apply config overrides for any late-joiners (who fetch it by the property snapshot).
		if ( !string.IsNullOrWhiteSpace( OverrideConfigJson ) )
		{
			OnOverrideConfigJsonChanged( string.Empty, OverrideConfigJson );
		}
		
		GameMode = GameModeDto.Default;
	}

	/// <summary>
	/// Marks the config as ready. Called after overrides are applied (host) or immediately when using public API defaults.
	/// </summary>
	public void MarkReady()
	{
		IsReady = true;
		IConfigEvents.Post( x => x.OnConfigAppliedHost() );
	}
	
	public void SetGameMode( GameModeDto gameMode )
	{
		var oldGameMode = GameMode;
		ClearGameModeConfigCache();
		GameMode = gameMode;
		IGameEvents.Post( x => x.OnGameModeUpdated(oldGameMode, gameMode ) );
	}

	private void OnOverrideConfigJsonChanged( string oldValue, string newValue )
	{
		if ( string.IsNullOrWhiteSpace( newValue ) )
		{
			return;
		}

		try
		{
			if ( JsonNode.Parse( newValue ) is JsonObject node )
			{
				ResetToDefaults();
				ApplyJsonObject( Game, node );
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"Failed to apply override config: {ex.Message}" );
			ResetToDefaults();
		}
		
		IConfigEvents.Post( x => x.OnConfigOverride() );
	}

	public static void ApplyOverride( string overrideConfigJson )
	{
		Current.OverrideConfigJson = overrideConfigJson;
	}

	/// <summary>
	/// Resets the game config back to the default values captured at startup.
	/// </summary>
	private static void ResetToDefaults()
	{
		var defaultJson = Current.DefaultConfigJson;
		if ( string.IsNullOrWhiteSpace( defaultJson ) )
		{
			return;
		}

		if ( JsonNode.Parse( defaultJson ) is JsonObject defaultNode )
		{
			ApplyJsonObject( Current.Game, defaultNode );
		}
	}

	private static void ApplyJsonObject( object target, JsonObject source )
	{
		var properties = TypeLibrary.GetSerializedObject( target );

		foreach ( var entry in source )
		{
			var property = properties.FirstOrDefault( p => string.Equals( p.Name, entry.Key, StringComparison.OrdinalIgnoreCase ) );
			if ( property == null )
			{
				continue;
			}

			// Skip GameResource-derived properties (e.g. DefaultJob) — they are asset references
			// and contain types (like Material+FlagsAccessor) that cannot be serialized/deserialized.
			var propertyType = property.GetValue<object>()?.GetType();
			if ( propertyType != null && typeof( GameResource ).IsAssignableFrom( propertyType ) )
			{
				continue;
			}

			// Allow null values to be applied (e.g. "MaxPropSize": null)
			if ( entry.Value == null || entry.Value.GetValueKind() == JsonValueKind.Null )
			{
				property.SetValue<object>( null );
				continue;
			}

			var existingValue = property.GetValue<object>();

			if ( existingValue == null )
			{
				try
				{
					var value = JsonSerializer.Deserialize( entry.Value.ToJsonString(), property.PropertyType );
					property.SetValue( value );
				}
				catch ( Exception ex )
				{
					Log.Warning( $"Failed to apply config override for '{property.Name}': {ex.Message}" );
				}
				continue;
			}

			if ( entry.Value is JsonObject childNode && existingValue is not string )
			{
				ApplyJsonObject( existingValue, childNode );
				continue;
			}

			try
			{
				var value = JsonSerializer.Deserialize( entry.Value.ToJsonString(), existingValue.GetType() );
				property.SetValue( value );
			}
			catch ( Exception ex )
			{
				Log.Warning( $"Failed to apply config override for '{property.Name}': {ex.Message}" );
			}
		}
	}

}
