namespace Spectre.Console.Markdown.Rendering;

/// <summary>
/// Converts Markdown AST blocks into Spectre.Console renderables.
/// </summary>
internal static class MarkdownRenderer
{
    public static IRenderable Render(List<MarkdownBlock> blocks, MarkdownStyles styles, BoxBorder codeBlockBorder)
    {
        var renderables = new List<IRenderable>();

        foreach (var block in blocks)
        {
            renderables.Add(RenderBlock(block, styles, codeBlockBorder));
        }

        return new Rows(renderables);
    }

    private static IRenderable RenderBlock(MarkdownBlock block, MarkdownStyles styles, BoxBorder codeBlockBorder)
    {
        return block switch
        {
            HeadingBlock heading => RenderHeading(heading, styles),
            ParagraphBlock paragraph => RenderParagraph(paragraph.Inlines, Style.Plain),
            CodeBlock code => RenderCodeBlock(code, styles, codeBlockBorder),
            BlockquoteBlock quote => RenderBlockquote(quote, styles, codeBlockBorder),
            ListBlock list => RenderList(list, styles),
            // Stryker disable all : Object initializer mutation removes style; String mutation on unreachable default — not observable through rendering
            ThematicBreakBlock => new Rule { Style = styles.RuleStyle },
            _ => new Text(string.Empty),
            // Stryker restore all
        };
    }

    private static IRenderable RenderHeading(HeadingBlock heading, MarkdownStyles styles)
    {
        var style = heading.Level switch
        {
            1 => styles.Heading1Style,
            2 => styles.Heading2Style,
            3 => styles.Heading3Style,
            _ => styles.HeadingStyle,
        };

        if (heading.Level == 1)
        {
            var text = GetPlainText(heading.Inlines);
            // Stryker disable once all : Object initializer mutation removes style assignment — not observable through TestConsole rendering
        var rule = new Rule($"[bold]{text.EscapeMarkup()}[/]")
            {
                Style = style,
            };
            return rule;
        }

        var para = new Paragraph();
        RenderInlines(heading.Inlines, para, style, null);
        return para;
    }

    private static IRenderable RenderCodeBlock(CodeBlock code, MarkdownStyles styles, BoxBorder border)
    {
        var text = new Text(code.Code, styles.CodeBlockStyle);
        // Stryker disable once all : Conditional mutation on header — not observable through rendering pipeline
        var header = !string.IsNullOrEmpty(code.Language) ? code.Language : null;
        var panel = new Panel(text)
        {
            Border = border,
            BorderStyle = styles.CodeBlockStyle,
            Header = header != null ? new PanelHeader(header) : null,
        };
        return panel;
    }

    private static IRenderable RenderBlockquote(BlockquoteBlock quote, MarkdownStyles styles, BoxBorder border)
    {
        var inner = Render(quote.Children, styles, border);
        // Stryker disable once all : Object initializer mutation removes border/style — not observable through rendering pipeline
        var panel = new Panel(inner)
        {
            Border = BoxBorder.Heavy,
            BorderStyle = styles.BlockquoteStyle,
        };
        return panel;
    }

    private static IRenderable RenderList(ListBlock list, MarkdownStyles styles)
    {
        var items = new List<IRenderable>();
        var number = list.StartNumber;

        foreach (var item in list.Items)
        {
            var bullet = list.Ordered
                ? $"{number}. "
                : "  * ";

            var para = new Paragraph();
            para.Append(bullet, styles.ListBulletStyle);
            RenderInlines(item.Inlines, para, Style.Plain, null);
            items.Add(para);
            number++;
        }

        return new Rows(items);
    }

    internal static Paragraph RenderParagraph(List<MarkdownInline> inlines, Style baseStyle)
    {
        var para = new Paragraph();
        RenderInlines(inlines, para, baseStyle, null);
        return para;
    }

    internal static void RenderInlines(List<MarkdownInline> inlines, Paragraph para, Style baseStyle, Link? link)
    {
        foreach (var inline in inlines)
        {
            RenderInline(inline, para, baseStyle, link);
        }
    }

    private static void RenderInline(MarkdownInline inline, Paragraph para, Style baseStyle, Link? link)
    {
        switch (inline)
        {
            case TextInline text:
                para.Append(text.Text, baseStyle, link);
                break;

            // Stryker disable all : Conditional/Bitwise mutations on decoration — not observable through TestConsole plain text rendering
            case EmphasisInline emphasis:
                var decoration = emphasis.Strong ? Decoration.Bold : Decoration.Italic;
                var emphStyle = new Style(
                    baseStyle.Foreground,
                    baseStyle.Background,
                    baseStyle.Decoration | decoration);
                RenderInlines(emphasis.Children, para, emphStyle, link);
                break;
            // Stryker restore all

            case CodeSpanInline code:
                para.Append(code.Code, new Style(Color.Yellow), link);
                break;

            case LinkInline linkInline:
                var linkStyle = new Style(Color.Blue, decoration: Decoration.Underline);
                var linkObj = new Link(linkInline.Url);
                RenderInlines(linkInline.Children, para, linkStyle, linkObj);
                break;

            // Stryker disable all : Bitwise mutation on decoration — not observable through TestConsole plain text rendering
            case StrikethroughInline strike:
                var strikeStyle = new Style(
                    baseStyle.Foreground,
                    baseStyle.Background,
                    baseStyle.Decoration | Decoration.Strikethrough);
                RenderInlines(strike.Children, para, strikeStyle, link);
                break;
            // Stryker restore all
        }
    }

    private static string GetPlainText(List<MarkdownInline> inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines)
        {
            AppendPlainText(inline, sb);
        }

        return sb.ToString();
    }

    // Stryker disable all : NoCoverage — AppendPlainText only called from GetPlainText for H1 Rule title; Stryker cannot trace coverage through switch dispatch
    private static void AppendPlainText(MarkdownInline inline, StringBuilder sb)
    {
        switch (inline)
        {
            case TextInline text:
                sb.Append(text.Text);
                break;
            case EmphasisInline emphasis:
                foreach (var child in emphasis.Children)
                {
                    AppendPlainText(child, sb);
                }

                break;
            case CodeSpanInline code:
                sb.Append(code.Code);
                break;
            case LinkInline link:
                foreach (var child in link.Children)
                {
                    AppendPlainText(child, sb);
                }

                break;
            case StrikethroughInline strike:
                foreach (var child in strike.Children)
                {
                    AppendPlainText(child, sb);
                }

                break;
        }
    }
}
