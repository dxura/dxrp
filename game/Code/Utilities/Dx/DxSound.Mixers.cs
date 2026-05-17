using Sandbox.Audio;
namespace Dxura.RP.Game;

public static partial class DxSound
{
	private const float DefaultEquipmentVolume = 0.7f;
	private const float DefaultFootstepsVolume = 0.25f;
	private const float DefaultPrinterVolume = 1f;
	private const float DefaultWorldVolume = 1f;
	private const float DefaultTtsVolume = 0.4f;
	private const float DefaultWireVolume = 0.3f;
	private const float DefaultNotificationVolume = 1f;
	private const float DefaultCinemaVolume = 1f;
	private const float DefaultLockdownVolume = 1f;
	private const float DefaultRadioVolume = 0.25f;
	private const float DefaultTvVolume = 0.3f;

	[ConVar( "dx_mixer_equipment_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnEquipmentVolumeChanged ) )]
	private static float EquipmentVolume { get; set; } = DefaultEquipmentVolume;

	[ConVar( "dx_mixer_footsteps_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnFootstepsVolumeChanged ) )]
	private static float FootstepsVolume { get; set; } = DefaultFootstepsVolume;

	[ConVar( "dx_mixer_printer_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnPrinterVolumeChanged ) )]
	private static float PrinterVolume { get; set; } = DefaultPrinterVolume;

	[ConVar( "dx_mixer_world_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnWorldVolumeChanged ) )]
	private static float WorldVolume { get; set; } = DefaultWorldVolume;

	[ConVar( "dx_mixer_tts_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnTtsVolumeChanged ) )]
	public static float TtsVolume { get; set; } = DefaultTtsVolume;

	[ConVar( "dx_mixer_wire_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnWireVolumeChanged ) )]
	public static float WireVolume { get; set; } = DefaultWireVolume;

	[ConVar( "dx_mixer_notification_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnNotificationVolumeChanged ) )]
	public static float NotificationVolume { get; set; } = DefaultNotificationVolume;

	[ConVar( "dx_mixer_cinema_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnCinemaVolumeChanged ) )]
	public static float CinemaVolume { get; set; } = DefaultCinemaVolume;

	[ConVar( "dx_mixer_lockdown_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnLockdownVolumeChanged ) )]
	public static float LockdownVolume { get; set; } = DefaultLockdownVolume;

	[ConVar( "dx_mixer_radio_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnRadioVolumeChanged ) )]
	public static float RadioVolume { get; set; } = DefaultRadioVolume;

	[ConVar( "dx_mixer_tv_volume", ConVarFlags.Saved, Min = 0, Max = 1f )]
	[Change( nameof( OnTvVolumeChanged ) )]
	public static float TvVolume { get; set; } = DefaultTvVolume;

	public static void InitializeMixers()
	{
		OnEquipmentVolumeChanged( 0f, EquipmentVolume );
		OnFootstepsVolumeChanged( 0f, FootstepsVolume );
		OnPrinterVolumeChanged( 0f, PrinterVolume );
		OnWorldVolumeChanged( 0f, WorldVolume );
		OnTtsVolumeChanged( 0f, TtsVolume );
		OnWireVolumeChanged( 0f, WireVolume );
		OnNotificationVolumeChanged( 0f, NotificationVolume );
		OnCinemaVolumeChanged( 0f, CinemaVolume );
		OnLockdownVolumeChanged( 0f, LockdownVolume );
		OnRadioVolumeChanged( 0f, RadioVolume );
		OnTvVolumeChanged( 0f, TvVolume );
	}

	private static void OnEquipmentVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "Equipment" );
		mixer?.Volume = newValue;
	}

	private static void OnFootstepsVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "Footsteps" );
		mixer?.Volume = newValue;
	}
	private static void OnPrinterVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "Printer" );
		mixer?.Volume = newValue;
	}

	private static void OnWorldVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "World" );
		mixer?.Volume = newValue;
	}

	private static void OnTtsVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "TTS" );
		mixer?.Volume = newValue;
	}

	private static void OnWireVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "Wire" );
		mixer?.Volume = newValue;
	}
	private static void OnNotificationVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "Notification" );
		mixer?.Volume = newValue;
	}

	private static void OnCinemaVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "Cinema" );
		mixer?.Volume = newValue;
	}

	private static void OnLockdownVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "Lockdown" );
		mixer?.Volume = newValue;
	}

	private static void OnRadioVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "Radio" );
		mixer?.Volume = newValue;
	}

	private static void OnTvVolumeChanged( float oldValue, float newValue )
	{
		var mixer = Mixer.FindMixerByName( "TV" );
		mixer?.Volume = newValue;
	}

	public static void ResetMixerVolumes()
	{
		ConsoleSystem.SetValue( "dx_mixer_equipment_volume", DefaultEquipmentVolume );
		ConsoleSystem.SetValue( "dx_mixer_footsteps_volume", DefaultFootstepsVolume );
		ConsoleSystem.SetValue( "dx_mixer_printer_volume", DefaultPrinterVolume );
		ConsoleSystem.SetValue( "dx_mixer_world_volume", DefaultWorldVolume );
		ConsoleSystem.SetValue( "dx_mixer_tts_volume", DefaultTtsVolume );
		ConsoleSystem.SetValue( "dx_mixer_wire_volume", DefaultWireVolume );
		ConsoleSystem.SetValue( "dx_mixer_notification_volume", DefaultNotificationVolume );
		ConsoleSystem.SetValue( "dx_mixer_cinema_volume", DefaultCinemaVolume );
		ConsoleSystem.SetValue( "dx_mixer_lockdown_volume", DefaultLockdownVolume );
		ConsoleSystem.SetValue( "dx_mixer_radio_volume", DefaultRadioVolume );
		ConsoleSystem.SetValue( "dx_mixer_tv_volume", DefaultTvVolume );
	}
}
