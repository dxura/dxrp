namespace Dxura.RP.Game;

public partial class Door
{
	[Property] [Group( "Sound" )] public SoundEvent? OpenSound { get; set; }
	[Property] [Group( "Sound" )] public SoundEvent? OpenFinishedSound { get; set; }
	[Property] [Group( "Sound" )] public SoundEvent? CloseSound { get; set; }
	[Property] [Group( "Sound" )] public SoundEvent? CloseFinishedSound { get; set; }

	[Property] [Group( "Sound" )] public SoundEvent? BuySound { get; set; }
	[Property] [Group( "Sound" )] public SoundEvent? SellSound { get; set; }

	[Property] [Group( "Sound" )] public SoundEvent? KnockSound { get; set; }

	[Property] [Group( "Sound" )] public SoundEvent? LockSound { get; set; }
	[Property] [Group( "Sound" )] public SoundEvent? UnlockSound { get; set; }

	[Property] [Group( "Sound" )] public SoundEvent? LockedSound { get; set; }
}
