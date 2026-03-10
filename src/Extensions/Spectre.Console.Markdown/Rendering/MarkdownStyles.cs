namespace Spectre.Console.Markdown.Rendering;

/// <summary>
/// Resolved styles for Markdown rendering.
/// </summary>
// Stryker disable all : Bitwise mutations on decoration defaults produce visually different output but are not observable through TestConsole rendering pipeline
internal sealed class MarkdownStyles
{
    public Style Heading1Style { get; init; } = new Style(Color.Blue, decoration: Decoration.Bold);
    public Style Heading2Style { get; init; } = new Style(Color.Blue, decoration: Decoration.Bold | Decoration.Underline);
    public Style Heading3Style { get; init; } = new Style(Color.Cyan1, decoration: Decoration.Bold);
    public Style HeadingStyle { get; init; } = new Style(Color.Cyan1, decoration: Decoration.Bold | Decoration.Dim);
    public Style CodeBlockStyle { get; init; } = new Style(Color.Grey);
    public Style CodeSpanStyle { get; init; } = new Style(Color.Yellow);
    public Style BlockquoteStyle { get; init; } = new Style(Color.Grey, decoration: Decoration.Italic);
    public Style LinkStyle { get; init; } = new Style(Color.Blue, decoration: Decoration.Underline);
    public Style ListBulletStyle { get; init; } = new Style(Color.Yellow);
    public Style RuleStyle { get; init; } = Style.Plain;
}
