namespace Spectre.Console.Rendering;

/// <summary>
/// Represents a renderable segment.
/// </summary>
[DebuggerDisplay("{Text,nq}")]
public class Segment
{
    /// <summary>
    /// Gets the segment text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets a value indicating whether or not this is an explicit line break
    /// that should be preserved.
    /// </summary>
    public bool IsLineBreak { get; }

    /// <summary>
    /// Gets a value indicating whether or not this is a whitespace
    /// that should be preserved but not taken into account when
    /// layouting text.
    /// </summary>
    public bool IsWhiteSpace { get; }

    /// <summary>
    /// Gets a value indicating whether or not this is a
    /// control code such as cursor movement.
    /// </summary>
    public bool IsControlCode { get; }

    /// <summary>
    /// Gets the segment style.
    /// </summary>
    public Style Style { get; }

    /// <summary>
    /// Gets the segment link.
    /// </summary>
    public Link? Link { get; }

    /// <summary>
    /// Gets a segment representing a line break.
    /// </summary>
    public static Segment LineBreak { get; } = new Segment(Environment.NewLine, Style.Plain, null, true, false);

    /// <summary>
    /// Gets an empty segment.
    /// </summary>
    public static Segment Empty { get; } = new Segment(string.Empty, Style.Plain, null, false, false);

    /// <summary>
    /// Creates padding segment.
    /// </summary>
    /// <param name="size">Number of whitespace characters.</param>
    /// <returns>Segment for specified padding size.</returns>
    public static Segment Padding(int size) => new(new string(' ', size));

    /// <summary>
    /// Initializes a new instance of the <see cref="Segment"/> class.
    /// </summary>
    /// <param name="text">The segment text.</param>
    public Segment(string text)
        : this(text, Style.Plain)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Segment"/> class.
    /// </summary>
    /// <param name="text">The segment text.</param>
    /// <param name="style">The segment style.</param>
    /// <param name="link">The segment link.</param>
    public Segment(string text, Style style, Link? link = null)
        : this(text, style, link, false, false)
    {
    }

    private Segment(string text, Style style, Link? link, bool lineBreak, bool control)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text.NormalizeNewLines();
        Style = style;
        Link = link;
        IsLineBreak = lineBreak;
        IsWhiteSpace = string.IsNullOrWhiteSpace(text);
        IsControlCode = control;
    }

    /// <summary>
    /// Creates a control segment.
    /// </summary>
    /// <param name="control">The control code.</param>
    /// <returns>A segment representing a control code.</returns>
    public static Segment Control(string control)
    {
        return new Segment(control, Style.Plain, null, false, true);
    }

    /// <summary>
    /// Gets the number of cells that this segment
    /// occupies in the console.
    /// </summary>
    /// <returns>The number of cells that this segment occupies in the console.</returns>
    public int CellCount()
    {
        if (IsControlCode)
        {
            return 0;
        }

        return Cell.GetCellLength(Text);
    }

    /// <summary>
    /// Gets the number of cells that the segments occupies in the console.
    /// </summary>
    /// <param name="segments">The segments to measure.</param>
    /// <returns>The number of cells that the segments occupies in the console.</returns>
    public static int CellCount(IEnumerable<Segment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var sum = 0;
        foreach (var segment in segments)
        {
            sum += segment.CellCount();
        }

        return sum;
    }

    /// <summary>
    /// Returns a new segment without any trailing line endings.
    /// </summary>
    /// <returns>A new segment without any trailing line endings.</returns>
    public Segment StripLineEndings()
    {
        return new Segment(Text.TrimEnd('\n').TrimEnd('\r'), Style);
    }

    /// <summary>
    /// Splits the segment at the offset.
    /// </summary>
    /// <param name="offset">The offset where to split the segment.</param>
    /// <returns>One or two new segments representing the split.</returns>
    public (Segment First, Segment? Second) Split(int offset)
    {
        if (offset < 0)
        {
            return (this, null);
        }

        if (offset >= CellCount())
        {
            return (this, null);
        }

        var index = 0;
        if (offset > 0)
        {
            var accumulated = 0;
#if !NETSTANDARD2_0
            foreach (var rune in Text.EnumerateRunes())
            {
                index += rune.Utf16SequenceLength;
                accumulated += Cell.GetCellLength(rune.Value);
                if (accumulated >= offset)
                {
                    break;
                }
            }
#else
            for (var i = 0; i < Text.Length; i++)
            {
                int codepoint;
                int charCount;
                if (char.IsHighSurrogate(Text[i]) && i + 1 < Text.Length && char.IsLowSurrogate(Text[i + 1]))
                {
                    codepoint = char.ConvertToUtf32(Text[i], Text[i + 1]);
                    charCount = 2;
                    i++;
                }
                else
                {
                    codepoint = Text[i];
                    charCount = 1;
                }

                index += charCount;
                accumulated += Cell.GetCellLength(codepoint);
                if (accumulated >= offset)
                {
                    break;
                }
            }
#endif
        }

        return (
            new Segment(Text.Substring(0, index), Style),
            new Segment(Text.Substring(index, Text.Length - index), Style));
    }

    /// <summary>
    /// Clones the segment.
    /// </summary>
    /// <returns>A new segment that's identical to this one.</returns>
    public Segment Clone()
    {
        return new Segment(Text, Style);
    }

    /// <summary>
    /// Splits the provided segments into lines.
    /// </summary>
    /// <param name="segments">The segments to split.</param>
    /// <returns>A collection of lines.</returns>
    public static List<SegmentLine> SplitLines(IEnumerable<Segment> segments)
    {
        // Stryker disable once all : Equivalent — delegates to 2-arg overload which also checks null
        ArgumentNullException.ThrowIfNull(segments);

        return SplitLines(segments, int.MaxValue);
    }

    /// <summary>
    /// Splits the provided segments into lines with a maximum width.
    /// </summary>
    /// <param name="segments">The segments to split into lines.</param>
    /// <param name="maxWidth">The maximum width.</param>
    /// <param name="height">The height (if any).</param>
    /// <returns>A list of lines.</returns>
    public static List<SegmentLine> SplitLines(IEnumerable<Segment> segments, int maxWidth, int? height = null)
    {
        // Stryker disable once all : Equivalent — LINQ Reverse() also throws ArgumentNullException for null
        ArgumentNullException.ThrowIfNull(segments);

        var lines = new List<SegmentLine>();
        var line = new SegmentLine();

        var stack = new Stack<Segment>(segments.Reverse());

        while (stack.Count > 0)
        {
            var segment = stack.Pop();
            var segmentLength = segment.CellCount();

            // Does this segment make the line exceed the max width?
            var lineLength = line.CellCount();
            if (lineLength + segmentLength > maxWidth)
            {
                var diff = -(maxWidth - (lineLength + segmentLength));
                var offset = segmentLength - diff;

                var (first, second) = segment.Split(offset);

                line.Add(first);
                lines.Add(line);
                line = [];

                if (second != null)
                {
                    stack.Push(second);
                }

                continue;
            }

            // Stryker disable once all : Equivalent — mutating "\n" to "" causes ContainsExact("") to always be true, but downstream SplitLines() handles both paths identically
            // Does the segment contain a newline?
            if (segment.Text.ContainsExact("\n"))
            {
                // Is it a new line?
                if (segment.Text == "\n")
                {
                    if (line.Length != 0 || segment.IsLineBreak)
                    {
                        lines.Add(line);
                        line = [];
                    }

                    continue; // Stryker disable once all : Equivalent — fallthrough re-processes "\n" via SplitLines which produces empty parts, no behavior change
                }

                var text = segment.Text;
                while (text != null)
                {
                    var parts = text.SplitLines();
                    // Stryker disable once all : Equivalent — SplitLines() always returns at least 1 element, so > 0 and >= 0 are both always true
                    if (parts.Length > 0)
                    {
                        if (parts[0].Length > 0)
                        {
                            line.Add(new Segment(parts[0], segment.Style));
                        }
                    }

                    if (parts.Length > 1)
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(line);
                            line = [];
                        }

                        // Stryker disable once all : Equivalent — Take(n+1) returns same elements as Take(n-1) when n==parts.Length, since Skip(1) has fewer than both
                        text = string.Join('\n', parts.Skip(1).Take(parts.Length - 1));
                    }
                    else
                    {
                        text = null;
                    }
                }
            }
            else
            {
                line.Add(segment);
            }
        }

        if (line.Count > 0)
        {
            lines.Add(line);
        }

        // Got a height specified?
        if (height != null)
        {
            // Stryker disable once all : Equivalent — when count==height, RemoveRange(height, 0) is a no-op; > and >= produce identical results
            if (lines.Count >= height)
            {
                // Remove lines
                lines.RemoveRange(height.Value, lines.Count - height.Value);
            }
            else
            {
                // Add lines
                var missing = height - lines.Count;
                for (var i = 0; i < missing; i++)
                {
                    lines.Add([]);
                }
            }
        }

        return lines;
    }

    /// <summary>
    /// Splits an overflowing segment into several new segments.
    /// </summary>
    /// <param name="segment">The segment to split.</param>
    /// <param name="overflow">The overflow strategy to use.</param>
    /// <param name="maxWidth">The maximum width.</param>
    /// <returns>A list of segments that has been split.</returns>
    public static List<Segment> SplitOverflow(Segment segment, Overflow? overflow, int maxWidth)
    {
        ArgumentNullException.ThrowIfNull(segment);

        if (segment.CellCount() <= maxWidth)
        {
            return [segment];
        }

        // Default to folding
        overflow ??= Overflow.Fold;

        var result = new List<Segment>();

        if (overflow == Overflow.Fold)
        {
            var splitted = SplitSegment(segment.Text, maxWidth);
            foreach (var str in splitted)
            {
                result.Add(new Segment(str, segment.Style));
            }
        }
        else if (overflow == Overflow.Crop)
        {
            // Stryker disable once all : Equivalent — at maxWidth==0, Truncate returns null, fallback produces same empty segment
            if (maxWidth <= 0)
            {
                result.Add(new Segment(string.Empty, segment.Style));
            }
            else
            {
                var truncated = Truncate(segment, maxWidth);
                result.Add(truncated ?? new Segment(string.Empty, segment.Style));
            }
        }
        else if (overflow == Overflow.Ellipsis)
        {
            // Stryker disable once all : Equivalent — mutation to maxWidth+1 changes which branch but else also produces "…" for maxWidth<=1
            if (Math.Max(0, maxWidth - 1) == 0)
            {
                result.Add(new Segment("…", segment.Style));
            }
            else
            {
                var truncated = Truncate(segment, maxWidth - 1);
                var prefix = truncated?.Text ?? string.Empty;
                result.Add(new Segment(prefix + "…", segment.Style));
            }
        }

        return result;
    }

    /// <summary>
    /// Truncates the segments to the specified width.
    /// </summary>
    /// <param name="segments">The segments to truncate.</param>
    /// <param name="maxWidth">The maximum width that the segments may occupy.</param>
    /// <returns>A list of segments that has been truncated.</returns>
    public static List<Segment> Truncate(IEnumerable<Segment> segments, int maxWidth)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var result = new List<Segment>();

        var totalWidth = 0;
        foreach (var segment in segments)
        {
            var segmentCellWidth = segment.CellCount();
            if (totalWidth + segmentCellWidth > maxWidth)
            {
                break;
            }

            result.Add(segment);
            totalWidth += segmentCellWidth;
        }

        if (result.Count == 0 && segments.Any())
        {
            // Stryker disable once all : Equivalent — segments.Any() guard ensures sequence is non-empty, so First()==FirstOrDefault()
            var segment = Truncate(segments.First(), maxWidth);
            if (segment != null)
            {
                result.Add(segment);
            }
        }

        return result;
    }

    /// <summary>
    /// Truncates the segment to the specified width.
    /// </summary>
    /// <param name="segment">The segment to truncate.</param>
    /// <param name="maxWidth">The maximum width that the segment may occupy.</param>
    /// <returns>A new truncated segment, or <c>null</c>.</returns>
    public static Segment? Truncate(Segment? segment, int maxWidth)
    {
        if (segment is null)
        {
            return null;
        }

        if (segment.CellCount() <= maxWidth)
        {
            return segment;
        }

        var builder = new StringBuilder();
        var accumulatedCellWidth = 0;
#if !NETSTANDARD2_0
        foreach (var rune in segment.Text.EnumerateRunes())
        {
            var characterWidth = UnicodeCalculator.GetWidth(rune.Value);
            if (accumulatedCellWidth + characterWidth > maxWidth)
            {
                break;
            }

            builder.Append(rune.ToString());
            accumulatedCellWidth += characterWidth;
        }
#else
        for (var i = 0; i < segment.Text.Length; i++)
        {
            int codepoint;
            if (char.IsHighSurrogate(segment.Text[i]) && i + 1 < segment.Text.Length && char.IsLowSurrogate(segment.Text[i + 1]))
            {
                codepoint = char.ConvertToUtf32(segment.Text[i], segment.Text[i + 1]);
            }
            else
            {
                codepoint = segment.Text[i];
            }

            var characterWidth = UnicodeCalculator.GetWidth(codepoint);
            if (accumulatedCellWidth + characterWidth > maxWidth)
            {
                break;
            }

            builder.Append(segment.Text[i]);
            if (codepoint > char.MaxValue)
            {
                builder.Append(segment.Text[++i]);
            }

            accumulatedCellWidth += characterWidth;
        }
#endif

        if (builder.Length == 0)
        {
            return null;
        }

        return new Segment(builder.ToString(), segment.Style);
    }

    internal static IEnumerable<Segment> Merge(IEnumerable<Segment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var segmentBuilder = (SegmentBuilder?)null;
        foreach (var segment in segments)
        {
            if (segmentBuilder == null)
            {
                segmentBuilder = new SegmentBuilder(segment);
                continue;
            }

            // Both control codes?
            if (segment.IsControlCode && segmentBuilder.IsControlCode())
            {
                segmentBuilder.Append(segment.Text);
                continue;
            }

            // Same style?
            if (segmentBuilder.StyleEquals(segment.Style) && !segmentBuilder.IsLineBreak() &&
                !segmentBuilder.IsControlCode())
            {
                segmentBuilder.Append(segment.Text);
                continue;
            }

            yield return segmentBuilder.Build();
            segmentBuilder.Reset(segment);
        }

        if (segmentBuilder != null)
        {
            yield return segmentBuilder.Build();
        }
    }

    internal static List<Segment> TruncateWithEllipsis(IEnumerable<Segment> segments, int maxWidth)
    {
        // Stryker disable once all : Equivalent — CellCount() at next line also has ThrowIfNull
        ArgumentNullException.ThrowIfNull(segments);

        if (CellCount(segments) <= maxWidth)
        {
            return [.. segments];
        }

        segments = TrimEnd(Truncate(segments, maxWidth - 1));
        if (!segments.Any())
        {
            return new List<Segment>(1);
        }

        var result = new List<Segment>(segments);
        result.Add(new Segment("…", result.Last().Style));
        return result;
    }

    internal static List<Segment> TrimEnd(IEnumerable<Segment> segments)
    {
        // Stryker disable once all : Equivalent — LINQ Reverse() also throws ArgumentNullException for null
        ArgumentNullException.ThrowIfNull(segments);

        var stack = new Stack<Segment>();
        var checkForWhitespace = true;
        foreach (var segment in segments.Reverse())
        {
            if (checkForWhitespace)
            {
                if (segment.IsWhiteSpace)
                {
                    continue;
                }

                checkForWhitespace = false;
            }

            stack.Push(segment);
        }

        return stack.ToList();
    }

    // TODO: Move this to Table
    internal static List<List<SegmentLine>> MakeSameHeight(int cellHeight, List<List<SegmentLine>> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        foreach (var cell in cells)
        {
            // Stryker disable once all : Equivalent — when count==cellHeight, <= enters if but while(count!=cellHeight) is false, no change; when count>cellHeight, both < and <= are false
            if (cell.Count < cellHeight)
            {
                while (cell.Count != cellHeight)
                {
                    cell.Add([]);
                }
            }
        }

        return cells;
    }

    internal static List<SegmentLine> MakeWidth(int expectedWidth, List<SegmentLine> lines)
    {
        foreach (var line in lines)
        {
            var width = line.CellCount();
            if (width < expectedWidth)
            {
                var diff = expectedWidth - width;
                line.Add(new Segment(new string(' ', diff)));
            }
        }

        return lines;
    }

    internal static List<string> SplitSegment(string text, int maxCellLength)
    {
        var list = new List<string>();

        var length = 0;
        var sb = new StringBuilder();
#if !NETSTANDARD2_0
        foreach (var rune in text.EnumerateRunes())
        {
            var w = UnicodeCalculator.GetWidth(rune.Value);
            if (length + w > maxCellLength)
            {
                list.Add(sb.ToString());
                sb.Clear();
                length = 0;
            }

            length += w;
            sb.Append(rune.ToString());
        }
#else
        for (var i = 0; i < text.Length; i++)
        {
            int codepoint;
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codepoint = char.ConvertToUtf32(text[i], text[i + 1]);
            }
            else
            {
                codepoint = text[i];
            }

            var w = UnicodeCalculator.GetWidth(codepoint);
            if (length + w > maxCellLength)
            {
                list.Add(sb.ToString());
                sb.Clear();
                length = 0;
            }

            length += w;
            sb.Append(text[i]);
            if (codepoint > char.MaxValue)
            {
                sb.Append(text[++i]);
            }
        }
#endif

        list.Add(sb.ToString());

        return list;
    }

    private class SegmentBuilder
    {
        private readonly StringBuilder _textBuilder = new();
        private Segment _originalSegment;

        public SegmentBuilder(Segment originalSegment)
        {
            _originalSegment = originalSegment;
            Reset(originalSegment);
        }

        public bool IsControlCode() => _originalSegment.IsControlCode;
        public bool IsLineBreak() => _originalSegment.IsLineBreak;
        public bool StyleEquals(Style segmentStyle) => segmentStyle.Equals(_originalSegment.Style);

        public void Append(string text)
        {
            _textBuilder.Append(text);
        }

        public Segment Build()
        {
            return new Segment(
                _textBuilder.ToString(),
                _originalSegment.Style,
                _originalSegment.Link,
                _originalSegment.IsLineBreak,
                _originalSegment.IsControlCode);
        }

        public void Reset(Segment segment)
        {
            _textBuilder.Clear();
            _textBuilder.Append(segment.Text);
            _originalSegment = segment;
        }
    }
}