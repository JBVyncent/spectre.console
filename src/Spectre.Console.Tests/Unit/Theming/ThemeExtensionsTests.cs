namespace Spectre.Console.Tests.Unit.Theming;

public sealed class ThemeExtensionsTests
{
    public sealed class TheUseThemeMethod
    {
        [Fact]
        public void Sets_Theme_On_Table()
        {
            // Arrange
            var table = new Table();
            var theme = Theme.Nord;

            // Act
            var result = table.UseTheme(theme);

            // Assert
            result.Theme.Should().BeSameAs(theme);
            result.Should().BeSameAs(table);
        }

        [Fact]
        public void Sets_Theme_On_Panel()
        {
            // Arrange
            var panel = new Panel("content");
            var theme = Theme.Dracula;

            // Act
            var result = panel.UseTheme(theme);

            // Assert
            result.Theme.Should().BeSameAs(theme);
            result.Should().BeSameAs(panel);
        }

        [Fact]
        public void Sets_Theme_On_Tree()
        {
            // Arrange
            var tree = new Tree("root");
            var theme = Theme.Monokai;

            // Act
            var result = tree.UseTheme(theme);

            // Assert
            result.Theme.Should().BeSameAs(theme);
            result.Should().BeSameAs(tree);
        }

        [Fact]
        public void Sets_Theme_On_Rule()
        {
            // Arrange
            var rule = new Rule("title");
            var theme = Theme.SolarizedDark;

            // Act
            var result = rule.UseTheme(theme);

            // Assert
            result.Theme.Should().BeSameAs(theme);
            result.Should().BeSameAs(rule);
        }

        [Fact]
        public void Sets_Theme_On_FigletText()
        {
            // Arrange
            var figlet = new FigletText("hello");
            var theme = Theme.Nord;

            // Act
            var result = figlet.UseTheme(theme);

            // Assert
            result.Theme.Should().BeSameAs(theme);
            result.Should().BeSameAs(figlet);
        }

        [Fact]
        public void Throws_On_Null_Widget()
        {
            // Arrange
            Table? table = null;

            // Act
            var act = () => table!.UseTheme(Theme.Nord);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("widget");
        }

        [Fact]
        public void Throws_On_Null_Theme()
        {
            // Arrange
            var table = new Table();

            // Act
            var act = () => table.UseTheme(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("theme");
        }

        [Fact]
        public void Returns_Same_Instance_For_Chaining()
        {
            // Arrange
            var table = new Table();
            var theme = Theme.Dracula;

            // Act
            var result = table.UseTheme(theme);

            // Assert
            result.Should().BeSameAs(table);
        }
    }
}
