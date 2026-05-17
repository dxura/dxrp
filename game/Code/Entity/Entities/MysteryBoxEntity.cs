using Dxura.RP.Game.UI;
using Dxura.RP.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Dxura.RP.Game;

public sealed class MysteryBoxEntity : BaseEntity, Component.IPressable
{
	private readonly SemaphoreSlim _openSemaphore = new( 1, 1 );

	[Property] 
	[RequireComponent]
	public required ModelRenderer Renderer {get; set;}
	
	[Property] private Model? OpenedModel {get; set;}
	[Property] private float HoldDuration { get; set; } = 5f;
	[Property] public SoundEvent? OpeningSound { get; set; }
	[Property] public SoundEvent? OpenedSound { get; set; }
	[Property] public SoundEvent? WinSound { get; set; }
	[Property] public SoundEvent? LoseSound { get; set; }
	[Property] public SoundEvent? RefundSound { get; set; }

	[Sync( SyncFlags.FromHost )]
	public MysteryBoxState State { get; set; } = new( MysteryBoxPhase.Idle, Guid.Empty );

	public bool IsOpened => State.Phase is MysteryBoxPhase.Win or MysteryBoxPhase.Lose or MysteryBoxPhase.Refund;
	public bool IsOpening => State.Phase == MysteryBoxPhase.Opening;

	private Guid? OpeningConnectionId { get; set; }
	private float OpeningStartedAt { get; set; }
	private float LocalOpeningStartedAt { get; set; }
	private bool LocalIsOpening { get; set; }
	private bool LocalDidComplete { get; set; }
	private SoundHandle? _openingSoundHandle;

	public struct MysteryBoxState
	{
		public MysteryBoxPhase Phase { get; set; }
		public Guid WonItemDefinitionId { get; set; }

		public MysteryBoxState( MysteryBoxPhase phase, Guid wonItemDefinitionId )
		{
			Phase = phase;
			WonItemDefinitionId = wonItemDefinitionId;
		}
	}

	public enum MysteryBoxPhase
	{
		Idle,
		Opening,
		Win,
		Lose,
		Refund
	}

	public bool CanPress( IPressable.Event e )
	{
		return !IsOpened && !IsOpening;
	}

	public bool Press( IPressable.Event e )
	{
		if ( IsOpened || IsOpening )
		{
			return false;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( "mystery-box:open", Config.Current.Game.ActionQuickCooldown, true ) )
		{
			return false;
		}

		LocalOpeningStartedAt = RealTime.Now;
		LocalIsOpening = true;
		LocalDidComplete = false;
		ShowOpeningOverlay( 0f );
		BeginOpenHost();

		return true;
	}

	public bool Pressing( IPressable.Event e )
	{
		if ( IsOpened )
		{
			ClearOpeningOverlay();
			return false;
		}

		if ( !LocalIsOpening )
		{
			LocalOpeningStartedAt = RealTime.Now;
			LocalIsOpening = true;
			LocalDidComplete = false;
		}

		var progress = ((RealTime.Now - LocalOpeningStartedAt) / HoldDuration).Clamp( 0f, 1f );
		ShowOpeningOverlay( progress );

		if ( progress < 1f || LocalDidComplete )
		{
			return true;
		}

		LocalDidComplete = true;
		FinishOpenHost();
		ClearOpeningOverlay();
		return true;
	}

	public void Release( IPressable.Event e )
	{
		var didComplete = LocalDidComplete;
		LocalIsOpening = false;
		LocalDidComplete = false;
		ClearOpeningOverlay();
		if ( !didComplete )
		{
			CancelOpenHost();
		}
	}

	[Rpc.Host]
	private void BeginOpenHost()
	{
		var callerId = Rpc.CallerId;
		if ( IsOpened || IsOpening )
		{
			return;
		}

		if ( Cooldown.Current.CheckAndStartCooldown( $"{callerId}:mystery-box:open", Config.Current.Game.ActionQuickCooldown ) )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !CanPlayerUseBox( player ) )
		{
			return;
		}

		OpeningConnectionId = callerId;
		OpeningStartedAt = RealTime.Now;
		SetState( MysteryBoxPhase.Opening );
		StartOpeningSound();
	}

	[Rpc.Host]
	private void CancelOpenHost()
	{
		if ( !IsOpening || OpeningConnectionId != Rpc.CallerId )
		{
			return;
		}

		SetState( MysteryBoxPhase.Idle );
		OpeningConnectionId = null;
		StopOpeningSound();
	}

	[Rpc.Host]
	private void FinishOpenHost()
	{
		var callerId = Rpc.CallerId;
		if ( !IsOpening || IsOpened || OpeningConnectionId != callerId )
		{
			return;
		}

		if ( RealTime.Now - OpeningStartedAt < HoldDuration * 0.9f )
		{
			return;
		}

		var player = GameUtils.GetPlayerByConnectionId( callerId );
		if ( !CanPlayerUseBox( player ) )
		{
			SetState( MysteryBoxPhase.Idle );
			OpeningConnectionId = null;
			StopOpeningSound();
			return;
		}

		_ = OpenHost( player! );
	}

	private async Task OpenHost( Player opener )
	{
		await _openSemaphore.WaitAsync();

		try
		{
			if ( IsOpened || GameObject.IsDestroyed )
			{
				return;
			}

			OpeningConnectionId = null;
			SetState( MysteryBoxPhase.Opening );
			ApplyOpenedModel();
			StopOpeningSound();
			OpenedSound?.Broadcast( WorldPosition, GameObject );

			var owner = Owner != 0 ? Owner : opener.SteamId;
			var ownerPlayer = GameUtils.GetPlayerById( owner );
			var rewards = Config.Current.Game.MysteryBoxRewards
				.Where( x => Guid.TryParse( x, out _ ) )
				.ToArray();

			if ( rewards.Length == 0 )
			{
				await RefundOwner( owner );
				SetState( MysteryBoxPhase.Refund );
				RefundSound?.Broadcast( WorldPosition, GameObject );
				if ( ownerPlayer.IsValid() )
				{
					ownerPlayer.Warn( string.Format(
						Language.GetPhrase( "notify.mystery_box.refund_no_rewards" ),
						GetMarketItemCost() ) );
				}
				GameObject.DestroyAsync( 5f );
				return;
			}

			if ( !DidWin() )
			{
				SetState( MysteryBoxPhase.Lose );
				LoseSound?.Broadcast( WorldPosition, GameObject );
				if ( ownerPlayer.IsValid() )
				{
					ownerPlayer.Warn( "#notify.mystery_box.empty" );
				}
				GameObject.DestroyAsync( 5f );
				return;
			}

			var prizeId = SelectPrize( rewards );
			var item = await ServerApiClient.GivePlayerItem( owner, new GiveItemDto
			{
				ItemId = prizeId,
				Quantity = 1
			} );

			if ( item == null )
			{
				await RefundOwner( owner );
				SetState( MysteryBoxPhase.Refund );
				RefundSound?.Broadcast( WorldPosition, GameObject );
				if ( ownerPlayer.IsValid() )
				{
					ownerPlayer.Error( "#notify.mystery_box.reward_failed" );
				}
				GameObject.DestroyAsync( 5f );
				return;
			}

			SetState( MysteryBoxPhase.Win, item.Definition.Id );
			WinSound?.Broadcast( WorldPosition, GameObject );
			_ = ServerApiClient.Audit( "MysteryBoxWin", $"{opener.SteamName} ({opener.SteamId}) won {item.Definition.Name} ({item.Definition.Id}) from mystery box", opener.SteamId );
			if ( ownerPlayer.IsValid() )
			{
				ownerPlayer.Inventory( string.Format(
					Language.GetPhrase( "notify.mystery_box.won" ),
					ResolvePhrase( item.Definition.Name ) ) );
				ownerPlayer.BroadcastInventoryRefresh( item.Definition.Id, item.Quantity );
			}
			GameObject.DestroyAsync( 8f );
		}
		finally
		{
			_openSemaphore.Release();
		}
	}

	private bool CanPlayerUseBox( Player? player )
	{
		if ( !player.IsValid() || IsOpened )
		{
			return false;
		}

		var tr = Scene.Trace.Ray( player.AimRay, Config.Current.Game.ReachDistance )
			.IgnoreGameObjectHierarchy( player.GameObject )
			.UseHitboxes()
			.Run();

		return tr.Hit && tr.GameObject.Root == GameObject.Root;
	}

	private static bool DidWin()
	{
		var winPercentage = Config.Current.Game.MysteryBoxWinPercentage.Clamp( 0f, 100f );
		return Sandbox.Game.Random.Float( 0f, 100f ) <= winPercentage;
	}

	private static Guid SelectPrize( string[] rewards )
	{
		return Guid.Parse( Sandbox.Game.Random.FromArray( rewards )! );
	}

	private int GetMarketItemCost()
	{
		var marketItem = GameModeMarketItems.All.FirstOrDefault( x =>
			x.Type == GameModeMarketItemType.Entity && x.ReferenceId == GameModeEntityId );
		return marketItem?.Cost ?? 0;
	}

	private async Task RefundOwner( long owner )
	{
		var amount = GetMarketItemCost();
		if ( amount <= 0 )
		{
			return;
		}

		var ownerPlayer = GameUtils.GetPlayerById( owner );
		if ( ownerPlayer.IsValid() )
		{
			await ownerPlayer.PayHost( (uint)amount, Language.GetPhrase( "payment.mystery_box.refund" ) );
			return;
		}

		await ServerApiClient.ModifyPlayerBalance( owner, (int)amount, Language.GetPhrase( "payment.mystery_box.refund" ) );
	}

	private static string ResolvePhrase( string text )
	{
		return text.StartsWith( '#' ) ? Language.GetPhrase( text[1..] ) : text;
	}

	private void SetState( MysteryBoxPhase phase, Guid? wonItemDefinitionId = null )
	{
		State = new MysteryBoxState( phase, phase == MysteryBoxPhase.Win ? wonItemDefinitionId ?? Guid.Empty : Guid.Empty );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void ApplyOpenedModel()
	{
		Renderer.Model = OpenedModel;
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void StartOpeningSound()
	{
		StopOpeningSoundLocal();
		_openingSoundHandle = OpeningSound?.Play( WorldPosition, GameObject );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	private void StopOpeningSound()
	{
		StopOpeningSoundLocal();
	}

	private void StopOpeningSoundLocal()
	{
		_openingSoundHandle?.Stop( 0.1f );
		_openingSoundHandle = null;
	}

	protected override void OnDestroy()
	{
		StopOpeningSoundLocal();
		base.OnDestroy();
	}

	protected override void OnDisabled()
	{
		StopOpeningSoundLocal();
		base.OnDisabled();
	}

	private static void ShowOpeningOverlay( float progress )
	{
		if ( !EquipmentOverlay.Instance.IsValid() )
		{
			return;
		}

		EquipmentOverlay.Instance.IsActive = true;
		EquipmentOverlay.Instance.Status = "#mystery_box.opening_status";
		EquipmentOverlay.Instance.Progress = progress;
	}

	private static void ClearOpeningOverlay()
	{
		if ( !EquipmentOverlay.Instance.IsValid() )
		{
			return;
		}

		EquipmentOverlay.Instance.IsActive = false;
		EquipmentOverlay.Instance.Progress = null;
	}
}
