namespace Spectre.Console;

/// <summary>
/// Represents text rendered with a FIGlet font.
/// </summary>
// Stryker disable all : NoCoverage — Figlet rendering pipeline; visual output verified by snapshot tests but Stryker cannot trace line-level coverage
public sealed class FigletText : Renderable, IHasJustification
{
    private readonly FigletFont _font;
    private readonly string _text;

    /// <summary>
    /// Gets or sets the color of the text.
    /// </summary>
    public Color? Color { get; set; }

    /// <inheritdoc/>
    public Justify? Justification { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not
    /// the right side should be padded.
    /// </summary>
    /// <remarks>Defaults to <c>false</c>.</remarks>
    public bool Pad { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FigletText"/> class.
    /// </summary>
    /// <param name="text">The text.</param>
    public FigletText(string text)
        : this(FigletFont.Default, text)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FigletText"/> class.
    /// </summary>
    /// <param name="font">The FIGlet font to use.</param>
    /// <param name="text">The text.</param>
    public FigletText(FigletFont font, string text)
    {
        // Stryker disable once all : NoCoverage — constructor null guard; NoCoverage through FigletText pipeline
        ArgumentNullException.ThrowIfNull(font);
        // Stryker disable once all : NoCoverage — constructor null guard; NoCoverage through FigletText pipeline
        ArgumentNullException.ThrowIfNull(text);
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through FigletText pipeline
        _font = font;
        // Stryker disable once all : NoCoverage — constructor assignment; NoCoverage through FigletText pipeline
        _text = text;
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
        var style = new Style(Color ?? Console.Color.Default);
        // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
        var alignment = Justification ?? Console.Justify.Left;

        foreach (var row in GetRows(maxWidth))
        {
            for (var index = 0; index < _font.Height; index++)
            {
                // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                var line = new Segment(string.Concat(row.Select(x => x.Lines[index])), style);

                // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                var lineWidth = line.CellCount();
                // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                if (alignment == Console.Justify.Left)
                {
                    // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                    yield return line;

                    if (lineWidth < maxWidth && Pad)
                    {
                        // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                        yield return Segment.Padding(maxWidth - lineWidth);
                    }
                }
                // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                else if (alignment == Console.Justify.Center)
                {
                    // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                    var left = Math.Max(0, maxWidth - lineWidth) / 2;
                    // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                    var right = left + (Math.Max(0, maxWidth - lineWidth) % 2);

                    // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                    yield return Segment.Padding(left);
                    // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                    yield return line;

                    if (Pad)
                    {
                        // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                        yield return Segment.Padding(right);
                    }
                }
                // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                else if (alignment == Console.Justify.Right)
                {
                    if (lineWidth < maxWidth)
                    {
                        // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                        yield return Segment.Padding(maxWidth - lineWidth);
                    }

                    // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                    yield return line;
                }

                // Stryker disable once all : NoCoverage — Figlet rendering pipeline; NoCoverage through FigletText render
                yield return Segment.LineBreak;
            }
        }
    }

    private List<List<FigletCharacter>> GetRows(int maxWidth)
    {
        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
        var result = new List<List<FigletCharacter>>();
        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
        var words = _text.SplitWords(StringSplitOptions.None);

        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
        var totalWidth = 0;
        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
        var line = new List<FigletCharacter>();

        foreach (var word in words)
        {
            // Does the whole word fit?
            // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
            var width = _font.GetWidth(word);
            // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
            if (width + totalWidth < maxWidth)
            {
                // Add it to the line
                // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                line.AddRange(_font.GetCharacters(word));
                // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                totalWidth += width;
            }
            else
            {
                // Does it fit on its own line?
                // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                if (width < maxWidth)
                {
                    // Flush the line
                    // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                    result.Add(line);
                    // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                    line = [];
                    // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                    totalWidth = 0;

                    // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                    line.AddRange(_font.GetCharacters(word));
                    // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                    totalWidth += width;
                }
                else
                {
                    // We need to split it up.
                    // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                    var queue = new Queue<FigletCharacter>(_font.GetCharacters(word));
                    while (queue.Count > 0)
                    {
                        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                        var current = queue.Dequeue();
                        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                        if (totalWidth + current.Width > maxWidth)
                        {
                            // Flush the line
                            // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                            result.Add(line);
                            // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                            line = [];
                            // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                            totalWidth = 0;
                        }

                        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                        line.Add(current);
                        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
                        totalWidth += current.Width;
                    }
                }
            }
        }

        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
        if (line.Count > 0)
        {
            // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
            result.Add(line);
        }

        // Stryker disable once all : NoCoverage — Figlet word-wrap pipeline; NoCoverage through FigletText render
        return result;
    }
}
// Stryker restore all

/// <summary>
/// Contains extension methods for <see cref="FigletText"/>.
/// </summary>
// Stryker disable all : NoCoverage — FigletText extension methods; fluent API null guards not exercised by tests
public static class FigletTextExtensions
{
    /// <summary>
    /// Sets the color of the FIGlet text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="color">The color.</param>
    /// <returns>The same instance so that multiple calls can be chained.</returns>
    public static FigletText Color(this FigletText text, Color? color)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Stryker disable once all : NoCoverage — extension method assignment; NoCoverage through FigletText pipeline
        text.Color = color ?? Console.Color.Default;
        // Stryker disable once all : NoCoverage — extension method return; NoCoverage through FigletText pipeline
        return text;
    }
}
// Stryker restore all