using FluentAssertions;
using Spectre.Console.Tui.Integration;
using Xunit;

namespace Spectre.Console.Tui.Tests.Integration;

public class TuiThemeTests
{
    [Fact]
    public void Default_Theme_Should_Have_Values()
    {
        var theme = TuiTheme.Default;
        theme.WindowTitle.Should().NotBe(Style.Plain);
        theme.ButtonNormal.Should().NotBe(Style.Plain);
    }

    [Fact]
    public void Dark_Theme_Should_Differ_From_Default()
    {
        TuiTheme.Dark.WindowTitle.Should().NotBe(TuiTheme.Default.WindowTitle);
    }

    [Fact]
    public void Blue_Theme_Should_Use_Blue()
    {
        TuiTheme.Blue.WindowBorder.Foreground.Should().Be(Color.Blue);
    }

    [Fact]
    public void Custom_Theme_Should_Override_Properties()
    {
        var theme = new TuiTheme
        {
            WindowTitle = new Style(Color.Red),
        };
        theme.WindowTitle.Foreground.Should().Be(Color.Red);
    }
}
