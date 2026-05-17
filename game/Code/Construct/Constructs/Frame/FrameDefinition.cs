namespace Dxura.RP.Game;

public class FrameDefinition : ConstructDefinition<Frame, FrameData>
{
	public override ConstructType Type => ConstructType.Frame;
	public override uint Limit => Config.Current.Game.FrameLimit;

	protected override ConstructDataValidationResult ValidateTyped( FrameData data )
	{
		var validatedUrl = ValidateImgurUrl( data.ImgurUrl );
		if ( string.IsNullOrEmpty( validatedUrl ) )
		{
			return ConstructDataValidationResult.Failure( "Invalid URL" );
		}

		// Size validation
		if ( data.Size.x > 3f || data.Size.x < 0.2f || data.Size.y > 3f || data.Size.y < 0.2f )
		{
			return ConstructDataValidationResult.Failure( "Invalid size" );
		}

		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( FrameData data, Vector3 position, Rotation rotation )
	{
		var frameGameObject = GameObject.GetPrefab( "prefabs/constructs/frame.prefab" ).Clone( position, rotation );

		return frameGameObject;
	}

	protected override bool CanOwnerPlace( long owner )
	{
		var player = GameUtils.GetPlayerById( owner );

		if ( Status.Current.HasStatus( owner, Constants.GaggedStatus ) )
		{
			player?.Warn( "#chat.gagged.restrict" );
			return false;
		}

		return true;
	}

	/// <summary>
	///     Validates if the provided URL is a direct i.imgur.com image link.
	/// </summary>
	/// <param name="input">Direct i.imgur.com URL</param>
	/// <returns>The validated URL or null if invalid</returns>
	public static string? ValidateImgurUrl( string input )
	{
		if ( string.IsNullOrWhiteSpace( input ) )
		{
			return null;
		}

		try
		{
			var uri = new Uri( input, UriKind.Absolute );

			// Only allow i.imgur.com domain
			if ( !uri.Host.Equals( "i.imgur.com", StringComparison.OrdinalIgnoreCase ) )
			{
				return null;
			}

			// Enforce HTTPS
			if ( !uri.Scheme.Equals( "https", StringComparison.OrdinalIgnoreCase ) )
			{
				return null;
			}

			// Validate path format and prevent traversal
			var path = Uri.UnescapeDataString( uri.AbsolutePath ).ToLowerInvariant();
			if ( path.Contains( ".." ) || path.Contains( "//" ) || path.Count( c => c == '/' ) != 1 )
			{
				return null;
			}

			// Verify image extension
			if ( !path.EndsWith( ".jpg" ) && !path.EndsWith( ".jpeg" ) && !path.EndsWith( ".png" ) )
			{
				return null;
			}

			return uri.AbsoluteUri;
		}
		catch ( UriFormatException )
		{
			return null;
		}
	}
}
