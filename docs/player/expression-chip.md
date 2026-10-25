---
title: Expression Chip
description: Program your own wire logic with the Expression chip, a scripting language modelled on Wiremod's E2. Full reference and worked examples.
order: 2
---

The **Expression** chip runs a small script you write. You give it inputs and outputs, wire it to buttons, keypads, screens, lights and other components, and it reacts to whatever arrives on its inputs. If you've used Wiremod's E2 in Garry's Mod, the syntax will feel familiar.

Scripts are pure logic. A chip only knows what is wired into it and only affects the world through what its outputs are wired to.

## Quick start

1. Equip the **Tool Gun**, open the build menu and pick **Expression** under *Logic*.
2. Press **Edit script**, write your script, then press **Apply**. The editor shows compile errors live at the bottom.
3. **Left click** a surface to place a chip, or left click an existing chip to upload the script to it. **Right click** a chip to copy its script back into the tool.
4. Wire the chip's ports with the **Wire** tool. The ports come from your `@inputs` and `@outputs` lines.

Your first chip, a light switch that flips every time a button is pressed:

```e2
@name Toggle
@inputs Button:bool
@outputs On:bool

if (~Button & Button) {
    On = !On
}
```

Wire a **Button** to `Button`, and `On` to a **Light** or **LED**.

> **Tip:** If a chip stops working, look at its label. Errors are shown under the chip's name. Fix the script and upload it again to restart the chip.

## How a chip runs

A script doesn't loop forever. The chip runs the **whole script from top to bottom** each time something happens, then stops and waits:

| What happens | How to detect it |
|---|---|
| The chip is placed or the script is uploaded | `first()` is true |
| An input changes | `~Input` is true for that input, `inputClk()` is true |
| An `interval()` fires | `clk()` is true |
| A named `timer()` fires | `clk("name")` is true |

Values declared with `@outputs` and `@persist` keep their value between runs. Every other variable is **reset at the start of each run**. To remember something, declare it with `@persist`.

Chips run on the server. If anything goes wrong (a type mismatch, an index out of range, too many operations), the chip **halts** and shows the error on its label until you upload a script again.

## Directives

Directives go at the very top of the script, one per line.

| Directive | Meaning |
|---|---|
| `@name My Chip` | Name shown on the chip's label |
| `@inputs A B:string C:bool` | Input ports. Read-only inside the script |
| `@outputs Out Text:string` | Output ports. Assign to them to send values |
| `@persist Count Log:array` | Variables kept between runs, not shown as ports |
| `@trigger all` | Any input change runs the script (the default) |
| `@trigger A B` | Only changes to `A` or `B` run the script. Other inputs still update their value |
| `@trigger none` | Input changes never run the script. Use with `interval()` or `timer()` |

Rules for directive variables:

- Names must start with an **uppercase letter** and contain only letters, digits and `_`.
- The type follows a colon. With no type, it's a `number`.
- Group several names with brackets: `@persist [X Y Z]:number`.
- `array` is only allowed in `@persist`. Ports can't be arrays.
- Up to 32 inputs, 32 outputs and 64 persist variables.

## Types

| Type | Example | Notes |
|---|---|---|
| `number` | `42`, `-1.5`, `0xFF`, `1e3` | 64-bit inside the script. Wires carry 32-bit floats |
| `string` | `"hello"` | Escapes: `\n` newline, `\t` tab, `\"` quote, `\\` backslash |
| `bool` | `1`, `0` | Stored as a number (1 or 0) and shown as a true/false port |
| `array` | `array(1, "two", 3)` | List of numbers and strings. Arrays can't contain arrays |

**Truthiness:** a number is true if it isn't `0`, a string if it isn't empty, and an array if it has elements. Comparisons and logic operators return `1` or `0`.

## Variables and scope

```e2
@persist Total          # kept between runs
Temp = 5                # plain global: exists until the end of this run
local Square = Temp * Temp   # local: only inside the current { } block
```

- Reading a variable that was never assigned is an error: `Variable 'X' is not defined`.
- `local` names can't reuse a directive name.
- Assigning a value of the wrong type to a declared variable is an error. For example, `Total = "hi"` fails when `Total` is a number.
- Inputs can't be assigned.

## Operators

From highest to lowest precedence:

| Operators | Meaning |
|---|---|
| `^` | Power, right associative. `-2^2` is `-4` |
| `-X` `!X` | Negate, logical not |
| `*` `/` `%` | Multiply, divide, modulo. Dividing by `0` gives `0`. `%` is floored: `-1 % 360` is `359` |
| `+` `-` | Add, subtract. `+` joins strings if either side is a string: `"Score: " + 10` |
| `<` `>` `<=` `>=` | Compare numbers, or strings alphabetically |
| `==` `!=` | Equal, not equal (numbers and strings) |
| `&` or `&&` | Logical and, short-circuits |
| `\|` or `\|\|` | Logical or, short-circuits |
| `C ? A : B` | Ternary: `A` if `C` is true, else `B` |
| `A ?: B` | `A` if it's true, else `B` |

Assignment: `=`, `+=`, `-=`, `*=`, `/=`, `%=`, `X++`, `X--`.

### Port operators

| Operator | Meaning |
|---|---|
| `~Input` | 1 if this run was caused by `Input` changing |
| `$X` | How much the number `X` changed since the previous run (`X` must be a declared number or bool) |
| `->Port` | 1 if the input or output `Port` has a wire attached |

## Control flow

```e2
if (Speed > 100) {
    Warning = 1
} elseif (Speed > 50) {      # "else if" also works
    Warning = 0.5
} else {
    Warning = 0
}

while (N > 1) {
    N /= 2
}

for (I = 1, 10) {            # 1 to 10 inclusive
    Sum += I
}

for (I = 10, 0, -2) {        # optional step: 10, 8, 6, 4, 2, 0
    if (I == 4) { continue }
    if (I == 2) { break }
}

foreach (K, V = Names) {     # K is the index (1, 2, 3...), V the value
    print(K, V)
}

foreach (K, V:number = Mixed) {   # with a type, values of other types are skipped
    Total += V
}
```

`return` with no value stops the current run early.

## Functions

```e2
function number square(X) {
    return X * X
}

function string greet(Name:string, Times) {
    return ("Hi " + Name + "! "):repeat(Times)
}

function reset() {           # no return type: returns nothing useful
    Count = 0
}

Out = square(4)              # 16
```

- Declare functions at the top level (not inside `if` or loops). They can be declared anywhere in the script and called from anywhere.
- Parameters can have a type (`Name:string`). Untyped parameters accept anything.
- A function sees its parameters and global variables, never the caller's `local`s.
- Recursion is allowed up to 32 calls deep.
- A function can't use the name of a built-in function.

### Method syntax

Any function can be called on a value with `:`. `Text:upper()` is the same as `upper(Text)`, and `List:push(5)` is the same as `push(List, 5)`. There must be no space before the `:`.

## Comments

```e2
# Single line comment
#[
    Block comment
    over several lines
]#
```

## Built-in functions

### Execution and timing

| Function | Returns | Description |
|---|---|---|
| `first()` | bool | True on the first run after placing or uploading |
| `inputClk()` | bool | True if an input change caused this run |
| `interval(Ms)` | | Run the script again in `Ms` milliseconds. `clk()` is true on that run. `interval(0)` cancels it |
| `clk()` | bool | True if `interval()` caused this run |
| `timer(Name, Ms)` | | Run the script in `Ms` milliseconds with `clk(Name)` true. Starting it again restarts it |
| `clk(Name)` | bool | True if the timer `Name` caused this run |
| `stoptimer(Name)` | | Cancel a timer |
| `stopAllTimers()` | | Cancel every timer |
| `curtime()` | number | Server time in seconds |
| `changed(X)` | bool | True if `X` differs from what this same `changed()` call saw last run (true the first time) |
| `print(A, ...)` | | Sends the values to the chip owner's chat, joined by spaces. At most once every 0.5 seconds |

The shortest interval or timer is one wire tick. Shorter values are rounded up. A chip can run up to 16 named timers at once.

### Math

Angles are in **degrees**. The `r` variants use radians.

| Function | Description |
|---|---|
| `abs(X)` | Absolute value |
| `ceil(X)` `floor(X)` | Round up, round down |
| `round(X)` `round(X, Decimals)` | Round to the nearest value (0 to 15 decimals) |
| `int(X)` `frac(X)` | Integer part, fractional part |
| `sqrt(X)` `cbrt(X)` | Square root, cube root |
| `exp(X)` `ln(X)` `log2(X)` `log10(X)` | Exponential and logarithms |
| `sign(X)` | -1, 0 or 1 |
| `min(A, B, ...)` `max(A, B, ...)` | Smallest, largest |
| `clamp(X, Min, Max)` | Keep `X` between `Min` and `Max` |
| `inrange(X, Min, Max)` | 1 if `Min <= X <= Max` |
| `lerp(A, B, T)` | Blend from `A` to `B`. `T` from 0 to 1 |
| `mod(A, B)` | Floored modulo, same as `%` |
| `sin(D)` `cos(D)` `tan(D)` | Trigonometry in degrees |
| `asin(X)` `acos(X)` `atan(X)` `atan(Y, X)` | Inverse trigonometry, returns degrees |
| `sinr(R)` `cosr(R)` `tanr(R)` | Trigonometry in radians |
| `toRad(D)` `toDeg(R)` | Convert between degrees and radians |
| `pi()` `e()` | Constants |
| `random()` `random(Max)` `random(Min, Max)` | Random decimal number |
| `randint(Max)` `randint(Min, Max)` | Random whole number, inclusive. `randint(6)` is 1 to 6 |

### Bitwise

Values are treated as whole numbers.

| Function | Description |
|---|---|
| `bAnd(A, B)` `bOr(A, B)` `bXor(A, B)` | And, or, exclusive or |
| `bNot(A)` | Invert all bits |
| `bShl(A, N)` `bShr(A, N)` | Shift left or right by `N` bits |

### Strings

Positions are **1-based**: the first character is at position 1. Strings can't be changed in place. Functions return a new string.

| Function | Description |
|---|---|
| `length(S)` | Number of characters |
| `S[I]` `index(S, I)` | Character at position `I` |
| `sub(S, Start)` `sub(S, Start, End)` | Part of a string, inclusive. `sub("hello", 2, 4)` is `"ell"` |
| `left(S, N)` `right(S, N)` | First or last `N` characters |
| `find(S, Needle)` `find(S, Needle, Start)` | Position of `Needle`, or 0 if not found |
| `replace(S, Find, With)` | Replace every occurrence |
| `explode(S, Separator)` | Split into an array. An empty separator splits into characters |
| `upper(S)` `lower(S)` `trim(S)` `reverse(S)` | Change case, strip spaces from both ends, reverse |
| `repeat(S, N)` | `S` repeated `N` times |
| `toString(X)` | Number or array as text |
| `toNumber(S)` | Text as a number, or 0 if it isn't one |
| `toChar(Code)` `toByte(S)` `toByte(S, I)` | Character from a code, code of a character (-1 for an empty string) |

### Arrays

Arrays are 1-based and hold numbers and strings, up to 1024 elements. They are **references**: `B = A` makes both names point to the same array. Use `clone()` to copy.

| Function | Description |
|---|---|
| `array(A, B, ...)` | New array with these values |
| `A[I]` | Element `I`. `A[I] = V` replaces it, and writing to `count + 1` appends |
| `count(A)` | Number of elements |
| `push(A, V)` `pop(A)` | Add to the end, remove and return the last element |
| `unshift(A, V)` `shift(A)` | Add to the front, remove and return the first element |
| `insert(A, I, V)` `remove(A, I)` | Insert at a position, remove and return the element at a position |
| `clear(A)` | Remove everything |
| `indexOf(A, V)` | Position of `V`, or 0 if not found |
| `sum(A)` | Sum of the numbers (strings are ignored) |
| `concat(A)` `concat(A, Separator)` | Join everything into one string |
| `clone(A)` | Independent copy |

## Limits

Limits keep chips from slowing the server down. Servers can change the defaults shown here.

| Limit | Default |
|---|---|
| Operations per run | 10,000 |
| Operations per chip per wire tick | 20,000 |
| Operations for all of one player's chips per wire tick | 40,000 |
| Script length | 32,768 characters |
| String length | 4,096 characters |
| Array length | 1,024 elements |
| Named timers | 16 |
| Output changes per wire tick | 64 |
| Remembered data (`@outputs`, `@persist`, `changed()`) | 65,536 characters/elements |
| Expression chips per player | Set by the server (2 on roleplay servers by default) |

Every statement, expression and function call costs operations. Long strings and big arrays cost more. Going over a limit halts the chip with a message that says which limit was hit.

`@persist` and `@outputs` values, plus running timers, survive server restarts and snapshot restores.

## Examples

### Counter with reset

```e2
@name Counter
@inputs Add:bool Reset:bool
@outputs Count

if (~Add & Add) {
    Count++
}
if (~Reset & Reset) {
    Count = 0
}
```

### Blinking light

```e2
@name Blinker
@inputs Enabled:bool
@outputs Light:bool

if (first() | clk()) {
    interval(500)
}
if (clk()) {
    Light = Enabled ? !Light : 0
}
```

### Code lock with auto relock

Wire a **Keypad** or any string output to `Code`, and `Unlock` to a door or fading door.

```e2
@name Code Lock
@inputs Code:string
@outputs Unlock:bool Status:string
@persist Attempts

if (first()) {
    Status = "Locked"
}

if (~Code) {
    if (Code == "1234") {
        Unlock = 1
        Attempts = 0
        Status = "Welcome!"
        timer("relock", 3000)
    } else {
        Attempts++
        Status = "Wrong code (" + Attempts + ")"
    }
}

if (clk("relock")) {
    Unlock = 0
    Status = "Locked"
}
```

### Traffic light state machine

```e2
@name Traffic Light
@outputs Red:bool Yellow:bool Green:bool
@persist State:string

if (first()) {
    State = "red"
    timer("next", 5000)
}

if (clk("next")) {
    if (State == "red") {
        State = "green"
        timer("next", 5000)
    } elseif (State == "green") {
        State = "yellow"
        timer("next", 1500)
    } else {
        State = "red"
        timer("next", 5000)
    }
}

Red = State == "red"
Yellow = State == "yellow"
Green = State == "green"
```

### Rolling average

```e2
@name Average
@inputs Value
@outputs Avg
@persist Samples:array

if (~Value) {
    Samples:push(Value)
    if (Samples:count() > 10) {
        Samples:shift()
    }
    Avg = Samples:sum() / Samples:count()
}
```

### Rate of change

`$X` gives the change since the last run. Divide it by the time between runs to get a rate.

```e2
@name Speedometer
@inputs Distance
@outputs Speed
@persist LastTime

if (~Distance) {
    local Dt = curtime() - LastTime
    if (Dt > 0) {
        Speed = $Distance / Dt
    }
    LastTime = curtime()
}
```

### Chat-style commands

```e2
@name Commands
@inputs Text:string
@outputs Total Reply:string

if (~Text) {
    local Parts = Text:explode(" ")
    local Cmd = Parts[1]:lower()

    if (Cmd == "!add" & Parts:count() >= 2) {
        Total += Parts[2]:toNumber()
        Reply = "Total: " + Total
    } elseif (Cmd == "!reset") {
        Total = 0
        Reply = "Reset"
    } else {
        Reply = "Unknown command: " + Cmd
    }
}
```

### Uptime clock with a helper function

Wire `Display` to a **Screen**.

```e2
@name Uptime
@outputs Display:string

function string pad(N) {
    return N < 10 ? "0" + N : toString(N)
}

if (first() | clk()) {
    interval(1000)
}

local T = floor(curtime())
Display = pad(floor(T / 3600) % 24) + ":" + pad(floor(T / 60) % 60) + ":" + pad(T % 60)
```

### Scoreboard text

Build multi-line text with `"\n"`.

```e2
@name Scoreboard
@inputs RedHit:bool BlueHit:bool Reset:bool
@outputs Board:string
@persist Red Blue

if (~RedHit & RedHit) { Red++ }
if (~BlueHit & BlueHit) { Blue++ }
if (~Reset & Reset) {
    Red = 0
    Blue = 0
}

local Leader = Red > Blue ? "Red leads" : (Blue > Red ? "Blue leads" : "Tied")
Board = "RED   " + Red + "\nBLUE  " + Blue + "\n" + Leader
```

## Common errors

| Error | Fix |
|---|---|
| `Variable 'X' is not defined` | Assign `X` before reading it, or declare it with `@persist` so it keeps its value |
| `Cannot assign string to 'X' (number)` | Declare `X` with the right type, e.g. `@persist X:string` |
| `Cannot assign to input 'X'` | Inputs are read-only. Copy the value into a `@persist` variable |
| `Index 0 out of range` | Arrays and strings start at 1 |
| `Exceeded ... ops in one execution` | A loop runs too long. Add a `break`, or spread the work across `interval()` runs |
| `More than 64 output changes in one wire tick` | Outputs are changing too often. Change them less often or use fewer outputs |
| `Directives must be at the top of the script` | Move every `@` line above the code |
