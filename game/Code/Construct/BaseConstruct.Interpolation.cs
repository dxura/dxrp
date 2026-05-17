public abstract partial class BaseConstruct
{
	private Vector3 _targetPosition;
	private Rotation _targetRotation;
	private Vector3 _currentPositionVelocity;
	private Vector3 _currentRotationVelocity;

	private bool _interpolating;
	
	private void OnSecondlyUpdateInterpolationState()
	{
		if ( !_interpolating )
		{
			return;
		}

		if ( IsNetworkOwner || IsPreview || IsFrozen )
		{
			ResetInterpolation();
			return;
		}
		
		// Snap check
		var positionDelta = WorldPosition.DistanceSquared( _targetPosition );
		var rotationDelta = WorldRotation.Distance( _targetRotation );

		if ( positionDelta < 0.05f && rotationDelta < 0.05f || positionDelta > 250f || rotationDelta > 45f )
		{
			ResetInterpolation();
		}
	}

	private void OnUpdateInterpolation()
	{
		if ( !_interpolating )
		{
			return;
		}

		WorldPosition = Vector3.SmoothDamp(
			WorldPosition,
			_targetPosition,
			ref _currentPositionVelocity,
			0.2f,
			Time.Delta
		);

		WorldRotation = Rotation.SmoothDamp(
			WorldRotation,
			_targetRotation,
			ref _currentRotationVelocity,
			0.2f,
			Time.Delta
		);
	}
	
	private void ResetInterpolation()
	{
		_interpolating = false;
		_targetPosition = WorldPosition;
		_targetRotation = WorldRotation;
		_currentPositionVelocity = Vector3.Zero;
		_currentRotationVelocity = Vector3.Zero;
		GameObject.Transform.ClearInterpolation();
	}
}
