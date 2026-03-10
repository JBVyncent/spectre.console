using Spectre.Console;
using Spectre.Console.Markdown;

AnsiConsole.Write(new FigletText("Markdown")
    .Color(Color.Cyan1)
    .Centered());
AnsiConsole.Write(new Rule("[grey]Spectre.Console Markdown Renderer[/]").RuleStyle(Style.Parse("cyan")));
AnsiConsole.WriteLine();

var markdown = @"# Markdown Rendering Demo

Welcome to the **Spectre.Console.Markdown** extension! This widget renders
Markdown text as rich, styled console output using existing Spectre.Console
rendering primitives.

## Inline Formatting

You can use **bold**, *italic*, ***bold italic***, `inline code`,
~~strikethrough~~, and [hyperlinks](https://spectreconsole.net).

## Lists

### Unordered
- First item
- Second item with **bold**
- Third item with `code`

### Ordered
1. Step one
2. Step two
3. Step three

## Code Blocks

```csharp
// A beautifully rendered code block
var markdown = new MarkdownText(content);
AnsiConsole.Write(markdown);
```

## Blockquotes

> Spectre.Console makes terminal apps beautiful.
> This blockquote is rendered inside a heavy-bordered panel.

---

## Custom Styling

The `MarkdownText` widget supports custom styles for all elements:";

AnsiConsole.Write(new MarkdownText(markdown));
AnsiConsole.WriteLine();

// Show with custom styles
AnsiConsole.Write(new Rule("[bold cyan]Custom Styled Markdown[/]").RuleStyle(Style.Parse("grey")));
AnsiConsole.WriteLine();

var customMarkdown = @"# Custom Colors

This **bold text** and *italic text* use custom heading colors.

- Bullet with custom style
- Another item

```python
print('Custom code block border')
```";

var customMd = new MarkdownText(customMarkdown)
{
    Heading1Style = new Style(Color.Green, decoration: Decoration.Bold),
    Heading2Style = new Style(Color.Yellow, decoration: Decoration.Bold),
    CodeBlockStyle = new Style(Color.Aqua),
    ListBulletStyle = new Style(Color.Green),
    CodeBlockBorder = BoxBorder.Heavy,
};

AnsiConsole.Write(customMd);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[bold cyan]Demo complete![/]");
