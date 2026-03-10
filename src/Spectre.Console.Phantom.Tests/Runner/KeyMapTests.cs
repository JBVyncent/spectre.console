using Spectre.Console.Phantom.Runner;

namespace Spectre.Console.Phantom.Tests.Runner;

public sealed class KeyMapTests
{
    [Fact]
    public void Enter_Maps_To_CarriageReturn()
    {
        KeyMap.ToVt100(ConsoleKey.Enter).Should().Be("\r");
    }

    [Fact]
    public void Escape_Maps_To_Escape_Byte()
    {
        KeyMap.ToVt100(ConsoleKey.Escape).Should().Be("\x1b");
    }

    [Fact]
    public void Backspace_Maps_To_Del()
    {
        KeyMap.ToVt100(ConsoleKey.Backspace).Should().Be("\x7f");
    }

    [Fact]
    public void Tab_Maps_To_Tab()
    {
        KeyMap.ToVt100(ConsoleKey.Tab).Should().Be("\t");
    }

    [Fact]
    public void ShiftTab_Maps_To_Backtab()
    {
        KeyMap.ToVt100(ConsoleKey.Tab, shift: true).Should().Be("\x1b[Z");
    }

    [Fact]
    public void Spacebar_Maps_To_Space()
    {
        KeyMap.ToVt100(ConsoleKey.Spacebar).Should().Be(" ");
    }

    [Theory]
    [InlineData(ConsoleKey.UpArrow, "\x1b[A")]
    [InlineData(ConsoleKey.DownArrow, "\x1b[B")]
    [InlineData(ConsoleKey.RightArrow, "\x1b[C")]
    [InlineData(ConsoleKey.LeftArrow, "\x1b[D")]
    public void Arrow_Keys_Map_To_Csi_Sequences(ConsoleKey key, string expected)
    {
        KeyMap.ToVt100(key).Should().Be(expected);
    }

    [Theory]
    [InlineData(ConsoleKey.Home, "\x1b[H")]
    [InlineData(ConsoleKey.End, "\x1b[F")]
    [InlineData(ConsoleKey.Insert, "\x1b[2~")]
    [InlineData(ConsoleKey.Delete, "\x1b[3~")]
    [InlineData(ConsoleKey.PageUp, "\x1b[5~")]
    [InlineData(ConsoleKey.PageDown, "\x1b[6~")]
    public void Navigation_Keys_Map_Correctly(ConsoleKey key, string expected)
    {
        KeyMap.ToVt100(key).Should().Be(expected);
    }

    [Theory]
    [InlineData(ConsoleKey.F1, "\x1bOP")]
    [InlineData(ConsoleKey.F2, "\x1bOQ")]
    [InlineData(ConsoleKey.F3, "\x1bOR")]
    [InlineData(ConsoleKey.F4, "\x1bOS")]
    [InlineData(ConsoleKey.F5, "\x1b[15~")]
    [InlineData(ConsoleKey.F12, "\x1b[24~")]
    public void Function_Keys_Map_Correctly(ConsoleKey key, string expected)
    {
        KeyMap.ToVt100(key).Should().Be(expected);
    }

    [Fact]
    public void Lowercase_Letters_Default()
    {
        KeyMap.ToVt100(ConsoleKey.A).Should().Be("a");
        KeyMap.ToVt100(ConsoleKey.Z).Should().Be("z");
        KeyMap.ToVt100(ConsoleKey.M).Should().Be("m");
    }

    [Fact]
    public void Shift_Letters_Are_Uppercase()
    {
        KeyMap.ToVt100(ConsoleKey.A, shift: true).Should().Be("A");
        KeyMap.ToVt100(ConsoleKey.Z, shift: true).Should().Be("Z");
    }

    [Fact]
    public void Ctrl_Letters_Are_Control_Codes()
    {
        KeyMap.ToVt100(ConsoleKey.A, ctrl: true).Should().Be("\x01");
        KeyMap.ToVt100(ConsoleKey.C, ctrl: true).Should().Be("\x03"); // Ctrl+C = ETX
        KeyMap.ToVt100(ConsoleKey.Z, ctrl: true).Should().Be("\x1a");
    }

    [Fact]
    public void Digit_Keys_Map_To_Characters()
    {
        KeyMap.ToVt100(ConsoleKey.D0).Should().Be("0");
        KeyMap.ToVt100(ConsoleKey.D5).Should().Be("5");
        KeyMap.ToVt100(ConsoleKey.D9).Should().Be("9");
    }

    [Fact]
    public void Char_Overload_Returns_String()
    {
        KeyMap.ToVt100('y').Should().Be("y");
        KeyMap.ToVt100('N').Should().Be("N");
    }

    [Fact]
    public void Punctuation_Keys_Map_Correctly()
    {
        KeyMap.ToVt100(ConsoleKey.OemPeriod).Should().Be(".");
        KeyMap.ToVt100(ConsoleKey.OemComma).Should().Be(",");
        KeyMap.ToVt100(ConsoleKey.OemMinus).Should().Be("-");
    }
}
