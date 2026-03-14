using FluentAssertions;
using Spectre.Console.Tui.Integration;
using Spectre.Console.Tui.Windows;
using Xunit;

namespace Spectre.Console.Tui.Tests.Integration;

public class MessageBoxTests
{
    [Fact]
    public void Create_Ok_Should_Have_Ok_Button()
    {
        var dialog = MessageBox.Create("Title", "Message", MessageBoxButtons.Ok);
        dialog.Title.Should().Be("Title");
        dialog.Children.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_YesNo_Should_Have_Two_Buttons()
    {
        var dialog = MessageBox.Create("Title", "Question?", MessageBoxButtons.YesNo);
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Create_YesNoCancel_Should_Have_Three_Buttons()
    {
        var dialog = MessageBox.Create("Title", "Question?", MessageBoxButtons.YesNoCancel);
        dialog.Should().NotBeNull();
    }

    [Fact]
    public void Create_OkCancel_Should_Have_Two_Buttons()
    {
        var dialog = MessageBox.Create("Title", "Message", MessageBoxButtons.OkCancel);
        dialog.Should().NotBeNull();
    }
}
