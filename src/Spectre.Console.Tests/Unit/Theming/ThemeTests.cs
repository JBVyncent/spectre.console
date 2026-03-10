namespace Spectre.Console.Tests.Unit.Theming;

public sealed class ThemeTests
{
    public sealed class TheResolveMethod
    {
        [Fact]
        public void Returns_Widget_Style_When_Set()
        {
            // Arrange
            var widgetStyle = new Style(Color.Red);
            var themeStyle = new Style(Color.Blue);
            var fallback = Style.Plain;

            // Act
            var result = Theme.Resolve(widgetStyle, themeStyle, fallback);

            // Assert
            result.Should().Be(widgetStyle);
        }

        [Fact]
        public void Returns_Theme_Style_When_Widget_Style_Is_Null()
        {
            // Arrange
            var themeStyle = new Style(Color.Blue);
            var fallback = Style.Plain;

            // Act
            var result = Theme.Resolve(null, themeStyle, fallback);

            // Assert
            result.Should().Be(themeStyle);
        }

        [Fact]
        public void Returns_Fallback_When_Both_Are_Null()
        {
            // Arrange
            var fallback = new Style(Color.Green);

            // Act
            var result = Theme.Resolve(null, null, fallback);

            // Assert
            result.Should().Be(fallback);
        }

        [Fact]
        public void Widget_Style_Takes_Precedence_Over_Theme_And_Fallback()
        {
            // Arrange
            var widgetStyle = new Style(Color.Red);
            var themeStyle = new Style(Color.Blue);
            var fallback = new Style(Color.Green);

            // Act
            var result = Theme.Resolve(widgetStyle, themeStyle, fallback);

            // Assert
            result.Foreground.Should().Be(Color.Red);
        }

        [Fact]
        public void Theme_Style_Takes_Precedence_Over_Fallback()
        {
            // Arrange
            var themeStyle = new Style(Color.Blue);
            var fallback = new Style(Color.Green);

            // Act
            var result = Theme.Resolve(null, themeStyle, fallback);

            // Assert
            result.Foreground.Should().Be(Color.Blue);
        }
    }

    public sealed class TheBuiltInThemes
    {
        [Fact]
        public void Default_Theme_Has_No_Overrides()
        {
            // Act
            var theme = Theme.Default;

            // Assert
            theme.Name.Should().Be("Default");
            theme.BorderStyle.Should().BeNull();
            theme.TreeStyle.Should().BeNull();
            theme.RuleStyle.Should().BeNull();
            theme.AccentStyle.Should().BeNull();
            theme.DimStyle.Should().BeNull();
            theme.HeaderStyle.Should().BeNull();
            theme.HighlightStyle.Should().BeNull();
            theme.ProgressCompletedStyle.Should().BeNull();
            theme.ProgressFinishedStyle.Should().BeNull();
            theme.ProgressRemainingStyle.Should().BeNull();
            theme.SpinnerStyle.Should().BeNull();
            theme.LinkStyle.Should().BeNull();
        }

        [Fact]
        public void Nord_Theme_Has_All_Styles_Set()
        {
            // Act
            var theme = Theme.Nord;

            // Assert
            theme.Name.Should().Be("Nord");
            theme.BorderStyle.Should().NotBeNull();
            theme.TreeStyle.Should().NotBeNull();
            theme.RuleStyle.Should().NotBeNull();
            theme.AccentStyle.Should().NotBeNull();
            theme.DimStyle.Should().NotBeNull();
            theme.HeaderStyle.Should().NotBeNull();
            theme.HighlightStyle.Should().NotBeNull();
            theme.ProgressCompletedStyle.Should().NotBeNull();
            theme.ProgressFinishedStyle.Should().NotBeNull();
            theme.ProgressRemainingStyle.Should().NotBeNull();
            theme.SpinnerStyle.Should().NotBeNull();
            theme.LinkStyle.Should().NotBeNull();
        }

        [Fact]
        public void Dracula_Theme_Has_All_Styles_Set()
        {
            var theme = Theme.Dracula;
            theme.Name.Should().Be("Dracula");
            theme.BorderStyle.Should().NotBeNull();
            theme.TreeStyle.Should().NotBeNull();
            theme.RuleStyle.Should().NotBeNull();
            theme.AccentStyle.Should().NotBeNull();
            theme.DimStyle.Should().NotBeNull();
            theme.HeaderStyle.Should().NotBeNull();
            theme.HighlightStyle.Should().NotBeNull();
            theme.ProgressCompletedStyle.Should().NotBeNull();
            theme.ProgressFinishedStyle.Should().NotBeNull();
            theme.ProgressRemainingStyle.Should().NotBeNull();
            theme.SpinnerStyle.Should().NotBeNull();
            theme.LinkStyle.Should().NotBeNull();
        }

        [Fact]
        public void SolarizedDark_Theme_Has_All_Styles_Set()
        {
            var theme = Theme.SolarizedDark;
            theme.Name.Should().Be("Solarized Dark");
            theme.BorderStyle.Should().NotBeNull();
            theme.TreeStyle.Should().NotBeNull();
            theme.RuleStyle.Should().NotBeNull();
            theme.AccentStyle.Should().NotBeNull();
        }

        [Fact]
        public void Monokai_Theme_Has_All_Styles_Set()
        {
            var theme = Theme.Monokai;
            theme.Name.Should().Be("Monokai");
            theme.BorderStyle.Should().NotBeNull();
            theme.TreeStyle.Should().NotBeNull();
            theme.RuleStyle.Should().NotBeNull();
            theme.AccentStyle.Should().NotBeNull();
        }

        [Fact]
        public void Built_In_Themes_Are_Singleton_Instances()
        {
            // Static properties should return the same instance
            Theme.Default.Should().BeSameAs(Theme.Default);
            Theme.Nord.Should().BeSameAs(Theme.Nord);
            Theme.Dracula.Should().BeSameAs(Theme.Dracula);
            Theme.SolarizedDark.Should().BeSameAs(Theme.SolarizedDark);
            Theme.Monokai.Should().BeSameAs(Theme.Monokai);
        }

        [Fact]
        public void Nord_Border_Style_Uses_Frost_Blue()
        {
            // Nord frost2 = #81A1C1 = (129, 161, 193)
            var theme = Theme.Nord;
            theme.BorderStyle!.Value.Foreground.R.Should().Be(129);
            theme.BorderStyle!.Value.Foreground.G.Should().Be(161);
            theme.BorderStyle!.Value.Foreground.B.Should().Be(193);
        }

        [Fact]
        public void Dracula_Border_Style_Uses_Purple()
        {
            // Dracula purple = #BD93F9 = (189, 147, 249)
            var theme = Theme.Dracula;
            theme.BorderStyle!.Value.Foreground.R.Should().Be(189);
            theme.BorderStyle!.Value.Foreground.G.Should().Be(147);
            theme.BorderStyle!.Value.Foreground.B.Should().Be(249);
        }

        [Fact]
        public void SolarizedDark_Border_Style_Uses_Blue()
        {
            // Solarized blue = #268BD2 = (38, 139, 210)
            var theme = Theme.SolarizedDark;
            theme.BorderStyle!.Value.Foreground.R.Should().Be(38);
            theme.BorderStyle!.Value.Foreground.G.Should().Be(139);
            theme.BorderStyle!.Value.Foreground.B.Should().Be(210);
        }

        [Fact]
        public void Monokai_Border_Style_Uses_Blue()
        {
            // Monokai blue = #66D9EF = (102, 217, 239)
            var theme = Theme.Monokai;
            theme.BorderStyle!.Value.Foreground.R.Should().Be(102);
            theme.BorderStyle!.Value.Foreground.G.Should().Be(217);
            theme.BorderStyle!.Value.Foreground.B.Should().Be(239);
        }

        [Fact]
        public void Nord_Header_Style_Has_Bold_Decoration()
        {
            var theme = Theme.Nord;
            theme.HeaderStyle!.Value.Decoration.Should().HaveFlag(Decoration.Bold);
        }

        [Fact]
        public void Nord_Link_Style_Has_Underline_Decoration()
        {
            var theme = Theme.Nord;
            theme.LinkStyle!.Value.Decoration.Should().HaveFlag(Decoration.Underline);
        }
    }

    public sealed class TheCustomTheme
    {
        [Fact]
        public void Can_Create_Custom_Theme_With_Init_Properties()
        {
            // Arrange & Act
            var theme = new Theme
            {
                Name = "MyTheme",
                BorderStyle = new Style(Color.Magenta1),
                AccentStyle = new Style(Color.Cyan1),
            };

            // Assert
            theme.Name.Should().Be("MyTheme");
            theme.BorderStyle!.Value.Foreground.Should().Be(Color.Magenta1);
            theme.AccentStyle!.Value.Foreground.Should().Be(Color.Cyan1);
            theme.TreeStyle.Should().BeNull();
        }

        [Fact]
        public void Default_Name_Is_Custom()
        {
            var theme = new Theme();
            theme.Name.Should().Be("Custom");
        }
    }
}
