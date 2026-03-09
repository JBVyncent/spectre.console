namespace Spectre.Console.Tests.Unit;

public sealed class SegmentTests
{
    public sealed class TheSplitMethod
    {
        [Theory]
        [InlineData("Foo Bar", 0, "", "Foo Bar")]
        [InlineData("Foo Bar", 1, "F", "oo Bar")]
        [InlineData("Foo Bar", 2, "Fo", "o Bar")]
        [InlineData("Foo Bar", 3, "Foo", " Bar")]
        [InlineData("Foo Bar", 4, "Foo ", "Bar")]
        [InlineData("Foo Bar", 5, "Foo B", "ar")]
        [InlineData("Foo Bar", 6, "Foo Ba", "r")]
        [InlineData("Foo Bar", 7, "Foo Bar", null)]
        [InlineData("Foo 测试 Bar", 0, "", "Foo 测试 Bar")]
        [InlineData("Foo 测试 Bar", 1, "F", "oo 测试 Bar")]
        [InlineData("Foo 测试 Bar", 2, "Fo", "o 测试 Bar")]
        [InlineData("Foo 测试 Bar", 3, "Foo", " 测试 Bar")]
        [InlineData("Foo 测试 Bar", 4, "Foo ", "测试 Bar")]
        [InlineData("Foo 测试 Bar", 5, "Foo 测", "试 Bar")]
        [InlineData("Foo 测试 Bar", 6, "Foo 测", "试 Bar")]
        [InlineData("Foo 测试 Bar", 7, "Foo 测试", " Bar")]
        [InlineData("Foo 测试 Bar", 8, "Foo 测试", " Bar")]
        [InlineData("Foo 测试 Bar", 9, "Foo 测试 ", "Bar")]
        [InlineData("Foo 测试 Bar", 10, "Foo 测试 B", "ar")]
        [InlineData("Foo 测试 Bar", 11, "Foo 测试 Ba", "r")]
        [InlineData("Foo 测试 Bar", 12, "Foo 测试 Bar", null)]
        public void Should_Split_Segment_Correctly(string text, int offset, string expectedFirst, string? expectedSecond)
        {
            // Given
            var style = new Style(Color.Red, Color.Green, Decoration.Bold);
            var segment = new Segment(text, style);

            // When
            var (first, second) = segment.Split(offset);

            // Then
            first.Text.Should().Be(expectedFirst);
            first.Style.Should().Be(style);
            second?.Text.Should().Be(expectedSecond);
            second?.Style.Should().Be(style);
        }
    }

    public sealed class TheSplitLinesMethod
    {
        [Fact]
        public void Should_Split_Segment()
        {
            // Given, When
            var lines = Segment.SplitLines(
            [
                new Segment("Foo"),
                        new Segment("Bar"),
                        new Segment("\n"),
                        new Segment("Baz"),
                        new Segment("Qux"),
                        new Segment("\n"),
                        new Segment("Corgi")
            ]);

            // Then
            lines.Count.Should().Be(3);

            lines[0].Count.Should().Be(2);
            lines[0][0].Text.Should().Be("Foo");
            lines[0][1].Text.Should().Be("Bar");

            lines[1].Count.Should().Be(2);
            lines[1][0].Text.Should().Be("Baz");
            lines[1][1].Text.Should().Be("Qux");

            lines[2].Count.Should().Be(1);
            lines[2][0].Text.Should().Be("Corgi");
        }

        [Fact]
        public void Should_Split_Segment_With_Windows_LineBreak()
        {
            // Given, When
            var lines = Segment.SplitLines(
            [
                new Segment("Foo"),
                        new Segment("Bar"),
                        new Segment("\r\n"),
                        new Segment("Baz"),
                        new Segment("Qux"),
                        new Segment("\r\n"),
                        new Segment("Corgi")
            ]);

            // Then
            lines.Count.Should().Be(3);

            lines[0].Count.Should().Be(2);
            lines[0][0].Text.Should().Be("Foo");
            lines[0][1].Text.Should().Be("Bar");

            lines[1].Count.Should().Be(2);
            lines[1][0].Text.Should().Be("Baz");
            lines[1][1].Text.Should().Be("Qux");

            lines[2].Count.Should().Be(1);
            lines[2][0].Text.Should().Be("Corgi");
        }

        [Fact]
        public void Should_Split_Segments_With_Linebreak_In_Text()
        {
            // Given, Given
            var lines = Segment.SplitLines(
            [
                new Segment("Foo\n"),
                        new Segment("Bar\n"),
                        new Segment("Baz"),
                        new Segment("Qux\n"),
                        new Segment("Corgi")
            ]);

            // Then
            lines.Count.Should().Be(4);

            lines[0].Count.Should().Be(1);
            lines[0][0].Text.Should().Be("Foo");

            lines[1].Count.Should().Be(1);
            lines[1][0].Text.Should().Be("Bar");

            lines[2].Count.Should().Be(2);
            lines[2][0].Text.Should().Be("Baz");
            lines[2][1].Text.Should().Be("Qux");

            lines[3].Count.Should().Be(1);
            lines[3][0].Text.Should().Be("Corgi");
        }

        [Fact]
        public void Should_Respect_Multiple_Linebreaks_Within_Single_Segment()
        {
            // Given, When — regression for #1785
            var lines = Segment.SplitLines(
            [
                new Segment("Foo\nBar"),
                new Segment("Baz"),
                new Segment("Qux\nTra\nLate"),
                new Segment("Corgi"),
            ]);

            // Then — "Qux" precedes the \n so it stays on line 1 with "Bar"+"Baz"
            lines.Count.Should().Be(4);

            lines[0].Count.Should().Be(1);
            lines[0][0].Text.Should().Be("Foo");

            lines[1].Count.Should().Be(3);
            lines[1][0].Text.Should().Be("Bar");
            lines[1][1].Text.Should().Be("Baz");
            lines[1][2].Text.Should().Be("Qux");

            lines[2].Count.Should().Be(1);
            lines[2][0].Text.Should().Be("Tra");

            lines[3].Count.Should().Be(2);
            lines[3][0].Text.Should().Be("Late");
            lines[3][1].Text.Should().Be("Corgi");
        }
    }

    public sealed class TheSplitOverflowMethod
    {
        [Fact]
        public void Should_Handle_Fullwidth_Text_When_Using_Ellipsis()
        {
            // Given
            var text = "神様達が下界に来る前は、魔法は特定の種族の専売特許に過ぎなかった。";
            var segment = new Segment(text);

            // When
            var result = Segment.SplitOverflow(segment, Overflow.Ellipsis, 10);

            // Then
            result.Count.Should().Be(1);
            result[0].CellCount().Should().BeLessThanOrEqualTo(10);
            result[0].Text.EndsWith("…", StringComparison.Ordinal).Should().BeTrue();
        }

        [Fact]
        public void Should_Handle_Fullwidth_Text_When_Using_Crop()
        {
            // Given
            var text = "神様達が下界に来る前は、魔法は特定の種族の専売特許に過ぎなかった。";
            var segment = new Segment(text);

            // When
            var result = Segment.SplitOverflow(segment, Overflow.Crop, 10);

            // Then
            result.Count.Should().Be(1);
            result[0].CellCount().Should().BeLessThanOrEqualTo(10);
            result[0].Text.EndsWith("…", StringComparison.Ordinal).Should().BeFalse();
        }
    }
}