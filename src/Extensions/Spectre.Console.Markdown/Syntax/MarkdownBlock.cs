namespace Spectre.Console.Markdown.Syntax;

/// <summary>
/// Base class for all block-level Markdown elements.
/// </summary>
internal abstract class MarkdownBlock
{
}

/// <summary>
/// A heading (h1-h6).
/// </summary>
internal sealed class HeadingBlock : MarkdownBlock
{
    public int Level { get; }
    public List<MarkdownInline> Inlines { get; }

    public HeadingBlock(int level, List<MarkdownInline> inlines)
    {
        Level = level;
        Inlines = inlines;
    }
}

/// <summary>
/// A paragraph of text.
/// </summary>
internal sealed class ParagraphBlock : MarkdownBlock
{
    public List<MarkdownInline> Inlines { get; }

    public ParagraphBlock(List<MarkdownInline> inlines)
    {
        Inlines = inlines;
    }
}

/// <summary>
/// A fenced or indented code block.
/// </summary>
internal sealed class CodeBlock : MarkdownBlock
{
    public string? Language { get; }
    public string Code { get; }

    public CodeBlock(string? language, string code)
    {
        Language = language;
        Code = code;
    }
}

/// <summary>
/// A blockquote.
/// </summary>
internal sealed class BlockquoteBlock : MarkdownBlock
{
    public List<MarkdownBlock> Children { get; }

    public BlockquoteBlock(List<MarkdownBlock> children)
    {
        Children = children;
    }
}

/// <summary>
/// A list (ordered or unordered).
/// </summary>
internal sealed class ListBlock : MarkdownBlock
{
    public bool Ordered { get; }
    public int StartNumber { get; }
    public List<ListItemBlock> Items { get; }

    public ListBlock(bool ordered, int startNumber, List<ListItemBlock> items)
    {
        Ordered = ordered;
        StartNumber = startNumber;
        Items = items;
    }
}

/// <summary>
/// A single list item.
/// </summary>
internal sealed class ListItemBlock : MarkdownBlock
{
    public List<MarkdownInline> Inlines { get; }

    public ListItemBlock(List<MarkdownInline> inlines)
    {
        Inlines = inlines;
    }
}

/// <summary>
/// A thematic break (horizontal rule).
/// </summary>
internal sealed class ThematicBreakBlock : MarkdownBlock
{
}
