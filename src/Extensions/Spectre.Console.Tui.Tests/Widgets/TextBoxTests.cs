using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class TextBoxTests
{
    [Fact]
    public void Constructor_Defaults()
    {
        var tb = new TextBox();
        tb.Text.Should().BeEmpty();
        tb.CursorPosition.Should().Be(0);
        tb.CanFocus.Should().BeTrue();
    }

    [Fact]
    public void Typing_Should_Insert_Characters()
    {
        var tb = new TextBox();
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.B, 'b'));
        tb.Text.Should().Be("ab");
        tb.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void Backspace_Should_Delete_Previous()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 3;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Backspace, '\b'));
        tb.Text.Should().Be("ab");
        tb.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void Delete_Should_Remove_Current()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 1;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Delete, '\0'));
        tb.Text.Should().Be("ac");
        tb.CursorPosition.Should().Be(1);
    }

    [Fact]
    public void LeftArrow_Should_Move_Cursor()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 2;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        tb.CursorPosition.Should().Be(1);
    }

    [Fact]
    public void RightArrow_Should_Move_Cursor()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 1;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        tb.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void Home_Should_Go_To_Start()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 3;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0'));
        tb.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void End_Should_Go_To_End()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 0;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0'));
        tb.CursorPosition.Should().Be(3);
    }

    [Fact]
    public void Enter_Should_Fire_Submitted()
    {
        var tb = new TextBox { Text = "hello" };
        string? submitted = null;
        tb.Submitted += (_, v) => submitted = v;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        submitted.Should().Be("hello");
    }

    [Fact]
    public void MaxLength_Should_Prevent_Excess_Input()
    {
        var tb = new TextBox { MaxLength = 3 };
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.B, 'b'));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.C, 'c'));
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.D, 'd'));
        tb.Text.Should().Be("abc");
    }

    [Fact]
    public void TextChanged_Should_Fire_On_Input()
    {
        var tb = new TextBox();
        string? lastText = null;
        tb.TextChanged += (_, v) => lastText = v;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a'));
        lastText.Should().Be("a");
    }

    [Fact]
    public void Backspace_At_Start_Should_Not_Change()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 0;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Backspace, '\b'));
        tb.Text.Should().Be("abc");
    }

    [Fact]
    public void LeftArrow_At_Start_Should_Not_Move()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 0;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.LeftArrow, '\0'));
        tb.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void RightArrow_At_End_Should_Not_Move()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 3;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.RightArrow, '\0'));
        tb.CursorPosition.Should().Be(3);
    }

    [Fact]
    public void Delete_At_End_Should_Not_Change()
    {
        var tb = new TextBox { Text = "abc" };
        tb.CursorPosition = 3;
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Delete, '\0'));
        tb.Text.Should().Be("abc");
    }

    [Fact]
    public void Control_Characters_Should_Not_Be_Inserted()
    {
        var tb = new TextBox();
        tb.OnKeyEvent(new KeyEvent(ConsoleKey.Escape, '\x1b')).Should().BeFalse();
    }
}
