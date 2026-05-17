namespace Dxura.RP.Game;

/// <summary>
/// Validation result for construct data
/// </summary>
public class ConstructDataValidationResult
{
	public bool IsValid { get; }
	public string ErrorMessage { get; }

	private ConstructDataValidationResult( bool isValid, string errorMessage )
	{
		IsValid = isValid;
		ErrorMessage = errorMessage;
	}

	public static ConstructDataValidationResult Success()
	{
		return new ConstructDataValidationResult( true, string.Empty );
	}
	public static ConstructDataValidationResult Failure( string error )
	{
		return new ConstructDataValidationResult( false, error );
	}
}
