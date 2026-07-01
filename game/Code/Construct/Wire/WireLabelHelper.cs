namespace Dxura.RP.Game.Wire;

public static class WireLabelHelper
{
	public const int MaxWireLabelLength = 50;

	public static ConstructDataValidationResult ValidateLabel( string? label )
	{
		if ( string.IsNullOrWhiteSpace( label ) )
		{
			return ConstructDataValidationResult.Success();
		}

		if ( label.Trim().Length > MaxWireLabelLength )
		{
			return ConstructDataValidationResult.Failure( $"Label cannot exceed {MaxWireLabelLength} characters" );
		}

		return ConstructDataValidationResult.Success();
	}


}
