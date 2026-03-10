using Spectre.Console;
using Spectre.Console.Markdown;

namespace Gallery.Demos.Markdown;

public sealed class MarkdownDemo : IDemoModule
{
    public string Name => "Markdown";
    public string Description => "Render Markdown text as rich console output";

    public void Run()
    {
        var markdown = @"# Welcome to Spectre.Console Markdown

This is a **rich** Markdown renderer for your terminal. It supports
*italic*, **bold**, and ***bold italic*** text formatting.

## Features

- Headings (H1 through H6)
- **Bold** and *italic* text
- `Inline code` spans
- Fenced code blocks with language headers
- Ordered and unordered lists
- Blockquotes
- Horizontal rules
- [Links](https://spectreconsole.net)
- ~~Strikethrough~~ text

### Code Block Example

```csharp
var table = new Table()
    .AddColumn(""Name"")
    .AddColumn(""Value"");
table.AddRow(""Hello"", ""World"");
AnsiConsole.Write(table);
```

### Ordered List

1. First item
2. Second item
3. Third item

> This is a blockquote. It can contain **formatted** text
> and spans multiple lines.

---

#### Styled Markdown

You can customize colors for headings, code, links, and more!";

        var md = new MarkdownText(markdown);
        AnsiConsole.Write(md);
    }
}
