using System.Globalization;
using System.Text;

namespace Dxura.RP.Game.Utilities;

public static class WordFilter
{
	private const string Mask = "***";

	// Rebuilt whenever the configured list changes (by reference).
	private static string[]? _cachedSource;
	private static HashSet<string> _words = new( StringComparer.Ordinal );

	public static string Filter( string input )
	{
		if ( string.IsNullOrEmpty( input ) )
			return input;

		var list = Config.Current?.Game?.TextWordBlacklist;
		if ( list == null || list.Length == 0 )
			return input;

		if ( !ReferenceEquals( list, _cachedSource ) )
		{
			RebuildWordSet( list );
		}

		if ( _words.Count == 0 )
			return input;

		var tokens = Tokenize( input );
		if ( tokens.Count == 0 )
			return input;

		// (start, end) ranges in the original string that should be masked.
		var ranges = new List<(int Start, int End)>();

		// Single-token matches.
		for ( var i = 0; i < tokens.Count; i++ )
		{
			if ( IsBadWord( tokens[i].Normalized ) )
			{
				ranges.Add( (tokens[i].Start, tokens[i].End) );
			}
		}

		// Spaced-out matches: fuse runs of very short tokens (1-2 normalized chars)
		// and check the fusion against the wordset. Catches "n i g g e r".
		for ( var i = 0; i < tokens.Count; i++ )
		{
			if ( tokens[i].Normalized.Length is < 1 or > 2 )
				continue;

			var sb = new StringBuilder();
			for ( var j = i; j < tokens.Count && j < i + 16; j++ )
			{
				if ( tokens[j].Normalized.Length is < 1 or > 2 )
					break;

				sb.Append( tokens[j].Normalized );
				if ( j > i && sb.Length >= 4 && IsBadWord( sb.ToString() ) )
				{
					ranges.Add( (tokens[i].Start, tokens[j].End) );
				}
			}
		}

		if ( ranges.Count == 0 )
			return input;

		return ApplyMask( input, ranges );
	}

	private static void RebuildWordSet( string[] source )
	{
		_cachedSource = source;
		_words = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var word in source )
		{
			var normalized = Normalize( word );
			if ( normalized.Length >= 3 )
			{
				_words.Add( normalized );
			}
		}
	}

	private static bool IsBadWord( string normalized )
	{
		if ( normalized.Length < 3 )
			return false;

		if ( _words.Contains( normalized ) )
			return true;

		// Strip common English suffixes so "niggers" / "faggots" / "retarded" hit
		// their base entries without needing every inflection in the list.
		ReadOnlySpan<string> suffixes = ["s", "z", "es", "ed", "er", "ers", "ing", "a", "ah", "as"];
		foreach ( var suffix in suffixes )
		{
			if ( normalized.Length <= suffix.Length + 2 )
				continue;

			if ( !normalized.EndsWith( suffix, StringComparison.Ordinal ) )
				continue;

			var stem = normalized[..^suffix.Length];
			if ( _words.Contains( stem ) )
				return true;
		}

		return false;
	}

	private readonly record struct Token( int Start, int End, string Normalized );

	private static List<Token> Tokenize( string input )
	{
		var tokens = new List<Token>();
		var i = 0;
		while ( i < input.Length )
		{
			if ( !IsWordChar( input[i] ) )
			{
				i++;
				continue;
			}

			var start = i;
			while ( i < input.Length && IsWordChar( input[i] ) )
			{
				i++;
			}

			var segment = input.AsSpan( start, i - start );
			var normalized = Normalize( segment );
			if ( normalized.Length > 0 )
			{
				tokens.Add( new Token( start, i, normalized ) );
			}
		}

		return tokens;
	}

	private static bool IsWordChar( char c )
	{
		return char.IsLetterOrDigit( c ) || c is '@' or '$' or '!' or '|';
	}

	private static string Normalize( ReadOnlySpan<char> input )
	{
		// 1. Lowercase + strip diacritics (so "nìgger" == "nigger").
		Span<char> lowerBuffer = stackalloc char[input.Length];
		for ( var i = 0; i < input.Length; i++ )
		{
			lowerBuffer[i] = char.ToLowerInvariant( input[i] );
		}

		var decomposed = new string( lowerBuffer ).Normalize( NormalizationForm.FormD );

		var sb = new StringBuilder( decomposed.Length );
		foreach ( var c in decomposed )
		{
			if ( CharUnicodeInfo.GetUnicodeCategory( c ) == UnicodeCategory.NonSpacingMark )
				continue;

			// Leet → letter map.
			var mapped = c switch
			{
				'0' => 'o',
				'1' or '!' or '|' or 'ı' or 'í' or 'ì' or 'î' or 'ï' => 'i',
				'3' or 'é' or 'è' or 'ê' or 'ë' => 'e',
				'4' or '@' or 'á' or 'à' or 'â' or 'ä' or 'ã' => 'a',
				'5' or '$' => 's',
				'6' or '9' => 'g',
				'7' => 't',
				'8' => 'b',
				'2' => 'z',
				_ => c
			};

			if ( char.IsLetter( mapped ) )
			{
				sb.Append( mapped );
			}
		}

		// 2. Collapse runs of the same letter to a single instance. Both the list
		//    entry and the input go through this, so "nigger" and "niiiiiger" both
		//    canonicalize to "niger" and match.
		if ( sb.Length <= 1 )
			return sb.ToString();

		var result = new StringBuilder( sb.Length );
		var prev = '\0';
		for ( var i = 0; i < sb.Length; i++ )
		{
			var c = sb[i];
			if ( c == prev )
				continue;
			result.Append( c );
			prev = c;
		}

		return result.ToString();
	}

	private static string ApplyMask( string input, List<(int Start, int End)> ranges )
	{
		ranges.Sort( ( a, b ) => a.Start.CompareTo( b.Start ) );

		var sb = new StringBuilder( input.Length );
		var cursor = 0;
		foreach ( var (start, end) in ranges )
		{
			if ( start < cursor )
				continue;

			sb.Append( input, cursor, start - cursor );
			sb.Append( Mask );
			cursor = end;
		}

		sb.Append( input, cursor, input.Length - cursor );
		return sb.ToString();
	}
}
