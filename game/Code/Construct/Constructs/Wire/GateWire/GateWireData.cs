namespace Dxura.RP.Game.Wire;

public record GateWireData : IConstructData
{
	public uint SchemaVersion => 1;
	public GateType Type { get; set; } = GateType.And;
}

public enum GateCategory
{
	Logic,
	Arithmetic,
	Comparison,
	Math,
	Control,
	String,
	Time,
	Vector,
	All
}

public enum GateType
{
	// Logic gates (0-99)
	And = 0,
	Or = 1,
	Not = 2,
	Xor = 3,
	Nand = 4,
	Nor = 5,
	If = 6,

	// Arithmetic operations (100-199)
	Add = 100,
	Subtract = 101,
	Multiply = 102,
	Divide = 103,
	Modulo = 104,
	Power = 105,
	Min = 106,
	Max = 107,

	// Comparison operations (200-299)
	Equal = 200,
	NotEqual = 201,
	GreaterThan = 202,
	LessThan = 203,
	GreaterEqual = 204,
	LessEqual = 205,

	// Math functions (300-399)
	Random = 300,
	Abs = 301,
	Floor = 302,
	Ceiling = 303,
	Round = 304,
	Sin = 305,
	Cos = 306,
	Tan = 307,
	Sqrt = 308,
	Log = 309,
	Clamp = 310,
	Lerp = 311,

	// Control flow (400-499)
	Select = 400,
	Latch = 401,
	Threshold = 402,
	Invert = 403,

	// String operations (500-599)
	Concat = 500,
	Length = 501,
	Substring = 502,
	ToUpper = 503,
	ToLower = 504,

	// Time functions (600-699)
	Time = 600,
	DeltaTime = 601,

	// Algebraic logic (700-799)
	Vector2 = 700,
	Vector3 = 701,
	VectorGet = 702
}
