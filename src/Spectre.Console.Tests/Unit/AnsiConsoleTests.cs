namespace Spectre.Console.Tests.Unit;

public partial class AnsiConsoleTests
{
    public sealed class Clear
    {
        [Theory]
        [InlineData(false, "Hello[2J[3JWorld")]
        [InlineData(true, "Hello[2J[3J[1;1HWorld")]
        public void Should_Clear_Screen(bool home, string expected)
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            console.Write("Hello");
            console.Clear(home);
            console.Write("World");

            // Then
            console.Output.Should().Be(expected);
        }

        [Fact]
        public void Should_Clear_Entire_Line()
        {
            // ESC[2K — erase entire current line; cursor does not move
            var console = new TestConsole().EmitAnsiSequences();
            console.Write("Hello");
            console.ClearLine();
            console.Write("World");
            console.Output.Should().Be("Hello\u001b[2KWorld");
        }

        [Fact]
        public void Should_Clear_Line_To_End()
        {
            // ESC[0K — erase from cursor to end of line
            var console = new TestConsole().EmitAnsiSequences();
            console.Write("Hello");
            console.ClearLineToEnd();
            console.Write("World");
            console.Output.Should().Be("Hello\u001b[0KWorld");
        }

        [Fact]
        public void Should_Clear_Line_To_Start()
        {
            // ESC[1K — erase from start of line to cursor
            var console = new TestConsole().EmitAnsiSequences();
            console.Write("Hello");
            console.ClearLineToStart();
            console.Write("World");
            console.Output.Should().Be("Hello\u001b[1KWorld");
        }

        [Fact]
        public void Should_Clear_To_Bottom()
        {
            // ESC[0J — erase from cursor to bottom of screen
            var console = new TestConsole().EmitAnsiSequences();
            console.Write("Hello");
            console.ClearToBottom();
            console.Write("World");
            console.Output.Should().Be("Hello\u001b[0JWorld");
        }

        [Fact]
        public void Should_Clear_To_Top()
        {
            // ESC[1J — erase from top of screen to cursor
            var console = new TestConsole().EmitAnsiSequences();
            console.Write("Hello");
            console.ClearToTop();
            console.Write("World");
            console.Output.Should().Be("Hello\u001b[1JWorld");
        }

        [Fact]
        public void ClearLine_Should_Throw_For_Null_Console()
        {
            var ex = Record.Exception(() => ((IAnsiConsole)null!).ClearLine());
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("console");
        }

        [Fact]
        public void ClearLineToEnd_Should_Throw_For_Null_Console()
        {
            var ex = Record.Exception(() => ((IAnsiConsole)null!).ClearLineToEnd());
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("console");
        }

        [Fact]
        public void ClearLineToStart_Should_Throw_For_Null_Console()
        {
            var ex = Record.Exception(() => ((IAnsiConsole)null!).ClearLineToStart());
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("console");
        }

        [Fact]
        public void ClearToBottom_Should_Throw_For_Null_Console()
        {
            var ex = Record.Exception(() => ((IAnsiConsole)null!).ClearToBottom());
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("console");
        }

        [Fact]
        public void ClearToTop_Should_Throw_For_Null_Console()
        {
            var ex = Record.Exception(() => ((IAnsiConsole)null!).ClearToTop());
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("console");
        }
    }

    public sealed class Write
    {
        [Fact]
        public void Should_Combine_Decoration_And_Colors()
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            console.Write(
                "Hello",
                new Style()
                    .Foreground(Color.RoyalBlue1)
                    .Background(Color.NavajoWhite1)
                    .Decoration(Decoration.Italic));

            // Then
            console.Output.Should().Be("\u001b[3;90;47mHello\u001b[0m");
        }

        [Fact]
        public void Should_Not_Include_Foreground_If_Set_To_Default_Color()
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            console.Write(
                "Hello",
                new Style()
                    .Foreground(Color.Default)
                    .Background(Color.NavajoWhite1)
                    .Decoration(Decoration.Italic));

            // Then
            console.Output.Should().Be("\u001b[3;47mHello\u001b[0m");
        }

        [Fact]
        public void Should_Not_Include_Background_If_Set_To_Default_Color()
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            console.Write(
                "Hello",
                new Style()
                    .Foreground(Color.RoyalBlue1)
                    .Background(Color.Default)
                    .Decoration(Decoration.Italic));

            // Then
            console.Output.Should().Be("\u001b[3;90mHello\u001b[0m");
        }

        [Fact]
        public void Should_Not_Include_Decoration_If_Set_To_None()
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            console.Write(
                "Hello",
                new Style()
                    .Foreground(Color.RoyalBlue1)
                    .Background(Color.NavajoWhite1)
                    .Decoration(Decoration.None));

            // Then
            console.Output.Should().Be("\u001b[90;47mHello\u001b[0m");
        }
    }

    public sealed class WriteLine
    {
        [Fact]
        public void Should_Reset_Colors_Correctly_After_Line_Break()
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            console.WriteLine("Hello", new Style().Background(ConsoleColor.Red));
            console.WriteLine("World", new Style().Background(ConsoleColor.Green));

            // Then
            console.Output.NormalizeLineEndings()
                .Should().Be("[101mHello[0m\n[102mWorld[0m\n");
        }

        [Fact]
        public void Should_Reset_Colors_Correctly_After_Line_Break_In_Text()
        {
            // Given
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            // When
            console.WriteLine("Hello\nWorld", new Style().Background(ConsoleColor.Red));

            // Then
            console.Output.NormalizeLineEndings()
                .Should().Be("[101mHello[0m\n[101mWorld[0m\n");
        }
    }

    public sealed class MarkupFormatOverload
    {
        [Fact]
        public void Should_Not_Throw_When_Markup_Format_String_Contains_Curly_Braces_And_No_Args()
        {
            // Given
            var console = new TestConsole();

            // Explicitly invoke the format+args overload with an empty array.
            // Before the fix, string.Format("{Pt.1}", []) would throw FormatException
            // because "{Pt.1}" looks like an invalid format placeholder.
            // Regression test for #1495.
            FluentActions.Invoking(() => console.Markup("{Pt.1} (TEST ~ 855D)", Array.Empty<object>())).Should().NotThrow();
            console.Output.Should().Contain("{Pt.1}");
        }

        [Fact]
        public void Should_Not_Throw_When_MarkupLine_Format_String_Contains_Curly_Braces_And_No_Args()
        {
            // Given
            var console = new TestConsole();

            // When / Then
            FluentActions.Invoking(() => console.MarkupLine("{Pt.1} (TEST ~ 855D)", Array.Empty<object>())).Should().NotThrow();
            console.Output.Should().Contain("{Pt.1}");
        }

        [Fact]
        public void Should_Not_Throw_When_Arg_Contains_Square_Brackets()
        {
            // Regression test for #1763 — interpolation crashes when an argument
            // contains square brackets that look like markup tags.
            var console = new TestConsole();
            var fileName = "[important].txt";

            FluentActions.Invoking(() => console.Markup("[blue]File:[/] {0}", fileName)).Should().NotThrow();
            console.Output.Should().Contain("[important].txt");
        }

        [Fact]
        public void Should_Render_Brackets_In_Args_As_Literal_Text()
        {
            // Brackets inside substituted args must be displayed literally,
            // not interpreted as markup tags. Regression test for #1763 and #1387.
            var console = new TestConsole();

            console.Markup("Value: {0}", "[not-a-tag]");

            console.Output.Should().Contain("[not-a-tag]");
        }

        [Fact]
        public void Should_Still_Apply_Markup_From_Format_String_When_Arg_Has_Brackets()
        {
            // The markup tags in the format string itself must still be applied;
            // only the substituted values should be escaped.
            var console = new TestConsole()
                .Colors(ColorSystem.Standard)
                .EmitAnsiSequences();

            console.Markup("[yellow]{0}[/]", "[raw]");

            // Output should contain the yellow ANSI code and the literal [raw] text.
            console.Output.Should().Contain("[93m");
            console.Output.Should().Contain("[raw]");
        }

        [Fact]
        public void Should_Not_Throw_When_MarkupLine_Arg_Contains_Square_Brackets()
        {
            // Regression test for #1763 — same escaping applies to MarkupLine overload.
            var console = new TestConsole();
            var tag = "[bold-value]";

            FluentActions.Invoking(() => console.MarkupLine("[red]Tag:[/] {0}", tag)).Should().NotThrow();
            console.Output.Should().Contain("[bold-value]");
        }

        [Fact]
        public void Should_Escape_Args_With_Provider_Overload()
        {
            // The IFormatProvider overload must also escape bracket-containing args.
            var console = new TestConsole();

            FluentActions.Invoking(() =>
                console.Markup(System.Globalization.CultureInfo.InvariantCulture, "[blue]{0}[/]", "[bracketed]")).Should().NotThrow();
            console.Output.Should().Contain("[bracketed]");
        }

        [Fact]
        public void Should_Escape_Args_In_MarkupLine_Provider_Overload()
        {
            // The IFormatProvider overload of MarkupLine must also escape bracket-containing args.
            var console = new TestConsole();

            FluentActions.Invoking(() =>
                console.MarkupLine(System.Globalization.CultureInfo.InvariantCulture, "[blue]{0}[/]", "[bracketed]")).Should().NotThrow();
            console.Output.Should().Contain("[bracketed]");
        }

        [Fact]
        public void Should_Escape_Object_Whose_ToString_Contains_Brackets()
        {
            // Regression test for #1763 — any object whose ToString() returns a string
            // with brackets must not corrupt the markup stream.
            var console = new TestConsole();

            var obj = new ObjectWithBracketsInToString();
            FluentActions.Invoking(() => console.Markup("Item: {0}", obj)).Should().NotThrow();
            console.Output.Should().Contain("[item]");
        }

        private sealed class ObjectWithBracketsInToString
        {
            public override string ToString() => "[item]";
        }
    }

    public sealed class WriteException
    {
        [Fact]
        public void Should_Not_Throw_If_Exception_Has_No_StackTrace()
        {
            // Given
            var console = new TestConsole();
            var exception = new InvalidOperationException("An exception.");

            // When
            void When() => console.WriteException(exception);

            // Then
            FluentActions.Invoking(When).Should().NotThrow();
        }
    }
}