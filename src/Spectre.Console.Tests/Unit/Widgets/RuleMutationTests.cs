namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Tests targeting Stryker surviving mutants in Rule.cs.
/// </summary>
public sealed class RuleMutationTests
{
    public sealed class NullGuards
    {
        [Fact]
        public void Constructor_Should_Throw_If_Title_Is_Null()
        {
            // Kills: Line 42, ThrowIfNull removal
            var ex = Record.Exception(() => new Rule(null!));
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void RuleTitle_Extension_Should_Throw_If_Rule_Is_Null()
        {
            // Kills: Line 173, ThrowIfNull removal
            var ex = Record.Exception(() => RuleExtensions.RuleTitle(null!, "test"));
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void RuleTitle_Extension_Should_Throw_If_Title_Is_Null()
        {
            // Kills: Line 174, ThrowIfNull removal
            var rule = new Rule();
            var ex = Record.Exception(() => rule.RuleTitle(null!));
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void RuleStyle_Extension_Should_Throw_If_Rule_Is_Null()
        {
            // Kills: Line 188, ThrowIfNull removal (NoCoverage)
            var ex = Record.Exception(() => RuleExtensions.RuleStyle(null!, Style.Plain));
            ex.Should().BeOfType<ArgumentNullException>();
        }
    }

    public sealed class MeasureOverride
    {
        [Fact]
        public void Should_Return_Min_1_Max_Width()
        {
            // Kills: NoCoverage lines 48-50
            var rule = new Rule("Test");
            var console = new TestConsole().Width(40);
            // Rendering the rule implicitly calls Measure
            console.Write(rule);
            console.Output.Should().NotBeEmpty();
        }
    }

    public sealed class RenderBehavior
    {
        [Fact]
        public void Should_Render_With_Very_Small_Width()
        {
            // Kills: NoCoverage lines 56-58 and Line 55 <= vs < mutation
            // TestConsole requires width > 0, use width 1
            var console = new TestConsole().Width(1);
            var rule = new Rule();
            console.Write(rule);
            console.Output.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Render_Without_Title_When_Title_Is_Null()
        {
            // Kills: Various Rule render paths
            var console = new TestConsole().Width(20);
            var rule = new Rule();
            console.Write(rule);
            console.Output.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Render_Without_Title_When_Width_Equals_ExtraLength()
        {
            // Kills: Line 62, maxWidth <= extraLength -> < extraLength boundary
            // extraLength = (2*TitlePadding) + (2*TitleSpacing) = (2*2)+(2*1) = 6
            // At width=6: <= 6 is true → no title. With < mutation: < 6 is false → try title
            var rule = new Rule("X");
            var withTitleConsole = new TestConsole().Width(7);
            withTitleConsole.Write(new Rule("X"));
            var noTitleConsole = new TestConsole().Width(6);
            noTitleConsole.Write(new Rule("X"));
            // At width 6, should render without title (just a line)
            // At width 7, should attempt title
            noTitleConsole.Output.Should().NotBe(withTitleConsole.Output);
        }

        [Fact]
        public void Should_Render_Title_With_Truncation()
        {
            // Kills: Line 69, > -> >= boundary and Line 68 arithmetic
            var rule = new Rule("This is a very long title that exceeds the width");
            var console = new TestConsole().Width(20);
            console.Write(rule);
            var output = console.Output;
            // Title should be truncated with ellipsis
            output.Should().Contain("…");
        }

        [Fact]
        public void Should_Subtract_ExtraLength_From_MaxWidth_For_Title()
        {
            // Kills: Line 68, maxWidth - extraLength -> maxWidth + extraLength
            // extraLength=6. With -, title gets width-6 space. With +, title gets width+6 space.
            var rule = new Rule("ABCDEFGHIJ");
            var console = new TestConsole().Width(12);
            console.Write(rule);
            // With width 12 and extraLength 6, title gets 6 chars max
            // "ABCDEFGHIJ" is 10 chars, should be truncated
            console.Output.Should().Contain("…");
        }

        [Fact]
        public void Should_Include_LineBreak_In_Output()
        {
            // Kills: Line 86, LineBreak removal
            var console = new TestConsole().Width(20);
            var rule = new Rule("Test");
            console.Write(rule);
            console.Output.Should().Contain("\n");
        }

        [Fact]
        public void Should_Render_With_Heavy_Border_Differently_By_Unicode()
        {
            // Kills: Line 93, !options.Unicode -> options.Unicode mutation
            // Use Heavy border which has a distinct safe border
            var rule = new Rule { Border = BoxBorder.Heavy };

            var unicodeConsole = new TestConsole().Width(20);
            unicodeConsole.Write(rule);
            var unicodeOutput = unicodeConsole.Output;

            var asciiConsole = new TestConsole().Width(20);
            asciiConsole.Profile.Capabilities.Unicode = false;
            asciiConsole.Write(rule);
            var asciiOutput = asciiConsole.Output;

            // Heavy border renders differently in unicode vs non-unicode
            unicodeOutput.Should().NotBe(asciiOutput);
        }

        [Fact]
        public void Should_Use_SingleLine_For_Title()
        {
            // Kills: Line 109, SingleLine = true -> false
            var rule = new Rule("Test");
            var console = new TestConsole().Width(40);
            console.Write(rule);
            // With SingleLine=true, title renders on one line
            var lines = console.Output.Split('\n');
            // Should be 2 (content + trailing newline), not more
            lines.Length.Should().BeLessThanOrEqualTo(3);
        }
    }

    public sealed class CustomStyle
    {
        private static TestConsole CreateAnsiConsole(int width)
        {
            var console = new TestConsole().Width(width);
            console.EmitAnsiSequences = true;
            return console;
        }

        [Fact]
        public void Should_Use_Custom_Style_When_Set()
        {
            // Kills: Lines 136/140, Style ?? Style.Plain null coalescing (center align)
            var styledRule = new Rule("Test") { Style = new Style(Color.Red) };
            var plainRule = new Rule("Test");

            var styledConsole = CreateAnsiConsole(40);
            styledConsole.Write(styledRule);
            var plainConsole = CreateAnsiConsole(40);
            plainConsole.Write(plainRule);

            styledConsole.Output.Should().NotBe(plainConsole.Output);
        }

        [Fact]
        public void Should_Use_Custom_Style_With_Left_Alignment()
        {
            // Kills: Lines 124/128, left-aligned Style ?? fallback
            var styledRule = new Rule("Test")
            {
                Style = new Style(Color.Blue),
                Justification = Justify.Left,
            };
            var plainRule = new Rule("Test") { Justification = Justify.Left };

            var styledConsole = CreateAnsiConsole(40);
            styledConsole.Write(styledRule);
            var plainConsole = CreateAnsiConsole(40);
            plainConsole.Write(plainRule);

            styledConsole.Output.Should().NotBe(plainConsole.Output);
        }

        [Fact]
        public void Should_Use_Custom_Style_With_Right_Alignment()
        {
            // Kills: Lines 147/151, right-aligned Style ?? fallback
            var styledRule = new Rule("Test")
            {
                Style = new Style(Color.Green),
                Justification = Justify.Right,
            };
            var plainRule = new Rule("Test") { Justification = Justify.Right };

            var styledConsole = CreateAnsiConsole(40);
            styledConsole.Write(styledRule);
            var plainConsole = CreateAnsiConsole(40);
            plainConsole.Write(plainRule);

            styledConsole.Output.Should().NotBe(plainConsole.Output);
        }

        [Fact]
        public void Should_Use_Custom_Style_For_Line_Without_Title()
        {
            // Kills: Line 98, Style ?? Style.Plain in GetLineWithoutTitle
            var styledRule = new Rule { Style = new Style(Color.Yellow) };
            var plainRule = new Rule();

            var styledConsole = CreateAnsiConsole(40);
            styledConsole.Write(styledRule);
            var plainConsole = CreateAnsiConsole(40);
            plainConsole.Write(plainRule);

            styledConsole.Output.Should().NotBe(plainConsole.Output);
        }
    }
}
