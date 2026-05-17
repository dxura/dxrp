namespace Dxura.RP.Game;

/// <summary>
/// Resolves a translation key with graceful fallbacks to a stored name or a humanized identifier,
/// so content without translations still displays sensibly.
/// </summary>
public static class LabelResolver
{
	public static string Resolve( string key, string? fallback, string? identifierFallback )
	{
		var translated = Language.GetPhrase( key );
		if ( !IsMissing( translated, key ) )
		{
			return translated;
		}

		var resolved = ResolveText( fallback );
		if ( !string.IsNullOrWhiteSpace( resolved ) )
		{
			return resolved;
		}

		if ( !string.IsNullOrWhiteSpace( identifierFallback ) )
		{
			return IdentifierToLabel( identifierFallback );
		}

		return key;
	}

	/// <summary>
	/// Resolve a value that may be a literal string or a <c>#translation.key</c> reference.
	/// </summary>
	public static string ResolveText( string? value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
		{
			return string.Empty;
		}

		if ( !value.StartsWith( '#' ) )
		{
			return value;
		}

		var key = value[1..];
		var translated = Language.GetPhrase( key );
		return IsMissing( translated, key ) ? value : translated;
	}

	private static bool IsMissing( string translated, string key )
	{
		return string.IsNullOrWhiteSpace( translated )
			|| string.Equals( translated, key, StringComparison.Ordinal )
			|| string.Equals( translated, $"#{key}", StringComparison.Ordinal );
	}

	private static string IdentifierToLabel( string identifier )
	{
		var chars = identifier.Replace( '_', ' ' ).Replace( '-', ' ' ).ToCharArray();
		var capitalize = true;
		for ( var i = 0; i < chars.Length; i++ )
		{
			if ( chars[i] == ' ' )
			{
				capitalize = true;
				continue;
			}

			if ( capitalize )
			{
				chars[i] = char.ToUpperInvariant( chars[i] );
				capitalize = false;
			}
		}

		return new string( chars );
	}
}
