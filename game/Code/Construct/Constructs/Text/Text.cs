using Sandbox.Diagnostics;
namespace Dxura.RP.Game;

/// <summary>
///     A component that represents a text prop in the game world.
/// </summary>
public sealed class Text() : BaseConstruct( ConstructType.Text )
{
	[Property]
	[RequireComponent]
	public required TextRenderer TextRenderer { get; set; }

	private const string RedactedText = "#generic.redacted";

	protected override void OnDataChanged( IConstructData oldData, IConstructData newData )
	{
		var textData = newData is TextData data ? data : default;
		var oldTextData = oldData is TextData old ? old : default;

		GameObject.Name = $"Text ({textData.Text})";
		(Collider as BoxCollider)?.Scale = new Vector3( 2f, textData.Text.Length * textData.FontSize * 0.06f, textData.FontSize * 0.1f );

		if ( !TextRenderer.IsValid() )
		{
			return;
		}

		TextRenderer.Enabled = true;

		TextRenderer.Scale = 0.1f;

		TextRenderer.TextScope = TextRenderer.TextScope with
		{
			Text = textData.Text,
			TextColor = textData.Color,
			FontSize = textData.FontSize,
			FontItalic = textData.Italic,
			FontWeight = textData.FontWeight,
			Outline = new TextRendering.Outline
			{
				Enabled = textData.Outline, Color = textData.OutlineColor, Size = textData.OutlineSize
			}
		};

		if ( Networking.IsHost && textData.Text != RedactedText && !IsPreview && textData.Text != oldTextData.Text )
		{
			var player = GameUtils.GetPlayerById( Owner );
			var filtered = GameManager.ModerateText( player?.SteamId ?? 0, "TEXT", textData.Text );

			if ( filtered != textData.Text )
			{
				// Use internal serialization for special redacted text case
				var serializationResult = Construct.Current.Serializer.Serialize( ConstructType.Text, new TextData
				{
					Text = RedactedText, Color = Color.Red, FontSize = 60
				} );
				
				BroadcastData( serializationResult.IsSuccess ? serializationResult.Value : string.Empty );
			}
		}
	}
}
