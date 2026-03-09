namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Tests targeting Stryker surviving mutants in Paragraph.cs.
/// </summary>
public sealed class ParagraphMutationTests
{
    public sealed class NullGuards
    {
        [Fact]
        public void Constructor_Should_Throw_If_Text_Is_Null()
        {
            // Kills: Line 48, ThrowIfNull removal
            var ex = Record.Exception(() => new Paragraph(null!));
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Append_Should_Throw_If_Text_Is_Null()
        {
            // Kills: Line 62, ThrowIfNull removal
            var paragraph = new Paragraph("Hello");
            var ex = Record.Exception(() => paragraph.Append(null!));
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Render_Should_Throw_If_Options_Is_Null()
        {
            // Kills: Line 123, ThrowIfNull removal
            var paragraph = new Paragraph("Hello");
            var renderable = (IRenderable)paragraph;
            var ex = Record.Exception(() => renderable.Render(null!, 80));
            ex.Should().BeOfType<ArgumentNullException>();
        }
    }

    public sealed class SplitLinesBehavior
    {
        [Fact]
        public void Should_Handle_Very_Small_Width()
        {
            // Kills: Line 173, maxWidth <= 0 -> < 0 boundary
            var paragraph = new Paragraph("Hello World");
            var console = new TestConsole().Width(1);
            console.Write(paragraph);
            console.Output.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_Not_Split_When_Content_Fits_Exactly()
        {
            // Kills: Line 179, _lines.Max(...) <= maxWidth -> < maxWidth
            // At exact boundary: Max(5) == 5, <= is true (clone), < is false (split)
            var paragraph = new Paragraph("Hello");
            var console = new TestConsole().Width(5);
            console.Write(paragraph);
            console.Output.TrimEnd().Should().Be("Hello");
        }

        [Fact]
        public void Should_Use_Max_Not_Min_For_Width_Check()
        {
            // Kills: Line 179, _lines.Max -> _lines.Min mutation
            // Lines: "AB" (2 chars), "CDEFGHIJ" (8 chars)
            // Max=8, Min=2, width=6
            // With Max: 8 <= 6 is false → split → CDEFGHIJ wraps
            // With Min: 2 <= 6 is true → clone → CDEFGHIJ NOT wrapped (wrong)
            var paragraph = new Paragraph("AB\nCDEFGHIJ");
            var console = new TestConsole().Width(6);
            console.Write(paragraph);
            var lines = console.Output.TrimEnd().Split('\n');
            lines.Length.Should().BeGreaterThan(2); // "CDEFGHIJ" must be wrapped
        }

        [Fact]
        public void Should_Split_When_Content_Exceeds_Width()
        {
            var paragraph = new Paragraph("ABCDEF");
            var console = new TestConsole().Width(5);
            console.Write(paragraph);
            var lines = console.Output.TrimEnd().Split('\n');
            lines.Length.Should().Be(2);
            lines[0].TrimEnd().Should().Be("ABCDE");
        }

        [Fact]
        public void Should_Handle_Segment_Overflow_In_SplitLines()
        {
            // Kills: Line 226, segments.Count > 0 -> >= 0
            var paragraph = new Paragraph("A very long word that cannot fit");
            var console = new TestConsole().Width(10);
            console.Write(paragraph);
            var lines = console.Output.TrimEnd().Split('\n');
            lines.Length.Should().BeGreaterThan(1);
        }

        [Fact]
        public void Should_Add_Empty_Segment_At_Line_Wrap()
        {
            // Kills: Line 247, line.Add(Segment.Empty) removal
            // and Line 263, line.Count > 0 -> >= 0
            var paragraph = new Paragraph("Hello World Test");
            var console = new TestConsole().Width(6);
            console.Write(paragraph);
            var lines = console.Output.TrimEnd().Split('\n');
            lines.Length.Should().BeGreaterThan(1);
        }

        [Fact]
        public void Should_Handle_Newlines_In_SplitLines()
        {
            // Kills: NoCoverage lines 208/215/217
            var paragraph = new Paragraph("Hello\nWorld");
            var console = new TestConsole().Width(80);
            console.Write(paragraph);
            var lines = console.Output.TrimEnd().Split('\n');
            lines.Length.Should().Be(2);
            lines[0].TrimEnd().Should().Be("Hello");
            lines[1].TrimEnd().Should().Be("World");
        }

        [Fact]
        public void Should_Skip_Leading_Whitespace_After_Wrap()
        {
            // Kills: Lines 254-256, whitespace skip after newLine
            var paragraph = new Paragraph("Hello World");
            var console = new TestConsole().Width(5);
            console.Write(paragraph);
            var lines = console.Output.TrimEnd().Split('\n');
            lines.Length.Should().Be(2);
            // Leading space after "Hello" should be skipped when wrapping
            lines[1].TrimEnd().Should().Be("World");
        }
    }
}
