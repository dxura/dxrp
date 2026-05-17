using Dxura.RP.Shared;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public static partial class ServerApiClient
{
	private static readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresAt)> _mockStore = new();

	public static async Task<List<StoreEntryDto>> ListStore( string? prefix = null )
	{
		prefix = string.IsNullOrWhiteSpace( prefix ) ? null : NormalizeKey( prefix );

		if ( !ServerApiLink.HasAuthorizationKey )
		{
			var now = DateTimeOffset.UtcNow;
			var entries = _mockStore
				.Where( entry => entry.Value.ExpiresAt == null || entry.Value.ExpiresAt > now )
				.Where( entry => prefix == null || entry.Key.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) )
				.Select( entry => new StoreEntryDto { Key = entry.Key, Value = entry.Value.Value, ExpiresAt = entry.Value.ExpiresAt } )
				.OrderBy( entry => entry.Key )
				.ToList();

			return entries;
		}

		return await SafeApiCall( async headers =>
			{
				var url = $"{Constants.ApiBaseUrl}/v1/server/store";
				if ( prefix != null )
				{
					url += $"?prefix={Uri.EscapeDataString( prefix )}";
				}

				var response = await ApiClientBase.RequestAsync(
					url, headers: headers );

				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				return JsonSerializer.Deserialize<List<StoreEntryDto>>(
					content,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true } ) ?? [];
			},
			"Failed to list store keys" ) ?? [];
	}

	public static async Task<string?> GetStore( string key )
	{
		key = NormalizeKey( key );

		if ( !ServerApiLink.HasAuthorizationKey )
		{
			if ( !_mockStore.TryGetValue( key, out var entry ) ) return null;
			if ( entry.ExpiresAt.HasValue && entry.ExpiresAt <= DateTimeOffset.UtcNow ) return null;
			return entry.Value;
		}

		return await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/store/{Uri.EscapeDataString( key )}", headers: headers );

				if ( response.StatusCode == HttpStatusCode.NotFound )
				{
					return null;
				}

				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var entry = JsonSerializer.Deserialize<StoreEntryDto>(
					content,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true } );

				return entry?.Value;
			},
			$"Failed to get store key '{key}'" );
	}

	public static async Task SetStore( string key, string value, DateTimeOffset? expiresAt = null )
	{
		key = NormalizeKey( key );

		if ( !ServerApiLink.HasAuthorizationKey )
		{
			_mockStore[key] = (value, expiresAt);
			return;
		}

		await SafeApiCall( async headers =>
			{
				var url = $"{Constants.ApiBaseUrl}/v1/server/store/{Uri.EscapeDataString( key )}";
				if ( expiresAt.HasValue )
					url += $"?expiresAt={Uri.EscapeDataString( expiresAt.Value.ToString( "O" ) )}";

				await ApiClientBase.RequestAsync( url, "PUT", Http.CreateJsonContent( value ), headers );
				return true;
			},
			$"Failed to set store key '{key}'" );
	}

	public static async Task DeleteStore( string key )
	{
		key = NormalizeKey( key );

		if ( !ServerApiLink.HasAuthorizationKey )
		{
			_mockStore.Remove( key );
			return;
		}

		await SafeApiCall( async headers =>
			{
				var response = await ApiClientBase.RequestAsync(
					$"{Constants.ApiBaseUrl}/v1/server/store/{Uri.EscapeDataString( key )}",
					"DELETE", headers: headers );

				if ( response.StatusCode == HttpStatusCode.NotFound )
				{
					return true;
				}

				response.EnsureSuccessStatusCode();

				return true;
			},
			$"Failed to delete store key '{key}'" );
	}

	private static string NormalizeKey( string key ) => key.Trim().ToLowerInvariant();

	public static async Task<T?> GetStoreJson<T>( string key )
	{
		var raw = await GetStore( key );
		if ( raw is null ) return default;

		try { return JsonSerializer.Deserialize<T>( raw ); }
		catch { return default; }
	}

	public static async Task SetStoreJson<T>( string key, T value, DateTimeOffset? expiresAt = null )
	{
		await SetStore( key, JsonSerializer.Serialize( value ), expiresAt );
	}

}
