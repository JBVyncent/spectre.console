using FluentAssertions;
using Spectre.Console.Markdown.Parsing;
using Spectre.Console.Markdown.Syntax;
using Xunit;

namespace Spectre.Console.Markdown.Tests.Parsing;

/// <summary>
/// Edge case tests for inline parser — kills boundary and statement mutations.
/// </summary>
public sealed class MarkdownInlineParserEdgeCaseTests
{
    [Fact]
    public void Parse_EscapedChar_AtEnd_AppendsBackslash()
    {
        // Backslash at very end of string — no char to escape, backslash is literal
        var result = MarkdownInlineParser.Parse("text\\");

        result.Should().HaveCount(1);
        ((TextInline)result[0]).Text.Should().Be("text\\");
    }

    [Fact]
    public void Parse_EscapedChar_OnlyBackslashAndChar()
    {
        var result = MarkdownInlineParser.Parse("\\*");

        result.Should().HaveCount(1);
        ((TextInline)result[0]).Text.Should().Be("*");
    }

    [Fact]
    public void Parse_BoldThenText_CorrectStructure()
    {
        var result = MarkdownInlineParser.Parse("**b** t");

        result.Should().HaveCount(2);
        var bold = result[0].Should().BeOfType<EmphasisInline>().Subject;
        bold.Strong.Should().BeTrue();
        ((TextInline)bold.Children[0]).Text.Should().Be("b");
        ((TextInline)result[1]).Text.Should().Be(" t");
    }

    [Fact]
    public void Parse_ItalicThenBold_CorrectOrder()
    {
        var result = MarkdownInlineParser.Parse("*i* **b**");

        result.Should().HaveCount(3);
        var italic = result[0].Should().BeOfType<EmphasisInline>().Subject;
        italic.Strong.Should().BeFalse();
        result[1].Should().BeOfType<TextInline>();
        var bold = result[2].Should().BeOfType<EmphasisInline>().Subject;
        bold.Strong.Should().BeTrue();
    }

    [Fact]
    public void Parse_CodeSpanThenText_CorrectStructure()
    {
        var result = MarkdownInlineParser.Parse("`c` text");

        result.Should().HaveCount(2);
        result[0].Should().BeOfType<CodeSpanInline>();
        ((CodeSpanInline)result[0]).Code.Should().Be("c");
        ((TextInline)result[1]).Text.Should().Be(" text");
    }

    [Fact]
    public void Parse_StrikethroughThenText_CorrectStructure()
    {
        var result = MarkdownInlineParser.Parse("~~s~~ text");

        result.Should().HaveCount(2);
        result[0].Should().BeOfType<StrikethroughInline>();
        ((TextInline)result[1]).Text.Should().Be(" text");
    }

    [Fact]
    public void Parse_BoldItalicContent_HasBothDecorations()
    {
        var result = MarkdownInlineParser.Parse("***both***");

        result.Should().HaveCount(1);
        // Outer is strong (bold), inner is emphasis (italic)
        var outer = result[0].Should().BeOfType<EmphasisInline>().Subject;
        outer.Strong.Should().BeTrue();
        var inner = outer.Children[0].Should().BeOfType<EmphasisInline>().Subject;
        inner.Strong.Should().BeFalse();
        ((TextInline)inner.Children[0]).Text.Should().Be("both");
    }

    [Fact]
    public void Parse_UnderscoreBoldItalic_HasBothDecorations()
    {
        var result = MarkdownInlineParser.Parse("___both___");

        var outer = result[0].Should().BeOfType<EmphasisInline>().Subject;
        outer.Strong.Should().BeTrue();
        var inner = outer.Children[0].Should().BeOfType<EmphasisInline>().Subject;
        inner.Strong.Should().BeFalse();
    }

    [Fact]
    public void Parse_Link_WithNestedBold()
    {
        var result = MarkdownInlineParser.Parse("[**bold**](url)");

        var link = result[0].Should().BeOfType<LinkInline>().Subject;
        link.Url.Should().Be("url");
        link.Children[0].Should().BeOfType<EmphasisInline>();
    }

    [Fact]
    public void Parse_Link_EmptyUrl()
    {
        var result = MarkdownInlineParser.Parse("[text]()");

        var link = result[0].Should().BeOfType<LinkInline>().Subject;
        link.Url.Should().BeEmpty();
    }

    [Fact]
    public void Parse_UnmatchedOpenBracketNoClose_BecomesText()
    {
        var result = MarkdownInlineParser.Parse("[no close bracket");

        result.Should().HaveCount(1);
        ((TextInline)result[0]).Text.Should().Be("[no close bracket");
    }

    [Fact]
    public void Parse_BracketCloseImmediatelyAtEnd_BecomesText()
    {
        var result = MarkdownInlineParser.Parse("[text]");

        // ] at end, no ( follows
        result.Should().HaveCount(1);
        ((TextInline)result[0]).Text.Should().Be("[text]");
    }

    [Fact]
    public void Parse_MultipleBoldSections()
    {
        var result = MarkdownInlineParser.Parse("**a** **b**");

        result.Should().HaveCount(3);
        result[0].Should().BeOfType<EmphasisInline>();
        result[1].Should().BeOfType<TextInline>();
        result[2].Should().BeOfType<EmphasisInline>();
    }

    [Fact]
    public void Parse_AdjacentItalicAndBold()
    {
        // Our simple parser greedily matches the first * with the nearest *
        var result = MarkdownInlineParser.Parse("*a* and **b**");

        result.Should().HaveCount(3);
        var italic = result[0].Should().BeOfType<EmphasisInline>().Subject;
        italic.Strong.Should().BeFalse();
        ((TextInline)italic.Children[0]).Text.Should().Be("a");
        result[1].Should().BeOfType<TextInline>();
        var bold = result[2].Should().BeOfType<EmphasisInline>().Subject;
        bold.Strong.Should().BeTrue();
    }

    [Fact]
    public void Parse_EmptyCodeSpan_IsValid()
    {
        var result = MarkdownInlineParser.Parse("``");

        // Two backticks — first ` tries to parse code span, finds second ` immediately
        result.Should().HaveCount(1);
        var code = result[0].Should().BeOfType<CodeSpanInline>().Subject;
        code.Code.Should().BeEmpty();
    }

    [Fact]
    public void Parse_CodeSpanWithSpecialChars()
    {
        var result = MarkdownInlineParser.Parse("`**not bold**`");

        var code = result[0].Should().BeOfType<CodeSpanInline>().Subject;
        code.Code.Should().Be("**not bold**");
    }

    [Fact]
    public void Parse_LinkMissingCloseParen()
    {
        var result = MarkdownInlineParser.Parse("[text](url incomplete");

        result[0].Should().BeOfType<TextInline>();
        ((TextInline)result[0]).Text.Should().StartWith("[");
    }

    [Fact]
    public void Parse_UnderscoreBold()
    {
        var result = MarkdownInlineParser.Parse("__bold__");

        var emphasis = result[0].Should().BeOfType<EmphasisInline>().Subject;
        emphasis.Strong.Should().BeTrue();
        ((TextInline)emphasis.Children[0]).Text.Should().Be("bold");
    }

    [Fact]
    public void Parse_UnderscoreItalic()
    {
        var result = MarkdownInlineParser.Parse("_italic_");

        var emphasis = result[0].Should().BeOfType<EmphasisInline>().Subject;
        emphasis.Strong.Should().BeFalse();
        ((TextInline)emphasis.Children[0]).Text.Should().Be("italic");
    }

    [Fact]
    public void Parse_EscapeAtStringEnd()
    {
        // Backslash is last char — nothing to escape, backslash is literal
        var result = MarkdownInlineParser.Parse("hello\\");

        ((TextInline)result[0]).Text.Should().Be("hello\\");
    }

    [Fact]
    public void Parse_ConsecutiveEscapes()
    {
        var result = MarkdownInlineParser.Parse("\\\\");

        ((TextInline)result[0]).Text.Should().Be("\\");
    }
}
