namespace Dxura.RP.Game;

public class TextDefinition : ConstructDefinition<Text, TextData>
{
	public override ConstructType Type => ConstructType.Text;
	public override uint Limit => Config.Current.Game.TextLimit;

	public const uint MinTextLength = 1;
	public const uint MaxTextLength = 150;
	public const uint MinFontSize = 30;
	public const uint MaxFontSize = 200;
	public const uint MinOutlineSize = 1;
	public const uint MaxOutlineSize = 20;
	public const uint MinFontWeight = 100;
	public const uint MaxFontWeight = 800;


	protected override ConstructDataValidationResult ValidateTyped( TextData data )
	{
		if ( string.IsNullOrEmpty( data.Text ) )
		{
			return ConstructDataValidationResult.Failure( "Text cannot be empty" );
		}

		if ( data.Text.Length < MinTextLength )
		{
			return ConstructDataValidationResult.Failure( $"Text must be greater than {MinTextLength} characters" );
		}

		if ( data.Text.Length > MaxTextLength )
		{
			return ConstructDataValidationResult.Failure( $"Text exceeds maximum length of {MaxTextLength} characters" );
		}

		if ( data.FontSize < MinFontSize )
		{
			return ConstructDataValidationResult.Failure( $"Font size must be greater than {MinFontSize}" );
		}

		if ( data.FontSize > MaxFontSize )
		{
			return ConstructDataValidationResult.Failure( $"Font size exceeds maximum of {MaxFontSize}" );
		}

		if ( data.OutlineSize < MinOutlineSize )
		{
			return ConstructDataValidationResult.Failure( $"Outline size must be greater than {MinOutlineSize}" );
		}

		if ( data.OutlineSize > MaxOutlineSize )
		{
			return ConstructDataValidationResult.Failure( $"Outline size exceed maximum of  {MinOutlineSize}" );
		}

		if ( data.FontWeight < MinFontWeight )
		{
			return ConstructDataValidationResult.Failure( $"Font weight must be greater than {MinFontWeight}" );
		}

		if ( data.FontWeight > MaxFontWeight )
		{
			return ConstructDataValidationResult.Failure( $"Font weight exceed maximum of  {MaxFontSize}" );
		}


		return ConstructDataValidationResult.Success();
	}

	protected override GameObject CreateConstructInternal( TextData data, Vector3 position, Rotation rotation )
	{
		var textGameObject = GameObject.GetPrefab( "prefabs/constructs/text.prefab" ).Clone( position, rotation );

		return textGameObject;
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
}
