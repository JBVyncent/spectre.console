namespace Spectre.Console.Markdown.Syntax;

/// <summary>
/// Base class for all inline Markdown elements.
/// </summary>
internal abstract class MarkdownInline
{
}

/// <summary>
/// Plain text content.
/// </summary>
internal sealed class TextInline : MarkdownInline
{
    public string Text { get; }

    public TextInline(string text)
    {
        Text = text;
    }
}

/// <summary>
/// Emphasized text (italic or bold).
/// </summary>
internal sealed class EmphasisInline : MarkdownInline
{
    public bool Strong { get; }
    public List<MarkdownInline> Children { get; }

    public EmphasisInline(bool strong, List<MarkdownInline> children)
    {
        Strong = strong;
        Children = children;
    }
}

/// <summary>
/// An inline code span.
/// </summary>
internal sealed class CodeSpanInline : MarkdownInline
{
    public string Code { get; }

    public CodeSpanInline(string code)
    {
        Code = code;
    }
}

/// <summary>
/// A hyperlink.
/// </summary>
internal sealed class LinkInline : MarkdownInline
{
    public string Url { get; }
    public List<MarkdownInline> Children { get; }

    public LinkInline(string url, List<MarkdownInline> children)
    {
        Url = url;
        Children = children;
    }
}

/// <summary>
/// Strikethrough text.
/// </summary>
internal sealed class StrikethroughInline : MarkdownInline
{
    public List<MarkdownInline> Children { get; }

    public StrikethroughInline(List<MarkdownInline> children)
    {
        Children = children;
    }
}
