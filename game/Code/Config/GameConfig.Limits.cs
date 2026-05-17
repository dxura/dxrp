namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Limits
	//
	public virtual uint PropLimit { get; set; } = 150;
	public virtual uint TextLimit { get; set; } = 10;
	public virtual uint FrameLimit { get; set; } = 15;
	public virtual uint DoorLimit { get; set; } = 10;
	public virtual uint LightLimit { get; set; } = 2;

	// Wire
	public virtual uint GateWireLimit { get; set; } = 35;
	public virtual uint ConstantWireLimit { get; set; } = 15;
	public virtual uint ButtonWireLimit { get; set; } = 5;
	public virtual uint KeypadWireLimit { get; set; } = 5;
	public virtual uint DelayWireLimit { get; set; } = 3;
	public virtual uint ForcerWireLimit { get; set; } = 3;
	public virtual uint IntervalWireLimit { get; set; } = 2;
	public virtual uint LedWireLimit { get; set; } = 5;
	public virtual uint MemoryWireLimit { get; set; } = 5;
	public virtual uint MoneyPotWireLimit { get; set; } = 3;
	public virtual uint NotiferWireLimit { get; set; } = 2;
	public virtual uint ScreenWireLimit { get; set; } = 3;
	public virtual uint SpeakerWireLimit { get; set; } = 3;
	public virtual uint TriggerWireLimit { get; set; } = 3;
	public virtual uint UserWireLimit { get; set; } = 3;
	public virtual uint CameraWireLimit { get; set; } = 1;
	public virtual uint TargetWireLimit { get; set; } = 3;
	public virtual uint SynthesizerWireLimit { get; set; } = 1;
}

