namespace Dxura.RP.Game.Statuses;

public class WeedHighStatus : BaseStatus
{
	public override string Id => Constants.WeedHighStatus;
	public override string Name => "Baked";
	public override string Icon => "ui/resources/hemp.png";
	public override Color Color => Color.FromRgb( 0x4CAF50 );

	public override float? DefaultDuration => 120f;

	public override bool PreventFallDamage => true;
	public override bool RemoveOnRespawn => true;

	private GameObject? _jointReference;
	private const string JointPrefab = "prefabs/player_modifers/player_joint.prefab";

	public override void OnAddedServer( Player player )
	{
		var joint = GameObject.GetPrefab( JointPrefab ).Clone( new CloneConfig
		{
			Transform = new Transform(), Parent = player.HatRoot
		} );

		joint.NetworkSpawn( player.Connection );
		_jointReference = joint;
	}

	public override void OnRemovedServer( Player player )
	{
		if ( _jointReference.IsValid() )
		{
			_jointReference.Destroy();
		}
	}

	public override void OnAddedOwner( Player player )
	{
		player.HueRotateTarget = 90f;
		player.Rigidbody.GravityScale = 0.25f;
	}

	public override void OnRemovedOwner( Player player )
	{
		player.HueRotateTarget = 0f;
		player.Rigidbody.GravityScale = 1f;
	}


}
