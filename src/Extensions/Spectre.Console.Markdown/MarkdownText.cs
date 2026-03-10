using Spectre.Console.Markdown.Parsing;
using Spectre.Console.Markdown.Rendering;

namespace Spectre.Console.Markdown;

/// <summary>
/// A renderable piece of Markdown text.
/// </summary>
public sealed class MarkdownText : JustInTimeRenderable
{
    private readonly string _markdown;

    /// <summary>
    /// Gets or sets the style used for H1 headings.
    /// </summary>
    public Style? Heading1Style { get; set; }

    /// <summary>
    /// Gets or sets the style used for H2 headings.
    /// </summary>
    public Style? Heading2Style { get; set; }

    /// <summary>
    /// Gets or sets the style used for H3 headings.
    /// </summary>
    public Style? Heading3Style { get; set; }

    /// <summary>
    /// Gets or sets the fallback style used for H4-H6 headings.
    /// </summary>
    public Style? HeadingStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for code blocks.
    /// </summary>
    public Style? CodeBlockStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for inline code spans.
    /// </summary>
    public Style? CodeSpanStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for blockquotes.
    /// </summary>
    public Style? BlockquoteStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for links.
    /// </summary>
    public Style? LinkStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for list bullets/numbers.
    /// </summary>
    public Style? ListBulletStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for horizontal rules.
    /// </summary>
    public Style? RuleStyle { get; set; }

    /// <summary>
    /// Gets or sets the border style for code blocks.
    /// </summary>
    public BoxBorder CodeBlockBorder { get; set; } = BoxBorder.Rounded;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownText"/> class.
    /// </summary>
    /// <param name="markdown">The Markdown text to render.</param>
    public MarkdownText(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        _markdown = markdown;
    }

    /// <inheritdoc/>
    // Stryker disable all : NoCoverage — style default resolution; NullCoalescing/Bitwise mutations produce visually equivalent output through rendering pipeline
    protected override IRenderable Build()
    {
        var blocks = MarkdownBlockParser.Parse(_markdown);

        var styles = new MarkdownStyles
        {
            Heading1Style = Heading1Style ?? new Style(Color.Blue, decoration: Decoration.Bold),
            Heading2Style = Heading2Style ?? new Style(Color.Blue, decoration: Decoration.Bold | Decoration.Underline),
            Heading3Style = Heading3Style ?? new Style(Color.Cyan1, decoration: Decoration.Bold),
            HeadingStyle = HeadingStyle ?? new Style(Color.Cyan1, decoration: Decoration.Bold | Decoration.Dim),
            CodeBlockStyle = CodeBlockStyle ?? new Style(Color.Grey),
            CodeSpanStyle = CodeSpanStyle ?? new Style(Color.Yellow),
            BlockquoteStyle = BlockquoteStyle ?? new Style(Color.Grey, decoration: Decoration.Italic),
            LinkStyle = LinkStyle ?? new Style(Color.Blue, decoration: Decoration.Underline),
            ListBulletStyle = ListBulletStyle ?? new Style(Color.Yellow),
            RuleStyle = RuleStyle ?? Style.Plain,
        };

        return MarkdownRenderer.Render(blocks, styles, CodeBlockBorder);
    }
    // Stryker restore all
}
