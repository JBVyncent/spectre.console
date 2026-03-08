namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Tests targeting Stryker surviving mutants in Segment.cs.
/// Each test documents the specific mutation it kills.
/// </summary>
public sealed class SegmentMutationTests
{
    public sealed class NullGuards
    {
        [Fact]
        public void Constructor_Should_Throw_If_Text_Is_Null()
        {
            // Kills: Line 82, ThrowIfNull removal
            var ex = Record.Exception(() => new Segment(null!));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void CellCount_Static_Should_Throw_If_Segments_Is_Null()
        {
            // Kills: Line 123, ThrowIfNull removal
            var ex = Record.Exception(() => Segment.CellCount(null!));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void SplitLines_Should_Throw_If_Segments_Is_Null()
        {
            // Kills: Line 196, ThrowIfNull removal
            var ex = Record.Exception(() => Segment.SplitLines(null!));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void SplitLines_WithMaxWidth_Should_Throw_If_Segments_Is_Null()
        {
            // Kills: Line 210, ThrowIfNull removal
            var ex = Record.Exception(() => Segment.SplitLines(null!, 80));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void SplitOverflow_Should_Throw_If_Segment_Is_Null()
        {
            // Kills: Line 328, ThrowIfNull removal
            var ex = Record.Exception(() => Segment.SplitOverflow(null!, Overflow.Fold, 80));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Truncate_Enumerable_Should_Throw_If_Segments_Is_Null()
        {
            // Kills: Line 385, ThrowIfNull removal
            var ex = Record.Exception(() => Segment.Truncate((IEnumerable<Segment>)null!, 80));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void TruncateWithEllipsis_Should_Throw_If_Segments_Is_Null()
        {
            // Kills: Line 494, ThrowIfNull removal
            var ex = Record.Exception(() => Segment.TruncateWithEllipsis(null!, 80));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void TrimEnd_Should_Throw_If_Segments_Is_Null()
        {
            // Kills: Line 514, ThrowIfNull removal
            var ex = Record.Exception(() => Segment.TrimEnd(null!));
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Merge_Should_Throw_If_Segments_Is_Null()
        {
            // Kills: Line 456, ThrowIfNull removal
            var ex = Record.Exception(() => Segment.Merge(null!).ToList());
            ex.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void MakeSameHeight_Should_Throw_If_Cells_Is_Null()
        {
            // Kills: Line 539, ThrowIfNull removal
            // Not equivalent — foreach on null throws NullReferenceException
            var ex = Record.Exception(() => Segment.MakeSameHeight(3, null!));
            ex.ShouldBeOfType<ArgumentNullException>();
        }
    }

    public sealed class EmptySegment
    {
        [Fact]
        public void Should_Not_Be_Control_Code()
        {
            // Kills: Line 51, false -> true mutation on Segment.Empty
            Segment.Empty.IsControlCode.ShouldBeFalse();
        }
    }

    public sealed class ControlCodeFactory
    {
        [Fact]
        public void Should_Return_Control_Code_Segment()
        {
            // Kills: Line 98, false -> true mutation on control code flag
            var segment = Segment.Control("\x1b[2J");
            segment.IsControlCode.ShouldBeTrue();
            segment.CellCount().ShouldBe(0);
        }

        [Fact]
        public void Should_Not_Be_Line_Break()
        {
            // Kills: Line 98, additional flag boolean mutation
            var segment = Segment.Control("test");
            segment.IsLineBreak.ShouldBeFalse();
        }
    }

    public sealed class StripLineEndings
    {
        [Fact]
        public void Should_Strip_Trailing_Newlines()
        {
            // Kills: NoCoverage lines 139-141
            var segment = new Segment("Hello\n");
            var stripped = segment.StripLineEndings();
            stripped.Text.ShouldBe("Hello");
        }

        [Fact]
        public void Should_Strip_Trailing_CR_LF()
        {
            var segment = new Segment("World\r\n");
            var stripped = segment.StripLineEndings();
            // Note: NormalizeNewLines in ctor converts \r\n to \n, so \r is already gone
            stripped.Text.ShouldBe("World");
        }

        [Fact]
        public void Should_Preserve_Style()
        {
            var style = new Style(Color.Red);
            var segment = new Segment("Test\n", style);
            var stripped = segment.StripLineEndings();
            stripped.Style.ShouldBe(style);
        }
    }

    public sealed class Clone
    {
        [Fact]
        public void Should_Clone_Segment()
        {
            // Kills: NoCoverage lines 185-187
            var original = new Segment("Hello", new Style(Color.Red));
            var cloned = original.Clone();
            cloned.Text.ShouldBe("Hello");
            cloned.Style.ShouldBe(original.Style);
        }
    }

    public sealed class SplitMethod
    {
        [Fact]
        public void Should_Return_Self_When_Offset_Is_Negative()
        {
            // Kills: NoCoverage lines 151-153
            var segment = new Segment("Hello");
            var (first, second) = segment.Split(-1);
            first.ShouldBe(segment);
            second.ShouldBeNull();
        }
    }

    public sealed class SplitLinesWithMaxWidth
    {
        [Fact]
        public void Should_Handle_Null_Second_From_Split()
        {
            // Kills: Line 235, second != null -> second == null
            // When splitting at exactly the segment boundary, second should be null
            var segments = new[] { new Segment("ABCDE") };
            var lines = Segment.SplitLines(segments, 5);
            // Should produce exactly 1 line containing the segment
            lines.Count.ShouldBe(1);
            lines[0].Count.ShouldBe(1);
            lines[0][0].Text.ShouldBe("ABCDE");
        }

        [Fact]
        public void Should_Split_Long_Segment_Into_Multiple_Lines()
        {
            // Kills: Line 237 (stack.Push removal) — without pushing second, content is lost
            var segments = new[] { new Segment("ABCDEFGHIJ") };
            var lines = Segment.SplitLines(segments, 5);
            lines.Count.ShouldBe(2);
            lines[0][0].Text.ShouldBe("ABCDE");
            lines[1][0].Text.ShouldBe("FGHIJ");
        }

        [Fact]
        public void Should_Contain_Newline_In_Text_Check()
        {
            // Kills: Line 244, "\n" -> "" mutation
            var segments = new[] { new Segment("Hello\nWorld") };
            var lines = Segment.SplitLines(segments);
            lines.Count.ShouldBe(2);
            lines[0][0].Text.ShouldBe("Hello");
            lines[1][0].Text.ShouldBe("World");
        }

        [Fact]
        public void Should_Not_Skip_Continue_In_Newline_Handling()
        {
            // Kills: Line 255, continue removal
            var segments = new[]
            {
                new Segment("A"),
                new Segment("\n"),
                new Segment("B"),
            };
            var lines = Segment.SplitLines(segments);
            lines.Count.ShouldBe(2);
            lines[0][0].Text.ShouldBe("A");
            lines[1][0].Text.ShouldBe("B");
        }

        [Fact]
        public void Should_Handle_Multi_Part_Split_Correctly()
        {
            // Kills: Line 262, parts.Length > 0 -> parts.Length >= 0
            // and Line 272, line.Length > 0 -> line.Length >= 0
            var segments = new[] { new Segment("Hello\nWorld\nFoo") };
            var lines = Segment.SplitLines(segments);
            lines.Count.ShouldBe(3);
            lines[0][0].Text.ShouldBe("Hello");
            lines[1][0].Text.ShouldBe("World");
            lines[2][0].Text.ShouldBe("Foo");
        }

        [Fact]
        public void Should_Join_Remaining_Parts_Correctly()
        {
            // Kills: Line 278, parts.Length - 1 -> parts.Length + 1
            var segments = new[] { new Segment("A\nB\nC") };
            var lines = Segment.SplitLines(segments);
            lines.Count.ShouldBe(3);
            lines[2][0].Text.ShouldBe("C");
        }

        [Fact]
        public void Should_Truncate_Lines_When_Height_Specified_And_Exceeded()
        {
            // Kills: Line 300, lines.Count >= height -> lines.Count > height
            // When lines.Count == height, we should NOT truncate
            var segments = new[]
            {
                new Segment("Line1"),
                new Segment("\n"),
                new Segment("Line2"),
            };
            var lines = Segment.SplitLines(segments, int.MaxValue, height: 2);
            lines.Count.ShouldBe(2);
            lines[0][0].Text.ShouldBe("Line1");
            lines[1][0].Text.ShouldBe("Line2");
        }

        [Fact]
        public void Should_Remove_Excess_Lines_When_Height_Exceeded()
        {
            // Kills: Line 303, RemoveRange removal
            var segments = new[]
            {
                new Segment("A"), new Segment("\n"),
                new Segment("B"), new Segment("\n"),
                new Segment("C"),
            };
            var lines = Segment.SplitLines(segments, int.MaxValue, height: 2);
            lines.Count.ShouldBe(2);
        }
    }

    public sealed class SplitOverflowMethod
    {
        [Fact]
        public void Should_Return_Segment_When_Within_MaxWidth()
        {
            // Kills: NoCoverage lines 331-333 and Line 330 <= vs < mutation
            var segment = new Segment("AB");
            var result = Segment.SplitOverflow(segment, Overflow.Fold, 2);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldBe("AB");
        }

        [Fact]
        public void Should_Return_Segment_When_Exactly_MaxWidth()
        {
            // Kills: Line 330, <= -> < boundary mutation
            var segment = new Segment("ABCDE");
            var result = Segment.SplitOverflow(segment, Overflow.Fold, 5);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldBe("ABCDE");
        }

        [Fact]
        public void Should_Handle_Crop_With_Zero_MaxWidth()
        {
            // Kills: NoCoverage lines 350-352 and Line 350 <= vs < mutation
            var segment = new Segment("Hello");
            var result = Segment.SplitOverflow(segment, Overflow.Crop, 0);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Handle_Crop_With_Positive_MaxWidth()
        {
            // Kills: NoCoverage lines 356-357
            var segment = new Segment("Hello World");
            var result = Segment.SplitOverflow(segment, Overflow.Crop, 5);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldBe("Hello");
        }

        [Fact]
        public void Crop_Should_Preserve_Style()
        {
            // Kills: Line 357, string mutation on empty fallback
            var style = new Style(Color.Green);
            var segment = new Segment("Hello", style);
            var result = Segment.SplitOverflow(segment, Overflow.Crop, 3);
            result.Count.ShouldBe(1);
            result[0].Style.ShouldBe(style);
        }

        [Fact]
        public void Should_Handle_Ellipsis_With_MaxWidth_1()
        {
            // Kills: Line 362, maxWidth - 1 -> maxWidth + 1
            // and NoCoverage lines 364
            var segment = new Segment("Hello");
            var result = Segment.SplitOverflow(segment, Overflow.Ellipsis, 1);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldBe("…");
        }

        [Fact]
        public void Should_Handle_Ellipsis_With_Truncation()
        {
            // Kills: NoCoverage lines 368-370
            var segment = new Segment("Hello World");
            var result = Segment.SplitOverflow(segment, Overflow.Ellipsis, 6);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldStartWith("Hello");
            result[0].Text.ShouldEndWith("…");
        }

        [Fact]
        public void Ellipsis_Should_Produce_Correct_Prefix()
        {
            // Kills: Line 362 arithmetic and Line 369 string mutation
            var segment = new Segment("ABCDEFGH");
            var result = Segment.SplitOverflow(segment, Overflow.Ellipsis, 4);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldBe("ABC…");
        }

        [Fact]
        public void Should_Return_Original_When_At_MaxWidth_Boundary()
        {
            // Kills: Line 330 <= vs < — early return should fire at exact width
            // With <=: CellCount(5) <= 5 is true → returns [segment] (1 item)
            // With <:  CellCount(5) < 5 is false → enters Fold/Crop/Ellipsis logic
            var segment = new Segment("ABCDE");
            var foldResult = Segment.SplitOverflow(segment, Overflow.Fold, 5);
            foldResult.Count.ShouldBe(1);
            foldResult[0].ShouldBe(segment); // Same instance for early return
        }
    }

    public sealed class TruncateEnumerable
    {
        [Fact]
        public void Should_Truncate_When_Segment_Exceeds_MaxWidth()
        {
            // Kills: Line 393, > -> >= boundary mutation
            var segments = new[]
            {
                new Segment("Hello"),
                new Segment(" "),
                new Segment("World"),
            };
            var result = Segment.Truncate(segments, 6);
            // "Hello" (5) fits, " " (1) fits (total 6), "World" (5) exceeds 6
            result.Count.ShouldBe(2);
        }

        [Fact]
        public void Should_Truncate_At_Exact_Boundary()
        {
            // Kills: Line 393, > vs >= — when total == maxWidth, should NOT break
            var segments = new[]
            {
                new Segment("ABC"),
                new Segment("DE"),
            };
            var result = Segment.Truncate(segments, 5);
            result.Count.ShouldBe(2); // Both fit exactly
        }

        [Fact]
        public void Should_Truncate_First_Segment_When_None_Fit()
        {
            // Kills: Line 404, First() -> FirstOrDefault()
            var segments = new[] { new Segment("Hello World") };
            var result = Segment.Truncate(segments, 5);
            result.Count.ShouldBe(1);
            result[0].CellCount().ShouldBeLessThanOrEqualTo(5);
        }
    }

    public sealed class TruncateSingle
    {
        [Fact]
        public void Should_Return_Null_For_Null_Segment()
        {
            // Kills: NoCoverage lines 422-424
            var result = Segment.Truncate((Segment?)null, 10);
            result.ShouldBeNull();
        }

        [Fact]
        public void Should_Return_Segment_When_Within_MaxWidth()
        {
            // Kills: NoCoverage lines 427-429 and <= vs < mutation
            var segment = new Segment("Hi");
            var result = Segment.Truncate(segment, 5);
            result.ShouldBe(segment);
        }

        [Fact]
        public void Should_Return_Segment_At_Exact_MaxWidth()
        {
            // Kills: Line 427, <= -> < boundary
            var segment = new Segment("Hello");
            var result = Segment.Truncate(segment, 5);
            result.ShouldBe(segment);
        }

        [Fact]
        public void Should_Return_Null_When_Nothing_Fits()
        {
            // Kills: NoCoverage lines 446-448, block returning null removal
            // A fullwidth character (width 2) in maxWidth 1 can't fit
            var segment = new Segment("测");
            var result = Segment.Truncate(segment, 1);
            result.ShouldBeNull();
        }
    }

    public sealed class TruncateWithEllipsis
    {
        [Fact]
        public void Should_Return_Segments_When_Within_MaxWidth()
        {
            // Kills: NoCoverage lines 496-498 and <= vs < mutation
            var segments = new[] { new Segment("Hi") };
            var result = Segment.TruncateWithEllipsis(segments, 10);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldBe("Hi");
        }

        [Fact]
        public void Should_Return_At_Exact_MaxWidth_Without_Ellipsis()
        {
            // Kills: Line 496, <= -> < boundary
            var segments = new[] { new Segment("Hello") };
            var result = Segment.TruncateWithEllipsis(segments, 5);
            result.Count.ShouldBe(1);
            result[0].Text.ShouldBe("Hello");
        }

        [Fact]
        public void Should_Append_Ellipsis_With_Last_Style()
        {
            // Kills: Line 508, Last() -> First()
            // Need multiple segments where at least 2 survive truncation at maxWidth-1
            var style1 = new Style(Color.Red);
            var style2 = new Style(Color.Blue);
            var style3 = new Style(Color.Green);
            var segments = new[]
            {
                new Segment("ABCD", style1),
                new Segment("EFGH", style2),
                new Segment("IJKL", style3),
            };
            // Total = 12, maxWidth = 9
            // TruncateWithEllipsis: Truncate at 8 → ["ABCD","EFGH"] survive
            // result.Last() = "EFGH" (Blue), ellipsis should get Blue style
            var result = Segment.TruncateWithEllipsis(segments, 9);
            var ellipsis = result[^1];
            ellipsis.Text.ShouldContain("…");
            ellipsis.Style.ShouldBe(style2);
        }
    }

    public sealed class MakeSameHeight
    {
        [Fact]
        public void Should_Pad_Cells_To_Target_Height()
        {
            // Kills: NoCoverage lines 539/543/545/547
            var cells = new List<List<SegmentLine>>
            {
                new() { new SegmentLine() },
                new() { new SegmentLine(), new SegmentLine(), new SegmentLine() },
            };
            var result = Segment.MakeSameHeight(3, cells);
            result[0].Count.ShouldBe(3);
            result[1].Count.ShouldBe(3);
        }

        [Fact]
        public void Should_Not_Pad_When_Already_At_Height()
        {
            // Kills: Line 543, < vs <= boundary
            // When cell.Count == cellHeight, < is false (correct: don't pad)
            // With <=: cell.Count <= cellHeight is true, would pad an extra line
            var cells = new List<List<SegmentLine>>
            {
                new() { new SegmentLine(), new SegmentLine() },
            };
            var result = Segment.MakeSameHeight(2, cells);
            result[0].Count.ShouldBe(2); // Should NOT grow to 3
        }
    }

    public sealed class MakeWidth
    {
        [Fact]
        public void Should_Pad_Lines_To_Expected_Width()
        {
            // Kills: NoCoverage lines 560/562/563
            var line = new SegmentLine();
            line.Add(new Segment("AB"));
            var lines = new List<SegmentLine> { line };

            var result = Segment.MakeWidth(5, lines);
            // Line should now be padded to width 5
            result[0].CellCount().ShouldBe(5);
        }

        [Fact]
        public void Should_Not_Pad_When_Already_Expected_Width()
        {
            // Kills: Line 560, < vs boundary mutations
            var line = new SegmentLine();
            line.Add(new Segment("ABCDE"));
            var lines = new List<SegmentLine> { line };

            var result = Segment.MakeWidth(5, lines);
            result[0].CellCount().ShouldBe(5);
            result[0].Count.ShouldBe(1); // No padding segment added
        }
    }
}
