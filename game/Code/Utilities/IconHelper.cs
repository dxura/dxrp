namespace Dxura.RP.Game;

public static class IconHelper
{
	public static string GetFileIcon( string filePath )
	{
		if ( filePath.EndsWith( ".cs" ) )
		{
			return "📄";
		}

		if ( filePath.EndsWith( ".json" ) || filePath.EndsWith( ".config" ) )
		{
			return "📋";
		}

		if ( filePath.EndsWith( ".png" ) || filePath.EndsWith( ".jpg" ) || filePath.EndsWith( ".svg" ) ||
		     filePath.EndsWith( ".vtex_c" ) )
		{
			return "🖼️";
		}

		if ( filePath.EndsWith( ".scss" ) )
		{
			return "🎨";
		}

		if ( filePath.EndsWith( ".ttf" ) )
		{
			return "🔤";
		}

		if ( filePath.EndsWith( ".vsnd_c" ) )
		{
			return "🔊";
		}

		if ( filePath.EndsWith( ".sound_c" ) )
		{
			return "🎶";
		}

		if ( filePath.EndsWith( ".sndscape_c" ) )
		{
			return "🎼";
		}
		// else if ( filePath.EndsWith( ".vtex_c" ) )
		// {
		//     return "🔳";
		// }

		if ( filePath.EndsWith( ".vmat_c" ) )
		{
			return "🌐";
		}

		if ( filePath.EndsWith( ".vmdl_c" ) )
		{
			return "🧊";
		}

		if ( filePath.EndsWith( ".vpcf_c" ) )
		{
			return "✨";
		}

		if ( filePath.EndsWith( ".scene_c" ) )
		{
			return "🌄";
		}

		if ( filePath.EndsWith( ".prefab_c" ) )
		{
			return "📦";
		}

		if ( filePath.EndsWith( "_c" ) )
		{
			return "💎";
		}

		return "❓";
	}
}
