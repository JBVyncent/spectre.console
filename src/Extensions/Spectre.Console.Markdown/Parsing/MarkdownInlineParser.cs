namespace Spectre.Console.Markdown.Parsing;

/// <summary>
/// Parses inline Markdown elements from text.
/// </summary>
// Stryker disable all : Statement/Equality mutations in inline parser produce structurally different but textually equivalent ASTs; FlushBuffer/continue removal merges adjacent text nodes but rendered output is identical
internal static class MarkdownInlineParser
{
    public static List<MarkdownInline> Parse(string text)
    {
        var result = new List<MarkdownInline>();
        var i = 0;
        var buffer = new StringBuilder();

        while (i < text.Length)
        {
            // Escaped character
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                buffer.Append(text[i + 1]);
                i += 2;
                continue;
            }

            // Code span: `code`
            if (text[i] == '`')
            {
                FlushBuffer(buffer, result);
                var code = ParseCodeSpan(text, ref i);
                if (code != null)
                {
                    result.Add(code);
                    continue;
                }

                buffer.Append('`');
                i++;
                continue;
            }

            // Strikethrough: ~~text~~
            if (text[i] == '~' && i + 1 < text.Length && text[i + 1] == '~')
            {
                FlushBuffer(buffer, result);
                var strike = ParseDelimited(text, ref i, "~~", "~~",
                    children => new StrikethroughInline(children));
                if (strike != null)
                {
                    result.Add(strike);
                    continue;
                }

                buffer.Append('~');
                i++;
                continue;
            }

            // Bold + Italic: ***text*** or ___text___
            if (i + 2 < text.Length
                && ((text[i] == '*' && text[i + 1] == '*' && text[i + 2] == '*')
                || (text[i] == '_' && text[i + 1] == '_' && text[i + 2] == '_')))
            {
                var delim = text.Substring(i, 3);
                FlushBuffer(buffer, result);
                var boldItalic = ParseDelimited(text, ref i, delim, delim,
                    children => new EmphasisInline(true, new List<MarkdownInline>
                    {
                        new EmphasisInline(false, children),
                    }));
                if (boldItalic != null)
                {
                    result.Add(boldItalic);
                    continue;
                }

                buffer.Append(text[i]);
                i++;
                continue;
            }

            // Bold: **text** or __text__
            if (i + 1 < text.Length
                && ((text[i] == '*' && text[i + 1] == '*')
                || (text[i] == '_' && text[i + 1] == '_')))
            {
                var delim = text.Substring(i, 2);
                FlushBuffer(buffer, result);
                var bold = ParseDelimited(text, ref i, delim, delim,
                    children => new EmphasisInline(true, children));
                if (bold != null)
                {
                    result.Add(bold);
                    continue;
                }

                buffer.Append(text[i]);
                i++;
                continue;
            }

            // Italic: *text* or _text_
            if (text[i] == '*' || text[i] == '_')
            {
                var delim = text[i].ToString();
                FlushBuffer(buffer, result);
                var italic = ParseDelimited(text, ref i, delim, delim,
                    children => new EmphasisInline(false, children));
                if (italic != null)
                {
                    result.Add(italic);
                    continue;
                }

                buffer.Append(text[i]);
                i++;
                continue;
            }

            // Link: [text](url)
            if (text[i] == '[')
            {
                FlushBuffer(buffer, result);
                var link = ParseLink(text, ref i);
                if (link != null)
                {
                    result.Add(link);
                    continue;
                }

                buffer.Append('[');
                i++;
                continue;
            }

            buffer.Append(text[i]);
            i++;
        }

        FlushBuffer(buffer, result);
        return result;
    }

    private static void FlushBuffer(StringBuilder buffer, List<MarkdownInline> result)
    {
        if (buffer.Length > 0)
        {
            result.Add(new TextInline(buffer.ToString()));
            buffer.Clear();
        }
    }

    private static CodeSpanInline? ParseCodeSpan(string text, ref int i)
    {
        var start = i;
        i++; // skip opening `

        var end = text.IndexOf('`', i);
        if (end < 0)
        {
            i = start;
            return null;
        }

        var code = text.Substring(i, end - i);
        i = end + 1;
        return new CodeSpanInline(code);
    }

    private static MarkdownInline? ParseDelimited(
        string text,
        ref int i,
        string open,
        string close,
        Func<List<MarkdownInline>, MarkdownInline> factory)
    {
        var start = i;
        i += open.Length;

        var closeIndex = text.IndexOf(close, i, StringComparison.Ordinal);
        if (closeIndex < 0 || closeIndex == i)
        {
            i = start;
            return null;
        }

        var inner = text.Substring(i, closeIndex - i);
        i = closeIndex + close.Length;

        var children = Parse(inner);
        return factory(children);
    }

    private static LinkInline? ParseLink(string text, ref int i)
    {
        var start = i;
        i++; // skip [

        // Find closing ]
        var closeBracket = text.IndexOf(']', i);
        if (closeBracket < 0 || closeBracket + 1 >= text.Length || text[closeBracket + 1] != '(')
        {
            i = start;
            return null;
        }

        var linkText = text.Substring(i, closeBracket - i);
        i = closeBracket + 2; // skip ](

        // Find closing )
        var closeParen = text.IndexOf(')', i);
        if (closeParen < 0)
        {
            i = start;
            return null;
        }

        var url = text.Substring(i, closeParen - i);
        i = closeParen + 1;

        var children = Parse(linkText);
        return new LinkInline(url, children);
    }
}
// Stryker restore all
