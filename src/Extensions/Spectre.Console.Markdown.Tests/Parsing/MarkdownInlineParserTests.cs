using FluentAssertions;
using Spectre.Console.Markdown.Parsing;
using Spectre.Console.Markdown.Syntax;
using Xunit;

namespace Spectre.Console.Markdown.Tests.Parsing;

public sealed class MarkdownInlineParserTests
{
    [Fact]
    public void Parse_PlainText_ReturnsSingleTextInline()
    {
        var result = MarkdownInlineParser.Parse("Hello world");

        result.Should().HaveCount(1);
        var text = result[0].Should().BeOfType<TextInline>().Subject;
        text.Text.Should().Be("Hello world");
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var result = MarkdownInlineParser.Parse(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_BoldAsterisk_ReturnsStrongEmphasis()
    {
        var result = MarkdownInlineParser.Parse("**bold**");

        result.Should().HaveCount(1);
        var emphasis = result[0].Should().BeOfType<EmphasisInline>().Subject;
        emphasis.Strong.Should().BeTrue();
        emphasis.Children.Should().HaveCount(1);
        ((TextInline)emphasis.Children[0]).Text.Should().Be("bold");
    }

    [Fact]
    public void Parse_BoldUnderscore_ReturnsStrongEmphasis()
    {
        var result = MarkdownInlineParser.Parse("__bold__");

        var emphasis = result[0].Should().BeOfType<EmphasisInline>().Subject;
        emphasis.Strong.Should().BeTrue();
    }

    [Fact]
    public void Parse_ItalicAsterisk_ReturnsEmphasis()
    {
        var result = MarkdownInlineParser.Parse("*italic*");

        result.Should().HaveCount(1);
        var emphasis = result[0].Should().BeOfType<EmphasisInline>().Subject;
        emphasis.Strong.Should().BeFalse();
        ((TextInline)emphasis.Children[0]).Text.Should().Be("italic");
    }

    [Fact]
    public void Parse_ItalicUnderscore_ReturnsEmphasis()
    {
        var result = MarkdownInlineParser.Parse("_italic_");

        var emphasis = result[0].Should().BeOfType<EmphasisInline>().Subject;
        emphasis.Strong.Should().BeFalse();
    }

    [Fact]
    public void Parse_BoldItalicAsterisk_ReturnsBoldWrappingItalic()
    {
        var result = MarkdownInlineParser.Parse("***bold italic***");

        result.Should().HaveCount(1);
        var outer = result[0].Should().BeOfType<EmphasisInline>().Subject;
        outer.Strong.Should().BeTrue();
        outer.Children.Should().HaveCount(1);
        var inner = outer.Children[0].Should().BeOfType<EmphasisInline>().Subject;
        inner.Strong.Should().BeFalse();
    }

    [Fact]
    public void Parse_BoldItalicUnderscore_ReturnsBoldWrappingItalic()
    {
        var result = MarkdownInlineParser.Parse("___bold italic___");

        var outer = result[0].Should().BeOfType<EmphasisInline>().Subject;
        outer.Strong.Should().BeTrue();
    }

    [Fact]
    public void Parse_CodeSpan_ReturnsCodeSpanInline()
    {
        var result = MarkdownInlineParser.Parse("`code`");

        result.Should().HaveCount(1);
        var code = result[0].Should().BeOfType<CodeSpanInline>().Subject;
        code.Code.Should().Be("code");
    }

    [Fact]
    public void Parse_CodeSpan_WithSpaces()
    {
        var result = MarkdownInlineParser.Parse("`var x = 1`");

        var code = result[0].Should().BeOfType<CodeSpanInline>().Subject;
        code.Code.Should().Be("var x = 1");
    }

    [Fact]
    public void Parse_Link_ReturnsLinkInline()
    {
        var result = MarkdownInlineParser.Parse("[click here](https://example.com)");

        result.Should().HaveCount(1);
        var link = result[0].Should().BeOfType<LinkInline>().Subject;
        link.Url.Should().Be("https://example.com");
        link.Children.Should().HaveCount(1);
        ((TextInline)link.Children[0]).Text.Should().Be("click here");
    }

    [Fact]
    public void Parse_Strikethrough_ReturnsStrikethroughInline()
    {
        var result = MarkdownInlineParser.Parse("~~deleted~~");

        result.Should().HaveCount(1);
        var strike = result[0].Should().BeOfType<StrikethroughInline>().Subject;
        strike.Children.Should().HaveCount(1);
        ((TextInline)strike.Children[0]).Text.Should().Be("deleted");
    }

    [Fact]
    public void Parse_MixedInlines()
    {
        var result = MarkdownInlineParser.Parse("Hello **bold** and *italic* end");

        result.Should().HaveCount(5);
        result[0].Should().BeOfType<TextInline>();
        result[1].Should().BeOfType<EmphasisInline>();
        result[2].Should().BeOfType<TextInline>();
        result[3].Should().BeOfType<EmphasisInline>();
        result[4].Should().BeOfType<TextInline>();

        ((TextInline)result[0]).Text.Should().Be("Hello ");
        ((EmphasisInline)result[1]).Strong.Should().BeTrue();
        ((TextInline)result[2]).Text.Should().Be(" and ");
        ((EmphasisInline)result[3]).Strong.Should().BeFalse();
        ((TextInline)result[4]).Text.Should().Be(" end");
    }

    [Fact]
    public void Parse_EscapedAsterisk_TreatedAsText()
    {
        var result = MarkdownInlineParser.Parse("\\*not italic\\*");

        result.Should().HaveCount(1);
        var text = result[0].Should().BeOfType<TextInline>().Subject;
        text.Text.Should().Be("*not italic*");
    }

    [Fact]
    public void Parse_EscapedBacktick_TreatedAsText()
    {
        var result = MarkdownInlineParser.Parse("\\`not code\\`");

        result.Should().HaveCount(1);
        ((TextInline)result[0]).Text.Should().Be("`not code`");
    }

    [Fact]
    public void Parse_UnmatchedBold_TreatedAsText()
    {
        var result = MarkdownInlineParser.Parse("**unclosed");

        // The ** doesn't close, so first char becomes text, rest parsed
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_UnmatchedBacktick_TreatedAsText()
    {
        var result = MarkdownInlineParser.Parse("`unclosed");

        result.Should().HaveCount(1);
        result[0].Should().BeOfType<TextInline>();
    }

    [Fact]
    public void Parse_UnmatchedLink_BracketBecomesText()
    {
        var result = MarkdownInlineParser.Parse("[no link");

        result.Should().HaveCount(1);
        result[0].Should().BeOfType<TextInline>();
    }

    [Fact]
    public void Parse_LinkWithoutParens_BracketBecomesText()
    {
        var result = MarkdownInlineParser.Parse("[text] not a link");

        result.Should().HaveCount(1);
        ((TextInline)result[0]).Text.Should().Be("[text] not a link");
    }

    [Fact]
    public void Parse_EmptyBoldDelimiter_TreatedAsText()
    {
        // **** with nothing inside
        var result = MarkdownInlineParser.Parse("****");

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_Strikethrough_Unmatched_TreatedAsText()
    {
        var result = MarkdownInlineParser.Parse("~~unclosed");

        result.Should().NotBeEmpty();
        // Should contain text starting with ~
    }

    [Fact]
    public void Parse_LinkWithBoldText()
    {
        var result = MarkdownInlineParser.Parse("[**bold link**](https://example.com)");

        var link = result[0].Should().BeOfType<LinkInline>().Subject;
        link.Url.Should().Be("https://example.com");
        link.Children[0].Should().BeOfType<EmphasisInline>();
    }

    [Fact]
    public void Parse_CodeSpanInsideBold_CodeTakesPrecedence()
    {
        // In our simple parser, backticks are checked first
        var result = MarkdownInlineParser.Parse("**`code`**");

        // The ** opens bold, inner contains code span
        var emphasis = result[0].Should().BeOfType<EmphasisInline>().Subject;
        emphasis.Children[0].Should().BeOfType<CodeSpanInline>();
    }

    [Fact]
    public void Parse_EscapedBracket_TreatedAsText()
    {
        var result = MarkdownInlineParser.Parse("\\[not a link\\]");

        result.Should().HaveCount(1);
        ((TextInline)result[0]).Text.Should().Be("[not a link]");
    }

    [Fact]
    public void Parse_LinkMissingCloseParen_BracketBecomesText()
    {
        var result = MarkdownInlineParser.Parse("[text](url");

        // [ becomes text, rest follows
        result.Should().NotBeEmpty();
        result[0].Should().BeOfType<TextInline>();
    }

    [Fact]
    public void Parse_SingleTilde_TreatedAsText()
    {
        var result = MarkdownInlineParser.Parse("~just a tilde~");

        result.Should().HaveCount(1);
        result[0].Should().BeOfType<TextInline>();
        ((TextInline)result[0]).Text.Should().Be("~just a tilde~");
    }
}
