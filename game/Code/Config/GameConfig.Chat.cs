namespace Dxura.RP.Game;

public abstract partial class GameConfig
{
	//
	// Chat
	//
	public virtual bool OocChatEnabled { get; set; } = true;
	public virtual int ChatMaxLength { get; set; } = 150;
	public virtual int ChatMaxDistance { get; set; } = 1000;
	public virtual bool LocalChatTts { get; set; } = true;
	public virtual string[] ChatEmojis { get; set; } =
	[
		"\U0001F600",
		"\U0001F602",
		"\U0001F604",
		"\U0001F609",
		"\U0001F60A",
		"\U0001F60E",
		"\U0001F914",
		"\U0001F62D",
		"\U0001F621",
		"\U0001F631",
		"\U0001F44D",
		"\U0001F44E",
		"\U0001F44F",
		"\U0001F64F",
		"\U0001F525",
		"\U0001F4AF",
		"\U0001F389",
		"\U0001F480",
		"\U0001F440",
		"\U0001F4A9",
		"\U00002764\U0000FE0F",
		"\U0001F494",
		"\U00002B50",
		"\U000026A0\U0000FE0F",
		"\U0001F91D"
	];

	// Auto Messages
	public virtual bool AutoMessagesEnabled { get; set; } = true;
	public virtual int AutoMessagesInterval { get; set; } = 600; // 5 minutes
	public virtual string[] AutoMessages { get; set; } =
	[
		"#system.automessage.discord",
		"#system.automessage.rules",
		"#system.automessage.staff_hint",
		"#system.automessage.rulebreakers",
	];
}

