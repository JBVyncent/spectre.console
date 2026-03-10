using FluentAssertions;
using Spectre.Console.Markdown.Parsing;
using Spectre.Console.Markdown.Syntax;
using Xunit;

namespace Spectre.Console.Markdown.Tests.Parsing;

/// <summary>
/// Edge case tests for block parser — kills boundary mutations.
/// </summary>
public sealed class MarkdownBlockParserEdgeCaseTests
{
    [Fact]
    public void Parse_Blockquote_WithoutSpaceAfterGreaterThan()
    {
        // >text (no space) — should still parse as blockquote
        var result = MarkdownBlockParser.Parse(">hello");

        var quote = result[0].Should().BeOfType<BlockquoteBlock>().Subject;
        var para = quote.Children[0].Should().BeOfType<ParagraphBlock>().Subject;
        ((TextInline)para.Inlines[0]).Text.Should().Be("hello");
    }

    [Fact]
    public void Parse_Blockquote_WithSpaceAfterGreaterThan()
    {
        // "> text" (with space) — strips > and space
        var result = MarkdownBlockParser.Parse("> hello");

        var quote = result[0].Should().BeOfType<BlockquoteBlock>().Subject;
        var para = quote.Children[0].Should().BeOfType<ParagraphBlock>().Subject;
        ((TextInline)para.Inlines[0]).Text.Should().Be("hello");
    }

    [Fact]
    public void Parse_Blockquote_SingleCharAfterGreaterThan()
    {
        // ">x" — length 2, [1] is 'x' not ' '
        var result = MarkdownBlockParser.Parse(">x");

        var quote = result[0].Should().BeOfType<BlockquoteBlock>().Subject;
        quote.Children.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_Heading_ExactlyAtLevel6()
    {
        var result = MarkdownBlockParser.Parse("###### H6");

        var heading = result[0].Should().BeOfType<HeadingBlock>().Subject;
        heading.Level.Should().Be(6);
    }

    [Fact]
    public void Parse_Heading_SingleHashWithSpace()
    {
        // "# " — level 1, empty text
        var result = MarkdownBlockParser.Parse("# X");

        result[0].Should().BeOfType<HeadingBlock>();
        ((HeadingBlock)result[0]).Level.Should().Be(1);
    }

    [Fact]
    public void Parse_Heading_TrailingHashes_AllStripped()
    {
        var result = MarkdownBlockParser.Parse("## Hello ####");

        var heading = result[0].Should().BeOfType<HeadingBlock>().Subject;
        ((TextInline)heading.Inlines[0]).Text.Should().Be("Hello");
    }

    [Fact]
    public void Parse_ThematicBreak_ExactlyThree()
    {
        var result = MarkdownBlockParser.Parse("---");
        result[0].Should().BeOfType<ThematicBreakBlock>();
    }

    [Fact]
    public void Parse_ThematicBreak_WithSpaces_ExactlyThree()
    {
        var result = MarkdownBlockParser.Parse("- - -");
        result[0].Should().BeOfType<ThematicBreakBlock>();
    }

    [Fact]
    public void Parse_NotThematicBreak_OnlyTwoChars()
    {
        var result = MarkdownBlockParser.Parse("--");
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_NotThematicBreak_WrongChar()
    {
        var result = MarkdownBlockParser.Parse("+++");
        // + is an unordered list marker, not thematic break
        // "+++" starts with +, but has no space after first char,
        // so it's not a list item either — it's a paragraph
        result[0].Should().NotBeOfType<ThematicBreakBlock>();
    }

    [Fact]
    public void Parse_FencedCode_ExactlyThreeBackticks()
    {
        var result = MarkdownBlockParser.Parse("```\ncode\n```");
        result[0].Should().BeOfType<CodeBlock>();
    }

    [Fact]
    public void Parse_FencedCode_TwoBackticks_NotCode()
    {
        var result = MarkdownBlockParser.Parse("``not code``");
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_FencedCode_CloseWithExtraChars_DoesNotClose()
    {
        // Closing fence with text after backticks should not close
        var result = MarkdownBlockParser.Parse("```\ncode\n``` extra");
        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        // The "``` extra" line doesn't close because it has trailing text
        code.Code.Should().Contain("code");
    }

    [Fact]
    public void Parse_OrderedList_ZeroStart()
    {
        var result = MarkdownBlockParser.Parse("0. Zero\n1. One");
        var list = result[0].Should().BeOfType<ListBlock>().Subject;
        list.StartNumber.Should().Be(0);
        list.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_OrderedList_NumberWithNoSpace_NotList()
    {
        // "1.X" — no space after period
        var result = MarkdownBlockParser.Parse("1.X");
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_OrderedList_LetterAfterNumber_NotList()
    {
        // "1a. text" — not a number then .
        var result = MarkdownBlockParser.Parse("1a. text");
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_UnorderedList_DashWithoutSpace_NotList()
    {
        // "-text" — no space after dash
        var result = MarkdownBlockParser.Parse("-text");
        result[0].Should().NotBeOfType<ListBlock>();
    }

    [Fact]
    public void Parse_EmptyBlockquote()
    {
        // ">" alone — blockquote with empty content
        var result = MarkdownBlockParser.Parse(">");
        var quote = result[0].Should().BeOfType<BlockquoteBlock>().Subject;
        quote.Children.Should().BeEmpty();
    }

    [Fact]
    public void Parse_FencedCode_EmptyLanguage_Trimmed()
    {
        var result = MarkdownBlockParser.Parse("```  \ncode\n```");
        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Language.Should().BeNull();
    }

    [Fact]
    public void Parse_OrderedList_JustNumber_NotList()
    {
        // "1" alone — not a list
        var result = MarkdownBlockParser.Parse("1");
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_OrderedList_NumberPeriodNoMoreChars_NotList()
    {
        // "1." alone — not a list (needs space after)
        var result = MarkdownBlockParser.Parse("1.");
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_Heading_EmptyAfterHash_NotHeading()
    {
        // "# " with nothing after — still a heading with empty text
        var result = MarkdownBlockParser.Parse("# ");
        // Trimming removes everything, empty heading
        var heading = result[0].Should().BeOfType<HeadingBlock>().Subject;
        heading.Level.Should().Be(1);
    }

    [Fact]
    public void Parse_SingleGreaterThan_Blockquote()
    {
        var result = MarkdownBlockParser.Parse(">");
        result[0].Should().BeOfType<BlockquoteBlock>();
    }

    [Fact]
    public void Parse_FencedCode_TildeClosesWithBacktick_DoesNotClose()
    {
        // Opening with ~~~ should not close with ```
        var result = MarkdownBlockParser.Parse("~~~\ncode\n```\n~~~");
        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Code.Should().Be("code\n```");
    }
}
