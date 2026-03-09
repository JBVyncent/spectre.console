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

    public sealed class UnicodeScalarValueSupport
    {
        // 🎉 = U+1F389 (Party Popper), a non-BMP emoji encoded as surrogate pair in UTF-16
        // Display width = 2 cells
        private const string PartyPopper = "\U0001F389";

        // 𠀀 = U+20000 (CJK Unified Ideographs Extension B), non-BMP wide char
        // Display width = 2 cells
        private const string CjkExtB = "\U00020000";

        [Fact]
        public void Cell_GetCellLength_Returns_Correct_Width_For_NonBMP_Emoji()
        {
            // A single emoji codepoint = 2 cells, but is 2 chars in UTF-16
            Cell.GetCellLength(PartyPopper).Should().Be(2);
        }

        [Fact]
        public void Cell_GetCellLength_Returns_Correct_Width_For_NonBMP_CJK()
        {
            Cell.GetCellLength(CjkExtB).Should().Be(2);
        }

        [Fact]
        public void Cell_GetCellLength_Handles_Mixed_BMP_And_NonBMP()
        {
            // "A" (1) + 🎉 (2) + "B" (1) = 4 cells
            var text = "A" + PartyPopper + "B";
            Cell.GetCellLength(text).Should().Be(4);
        }

        [Fact]
        public void Cell_GetCellLength_Int_Overload_Works_For_NonBMP()
        {
            // U+1F389 should be width 2
            Cell.GetCellLength(0x1F389).Should().Be(2);
        }

        [Fact]
        public void Segment_CellCount_Returns_Correct_Width_For_NonBMP()
        {
            var segment = new Segment("A" + PartyPopper + "B");
            segment.CellCount().Should().Be(4);
        }

        [Theory]
        [InlineData(0, "", null)]  // offset 0 → split at start
        [InlineData(1, "A", null)] // offset 1 → "A" (1 cell), but 🎉 won't fit in remainder check
        public void Split_Handles_NonBMP_At_Boundary(int offset, string expectedFirst, string? expectedSecond)
        {
            // "A🎉" = "A" (1 cell) + 🎉 (2 cells) = 3 cells
            var segment = new Segment("A" + PartyPopper);
            var (first, second) = segment.Split(offset);

            first.Text.Should().Be(expectedFirst);
            if (expectedSecond == null)
            {
                // When offset >= CellCount, second is null
                // When offset == 0 (returns empty first, full second) or offset == 1 (A, 🎉)
                if (offset == 0)
                {
                    second.Should().NotBeNull();
                    second!.Text.Should().Be("A" + PartyPopper);
                }
                else
                {
                    second.Should().NotBeNull();
                    second!.Text.Should().Be(PartyPopper);
                }
            }
        }

        [Fact]
        public void Split_Does_Not_Break_Surrogate_Pairs()
        {
            // "A🎉B" — split at offset 1 should yield "A" and "🎉B"
            var segment = new Segment("A" + PartyPopper + "B");
            var (first, second) = segment.Split(1);

            first.Text.Should().Be("A");
            second.Should().NotBeNull();
            second!.Text.Should().Be(PartyPopper + "B");
        }

        [Fact]
        public void Truncate_Preserves_Surrogate_Pairs()
        {
            // "A🎉B" = 4 cells, truncate to 3 should yield "A🎉" (3 cells)
            var segment = new Segment("A" + PartyPopper + "B");
            var result = Segment.Truncate(segment, 3);

            result.Should().NotBeNull();
            result!.Text.Should().Be("A" + PartyPopper);
            result.CellCount().Should().Be(3);
        }

        [Fact]
        public void Truncate_Does_Not_Include_Partial_NonBMP_Char()
        {
            // "🎉B" = 3 cells, truncate to 1 should yield null (can't fit the 2-cell emoji)
            var segment = new Segment(PartyPopper + "B");
            var result = Segment.Truncate(segment, 1);

            result.Should().BeNull();
        }

        [Fact]
        public void SplitOverflow_Fold_Preserves_Surrogate_Pairs()
        {
            // 3 emoji = 6 cells, fold at maxWidth=4 should split between emoji boundaries
            var text = PartyPopper + PartyPopper + PartyPopper;
            var segment = new Segment(text);

            var result = Segment.SplitOverflow(segment, Overflow.Fold, 4);

            result.Count.Should().Be(2);
            result[0].Text.Should().Be(PartyPopper + PartyPopper);
            result[0].CellCount().Should().Be(4);
            result[1].Text.Should().Be(PartyPopper);
            result[1].CellCount().Should().Be(2);
        }

        [Fact]
        public void SplitLines_With_MaxWidth_Splits_NonBMP_Correctly()
        {
            // "A🎉B" = 4 cells, maxWidth=3 should split as "A🎉" (3) and "B" (1)
            var segments = new[] { new Segment("A" + PartyPopper + "B") };
            var lines = Segment.SplitLines(segments, 3);

            lines.Count.Should().Be(2);
            lines[0].Count.Should().Be(1);
            lines[0][0].Text.Should().Be("A" + PartyPopper);
            lines[1].Count.Should().Be(1);
            lines[1][0].Text.Should().Be("B");
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