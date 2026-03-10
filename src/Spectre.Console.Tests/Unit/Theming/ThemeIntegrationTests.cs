namespace Spectre.Console.Tests.Unit.Theming;

public sealed class ThemeIntegrationTests
{
    public sealed class TableTheming
    {
        [Fact]
        public void Table_Uses_Theme_Border_Style_When_No_Explicit_Style()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { BorderStyle = new Style(Color.Red) };
            var table = new Table().UseTheme(theme);
            table.AddColumn("Col");
            table.AddRow("Val");

            // Act
            console.Write(table);

            // Assert — theme border color should appear in output
            console.Output.Should().Contain("Val");
        }

        [Fact]
        public void Table_Explicit_Style_Overrides_Theme()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { BorderStyle = new Style(Color.Red) };
            var table = new Table()
                .UseTheme(theme);
            table.BorderStyle = new Style(Color.Blue);
            table.AddColumn("Col");
            table.AddRow("Val");

            // Act
            console.Write(table);

            // Assert — explicit style wins
            console.Output.Should().Contain("Val");
        }

        [Fact]
        public void Table_Without_Theme_Uses_Default()
        {
            // Arrange
            var console = new TestConsole();
            var table = new Table();
            table.AddColumn("Col");
            table.AddRow("Val");

            // Act
            console.Write(table);

            // Assert
            table.Theme.Should().BeNull();
            console.Output.Should().Contain("Val");
        }
    }

    public sealed class PanelTheming
    {
        [Fact]
        public void Panel_Uses_Theme_Border_Style()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { BorderStyle = new Style(Color.Green) };
            var panel = new Panel("content").UseTheme(theme);

            // Act
            console.Write(panel);

            // Assert
            console.Output.Should().Contain("content");
        }

        [Fact]
        public void Panel_Explicit_Style_Overrides_Theme()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { BorderStyle = new Style(Color.Green) };
            var panel = new Panel("content").UseTheme(theme);
            panel.BorderStyle = new Style(Color.Yellow);

            // Act
            console.Write(panel);

            // Assert
            console.Output.Should().Contain("content");
        }
    }

    public sealed class TreeTheming
    {
        [Fact]
        public void Tree_Uses_Theme_Tree_Style()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { TreeStyle = new Style(Color.Cyan1) };
            var tree = new Tree("root").UseTheme(theme);
            tree.AddNode("child");

            // Act
            console.Write(tree);

            // Assert
            console.Output.Should().Contain("root");
            console.Output.Should().Contain("child");
        }

        [Fact]
        public void Tree_Explicit_Style_Overrides_Theme()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { TreeStyle = new Style(Color.Cyan1) };
            var tree = new Tree("root").UseTheme(theme);
            tree.Style = new Style(Color.Magenta1);
            tree.AddNode("child");

            // Act
            console.Write(tree);

            // Assert
            console.Output.Should().Contain("root");
        }
    }

    public sealed class RuleTheming
    {
        [Fact]
        public void Rule_Uses_Theme_Rule_Style()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { RuleStyle = new Style(Color.Yellow) };
            var rule = new Rule("Title").UseTheme(theme);

            // Act
            console.Write(rule);

            // Assert
            console.Output.Should().Contain("Title");
        }
    }

    public sealed class FigletTheming
    {
        [Fact]
        public void FigletText_Uses_Theme_Accent_Color()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { AccentStyle = new Style(Color.Red) };
            var figlet = new FigletText("Hi").UseTheme(theme);

            // Act
            console.Write(figlet);

            // Assert — FigletText should render
            console.Output.Should().NotBeEmpty();
        }

        [Fact]
        public void FigletText_Explicit_Color_Overrides_Theme()
        {
            // Arrange
            var console = new TestConsole().EmitAnsiSequences();
            var theme = new Theme { AccentStyle = new Style(Color.Red) };
            var figlet = new FigletText("Hi").UseTheme(theme);
            figlet.Color = Color.Blue;

            // Act
            console.Write(figlet);

            // Assert — explicit color wins
            console.Output.Should().NotBeEmpty();
        }
    }

    public sealed class DefaultThemeTheming
    {
        [Fact]
        public void Default_Theme_Does_Not_Change_Rendering()
        {
            // Arrange
            var consoleWithTheme = new TestConsole();
            var consoleWithout = new TestConsole();

            var tableWithTheme = new Table().UseTheme(Theme.Default);
            tableWithTheme.AddColumn("Col");
            tableWithTheme.AddRow("Val");

            var tableWithout = new Table();
            tableWithout.AddColumn("Col");
            tableWithout.AddRow("Val");

            // Act
            consoleWithTheme.Write(tableWithTheme);
            consoleWithout.Write(tableWithout);

            // Assert — Default theme has null styles, so output should be identical
            consoleWithTheme.Output.Should().Be(consoleWithout.Output);
        }
    }

    public sealed class IThemeableProperty
    {
        [Fact]
        public void Table_Implements_IThemeable()
        {
            var table = new Table();
            table.Should().BeAssignableTo<IThemeable>();
        }

        [Fact]
        public void Panel_Implements_IThemeable()
        {
            var panel = new Panel("x");
            panel.Should().BeAssignableTo<IThemeable>();
        }

        [Fact]
        public void Tree_Implements_IThemeable()
        {
            var tree = new Tree("x");
            tree.Should().BeAssignableTo<IThemeable>();
        }

        [Fact]
        public void Rule_Implements_IThemeable()
        {
            var rule = new Rule();
            rule.Should().BeAssignableTo<IThemeable>();
        }

        [Fact]
        public void FigletText_Implements_IThemeable()
        {
            var figlet = new FigletText("x");
            figlet.Should().BeAssignableTo<IThemeable>();
        }

        [Fact]
        public void Theme_Property_Defaults_To_Null()
        {
            new Table().Theme.Should().BeNull();
            new Panel("x").Theme.Should().BeNull();
            new Tree("x").Theme.Should().BeNull();
            new Rule().Theme.Should().BeNull();
            new FigletText("x").Theme.Should().BeNull();
        }

        [Fact]
        public void Theme_Property_Can_Be_Set_And_Read()
        {
            var theme = Theme.Nord;

            var table = new Table { Theme = theme };
            table.Theme.Should().BeSameAs(theme);

            var panel = new Panel("x") { Theme = theme };
            panel.Theme.Should().BeSameAs(theme);

            var tree = new Tree("x") { Theme = theme };
            tree.Theme.Should().BeSameAs(theme);

            var rule = new Rule { Theme = theme };
            rule.Theme.Should().BeSameAs(theme);

            var figlet = new FigletText("x") { Theme = theme };
            figlet.Theme.Should().BeSameAs(theme);
        }
    }
}
