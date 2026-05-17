namespace Dxura.RP.Game;

public sealed class AnimationHelper : Component
{
	public enum Hand
	{
		Both,
		Right,
		Left
	}

	public enum HoldTypes
	{
		None,
		Pistol,
		Rifle,
		Shotgun,
		HoldItem,
		Punch,
		Swing,
		Rpg
	}

	public enum MoveStyles
	{
		Auto,
		Walk,
		Run
	}

	[Property] public SkinnedModelRenderer Target { get; set; } = null!;

	[Property] public GameObject? EyeSource { get; set; }


	[Property] [Range( 0.5f, 1.5f )] [Change( nameof( OnHeightChanged ) )] public float Height { get; set; } = 1.0f;

	[Property] public GameObject? IkLeftHand { get; set; }
	[Property] public GameObject? IkRightHand { get; set; }
	[Property] public GameObject? IkLeftFoot { get; set; }
	[Property] public GameObject? IkRightFoot { get; set; }

	private GameObject? _appliedIkLeftHand;
	private GameObject? _appliedIkRightHand;
	private GameObject? _appliedIkLeftFoot;
	private GameObject? _appliedIkRightFoot;


	public Transform GetEyeWorldTransform
	{
		get
		{
			if ( EyeSource.IsValid() )
			{
				return EyeSource.Transform.World;
			}

			return Transform.World;
		}
	}

	public Rotation AimAngle
	{
		set
		{
			value = Target.WorldRotation.Inverse * value;
			var ang = value.Angles();

			Target.Set( "aim_body_pitch", ang.pitch );
			Target.Set( "aim_body_yaw", ang.yaw );
		}
	}

	public float AimEyesWeight
	{
		get => Target.GetFloat( "aim_eyes_weight" );
		set => Target.Set( "aim_eyes_weight", value );
	}

	public float AimHeadWeight
	{
		get => Target.GetFloat( "aim_head_weight" );
		set => Target.Set( "aim_head_weight", value );
	}

	public float AimBodyWeight
	{
		get => Target.GetFloat( "aim_body_weight" );
		set => Target.Set( "aim_body_weight", value );
	}


	public float FootShuffle
	{
		get => Target.GetFloat( "move_shuffle" );
		set => Target.Set( "move_shuffle", value );
	}

	public float DuckLevel
	{
		get => Target.GetFloat( "duck" );
		set => Target.Set( "duck", value );
	}

	public float VoiceLevel
	{
		get => Target.GetFloat( "voice" );
		set => Target.Set( "voice", value );
	}

	public SitType? SitType
	{
		get => (SitType)Target.GetInt( "sit" );
		set => Target.Set( "sit", (int)(value ?? 0) );
	}

	public bool IsGrounded
	{
		get => Target.GetBool( "b_grounded" );
		set => Target.Set( "b_grounded", value );
	}

	public bool IsSwimming
	{
		get => Target.GetBool( "b_swim" );
		set => Target.Set( "b_swim", value );
	}

	public float SkidAmount
	{
		get => Target.GetFloat( "skid" );
		set => Target.Set( "skid", value );
	}

	public bool IsClimbing
	{
		get => Target.GetBool( "b_climbing" );
		set => Target.Set( "b_climbing", value );
	}

	public bool IsNoclipping
	{
		get => Target.GetBool( "b_noclip" );
		set => Target.Set( "b_noclip", value );
	}

	public bool IsWeaponLowered
	{
		get => Target.GetBool( "b_weapon_lower" );
		set => Target.Set( "b_weapon_lower", value );
	}

	public HoldTypes HoldType
	{
		get => (HoldTypes)Target.GetInt( "holdtype" );
		set => Target.Set( "holdtype", (int)value );
	}

	public Hand Handedness
	{
		get => (Hand)Target.GetInt( "holdtype_handedness" );
		set => Target.Set( "holdtype_handedness", (int)value );
	}

	/// <summary>
	///     We can force the model to walk or run, or let it decide based on the speed.
	/// </summary>
	public MoveStyles MoveStyle
	{
		get => (MoveStyles)Target.GetInt( "move_style" );
		set => Target.Set( "move_style", (int)value );
	}

	public int SitPose
	{
		get => Target.GetInt( "sit" );
		set => Target.Set( "sit", value );
	}

	public int HoldTypePose
	{
		get => Target.GetInt( "holdtype_pose" );
		set => Target.Set( "holdtype_pose", value );
	}

	public int HoldTypePoseHand
	{
		get => Target.GetInt( "hold_type_pose_hand" );
		set => Target.Set( "hold_type_pose_hand", value );
	}

	public Vector3 AimBody
	{
		set => Target.Set( "aim_body", value );
	}

	public Vector3 AimEyes
	{
		set => Target.Set( "aim_eyes", value );
	}

	public Vector3 AimHead
	{
		set => Target.Set( "aim_head", value );
	}

	public int MoveGroundSpeed
	{
		get => Target.GetInt( "move_groundspeed" );
		set => Target.Set( "move_groundspeed", value );
	}

	public void ProceduralHitReaction( float damageScale = 1.0f, Vector3 force = default )
	{
		var boneId = 0;
		var tx = Target.GetBoneObject( boneId );

		if ( !tx.IsValid() )
		{
			return;
		}

		var localToBone = tx.Transform.Local.Position;
		if ( localToBone == Vector3.Zero )
		{
			localToBone = Vector3.One;
		}

		Target.Set( "hit", true );
		Target.Set( "hit_bone", boneId );
		Target.Set( "hit_offset", localToBone );
		Target.Set( "hit_direction", force.Normal );
		Target.Set( "hit_strength", force.Length / 1000.0f * damageScale );
	}

	protected override void OnStart()
	{
		ApplyHeight();
	}

	private void OnHeightChanged( float before, float after )
	{
		ApplyHeight();
	}

	private void ApplyHeight()
	{
		if ( Target.IsValid() )
		{
			Target.Set( "scale_height", Height );
		}
	}

	protected override void OnUpdate()
	{
		UpdateIkState( "hand_left", IkLeftHand, ref _appliedIkLeftHand );
		UpdateIkState( "hand_right", IkRightHand, ref _appliedIkRightHand );
		UpdateIkState( "foot_left", IkLeftFoot, ref _appliedIkLeftFoot );
		UpdateIkState( "foot_right", IkRightFoot, ref _appliedIkRightFoot );
	}

	private void UpdateIkState( string name, GameObject? target, ref GameObject? appliedTarget )
	{
		var activeTarget = target.IsValid() && target.Active ? target : null;

		if ( activeTarget == appliedTarget )
		{
			return;
		}

		appliedTarget = activeTarget;

		if ( activeTarget.IsValid() )
		{
			SetIk( name, activeTarget.Transform.World );
		}
		else
		{
			ClearIk( name );
		}
	}

	public void SetIk( string name, Transform tx )
	{
		// convert local to model
		tx = Target.Transform.World.ToLocal( tx );

		Target.Set( $"ik.{name}.enabled", true );
		Target.Set( $"ik.{name}.position", tx.Position );
		Target.Set( $"ik.{name}.rotation", tx.Rotation );
	}

	public void ClearIk( string name )
	{
		Target.Set( $"ik.{name}.enabled", false );
	}
}
