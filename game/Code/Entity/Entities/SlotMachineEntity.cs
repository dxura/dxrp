using Dxura.RP.Game.Equipments;
using Sandbox.Diagnostics;
using WorldPanel=Sandbox.WorldPanel;

namespace Dxura.RP.Game.Entities;

public class SlotMachineEntity : BaseEntity, IGameEvents
{
	[Property] public required WorldPanel GamePanel { get; set; }
	[Property] public Component? GameComponent { get; set; }
	[Property] public required ModelRenderer ModelRenderer { get; set; }
	[Property] public required Decal Decal { get; set; }
	[Property] public required ContinuousSoundPoint ContinuousSoundPoint { get; set; }

	[Property] public Color? Tint { get; set; }
	[Property] public Texture? Graphic { get; set; }
	[Property] public SoundEvent? BackgroundMusic { get; set; }
	[Property] public string? SlotGameComponent { get; set; }
	[Property] public SoundEvent? ProcessSound { get; set; }
	[Property] public SoundEvent? WinSound { get; set; }
	[Property] public SoundEvent? LoseSound { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		UpdateState();
	}

	public void OnSecondlyUpdate()
	{
		if ( Player.Local.IsValid() )
		{
			var playerDistance = Player.Local.WorldPosition.Distance( GameObject.WorldPosition );

			if ( GamePanel.IsValid() )
			{
				GamePanel.Enabled = playerDistance < 500f;
			}
		}
	}

	private void UpdateState()
	{
		if ( !ModelRenderer.IsValid() )
		{
			return;
		}

		if ( Tint.HasValue )
		{
			ModelRenderer.Tint = Tint.Value;
		}

		if ( Graphic.IsValid() && Decal.IsValid() )
		{
			Decal.Decals =
			[
				new DecalDefinition
				{
					ColorTexture = Graphic
				}
			];
		}

		if ( BackgroundMusic.IsValid() )
		{
			ContinuousSoundPoint.SoundEvent = BackgroundMusic;
		}

		if ( Networking.IsHost )
		{
			if ( GameComponent.IsValid() )
			{
				GameComponent.Destroy();
				GameComponent = null;
			}
			var gameComponent = GamePanel.GameObject.Components.Create( TypeLibrary.GetType( SlotGameComponent ) );
			GameComponent = gameComponent;

			Network.Refresh( gameComponent );
		}

	}
}
