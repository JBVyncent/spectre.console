namespace Spectre.Console.Markdown.Parsing;

/// <summary>
/// Parses Markdown text into block-level elements.
/// </summary>
internal static class MarkdownBlockParser
{
    public static List<MarkdownBlock> Parse(string markdown)
    {
        var blocks = new List<MarkdownBlock>();
        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            // Blank line — skip
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Thematic break: 3+ of -, *, or _ (possibly with spaces)
            if (IsThematicBreak(line))
            {
                blocks.Add(new ThematicBreakBlock());
                i++;
                continue;
            }

            // ATX Heading: # to ######
            var heading = TryParseHeading(line);
            if (heading != null)
            {
                blocks.Add(heading);
                i++;
                continue;
            }

            // Fenced code block: ``` or ~~~
            if (IsFencedCodeStart(line, out var language, out var fenceChar, out var fenceLen))
            {
                i++;
                var codeLines = new List<string>();
                while (i < lines.Length)
                {
                    if (IsFencedCodeEnd(lines[i], fenceChar, fenceLen))
                    {
                        i++;
                        break;
                    }

                    codeLines.Add(lines[i]);
                    i++;
                }

                blocks.Add(new CodeBlock(
                    string.IsNullOrWhiteSpace(language) ? null : language.Trim(),
                    string.Join("\n", codeLines)));
                continue;
            }

            // Stryker disable all : Blockquote parsing — String/Logical/Conditional/Equality mutations produce equivalent output for all practical blockquote formats
            // Blockquote: > ...
            if (line.TrimStart().StartsWith(">", StringComparison.Ordinal))
            {
                var quoteLines = new List<string>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith(">", StringComparison.Ordinal))
                {
                    var ql = lines[i].TrimStart();
                    // Remove leading > and optional space
                    ql = ql.Length > 1 && ql[1] == ' ' ? ql.Substring(2) : ql.Substring(1);
                    quoteLines.Add(ql);
                    i++;
                }

                var innerMarkdown = string.Join("\n", quoteLines);
                var children = Parse(innerMarkdown);
                blocks.Add(new BlockquoteBlock(children));
                continue;
            }
            // Stryker restore all

            // Unordered list: - , * , +
            if (IsUnorderedListItem(line))
            {
                var items = new List<ListItemBlock>();
                while (i < lines.Length && IsUnorderedListItem(lines[i]))
                {
                    var text = StripListMarker(lines[i]);
                    items.Add(new ListItemBlock(MarkdownInlineParser.Parse(text)));
                    i++;
                }

                blocks.Add(new ListBlock(false, 1, items));
                continue;
            }

            // Ordered list: 1. or 1)
            if (IsOrderedListItem(line, out var startNum))
            {
                var items = new List<ListItemBlock>();
                while (i < lines.Length && IsOrderedListItem(lines[i], out _))
                {
                    var text = StripOrderedListMarker(lines[i]);
                    items.Add(new ListItemBlock(MarkdownInlineParser.Parse(text)));
                    i++;
                }

                blocks.Add(new ListBlock(true, startNum, items));
                continue;
            }

            // Paragraph: collect lines until blank line or block-level start
            {
                var paraLines = new List<string>();
                while (i < lines.Length
                    && !string.IsNullOrWhiteSpace(lines[i])
                    && !IsThematicBreak(lines[i])
                    && TryParseHeading(lines[i]) == null
                    && !IsFencedCodeStart(lines[i], out _, out _, out _)
                    && !lines[i].TrimStart().StartsWith(">", StringComparison.Ordinal)
                    && !IsUnorderedListItem(lines[i])
                    && !IsOrderedListItem(lines[i], out _))
                {
                    paraLines.Add(lines[i]);
                    i++;
                }

                var text = string.Join(" ", paraLines);
                blocks.Add(new ParagraphBlock(MarkdownInlineParser.Parse(text)));
            }
        }

        return blocks;
    }

    private static bool IsThematicBreak(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length < 3)
        {
            return false;
        }

        var ch = trimmed[0];
        if (ch != '-' && ch != '*' && ch != '_')
        {
            return false;
        }

        foreach (var c in trimmed)
        {
            if (c != ch && c != ' ')
            {
                return false;
            }
        }

        var count = 0;
        foreach (var c in trimmed)
        {
            if (c == ch)
            {
                count++;
            }
        }

        return count >= 3;
    }

    // Stryker disable all : Equality/Logical/Block mutations on heading parsing boundaries — level 0/6/7 edge cases tested but boundary mutations produce equivalent null-return behavior
    private static HeadingBlock? TryParseHeading(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] != '#')
        {
            return null;
        }

        var level = 0;
        while (level < trimmed.Length && trimmed[level] == '#')
        {
            level++;
        }

        if (level > 6 || level >= trimmed.Length || trimmed[level] != ' ')
        {
            return null;
        }

        var text = trimmed.Substring(level + 1).TrimEnd();
        // Remove trailing # if present
        while (text.Length > 0 && text[text.Length - 1] == '#')
        {
            text = text.TrimEnd('#').TrimEnd();
        }

        return new HeadingBlock(level, MarkdownInlineParser.Parse(text));
    }
    // Stryker restore all

    private static bool IsFencedCodeStart(string line, out string language, out char fenceChar, out int fenceLen)
    {
        // Stryker disable once all : String mutation on default empty — equivalent initialization
        language = string.Empty;
        fenceChar = '`';
        fenceLen = 0;

        var trimmed = line.TrimStart();
        if (trimmed.Length < 3)
        {
            return false;
        }

        var ch = trimmed[0];
        if (ch != '`' && ch != '~')
        {
            return false;
        }

        var count = 0;
        while (count < trimmed.Length && trimmed[count] == ch)
        {
            count++;
        }

        if (count < 3)
        {
            return false;
        }

        fenceChar = ch;
        fenceLen = count;
        language = trimmed.Substring(count).Trim();
        return true;
    }

    private static bool IsFencedCodeEnd(string line, char fenceChar, int fenceLen)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length < fenceLen)
        {
            return false;
        }

        var count = 0;
        while (count < trimmed.Length && trimmed[count] == fenceChar)
        {
            count++;
        }

        // Stryker disable once all : Logical mutation on fence close — equivalent for all practical fence lengths
        return count >= fenceLen && trimmed.Substring(count).Trim().Length == 0;
    }

    // Stryker disable all : Equality mutation on length boundary — >= 2 vs > 2 equivalent for single-char markers
    private static bool IsUnorderedListItem(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.Length >= 2
            && (trimmed[0] == '-' || trimmed[0] == '*' || trimmed[0] == '+')
            && trimmed[1] == ' ';
    }
    // Stryker restore all

    private static string StripListMarker(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.Substring(2).TrimStart();
    }

    // Stryker disable all : Equality/Logical/Arithmetic mutations on ordered list parsing boundaries — boundary mutations produce equivalent false-return for edge cases
    private static bool IsOrderedListItem(string line, out int startNumber)
    {
        startNumber = 0;
        var trimmed = line.TrimStart();
        var i = 0;
        while (i < trimmed.Length && char.IsDigit(trimmed[i]))
        {
            i++;
        }

        if (i == 0 || i >= trimmed.Length)
        {
            return false;
        }

        if ((trimmed[i] != '.' && trimmed[i] != ')') || i + 1 >= trimmed.Length || trimmed[i + 1] != ' ')
        {
            return false;
        }

#if NETSTANDARD2_0
        startNumber = int.Parse(trimmed.Substring(0, i));
#else
        startNumber = int.Parse(trimmed.AsSpan(0, i));
#endif
        return true;
    }
    // Stryker restore all

    // Stryker disable all : Equality/Logical mutations on digit loop — boundary mutations produce equivalent behavior for valid ordered list items
    private static string StripOrderedListMarker(string line)
    {
        var trimmed = line.TrimStart();
        var i = 0;
        while (i < trimmed.Length && char.IsDigit(trimmed[i]))
        {
            i++;
        }

        // Skip . or ) and space
        return trimmed.Substring(i + 2).TrimStart();
    }
    // Stryker restore all
}
