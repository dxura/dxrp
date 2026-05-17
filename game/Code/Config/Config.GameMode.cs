using System.Text.Json;
using System.Text.Json.Nodes;
using Dxura.RP.Shared;

namespace Dxura.RP.Game;

public partial class Config
{
	private static readonly JsonSerializerOptions GameModeConfigSerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};
	private readonly Dictionary<(Type Type, Guid Id), object> _contentConfigCache = [];
	private readonly Dictionary<(Type Type, Guid Id), object> _addonConfigCache = [];

	public TConfig Content<TConfig>( Guid id ) where TConfig : class, new()
	{
		if ( id == Guid.Empty )
		{
			return new TConfig();
		}

		var key = (typeof( TConfig ), id);
		if ( _contentConfigCache.TryGetValue( key, out var cached ) )
		{
			return (TConfig)cached;
		}

		var content = GameModeAddonContents.FindById( id );
		var resolved = content == null
			? new TConfig()
			: ResolveGameModeConfig<TConfig>( content.BaseConfig, content.ConfigOverride );
		_contentConfigCache[key] = resolved;
		return resolved;
	}

	public TConfig Content<TConfig>( Guid id, TConfig fallback ) where TConfig : class, new()
	{
		if ( id == Guid.Empty )
		{
			return fallback;
		}

		var content = GameModeAddonContents.FindById( id );
		return content == null
			? fallback
			: ResolveGameModeConfig<TConfig>( SerializeGameModeConfig( fallback ), content.BaseConfig, content.ConfigOverride );
	}

	public TConfig Addon<TConfig>( Guid id ) where TConfig : class, new()
	{
		if ( id == Guid.Empty )
		{
			return new TConfig();
		}

		var key = (typeof( TConfig ), id);
		if ( _addonConfigCache.TryGetValue( key, out var cached ) )
		{
			return (TConfig)cached;
		}

		var addon = GameMode.Addons.FirstOrDefault( x => x.Id == id );
		var resolved = addon == null
			? new TConfig()
			: ResolveGameModeConfig<TConfig>( addon.GlobalBaseConfig, addon.GlobalConfigOverride );
		_addonConfigCache[key] = resolved;
		return resolved;
	}

	public TConfig Addon<TConfig>( Guid id, TConfig fallback ) where TConfig : class, new()
	{
		if ( id == Guid.Empty )
		{
			return fallback;
		}

		var addon = GameMode.Addons.FirstOrDefault( x => x.Id == id );
		return addon == null
			? fallback
			: ResolveGameModeConfig<TConfig>( SerializeGameModeConfig( fallback ), addon.GlobalBaseConfig, addon.GlobalConfigOverride );
	}

	private void ClearGameModeConfigCache()
	{
		_contentConfigCache.Clear();
		_addonConfigCache.Clear();
	}

	private static TConfig ResolveGameModeConfig<TConfig>( string? baseConfigJson, string? overrideConfigJson ) where TConfig : class, new()
	{
		return ResolveGameModeConfig<TConfig>( null, baseConfigJson, overrideConfigJson );
	}

	private static TConfig ResolveGameModeConfig<TConfig>( string? fallbackConfigJson, string? baseConfigJson, string? overrideConfigJson ) where TConfig : class, new()
	{
		var effectiveNode = MergeGameModeConfigNodes(
			MergeGameModeConfigNodes( ParseGameModeConfigNode( fallbackConfigJson ), ParseGameModeConfigNode( baseConfigJson ) ),
			ParseGameModeConfigNode( overrideConfigJson ) );
		if ( effectiveNode == null )
		{
			return new TConfig();
		}

		try
		{
			return effectiveNode.Deserialize<TConfig>( GameModeConfigSerializerOptions ) ?? new TConfig();
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Failed to deserialize game mode config for {typeof( TConfig ).Name}: {ex.Message}" );
			return new TConfig();
		}
	}

	private static string? SerializeGameModeConfig<TConfig>( TConfig config ) where TConfig : class
	{
		try
		{
			return JsonSerializer.Serialize( config, GameModeConfigSerializerOptions );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Failed to serialize game mode fallback config for {typeof( TConfig ).Name}: {ex.Message}" );
			return null;
		}
	}

	private static JsonNode? ParseGameModeConfigNode( string? json )
	{
		if ( string.IsNullOrWhiteSpace( json ) )
		{
			return null;
		}

		try
		{
			return JsonNode.Parse( json );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Failed to parse game mode config JSON: {ex.Message}" );
			return null;
		}
	}

	private static JsonNode? MergeGameModeConfigNodes( JsonNode? baseNode, JsonNode? overrideNode )
	{
		if ( overrideNode == null )
		{
			return baseNode?.DeepClone();
		}

		if ( baseNode == null )
		{
			return overrideNode.DeepClone();
		}

		if ( baseNode is JsonObject baseObject && overrideNode is JsonObject overrideObject )
		{
			var merged = (JsonObject)baseObject.DeepClone();
			foreach ( var pair in overrideObject )
			{
				merged[pair.Key] = MergeGameModeConfigNodes( merged[pair.Key], pair.Value );
			}

			return merged;
		}

		return overrideNode.DeepClone();
	}
}
