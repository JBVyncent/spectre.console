namespace Spectre.Console.Tests.Properties;

public sealed class SegmentProperties
{
    [Fact]
    public void Empty_HasCellCountZero()
    {
        Segment.Empty.CellCount().Should().Be(0);
    }

    [Fact]
    public void Empty_IsNotLineBreak()
    {
        Segment.Empty.IsLineBreak.Should().BeFalse();
    }

    [Fact]
    public void LineBreak_IsLineBreak()
    {
        Segment.LineBreak.IsLineBreak.Should().BeTrue();
    }

    [Fact]
    public void LineBreak_IsWhiteSpace()
    {
        Segment.LineBreak.IsWhiteSpace.Should().BeTrue();
    }

    [Fact]
    public void Control_HasCellCountZero()
    {
        Segment.Control("\x1b[1m").CellCount().Should().Be(0);
    }

    [Fact]
    public void Control_IsControlCode()
    {
        Segment.Control("\x1b[1m").IsControlCode.Should().BeTrue();
    }

    [Property]
    public bool Padding_HasCorrectCellCount(PositiveInt size)
    {
        var n = size.Get;
        return Segment.Padding(n).CellCount() == n;
    }

    [Property]
    public bool AsciiText_CellCountEqualsLength(NonEmptyString s)
    {
        // ASCII characters all have cell width 1.
        var ascii = new string(s.Get.Where(c => c > 0x1F && c < 0x7F).ToArray());
        if (ascii.Length == 0) return true;
        var seg = new Segment(ascii);
        return seg.CellCount() == ascii.Length;
    }

    [Property]
    public bool SplitAtNegative_ReturnsOriginalWithNoSecond(NonEmptyString s)
    {
        var ascii = new string(s.Get.Where(c => c > 0x1F && c < 0x7F).ToArray());
        if (ascii.Length == 0) return true;
        var seg = new Segment(ascii);
        var (first, second) = seg.Split(-1);
        return first.Text == seg.Text && second is null;
    }

    [Property]
    public bool SplitBeyondLength_ReturnsOriginalWithNoSecond(NonEmptyString s)
    {
        var ascii = new string(s.Get.Where(c => c > 0x1F && c < 0x7F).ToArray());
        if (ascii.Length == 0) return true;
        var seg = new Segment(ascii);
        var (first, second) = seg.Split(ascii.Length + 100);
        return first.Text == seg.Text && second is null;
    }

    [Property]
    public bool Split_ConcatReconstitutesOriginalText(NonEmptyString s, PositiveInt offset)
    {
        var ascii = new string(s.Get.Where(c => c > 0x1F && c < 0x7F).ToArray());
        if (ascii.Length < 2) return true;
        var seg = new Segment(ascii);
        var splitAt = (offset.Get % (ascii.Length - 1)) + 1; // in [1, length-1]
        var (first, second) = seg.Split(splitAt);
        if (second is null) return true; // split at boundary
        return (first.Text + second.Text) == ascii;
    }

    [Property]
    public bool Split_BothPartsPreserveStyle(NonEmptyString s, PositiveInt offset)
    {
        var ascii = new string(s.Get.Where(c => c > 0x1F && c < 0x7F).ToArray());
        if (ascii.Length < 2) return true;
        var style = new Style(Color.Red, null, Decoration.Bold);
        var seg = new Segment(ascii, style);
        var splitAt = (offset.Get % (ascii.Length - 1)) + 1;
        var (first, second) = seg.Split(splitAt);
        if (second is null) return true;
        return first.Style.Equals(style) && second.Style.Equals(style);
    }

    [Property]
    public bool Clone_IsEqualToOriginal(NonNull<string> text)
    {
        var seg = new Segment(text.Get, Style.Plain);
        var clone = seg.Clone();
        return clone.Text == seg.Text && clone.Style.Equals(seg.Style);
    }
}
