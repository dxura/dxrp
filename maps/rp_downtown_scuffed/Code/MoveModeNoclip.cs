namespace Sandbox.Movement;

/// <summary>
/// The character is clipping bro
/// </summary>
[Icon("scuba_diving"), Group("Movement"), Title("Move Mode Noclip"), Tint(EditorTint.Yellow)]
public partial class MoveModeNoclip : MoveMode
{
	[Sync]
	private bool isNoclip { get; set; }

	public override void UpdateRigidBody(Rigidbody body)
	{
		body.Gravity = false;
		body.LinearDamping = 10f;
		body.AngularDamping = 10f;
	}

	public override void OnModeBegin()
	{
		Controller.ColliderObject.Enabled = false;
	}

	public override void OnModeEnd(MoveMode next)
	{
		Controller.ColliderObject.Enabled = true;
	}

	public override int Score(PlayerController controller)
	{
		return isNoclip ? 10000 : -10000;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy)
			return;

		if (Input.Pressed("Voice"))
		{
			isNoclip = !isNoclip;
		}
	}

	public override Vector3 UpdateMove(Rotation eyes, Vector3 input)
	{
		var rot = eyes.Angles() with { pitch = Scene.Camera.WorldRotation.Pitch() };

		if (Input.Down("jump"))
		{
			input += Vector3.Up;
		}

		if (Input.Down("Duck"))
		{
			input += Vector3.Down;
		}

		return rot.ToRotation() * input * 3500f;
	}
}