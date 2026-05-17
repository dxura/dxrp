using Sandbox;
using System;

namespace Dxura.RP.Game;

public sealed class Elevator : Component
{
	[Property] public float Speed { get; set; } = 10f;
	[Property] public float WaitPeriod { get; set; } = 3f;
	[Property] public Vector3 TopPosition { get; set; }

	private Vector3 _startPosition;
	private Vector3 _targetPosition;
	private TimeSince _idleTime = 0;

	protected override void OnStart()
	{
		_startPosition = WorldPosition;
		_targetPosition = TopPosition;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( _idleTime <= WaitPeriod )
		{
			return;
		}

		// Check if we've reached the target position (or are very close)
		if ( Vector3.DistanceBetween( WorldPosition, _targetPosition ) < 0.5f )
		{
			// Reached target, now wait
			WorldPosition = _targetPosition; // Snap to exact position
			_idleTime = 0;

			// Toggle between top and bottom positions
			_targetPosition = _targetPosition == _startPosition ? TopPosition : _startPosition;

			return;
		}

		// Move at constant speed regardless of distance
		var moveDirection = (_targetPosition - WorldPosition).Normal;
		var distanceToMove = Math.Min( Speed * Time.Delta, Vector3.DistanceBetween( WorldPosition, _targetPosition ) );
		WorldPosition += moveDirection * distanceToMove;
	}
}
