using FluentAssertions;
using Spectre.Console.Markdown.Parsing;
using Spectre.Console.Markdown.Syntax;
using Xunit;

namespace Spectre.Console.Markdown.Tests.Parsing;

public sealed class MarkdownBlockParserTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_BlankLines_ReturnsEmpty()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("\n\n\n");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_SingleParagraph_ReturnsParagraphBlock()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("Hello world");

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeOfType<ParagraphBlock>();
        var para = (ParagraphBlock)result[0];
        para.Inlines.Should().HaveCount(1);
        para.Inlines[0].Should().BeOfType<TextInline>();
        ((TextInline)para.Inlines[0]).Text.Should().Be("Hello world");
    }

    [Fact]
    public void Parse_MultiLineParagraph_JoinsLinesWithSpace()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("Line one\nLine two");

        // Assert
        result.Should().HaveCount(1);
        var para = result[0].Should().BeOfType<ParagraphBlock>().Subject;
        para.Inlines.Should().HaveCount(1);
        ((TextInline)para.Inlines[0]).Text.Should().Be("Line one Line two");
    }

    [Fact]
    public void Parse_TwoParagraphsSeparatedByBlankLine_ReturnsTwoBlocks()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("First paragraph\n\nSecond paragraph");

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeOfType<ParagraphBlock>();
        result[1].Should().BeOfType<ParagraphBlock>();
    }

    [Theory]
    [InlineData("# H1", 1, "H1")]
    [InlineData("## H2", 2, "H2")]
    [InlineData("### H3", 3, "H3")]
    [InlineData("#### H4", 4, "H4")]
    [InlineData("##### H5", 5, "H5")]
    [InlineData("###### H6", 6, "H6")]
    public void Parse_AtxHeading_ReturnsCorrectLevel(string input, int expectedLevel, string expectedText)
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(1);
        var heading = result[0].Should().BeOfType<HeadingBlock>().Subject;
        heading.Level.Should().Be(expectedLevel);
        heading.Inlines.Should().HaveCount(1);
        ((TextInline)heading.Inlines[0]).Text.Should().Be(expectedText);
    }

    [Fact]
    public void Parse_HeadingWithTrailingHashes_StripsTrailingHashes()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("## Hello ##");

        // Assert
        var heading = result[0].Should().BeOfType<HeadingBlock>().Subject;
        heading.Level.Should().Be(2);
        ((TextInline)heading.Inlines[0]).Text.Should().Be("Hello");
    }

    [Fact]
    public void Parse_SevenHashes_NotAHeading()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("####### Not a heading");

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_HashWithoutSpace_NotAHeading()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("#NoSpace");

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Theory]
    [InlineData("---")]
    [InlineData("***")]
    [InlineData("___")]
    [InlineData("- - -")]
    [InlineData("* * *")]
    [InlineData("----------")]
    public void Parse_ThematicBreak_ReturnsThematicBreakBlock(string input)
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeOfType<ThematicBreakBlock>();
    }

    [Fact]
    public void Parse_TwoCharDashes_NotThematicBreak()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("--");

        // Assert
        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_FencedCodeBlock_Backticks()
    {
        // Arrange
        var input = "```\nvar x = 1;\nvar y = 2;\n```";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(1);
        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Language.Should().BeNull();
        code.Code.Should().Be("var x = 1;\nvar y = 2;");
    }

    [Fact]
    public void Parse_FencedCodeBlock_WithLanguage()
    {
        // Arrange
        var input = "```csharp\nConsole.WriteLine();\n```";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Language.Should().Be("csharp");
        code.Code.Should().Be("Console.WriteLine();");
    }

    [Fact]
    public void Parse_FencedCodeBlock_Tildes()
    {
        // Arrange
        var input = "~~~\ncode here\n~~~";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Code.Should().Be("code here");
    }

    [Fact]
    public void Parse_FencedCodeBlock_UnterminatedIncludesRemaining()
    {
        // Arrange
        var input = "```\nunclosed code";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Code.Should().Be("unclosed code");
    }

    [Fact]
    public void Parse_Blockquote_Simple()
    {
        // Arrange
        var input = "> This is a quote";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(1);
        var quote = result[0].Should().BeOfType<BlockquoteBlock>().Subject;
        quote.Children.Should().HaveCount(1);
        quote.Children[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_Blockquote_MultiLine()
    {
        // Arrange
        var input = "> Line one\n> Line two";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        var quote = result[0].Should().BeOfType<BlockquoteBlock>().Subject;
        quote.Children.Should().HaveCount(1);
        var para = (ParagraphBlock)quote.Children[0];
        ((TextInline)para.Inlines[0]).Text.Should().Be("Line one Line two");
    }

    [Fact]
    public void Parse_UnorderedList_Dash()
    {
        // Arrange
        var input = "- Item 1\n- Item 2\n- Item 3";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(1);
        var list = result[0].Should().BeOfType<ListBlock>().Subject;
        list.Ordered.Should().BeFalse();
        list.Items.Should().HaveCount(3);
        ((TextInline)list.Items[0].Inlines[0]).Text.Should().Be("Item 1");
        ((TextInline)list.Items[1].Inlines[0]).Text.Should().Be("Item 2");
        ((TextInline)list.Items[2].Inlines[0]).Text.Should().Be("Item 3");
    }

    [Fact]
    public void Parse_UnorderedList_Asterisk()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("* A\n* B");

        // Assert
        var list = result[0].Should().BeOfType<ListBlock>().Subject;
        list.Ordered.Should().BeFalse();
        list.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_UnorderedList_Plus()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("+ X\n+ Y");

        // Assert
        var list = result[0].Should().BeOfType<ListBlock>().Subject;
        list.Ordered.Should().BeFalse();
        list.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_OrderedList()
    {
        // Arrange
        var input = "1. First\n2. Second\n3. Third";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(1);
        var list = result[0].Should().BeOfType<ListBlock>().Subject;
        list.Ordered.Should().BeTrue();
        list.StartNumber.Should().Be(1);
        list.Items.Should().HaveCount(3);
        ((TextInline)list.Items[0].Inlines[0]).Text.Should().Be("First");
    }

    [Fact]
    public void Parse_OrderedList_StartAt5()
    {
        // Arrange
        var input = "5. Item five\n6. Item six";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        var list = result[0].Should().BeOfType<ListBlock>().Subject;
        list.StartNumber.Should().Be(5);
        list.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_OrderedList_ParenDelimiter()
    {
        // Arrange & Act
        var result = MarkdownBlockParser.Parse("1) Alpha\n2) Beta");

        // Assert
        var list = result[0].Should().BeOfType<ListBlock>().Subject;
        list.Ordered.Should().BeTrue();
        list.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_MixedContent()
    {
        // Arrange
        var input = "# Title\n\nA paragraph.\n\n- Item\n\n---\n\n```\ncode\n```";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(5);
        result[0].Should().BeOfType<HeadingBlock>();
        result[1].Should().BeOfType<ParagraphBlock>();
        result[2].Should().BeOfType<ListBlock>();
        result[3].Should().BeOfType<ThematicBreakBlock>();
        result[4].Should().BeOfType<CodeBlock>();
    }

    [Fact]
    public void Parse_CrLf_HandledCorrectly()
    {
        // Arrange
        var input = "# Title\r\n\r\nParagraph";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeOfType<HeadingBlock>();
        result[1].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_CarriageReturn_HandledCorrectly()
    {
        // Arrange
        var input = "# Title\r\rParagraph";

        // Act
        var result = MarkdownBlockParser.Parse(input);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeOfType<HeadingBlock>();
        result[1].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_ThematicBreak_MixedChars_NotABreak()
    {
        // - and * mixed — should be a paragraph
        var result = MarkdownBlockParser.Parse("-*-");

        result[0].Should().BeOfType<ParagraphBlock>();
    }

    [Fact]
    public void Parse_BlockquoteWithoutSpace_StillParsed()
    {
        // >text (no space after >)
        var result = MarkdownBlockParser.Parse(">text");

        var quote = result[0].Should().BeOfType<BlockquoteBlock>().Subject;
        quote.Children.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_FencedCode_LongerCloseFence()
    {
        // Opening fence with 3 backticks, closing with 5 should still close
        var input = "```\ncode\n`````";

        var result = MarkdownBlockParser.Parse(input);

        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Code.Should().Be("code");
    }

    [Fact]
    public void Parse_FencedCode_ShorterCloseFence_DoesNotClose()
    {
        // Opening with 4 backticks, closing with 3 should not close
        var input = "````\ncode\n```\nmore\n````";

        var result = MarkdownBlockParser.Parse(input);

        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Code.Should().Be("code\n```\nmore");
    }

    [Fact]
    public void Parse_EmptyFencedCodeBlock()
    {
        var input = "```\n```";

        var result = MarkdownBlockParser.Parse(input);

        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Code.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhitespaceOnlyLanguage_IsNull()
    {
        var input = "```   \ncode\n```";

        var result = MarkdownBlockParser.Parse(input);

        var code = result[0].Should().BeOfType<CodeBlock>().Subject;
        code.Language.Should().BeNull();
    }
}
