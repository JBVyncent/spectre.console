using System.Collections.Generic;
using FluentAssertions;
using Spectre.Console.Markdown.Rendering;
using Spectre.Console.Markdown.Syntax;
using Spectre.Console.Testing;
using Xunit;

namespace Spectre.Console.Markdown.Tests.Rendering;

public sealed class MarkdownRendererTests
{
    private static MarkdownStyles DefaultStyles => new MarkdownStyles();

    [Fact]
    public void Render_EmptyBlockList_ReturnsRows()
    {
        // Arrange
        var blocks = new List<MarkdownBlock>();

        // Act
        var result = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Render_SingleParagraph_ProducesOutput()
    {
        // Arrange
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new ParagraphBlock(new List<MarkdownInline>
            {
                new TextInline("Hello"),
            }),
        };

        // Act
        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        // Assert
        console.Output.Should().Contain("Hello");
    }

    [Fact]
    public void Render_Heading1_UsesRule()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new HeadingBlock(1, new List<MarkdownInline>
            {
                new TextInline("Title"),
            }),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        // Rule renders centered text between ─ chars
        console.Output.Should().Contain("Title");
        console.Output.Should().Contain("─");
    }

    [Fact]
    public void Render_Heading2_UsesParagraph()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new HeadingBlock(2, new List<MarkdownInline>
            {
                new TextInline("Subtitle"),
            }),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("Subtitle");
    }

    [Fact]
    public void Render_Heading3_UsesParagraph()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new HeadingBlock(3, new List<MarkdownInline>
            {
                new TextInline("Section"),
            }),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("Section");
    }

    [Fact]
    public void Render_Heading4_UsesFallbackStyle()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new HeadingBlock(4, new List<MarkdownInline>
            {
                new TextInline("Minor"),
            }),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("Minor");
    }

    [Fact]
    public void Render_CodeBlock_WithLanguageHeader()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new CodeBlock("python", "print('hello')"),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("python");
        console.Output.Should().Contain("print('hello')");
    }

    [Fact]
    public void Render_CodeBlock_WithoutLanguage()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new CodeBlock(null, "plain code"),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("plain code");
    }

    [Fact]
    public void Render_Blockquote_UsesHeavyPanel()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new BlockquoteBlock(new List<MarkdownBlock>
            {
                new ParagraphBlock(new List<MarkdownInline>
                {
                    new TextInline("Quoted text"),
                }),
            }),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("Quoted text");
    }

    [Fact]
    public void Render_UnorderedList_ShowsBullets()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new ListBlock(false, 1, new List<ListItemBlock>
            {
                new ListItemBlock(new List<MarkdownInline> { new TextInline("Alpha") }),
                new ListItemBlock(new List<MarkdownInline> { new TextInline("Beta") }),
            }),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("*");
        console.Output.Should().Contain("Alpha");
        console.Output.Should().Contain("Beta");
    }

    [Fact]
    public void Render_OrderedList_ShowsNumbers()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new ListBlock(true, 1, new List<ListItemBlock>
            {
                new ListItemBlock(new List<MarkdownInline> { new TextInline("First") }),
                new ListItemBlock(new List<MarkdownInline> { new TextInline("Second") }),
            }),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("1.");
        console.Output.Should().Contain("2.");
        console.Output.Should().Contain("First");
        console.Output.Should().Contain("Second");
    }

    [Fact]
    public void Render_OrderedList_StartAtCustomNumber()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new ListBlock(true, 5, new List<ListItemBlock>
            {
                new ListItemBlock(new List<MarkdownInline> { new TextInline("Item") }),
            }),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("5.");
    }

    [Fact]
    public void Render_ThematicBreak_ProducesRule()
    {
        var console = new TestConsole();
        var blocks = new List<MarkdownBlock>
        {
            new ThematicBreakBlock(),
        };

        var renderable = MarkdownRenderer.Render(blocks, DefaultStyles, BoxBorder.Rounded);
        console.Write(renderable);

        console.Output.Should().Contain("─");
    }

    [Fact]
    public void RenderParagraph_WithTextInline_ProducesParagraph()
    {
        var inlines = new List<MarkdownInline>
        {
            new TextInline("Hello"),
        };

        var para = MarkdownRenderer.RenderParagraph(inlines, Style.Plain);

        para.Should().NotBeNull();
    }

    [Fact]
    public void RenderInlines_EmphasisBold_ProducesOutput()
    {
        var console = new TestConsole();
        var inlines = new List<MarkdownInline>
        {
            new EmphasisInline(true, new List<MarkdownInline>
            {
                new TextInline("bold"),
            }),
        };

        var para = new Paragraph();
        MarkdownRenderer.RenderInlines(inlines, para, Style.Plain, null);
        console.Write(para);

        console.Output.Should().Contain("bold");
    }

    [Fact]
    public void RenderInlines_EmphasisItalic_ProducesOutput()
    {
        var console = new TestConsole();
        var inlines = new List<MarkdownInline>
        {
            new EmphasisInline(false, new List<MarkdownInline>
            {
                new TextInline("italic"),
            }),
        };

        var para = new Paragraph();
        MarkdownRenderer.RenderInlines(inlines, para, Style.Plain, null);
        console.Write(para);

        console.Output.Should().Contain("italic");
    }

    [Fact]
    public void RenderInlines_CodeSpan_ProducesOutput()
    {
        var console = new TestConsole();
        var inlines = new List<MarkdownInline>
        {
            new CodeSpanInline("code"),
        };

        var para = new Paragraph();
        MarkdownRenderer.RenderInlines(inlines, para, Style.Plain, null);
        console.Write(para);

        console.Output.Should().Contain("code");
    }

    [Fact]
    public void RenderInlines_Link_ProducesOutput()
    {
        var console = new TestConsole();
        var inlines = new List<MarkdownInline>
        {
            new LinkInline("https://example.com", new List<MarkdownInline>
            {
                new TextInline("link text"),
            }),
        };

        var para = new Paragraph();
        MarkdownRenderer.RenderInlines(inlines, para, Style.Plain, null);
        console.Write(para);

        console.Output.Should().Contain("link text");
    }

    [Fact]
    public void RenderInlines_Strikethrough_ProducesOutput()
    {
        var console = new TestConsole();
        var inlines = new List<MarkdownInline>
        {
            new StrikethroughInline(new List<MarkdownInline>
            {
                new TextInline("struck"),
            }),
        };

        var para = new Paragraph();
        MarkdownRenderer.RenderInlines(inlines, para, Style.Plain, null);
        console.Write(para);

        console.Output.Should().Contain("struck");
    }
}
