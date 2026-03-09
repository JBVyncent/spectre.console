using System.IO;

namespace Spectre.Console.Ansi.Tests;

/// <summary>
/// Comprehensive tests targeting surviving Stryker mutants in Spectre.Console.Ansi.
/// Each region documents which mutant lines it targets and why.
/// </summary>
public sealed class MutantKillerTests
{
    // -------------------------------------------------------------------------
    // Helper: create a TrueColor ANSI writer backed by a StringWriter
    // -------------------------------------------------------------------------
    private static (AnsiWriter writer, StringWriter output) CreateWriter(
        bool ansi = true,
        bool links = true,
        ColorSystem colorSystem = ColorSystem.TrueColor)
    {
        var sw = new StringWriter();
        var caps = new AnsiCapabilities
        {
            Ansi = ansi,
            Links = links,
            ColorSystem = colorSystem,
            AlternateBuffer = ansi,
        };
        return (new AnsiWriter(sw, caps), sw);
    }

    // =========================================================================
    // Color.GetHashCode  (lines 98-100)
    // Mutations: swap XOR→ADD, remove individual component contributions,
    //            change FNV prime 16777619.
    // =========================================================================
    #region Color.GetHashCode

    [Fact]
    public void Color_GetHashCode_DifferentR_ProducesDifferentHash()
    {
        var a = new Color(1, 0, 0);
        var b = new Color(2, 0, 0);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Color_GetHashCode_DifferentG_ProducesDifferentHash()
    {
        var a = new Color(0, 1, 0);
        var b = new Color(0, 2, 0);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Color_GetHashCode_DifferentB_ProducesDifferentHash()
    {
        var a = new Color(0, 0, 1);
        var b = new Color(0, 0, 2);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Color_GetHashCode_SameComponents_ProducesSameHash()
    {
        var a = new Color(42, 128, 255);
        var b = new Color(42, 128, 255);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Color_GetHashCode_SwappedRAndG_ProducesDifferentHash()
    {
        // Ensures each component contributes at a different XOR position.
        // If the prime were removed (×1 instead of ×16777619) swapping R and G
        // would accidentally yield the same hash.
        var a = new Color(10, 20, 0);
        var b = new Color(20, 10, 0);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Color_GetHashCode_SwappedRAndB_ProducesDifferentHash()
    {
        var a = new Color(10, 0, 20);
        var b = new Color(20, 0, 10);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Color_GetHashCode_SwappedGAndB_ProducesDifferentHash()
    {
        var a = new Color(0, 10, 20);
        var b = new Color(0, 20, 10);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    // Verify consistency with ==
    [Fact]
    public void Color_GetHashCode_EqualColors_HaveSameHash()
    {
        var a = new Color(100, 150, 200);
        var b = new Color(100, 150, 200);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    // =========================================================================
    // Color.ToConsoleColor  (lines 179, 185-207)
    // Line 179 mutation: `>= 16` changed to `> 16` — color #16 would no longer
    // be quantized to standard palette, causing a switch default exception.
    // Switch-arm mutations: individual ConsoleColor mappings can be swapped.
    // =========================================================================
    #region Color.ToConsoleColor

    [Fact]
    public void Color_ToConsoleColor_Number0_ReturnsBlack()
    {
        Color c = 0; // implicit from int, sets Number=0
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.Black);
    }

    [Fact]
    public void Color_ToConsoleColor_Number1_ReturnsDarkRed()
    {
        Color c = 1;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.DarkRed);
    }

    [Fact]
    public void Color_ToConsoleColor_Number2_ReturnsDarkGreen()
    {
        Color c = 2;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.DarkGreen);
    }

    [Fact]
    public void Color_ToConsoleColor_Number3_ReturnsDarkYellow()
    {
        Color c = 3;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.DarkYellow);
    }

    [Fact]
    public void Color_ToConsoleColor_Number4_ReturnsDarkBlue()
    {
        Color c = 4;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.DarkBlue);
    }

    [Fact]
    public void Color_ToConsoleColor_Number5_ReturnsDarkMagenta()
    {
        Color c = 5;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.DarkMagenta);
    }

    [Fact]
    public void Color_ToConsoleColor_Number6_ReturnsDarkCyan()
    {
        Color c = 6;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.DarkCyan);
    }

    [Fact]
    public void Color_ToConsoleColor_Number7_ReturnsGray()
    {
        Color c = 7;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.Gray);
    }

    [Fact]
    public void Color_ToConsoleColor_Number8_ReturnsDarkGray()
    {
        Color c = 8;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.DarkGray);
    }

    [Fact]
    public void Color_ToConsoleColor_Number9_ReturnsRed()
    {
        Color c = 9;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.Red);
    }

    [Fact]
    public void Color_ToConsoleColor_Number10_ReturnsGreen()
    {
        Color c = 10;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.Green);
    }

    [Fact]
    public void Color_ToConsoleColor_Number11_ReturnsYellow()
    {
        Color c = 11;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.Yellow);
    }

    [Fact]
    public void Color_ToConsoleColor_Number12_ReturnsBlue()
    {
        Color c = 12;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.Blue);
    }

    [Fact]
    public void Color_ToConsoleColor_Number13_ReturnsMagenta()
    {
        Color c = 13;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.Magenta);
    }

    [Fact]
    public void Color_ToConsoleColor_Number14_ReturnsCyan()
    {
        Color c = 14;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.Cyan);
    }

    [Fact]
    public void Color_ToConsoleColor_Number15_ReturnsWhite()
    {
        Color c = 15;
        Color.ToConsoleColor(c).Should().Be(ConsoleColor.White);
    }

    /// <summary>
    /// Color #16 is just outside the standard 16-color palette.
    /// The guard `>= 16` must quantize it. If mutated to `> 16` then
    /// color 16 would hit the switch default and throw.
    /// </summary>
    [Fact]
    public void Color_ToConsoleColor_Number16_QuantizesWithoutThrowing()
    {
        Color c = 16; // 8-bit color — must be remapped to 0-15
        var act = () => Color.ToConsoleColor(c);
        act.Should().NotThrow();
    }

    [Fact]
    public void Color_ToConsoleColor_Default_ReturnsMinusOne()
    {
        Color.ToConsoleColor(Color.Default).Should().Be((ConsoleColor)(-1));
    }

    #endregion

    // =========================================================================
    // AnsiMarkupTagParser — numeric color boundary checks (lines 91, 96)
    // Line 91: `number < 0`  — mutation changes to `number <= 0`
    // Line 96: `number > 255` — mutation changes to `number >= 255`
    // =========================================================================
    #region AnsiMarkupTagParser numeric boundaries

    [Fact]
    public void Style_Parse_Color0_Succeeds()
    {
        // number == 0 is valid; if < is mutated to <= this would throw
        var act = () => Style.Parse("0");
        act.Should().NotThrow();
    }

    [Fact]
    public void Style_Parse_NegativeColor_Throws()
    {
        // -1 must fail validation
        var act = () => Style.Parse("-1");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Style_Parse_Color255_Succeeds()
    {
        // 255 is valid; if > is mutated to >= this would throw
        var act = () => Style.Parse("255");
        act.Should().NotThrow();
    }

    [Fact]
    public void Style_Parse_Color256_Throws()
    {
        // 256 is invalid
        var act = () => Style.Parse("256");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Style_TryParse_NegativeColor_ReturnsFalse()
    {
        Style.TryParse("-1", out _).Should().BeFalse();
    }

    [Fact]
    public void Style_TryParse_Color256_ReturnsFalse()
    {
        Style.TryParse("256", out _).Should().BeFalse();
    }

    [Fact]
    public void Style_TryParse_Color0_ReturnsTrue()
    {
        Style.TryParse("0", out _).Should().BeTrue();
    }

    [Fact]
    public void Style_TryParse_Color255_ReturnsTrue()
    {
        Style.TryParse("255", out _).Should().BeTrue();
    }

    #endregion

    // =========================================================================
    // DecorationTable  (lines 12, 21, 23, 26)
    // Dictionary literal mutations: wrong Decoration enum value assigned.
    // =========================================================================
    #region DecorationTable entries

    [Fact]
    public void Style_Parse_None_HasNoDecoration()
    {
        Style.Parse("none").Decoration.Should().Be(Decoration.None);
    }

    [Fact]
    public void Style_Parse_Invert_HasInvertDecoration()
    {
        Style.Parse("invert").Decoration.Should().Be(Decoration.Invert);
    }

    [Fact]
    public void Style_Parse_Reverse_HasInvertDecoration()
    {
        // "reverse" is an alias for Invert
        Style.Parse("reverse").Decoration.Should().Be(Decoration.Invert);
    }

    [Fact]
    public void Style_Parse_Blink_HasSlowBlinkDecoration()
    {
        Style.Parse("blink").Decoration.Should().Be(Decoration.SlowBlink);
    }

    [Fact]
    public void Style_Parse_SlowBlink_HasSlowBlinkDecoration()
    {
        Style.Parse("slowblink").Decoration.Should().Be(Decoration.SlowBlink);
    }

    [Fact]
    public void Style_Parse_Strike_HasStrikethroughDecoration()
    {
        Style.Parse("strike").Decoration.Should().Be(Decoration.Strikethrough);
    }

    [Fact]
    public void Style_Parse_Strikethrough_HasStrikethroughDecoration()
    {
        Style.Parse("strikethrough").Decoration.Should().Be(Decoration.Strikethrough);
    }

    [Fact]
    public void Style_Parse_Bold_HasBoldDecoration()
    {
        Style.Parse("bold").Decoration.Should().Be(Decoration.Bold);
    }

    [Fact]
    public void Style_Parse_Dim_HasDimDecoration()
    {
        Style.Parse("dim").Decoration.Should().Be(Decoration.Dim);
    }

    [Fact]
    public void Style_Parse_Italic_HasItalicDecoration()
    {
        Style.Parse("italic").Decoration.Should().Be(Decoration.Italic);
    }

    [Fact]
    public void Style_Parse_Underline_HasUnderlineDecoration()
    {
        Style.Parse("underline").Decoration.Should().Be(Decoration.Underline);
    }

    [Fact]
    public void Style_Parse_Conceal_HasConcealDecoration()
    {
        Style.Parse("conceal").Decoration.Should().Be(Decoration.Conceal);
    }

    [Fact]
    public void Style_Parse_RapidBlink_HasRapidBlinkDecoration()
    {
        Style.Parse("rapidblink").Decoration.Should().Be(Decoration.RapidBlink);
    }

    #endregion

    // =========================================================================
    // ColorTable  (lines 21, 36)
    // Line 21: `number < 0 || number > 255` — boundary mutations
    // Line 36: `number > ColorPalette.EightBit.Count - 1` — off-by-one
    // =========================================================================
    #region ColorTable boundaries

    [Fact]
    public void Color_FromInt32_Zero_ReturnsColor()
    {
        var c = Color.FromInt32(0);
        c.Should().NotBe(Color.Default);
    }

    [Fact]
    public void Color_FromInt32_255_ReturnsColor()
    {
        var c = Color.FromInt32(255);
        c.Should().NotBe(Color.Default);
    }

    [Fact]
    public void Color_FromInt32_Negative_Throws()
    {
        var act = () => Color.FromInt32(-1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Color_FromInt32_256_Throws()
    {
        var act = () => Color.FromInt32(256);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Color_FromName_Red_ReturnsNonNull()
    {
        // Exercises the ColorTable.GetColor(string) path.
        // Also indirectly confirms Count-1 boundary is correct.
        Style.TryParse("red", out var style).Should().BeTrue();
        style.Foreground.Should().NotBe(Color.Default);
    }

    #endregion

    // =========================================================================
    // Style() constructor (line 33)
    // Mutation: Foreground initialiser removed or changed from Color.Default.
    // =========================================================================
    #region Style default constructor

    [Fact]
    public void Style_DefaultConstructor_ForegroundIsDefault()
    {
        var s = new Style();
        s.Foreground.Should().Be(Color.Default);
    }

    [Fact]
    public void Style_DefaultConstructor_BackgroundIsDefault()
    {
        var s = new Style();
        s.Background.Should().Be(Color.Default);
    }

    [Fact]
    public void Style_DefaultConstructor_DecorationIsNone()
    {
        var s = new Style();
        s.Decoration.Should().Be(Decoration.None);
    }

    #endregion

    // =========================================================================
    // AnsiMarkupHighlighter (many lines)
    // Exercises the Highlight() path through AnsiMarkup.Highlight().
    // Mutations include: off-by-one on endIndex, wrong slice indices,
    //                    wrong comparison operators in the foreach loop,
    //                    merge condition negation.
    // =========================================================================
    #region AnsiMarkupHighlighter

    private static readonly Style HighlightStyle = new Style(foreground: Color.FromInt32(9));

    [Fact]
    public void Highlight_EmptyQuery_ReturnsOriginalMarkup()
    {
        var markup = "Hello World";
        var result = AnsiMarkup.Highlight(markup, string.Empty, HighlightStyle);
        result.Should().Be(markup);
    }

    [Fact]
    public void Highlight_QueryNotFound_ReturnsOriginalMarkup()
    {
        var markup = "Hello World";
        var result = AnsiMarkup.Highlight(markup, "xyz", HighlightStyle);
        result.Should().Be(markup);
    }

    [Fact]
    public void Highlight_QueryAtBeginning_HighlightsCorrectPortion()
    {
        // "Hello World" — highlight "Hello"
        var result = AnsiMarkup.Highlight("Hello World", "Hello", HighlightStyle);

        // The result should contain the highlight style for "Hello" and plain for " World"
        result.Should().Contain("Hello");
        result.Should().Contain("World");

        // The result should be valid markup (parseable)
        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("Hello World");

        // First segment should have the highlight style
        var highlightedSegments = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlightedSegments.Should().NotBeEmpty();
        string.Concat(highlightedSegments.Select(s => s.Text)).Should().Be("Hello");
    }

    [Fact]
    public void Highlight_QueryAtEnd_HighlightsCorrectPortion()
    {
        var result = AnsiMarkup.Highlight("Hello World", "World", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("Hello World");

        var highlightedSegments = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlightedSegments.Should().NotBeEmpty();
        string.Concat(highlightedSegments.Select(s => s.Text)).Should().Be("World");
    }

    [Fact]
    public void Highlight_QueryInMiddle_HighlightsCorrectPortion()
    {
        var result = AnsiMarkup.Highlight("Hello World", "lo Wo", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("Hello World");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("lo Wo");
    }

    [Fact]
    public void Highlight_QueryIsSingleChar_HighlightsCorrectly()
    {
        var result = AnsiMarkup.Highlight("abc", "b", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("abc");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("b");
    }

    [Fact]
    public void Highlight_QueryIsEntireText_HighlightsEverything()
    {
        var result = AnsiMarkup.Highlight("Hello", "Hello", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("Hello");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("Hello");
    }

    [Fact]
    public void Highlight_WithExistingMarkup_PreservesPlainText()
    {
        // "[bold]Hello[/] World" — highlight "World"
        var result = AnsiMarkup.Highlight("[bold]Hello[/] World", "World", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("Hello World");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("World");
    }

    [Fact]
    public void Highlight_WithExistingMarkup_HighlightInStyledSection()
    {
        // "[bold]Hello World[/]" — highlight "Hello"
        var result = AnsiMarkup.Highlight("[bold]Hello World[/]", "Hello", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("Hello World");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("Hello");
    }

    [Fact]
    public void Highlight_QuerySpansMarkupBoundary_HighlightsCorrectly()
    {
        // "[bold]Hel[/]lo World" — query "Hello"
        // The query spans the markup boundary between styled "Hel" and plain "lo World"
        var result = AnsiMarkup.Highlight("[bold]Hel[/]lo World", "Hello", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("Hello World");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        var highlightedText = string.Concat(highlighted.Select(s => s.Text));
        highlightedText.Should().Be("Hello");
    }

    [Fact]
    public void Highlight_MatchNotInFirstSegment_HighlightsCorrectly()
    {
        // "AAA [bold]BBB[/] CCC" — query "CCC" (last segment)
        var result = AnsiMarkup.Highlight("AAA [bold]BBB[/] CCC", "CCC", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("AAA BBB CCC");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("CCC");
    }

    [Fact]
    public void Highlight_AdjacentSegmentsWithSameStyle_MergedCorrectly()
    {
        // Plain text: the highlighted segment and surrounding text all have Style.Plain.
        // Ensure MergeSegments consolidates them properly.
        var result = AnsiMarkup.Highlight("abc", "b", Style.Plain);
        // With plain highlight style, the result text content should still be "abc"
        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("abc");
    }

    [Fact]
    public void Highlight_NullMarkup_Throws()
    {
        var act = () => AnsiMarkup.Highlight(null!, "query", HighlightStyle);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Highlight_NullQuery_Throws()
    {
        var act = () => AnsiMarkup.Highlight("markup", null!, HighlightStyle);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    // =========================================================================
    // AnsiWriter — WriteSgr (line 94: shouldClose = WriteSgr(...)  )
    // Mutation: shouldClose is always false (never resets), or style codes
    //           for background are not added.
    // =========================================================================
    #region AnsiWriter.Write(text, style)

    [Fact]
    public void AnsiWriter_WriteWithBoldStyle_EmitsOpenAndResetSgr()
    {
        var (writer, output) = CreateWriter();
        writer.Write("X", new Style(decoration: Decoration.Bold));
        var result = output.ToString();

        // Should contain SGR bold (1m) and reset (0m)
        result.Should().Contain("\e[1m");
        result.Should().Contain("\e[0m");
        result.Should().Contain("X");
    }

    [Fact]
    public void AnsiWriter_WriteWithForeground_EmitsColorEscape()
    {
        var (writer, output) = CreateWriter();
        // TrueColor with RGB(255,0,0)
        writer.Write("X", new Style(foreground: new Color(255, 0, 0)));
        var result = output.ToString();

        // TrueColor foreground: ESC[38;2;255;0;0m
        result.Should().Contain("38;2;255;0;0");
        result.Should().Contain("X");
    }

    [Fact]
    public void AnsiWriter_WriteWithBackground_EmitsBackgroundColorEscape()
    {
        var (writer, output) = CreateWriter();
        writer.Write("X", new Style(background: new Color(0, 0, 255)));
        var result = output.ToString();

        // TrueColor background: ESC[48;2;0;0;255m
        result.Should().Contain("48;2;0;0;255");
        result.Should().Contain("X");
    }

    [Fact]
    public void AnsiWriter_WriteNoStyle_NoEscapeSequences()
    {
        var (writer, output) = CreateWriter();
        writer.Write("Hello", Style.Plain);
        var result = output.ToString();

        result.Should().Be("Hello");
    }

    [Fact]
    public void AnsiWriter_WriteWithAnsiDisabled_NoEscapeSequences()
    {
        var (writer, output) = CreateWriter(ansi: false);
        writer.Write("Hello", new Style(decoration: Decoration.Bold));
        output.ToString().Should().Be("Hello");
    }

    #endregion

    // =========================================================================
    // AnsiWriter.ResetStyle  (line 191: WriteSgr(0) — mutation removes call)
    // =========================================================================
    #region AnsiWriter.ResetStyle

    [Fact]
    public void AnsiWriter_ResetStyle_EmitsSgr0()
    {
        var (writer, output) = CreateWriter();
        writer.ResetStyle();
        output.ToString().Should().Contain("\e[0m");
    }

    [Fact]
    public void AnsiWriter_ResetStyle_AnsiDisabled_EmitsNothing()
    {
        var (writer, output) = CreateWriter(ansi: false);
        writer.ResetStyle();
        output.ToString().Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // AnsiWriter.Decoration  (line 209: WriteSgr codes)
    // =========================================================================
    #region AnsiWriter.Decoration

    [Fact]
    public void AnsiWriter_Decoration_Bold_EmitsSgr1()
    {
        var (writer, output) = CreateWriter();
        writer.Decoration(Decoration.Bold);
        output.ToString().Should().Contain("\e[1m");
    }

    [Fact]
    public void AnsiWriter_Decoration_Italic_EmitsSgr3()
    {
        var (writer, output) = CreateWriter();
        writer.Decoration(Decoration.Italic);
        output.ToString().Should().Contain("\e[3m");
    }

    [Fact]
    public void AnsiWriter_Decoration_Underline_EmitsSgr4()
    {
        var (writer, output) = CreateWriter();
        writer.Decoration(Decoration.Underline);
        output.ToString().Should().Contain("\e[4m");
    }

    [Fact]
    public void AnsiWriter_Decoration_Strikethrough_EmitsSgr9()
    {
        var (writer, output) = CreateWriter();
        writer.Decoration(Decoration.Strikethrough);
        output.ToString().Should().Contain("\e[9m");
    }

    [Fact]
    public void AnsiWriter_Decoration_AnsiDisabled_EmitsNothing()
    {
        var (writer, output) = CreateWriter(ansi: false);
        writer.Decoration(Decoration.Bold);
        output.ToString().Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // AnsiWriter.BeginLink / EndLink  (lines 285, 292, 309)
    // Line 285: null guard
    // Line 292: OSC 8 link output — mutation could remove linkId branch
    // Line 309: EndLink guard `_linkCount > 0`
    // =========================================================================
    #region AnsiWriter.BeginLink / EndLink

    [Fact]
    public void AnsiWriter_BeginLink_NullString_Throws()
    {
        var (writer, _) = CreateWriter();
        var act = () => writer.BeginLink((string)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AnsiWriter_BeginLink_NullLink_Throws()
    {
        var (writer, _) = CreateWriter();
        var act = () => writer.BeginLink((Link)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AnsiWriter_BeginLink_WithoutLinkId_EmitsOsc8WithoutId()
    {
        var (writer, output) = CreateWriter(links: true);
        writer.BeginLink("https://example.com");
        var result = output.ToString();

        result.Should().Contain("\e]8;;https://example.com");
        result.Should().NotContain("id=");
    }

    [Fact]
    public void AnsiWriter_BeginLink_WithLinkId_EmitsOsc8WithId()
    {
        var (writer, output) = CreateWriter(links: true);
        writer.BeginLink("https://example.com", linkId: 42);
        var result = output.ToString();

        result.Should().Contain("\e]8;id=42;https://example.com");
    }

    [Fact]
    public void AnsiWriter_EndLink_AfterBeginLink_EmitsOsc8Close()
    {
        var (writer, output) = CreateWriter(links: true);
        writer.BeginLink("https://example.com");
        output.GetStringBuilder().Clear();
        writer.EndLink();
        output.ToString().Should().Contain("\e]8;;\e\\");
    }

    [Fact]
    public void AnsiWriter_EndLink_WithoutBeginLink_EmitsNothing()
    {
        // _linkCount == 0, so no output should be emitted
        var (writer, output) = CreateWriter(links: true);
        writer.EndLink();
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void AnsiWriter_BeginLink_LinksDisabled_EmitsNothing()
    {
        var (writer, output) = CreateWriter(links: false);
        writer.BeginLink("https://example.com");
        output.ToString().Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // AnsiWriter.WriteCsi (line 600-601)
    // Line 600: `decPrivateMode ? $"\e[?{parameters}" : $"\e[{parameters}"`
    // Mutation: always emits private mode prefix, or never does.
    // =========================================================================
    #region AnsiWriter.WriteCsi

    [Fact]
    public void AnsiWriter_CursorUp_EmitsCorrectCsiSequence()
    {
        var (writer, output) = CreateWriter();
        writer.CursorUp(3);
        output.ToString().Should().Be("\e[3A");
    }

    [Fact]
    public void AnsiWriter_CursorDown_EmitsCorrectCsiSequence()
    {
        var (writer, output) = CreateWriter();
        writer.CursorDown(2);
        output.ToString().Should().Be("\e[2B");
    }

    [Fact]
    public void AnsiWriter_CursorRight_EmitsCorrectCsiSequence()
    {
        var (writer, output) = CreateWriter();
        writer.CursorRight(5);
        output.ToString().Should().Be("\e[5C");
    }

    [Fact]
    public void AnsiWriter_CursorLeft_EmitsCorrectCsiSequence()
    {
        var (writer, output) = CreateWriter();
        writer.CursorLeft(1);
        output.ToString().Should().Be("\e[1D");
    }

    [Fact]
    public void AnsiWriter_ShowCursor_EmitsDecPrivateMode()
    {
        var (writer, output) = CreateWriter();
        writer.ShowCursor();
        // Must use DEC private mode: ESC[?25h
        output.ToString().Should().Be("\e[?25h");
    }

    [Fact]
    public void AnsiWriter_HideCursor_EmitsDecPrivateMode()
    {
        var (writer, output) = CreateWriter();
        writer.HideCursor();
        output.ToString().Should().Be("\e[?25l");
    }

    [Fact]
    public void AnsiWriter_CursorPosition_EmitsCorrectSequence()
    {
        var (writer, output) = CreateWriter();
        writer.CursorPosition(5, 10);
        output.ToString().Should().Be("\e[5;10H");
    }

    [Fact]
    public void AnsiWriter_SaveCursor_EmitsCorrectSequence()
    {
        var (writer, output) = CreateWriter();
        writer.SaveCursor();
        output.ToString().Should().Be("\e[s");
    }

    [Fact]
    public void AnsiWriter_RestoreCursor_EmitsCorrectSequence()
    {
        var (writer, output) = CreateWriter();
        writer.RestoreCursor();
        output.ToString().Should().Be("\e[u");
    }

    [Fact]
    public void AnsiWriter_CursorHome_EmitsCorrectSequence()
    {
        var (writer, output) = CreateWriter();
        writer.CursorHome();
        output.ToString().Should().Be("\e[H");
    }

    [Fact]
    public void AnsiWriter_EraseInLine_DefaultMode_EmitsMode0()
    {
        var (writer, output) = CreateWriter();
        writer.EraseInLine();
        output.ToString().Should().Be("\e[0K");
    }

    [Fact]
    public void AnsiWriter_EraseInDisplay_DefaultMode_EmitsMode0()
    {
        var (writer, output) = CreateWriter();
        writer.EraseInDisplay();
        output.ToString().Should().Be("\e[0J");
    }

    [Fact]
    public void AnsiWriter_ClearScrollback_EmitsMode3J()
    {
        var (writer, output) = CreateWriter();
        writer.ClearScrollback();
        output.ToString().Should().Be("\e[3J");
    }

    [Fact]
    public void AnsiWriter_EnterAltScreen_EmitsDecPrivateMode1049h()
    {
        var (writer, output) = CreateWriter();
        writer.EnterAltScreen();
        output.ToString().Should().Be("\e[?1049h");
    }

    [Fact]
    public void AnsiWriter_ExitAltScreen_EmitsDecPrivateMode1049l()
    {
        var (writer, output) = CreateWriter();
        writer.ExitAltScreen();
        output.ToString().Should().Be("\e[?1049l");
    }

    [Fact]
    public void AnsiWriter_AnsiDisabled_CursorUp_EmitsNothing()
    {
        var (writer, output) = CreateWriter(ansi: false);
        writer.CursorUp(5);
        output.ToString().Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // AnsiWriter.WriteOsc (line 611)
    // Mutation: missing "\e]" prefix.
    // =========================================================================
    #region AnsiWriter.WriteOsc

    [Fact]
    public void AnsiWriter_BeginLink_OscPrefix_IsEscBracket()
    {
        var (writer, output) = CreateWriter(links: true);
        writer.BeginLink("http://test.example");
        // The OSC sequence must start with ESC ]
        output.ToString().Should().StartWith("\e]");
    }

    #endregion

    // =========================================================================
    // AnsiCodeBuilder color methods (lines 701-730)
    // GetFourBit: `number < 8` decides mod (foreground 30/40 vs 82/92)
    // GetEightBit: foreground mod 38 vs 48
    // GetTrueColor: RGB bytes in correct order
    // =========================================================================
    #region AnsiCodeBuilder color output (via AnsiWriter.Foreground / Background)

    // --- Four-bit (Standard color system) ---

    [Fact]
    public void AnsiWriter_Foreground_FourBit_LowColor_EmitsMod30()
    {
        // Color 0 (black) is < 8 → mod 30 → code 30
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        writer.Foreground((Color)0);
        output.ToString().Should().Contain("\e[30m");
    }

    [Fact]
    public void AnsiWriter_Foreground_FourBit_HighColor_EmitsMod82()
    {
        // Color 8 (dark gray) is >= 8 → foreground mod 82 → code 90
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        writer.Foreground((Color)8);
        output.ToString().Should().Contain("\e[90m");
    }

    [Fact]
    public void AnsiWriter_Background_FourBit_LowColor_EmitsMod40()
    {
        // Color 0 (black) is < 8 → background mod 40 → code 40
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        writer.Background((Color)0);
        output.ToString().Should().Contain("\e[40m");
    }

    [Fact]
    public void AnsiWriter_Background_FourBit_HighColor_EmitsMod92()
    {
        // Color 8 is >= 8 → background mod 92 → code 100
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        writer.Background((Color)8);
        output.ToString().Should().Contain("\e[100m");
    }

    // --- Eight-bit color system ---

    [Fact]
    public void AnsiWriter_Foreground_EightBit_EmitsMod38()
    {
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.EightBit);
        writer.Foreground((Color)9); // ANSI Red
        var result = output.ToString();
        result.Should().Contain("38;5;9");
    }

    [Fact]
    public void AnsiWriter_Background_EightBit_EmitsMod48()
    {
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.EightBit);
        writer.Background((Color)9);
        var result = output.ToString();
        result.Should().Contain("48;5;9");
    }

    // --- TrueColor color system (RGB) ---

    [Fact]
    public void AnsiWriter_Foreground_TrueColor_EmitsRgbComponents()
    {
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Foreground(new Color(100, 150, 200));
        var result = output.ToString();
        // ESC[38;2;100;150;200m
        result.Should().Contain("38;2;100;150;200");
    }

    [Fact]
    public void AnsiWriter_Background_TrueColor_EmitsRgbComponents()
    {
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Background(new Color(10, 20, 30));
        var result = output.ToString();
        // ESC[48;2;10;20;30m
        result.Should().Contain("48;2;10;20;30");
    }

    [Fact]
    public void AnsiWriter_Foreground_TrueColor_RgbOrdering_RedFirst()
    {
        // Verify R comes before G and B in the sequence
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Foreground(new Color(255, 1, 2));
        var result = output.ToString();
        result.Should().Contain("38;2;255;1;2");
    }

    [Fact]
    public void AnsiWriter_Background_TrueColor_RgbOrdering_BlueIsLast()
    {
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Background(new Color(1, 2, 255));
        var result = output.ToString();
        result.Should().Contain("48;2;1;2;255");
    }

    [Fact]
    public void AnsiWriter_Foreground_TrueColor_WithNamedColor_EmitsEightBitFallback()
    {
        // Named colors have a Number, so TrueColor falls back to 8-bit path
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Foreground((Color)9); // Number = 9, TrueColor uses GetEightBit
        var result = output.ToString();
        result.Should().Contain("38;5;9");
    }

    // --- NoColors system ---

    [Fact]
    public void AnsiWriter_Foreground_NoColors_EmitsNoColorCodes()
    {
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.NoColors);
        writer.Foreground(new Color(255, 0, 0));
        output.ToString().Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // AnsiWriter chaining (fluent interface)
    // =========================================================================
    #region Fluent chaining

    [Fact]
    public void AnsiWriter_FluentChain_ReturnsItself()
    {
        var (writer, _) = CreateWriter();
        var result = writer.Write("a").Write("b").Write("c");
        result.Should().BeSameAs(writer);
    }

    [Fact]
    public void AnsiWriter_WriteLine_NoArgs_WritesNewLine()
    {
        var (writer, output) = CreateWriter();
        writer.WriteLine();
        output.ToString().Should().Be(Environment.NewLine);
    }

    [Fact]
    public void AnsiWriter_WriteLineText_WritesTextThenNewLine()
    {
        var (writer, output) = CreateWriter();
        writer.WriteLine("hello");
        output.ToString().Should().Be("hello" + Environment.NewLine);
    }

    [Fact]
    public void AnsiWriter_WriteLineWithStyle_WritesStyledTextThenNewLine()
    {
        var (writer, output) = CreateWriter();
        writer.WriteLine("hello", new Style(decoration: Decoration.Bold));
        var result = output.ToString();
        result.Should().Contain("hello");
        result.Should().EndWith(Environment.NewLine);
    }

    #endregion

    // =========================================================================
    // AnsiWriter.Style() method  (separate from the struct — line 173)
    // =========================================================================
    #region AnsiWriter.Style method

    [Fact]
    public void AnsiWriter_StyleMethod_EmitsSgrWithoutText()
    {
        var (writer, output) = CreateWriter();
        writer.Style(new Style(decoration: Decoration.Bold));
        var result = output.ToString();
        result.Should().Contain("\e[1m");
        // No reset — Style() doesn't close
        result.Should().NotContain("\e[0m");
    }

    [Fact]
    public void AnsiWriter_StyleMethod_AnsiDisabled_EmitsNothing()
    {
        var (writer, output) = CreateWriter(ansi: false);
        writer.Style(new Style(decoration: Decoration.Bold));
        output.ToString().Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // AnsiWriter constructor null guards
    // =========================================================================
    #region Constructor null guards

    [Fact]
    public void AnsiWriter_Constructor_NullOutput_Throws()
    {
        var act = () => new AnsiWriter(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AnsiWriter_ConstructorWithCapabilities_NullOutput_Throws()
    {
        var caps = new AnsiCapabilities { Ansi = true, ColorSystem = ColorSystem.TrueColor };
        var act = () => new AnsiWriter(null!, caps);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AnsiWriter_ConstructorWithCapabilities_NullCapabilities_Throws()
    {
        var act = () => new AnsiWriter(new StringWriter(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    // =========================================================================
    // AnsiMarkup constructor + null guards  (line 16, 51, 53, 68)
    // =========================================================================
    #region AnsiMarkup construction and null guards

    [Fact]
    public void AnsiMarkup_Constructor_NullWriter_Throws()
    {
        var act = () => new AnsiMarkup(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AnsiMarkup_Parse_NullMarkup_Throws()
    {
        // Exercises the ThrowIfNull at AnsiMarkup.Parse line 51
        var act = () => AnsiMarkup.Parse(null!).ToList();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AnsiMarkup_Parse_NullStyle_UsesPlainStyle()
    {
        // `style ??= Style.Plain` on line 53 — if removed, style stays null → NullReferenceException
        var segments = AnsiMarkup.Parse("hello", null).ToList();
        segments.Should().ContainSingle();
        segments[0].Style.Should().Be(Style.Plain);
    }

    [Fact]
    public void AnsiMarkup_Parse_LinkTag_SetsLinkOnSegment()
    {
        // `link ??= parsed.Link` on line 68 — if removed, link remains null
        var segments = AnsiMarkup.Parse("[link=https://example.com]click[/]").ToList();
        segments.Should().ContainSingle();
        segments[0].Link.Should().NotBeNull();
        segments[0].Text.Should().Be("click");
    }

    [Fact]
    public void AnsiMarkup_Parse_MalformedOpenBracketAtEnd_Throws()
    {
        // ThrowMalformed at line 301 — single '[' at end is malformed
        var act = () => AnsiMarkup.Parse("[").ToList();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AnsiMarkup_Parse_CloseTagWithNoOpen_Throws()
    {
        // Unbalanced close tag should throw
        var act = () => AnsiMarkup.Parse("[/]").ToList();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AnsiMarkup_Parse_BalancedTags_Succeeds()
    {
        // Verifies close token round-trip (line 324 — string.Empty for Close token value)
        var segments = AnsiMarkup.Parse("[bold]text[/]").ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("text");
    }

    [Fact]
    public void AnsiMarkup_Parse_MultipleSegments_MergesSameStyle()
    {
        // Tests that adjacent same-style segments are merged (covers AnsiMarkup.Parse line 84)
        var segments = AnsiMarkup.Parse("hello world").ToList();
        segments.Should().ContainSingle();
        segments[0].Text.Should().Be("hello world");
    }

    [Fact]
    public void AnsiMarkup_Parse_UnbalancedOpenTag_Throws()
    {
        var act = () => AnsiMarkup.Parse("[bold]unclosed").ToList();
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    // =========================================================================
    // AnsiMarkup link/bracket tokenizer paths  (lines 329, 347, 360, 363, 366)
    // Tests the `currentStylePartCanContainMarkup` flag in ReadMarkup()
    // =========================================================================
    #region AnsiMarkup link tokenizer

    [Fact]
    public void AnsiMarkup_Parse_LinkTag_WithUrl_ParsesCorrectly()
    {
        // Exercises the link= detection path (lines 363-366)
        var segments = AnsiMarkup.Parse("[link=https://example.com]visit[/]").ToList();
        segments[0].Link.Should().NotBeNull();
        segments[0].Link!.Url.Should().Be("https://example.com");
    }

    [Fact]
    public void AnsiMarkup_Parse_StyleAndLinkTag_ParsesBoth()
    {
        // Tests space resets currentStylePartCanContainMarkup (line 360)
        // Then link= after the space triggers link mode
        var segments = AnsiMarkup.Parse("[bold link=https://example.com]text[/]").ToList();
        segments[0].Style.Decoration.Should().HaveFlag(Decoration.Bold);
        segments[0].Link.Should().NotBeNull();
    }

    [Fact]
    public void AnsiMarkup_Parse_NonLinkBracketInStyle_Throws()
    {
        // `[` in non-link markup that's not `[[` should throw (line 347)
        var act = () => AnsiMarkup.Parse("[bold[x]text[/]").ToList();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AnsiMarkup_Escape_ThenParse_RoundTrip()
    {
        // Tests that escaped text round-trips correctly through the tokenizer
        var original = "Hello [World]";
        var escaped = AnsiMarkup.Escape(original);
        var removed = AnsiMarkup.Remove(escaped);
        removed.Should().Be(original);
    }

    #endregion

    // =========================================================================
    // AnsiMarkupHighlighter: edge cases for surviving mutants
    // Line 7:  null markup with empty query (not caught by deeper null check)
    // Line 21: block removal — query not found with link markup (loses link info)
    // Line 33: equality — query starts at boundary (== EndIndex of previous part)
    // Line 62: equality — part.StartIndex < endIndex boundary
    // Line 65: equality — remaining > part.Text.Length (query spans entire part)
    // Line 73: equality — remaining < part.Text.Length (partial end in part)
    // Line 97: MergeSegments index > 0 guard
    // =========================================================================
    #region AnsiMarkupHighlighter edge cases

    [Fact]
    public void Highlight_NullMarkupWithEmptyQuery_Throws()
    {
        // With empty query, line 10-13 returns markup before calling Parse.
        // If line 7 ThrowIfNull is removed, null is returned instead of throwing.
        var act = () => AnsiMarkup.Highlight(null!, string.Empty, HighlightStyle);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Highlight_QueryNotFound_WithLinkMarkup_PreservesOriginalString()
    {
        // Line 21 block removal: reconstructed segments drop link info from ToString(),
        // producing "visit" instead of "[link=...]visit[/]"
        var markup = "[link=https://example.com]visit[/]";
        var result = AnsiMarkup.Highlight(markup, "xyz", HighlightStyle);
        result.Should().Be(markup);
    }

    [Fact]
    public void Highlight_QueryAtExactStartOfSecondSegment_HighlightsCorrectly()
    {
        // Line 33: startIndex == part.StartIndex of second segment
        // Mutation >= → > would fail since startIndex == part.StartIndex
        // Use "[bold]AAA[/]BBB" and query "BBB" → startIndex=3, second part starts at 3
        var markup = "[bold]AAA[/]BBB";
        var result = AnsiMarkup.Highlight(markup, "BBB", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("AAABBB");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("BBB");
    }

    [Fact]
    public void Highlight_QuerySpansEntireSecondPart_HighlightsEntirePart()
    {
        // Line 62: part.StartIndex < endIndex — after first part found, subsequent parts
        // Line 65: remaining > part.Text.Length — query spans entire subsequent part
        // Markup: "[bold]Hello[/][italic] World[/]", query "ello W"
        // startIndex=1 (in "Hello" part), endIndex=7, " World" part starts at 5
        var markup = "[bold]Hello[/][italic] World[/]";
        var result = AnsiMarkup.Highlight(markup, "ello W", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("Hello World");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("ello W");
    }

    [Fact]
    public void Highlight_QueryEndsPartwayThroughSecondSegment_HighlightsPartially()
    {
        // Line 73: remaining < part.Text.Length — query ends partway through a subsequent part
        // "[bold]AB[/]CDE", query "ABC" → startIndex=0, endIndex=3
        // Second part "CDE" starts at 2, remaining=3-2=1 < 3 → partial end
        var markup = "[bold]AB[/]CDE";
        var result = AnsiMarkup.Highlight(markup, "ABC", HighlightStyle);

        var segments = AnsiMarkup.Parse(result).ToList();
        var plain = string.Concat(segments.Select(s => s.Text));
        plain.Should().Be("ABCDE");

        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("ABC");
    }

    [Fact]
    public void Highlight_SingleSegment_MergeSegments_SingleElementPath()
    {
        // Line 97: index > 0 && result.Count > 0 — first element goes to else branch
        // Single segment, verify it is emitted correctly (index=0 path)
        var result = AnsiMarkup.Highlight("Hello", "Hello", HighlightStyle);
        var segments = AnsiMarkup.Parse(result).ToList();
        var highlighted = segments.Where(s => s.Style.Foreground == HighlightStyle.Foreground).ToList();
        highlighted.Should().NotBeEmpty();
        string.Concat(highlighted.Select(s => s.Text)).Should().Be("Hello");
    }

    #endregion

    // =========================================================================
    // AnsiMarkupTagParser: rgb color parsing (lines 193, 198)
    // Line 193: normalized.Length >= 3  — mutation >=3 → >3 would reject "rgb"
    // Line 198: normalized.StartsWith("(") — string mutation
    // =========================================================================
    #region AnsiMarkupTagParser rgb color

    [Fact]
    public void Style_Parse_RgbColor_Succeeds()
    {
        // Exercises the rgb(...) path in AnsiMarkupTagParser (lines 193, 198)
        var act = () => Style.Parse("rgb(255,0,0)");
        act.Should().NotThrow();
        Style.Parse("rgb(255,0,0)").Foreground.R.Should().Be(255);
    }

    [Fact]
    public void Style_Parse_RgbColorInBackground_Succeeds()
    {
        var act = () => Style.Parse("on rgb(0,128,255)");
        act.Should().NotThrow();
        Style.Parse("on rgb(0,128,255)").Background.B.Should().Be(255);
    }

    #endregion

    // =========================================================================
    // AnsiWriter.Style method: foreground/background codes (lines 168-171)
    // Line 168: _codes.Clear() — sequential calls must not accumulate
    // Line 170: AddRange foreground (Statement + Boolean mutations)
    // Line 171: AddRange background (Statement + Boolean mutations)
    // =========================================================================
    #region AnsiWriter.Style method foreground/background

    [Fact]
    public void AnsiWriter_StyleMethod_WithForeground_EmitsForegroundCode()
    {
        // Kills line 170 Statement mutation (remove AddRange foreground)
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Style(new Style(foreground: new Color(200, 100, 50)));
        output.ToString().Should().Contain("38;2;200;100;50");
    }

    [Fact]
    public void AnsiWriter_StyleMethod_WithBackground_EmitsBackgroundCode()
    {
        // Kills line 171 Statement mutation (remove AddRange background)
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Style(new Style(background: new Color(10, 20, 30)));
        output.ToString().Should().Contain("48;2;10;20;30");
    }

    [Fact]
    public void AnsiWriter_StyleMethod_ForegroundParam_IsTrueNotFalse()
    {
        // Kills line 170 Boolean mutation (true → false for foreground param)
        // If foreground=false is used, produces 48;2;... instead of 38;2;...
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Style(new Style(foreground: new Color(255, 0, 0)));
        output.ToString().Should().Contain("38;2;255;0;0");
        output.ToString().Should().NotContain("48;2;255;0;0");
    }

    [Fact]
    public void AnsiWriter_StyleMethod_BackgroundParam_IsFalseNotTrue()
    {
        // Kills line 171 Boolean mutation (false → true for background param)
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Style(new Style(background: new Color(0, 0, 255)));
        output.ToString().Should().Contain("48;2;0;0;255");
        output.ToString().Should().NotContain("38;2;0;0;255");
    }

    [Fact]
    public void AnsiWriter_StyleMethod_CalledTwice_SecondCallHasCleanCodes()
    {
        // Kills line 168 Statement mutation (_codes.Clear() removed)
        // Without Clear(), second call includes codes from first call
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Style(new Style(foreground: new Color(255, 0, 0)));
        output.GetStringBuilder().Clear();
        writer.Style(new Style(foreground: new Color(0, 255, 0)));
        // Must emit ONLY green foreground, not red+green
        var result = output.ToString();
        result.Should().Contain("38;2;0;255;0");
        result.Should().NotContain("255;0;0");
    }

    [Fact]
    public void AnsiWriter_Decoration_CalledTwice_SecondCallHasCleanCodes()
    {
        // Kills line 209 Statement mutation (_codes.Clear() in Decoration removed)
        var (writer, output) = CreateWriter();
        writer.Decoration(Decoration.Bold);
        output.GetStringBuilder().Clear();
        writer.Decoration(Decoration.Italic);
        output.ToString().Should().Be("\e[3m");
    }

    [Fact]
    public void AnsiWriter_Background_CalledTwice_SecondCallHasCleanCodes()
    {
        // Kills line 230 Statement mutation (_codes.Clear() in Background removed)
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Background(new Color(255, 0, 0));
        output.GetStringBuilder().Clear();
        writer.Background(new Color(0, 0, 255));
        var result = output.ToString();
        result.Should().Contain("48;2;0;0;255");
        result.Should().NotContain("255;0;0");
    }

    #endregion

    // =========================================================================
    // AnsiCodeBuilder.GetFourBit: unnamed color + color boundary (lines 701-712)
    // Line 701: number == null || color.Number >= 16 — mutations: ||→&&, ==null→!=null, >=16→>16
    // Line 712: number < 8 ? ... : ... — mod selection
    // =========================================================================
    #region AnsiCodeBuilder GetFourBit boundary cases

    [Fact]
    public void AnsiWriter_Foreground_FourBit_UnnamedColor_QuantizesToValidCode()
    {
        // unnamed color (Number=null) in 4-bit mode: must quantize (line 701 number==null)
        // Kills ||→&& and ==null→!=null mutations
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        var act = () => writer.Foreground(new Color(0, 0, 0)); // unnamed black — R,G,B but no Number
        act.Should().NotThrow();
        output.ToString().Should().Contain("\e["); // some escape code emitted
    }

    [Fact]
    public void AnsiWriter_Foreground_FourBit_Color16_QuantizesWithoutException()
    {
        // color.Number >= 16 in 4-bit mode: must quantize (line 701 >=16 → >16 mutation)
        // With >16 mutation: color 16 not quantized → switch _ → throws
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        var act = () => writer.Foreground((Color)16);
        act.Should().NotThrow();
        // Must produce a valid 4-bit code in the 30-47 / 90-97 range
        output.ToString().Should().MatchRegex(@"\x1b\[(?:3[0-7]|4[0-7]|9[0-7]|10[0-7])m");
    }

    [Fact]
    public void AnsiWriter_Foreground_FourBit_Color7_EmitsMod37NotMod97()
    {
        // Color 7 (Gray) — number < 8, so mod=30, code=37 (not 90+7=97)
        // Kills the number < 8 equality/logical mutations at line 712
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        writer.Foreground((Color)7);
        output.ToString().Should().Contain("\e[37m");
    }

    [Fact]
    public void AnsiWriter_Background_FourBit_Color7_EmitsMod47NotMod102()
    {
        // Color 7 background — number < 8, mod=40, code=47
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        writer.Background((Color)7);
        output.ToString().Should().Contain("\e[47m");
    }

    [Fact]
    public void AnsiWriter_Foreground_FourBit_Color8_EmitsMod90()
    {
        // Color 8 (DarkGray) — number >= 8, foreground mod=82, code=90
        // Kills number<8 boundary mutation
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.Standard);
        writer.Foreground((Color)8);
        output.ToString().Should().Contain("\e[90m");
    }

    #endregion

    // =========================================================================
    // AnsiCodeBuilder.GetEightBit: null coalescing (line 714)
    // `var number = color.Number ?? color.ExactOrClosest(ColorSystem.EightBit).Number`
    // Mutations: remove left (always quantize), remove right (crash if Number is null)
    // =========================================================================
    #region AnsiCodeBuilder GetEightBit null coalescing

    [Fact]
    public void AnsiWriter_Foreground_EightBit_NamedColor_UsesNumberDirectly()
    {
        // color.Number is set → uses Number without calling ExactOrClosest
        // Kills "remove left" mutation (would always call ExactOrClosest)
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.EightBit);
        writer.Foreground((Color)200); // named 8-bit color with Number=200
        output.ToString().Should().Contain("38;5;200");
    }

    [Fact]
    public void AnsiWriter_Foreground_EightBit_UnnamedColor_QuantizesViaExactOrClosest()
    {
        // color.Number is null → falls back to ExactOrClosest
        // Kills "remove right" mutation (would throw NullReferenceException)
        var (writer, output) = CreateWriter(colorSystem: ColorSystem.EightBit);
        var act = () => writer.Foreground(new Color(128, 0, 0)); // unnamed color
        act.Should().NotThrow();
        output.ToString().Should().Contain("38;5;"); // some 8-bit code
    }

    #endregion

    // =========================================================================
    // Color.GetHashCode: exact FNV-1a formula verification (lines 98-100)
    // Kills arithmetic (×→+), bitwise (^→|/&) mutations precisely
    // =========================================================================
    #region Color.GetHashCode exact FNV formula

    [Fact]
    public void Color_GetHashCode_MatchesExactFnvFormula_AllComponents()
    {
        // Computes the expected hash using the exact formula and verifies it matches.
        // Any mutation of * → +, ^ → |, ^ → &, or component removal will fail.
        unchecked
        {
            var expected = (int)2166136261;
            expected = (expected * 16777619) ^ 42;   // R=42
            expected = (expected * 16777619) ^ 128;  // G=128
            expected = (expected * 16777619) ^ 200;  // B=200
            new Color(42, 128, 200).GetHashCode().Should().Be(expected);
        }
    }

    [Fact]
    public void Color_GetHashCode_MatchesExactFnvFormula_ROnly()
    {
        unchecked
        {
            var expected = (int)2166136261;
            expected = (expected * 16777619) ^ 1;    // R=1
            expected = (expected * 16777619) ^ 0;    // G=0
            expected = (expected * 16777619) ^ 0;    // B=0
            new Color(1, 0, 0).GetHashCode().Should().Be(expected);
        }
    }

    [Fact]
    public void Color_GetHashCode_MatchesExactFnvFormula_GOnly()
    {
        unchecked
        {
            var expected = (int)2166136261;
            expected = (expected * 16777619) ^ 0;    // R=0
            expected = (expected * 16777619) ^ 1;    // G=1
            expected = (expected * 16777619) ^ 0;    // B=0
            new Color(0, 1, 0).GetHashCode().Should().Be(expected);
        }
    }

    [Fact]
    public void Color_GetHashCode_MatchesExactFnvFormula_BOnly()
    {
        unchecked
        {
            var expected = (int)2166136261;
            expected = (expected * 16777619) ^ 0;    // R=0
            expected = (expected * 16777619) ^ 0;    // G=0
            expected = (expected * 16777619) ^ 1;    // B=1
            new Color(0, 0, 1).GetHashCode().Should().Be(expected);
        }
    }

    #endregion

    // =========================================================================
    // Color.Equals / equality  (lines 185-186 in Equals method)
    // IsDefault path, R/G/B equality, logical operators
    // =========================================================================
    #region Color.Equals edge cases

    [Fact]
    public void Color_Equals_BothDefault_ReturnsTrue()
    {
        // (IsDefault && other.IsDefault) branch
        Color.Default.Equals(Color.Default).Should().BeTrue();
    }

    [Fact]
    public void Color_Equals_OneDefault_ReturnsFalse()
    {
        // !IsDefault vs IsDefault — logical mutation || → &&
        var a = new Color(0, 0, 0);   // NOT default
        Color.Default.Equals(a).Should().BeFalse();
        a.Equals(Color.Default).Should().BeFalse();
    }

    [Fact]
    public void Color_Equals_SameRgb_ReturnsTrue()
    {
        var a = new Color(10, 20, 30);
        var b = new Color(10, 20, 30);
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Color_Equals_DifferentR_ReturnsFalse()
    {
        new Color(1, 0, 0).Equals(new Color(2, 0, 0)).Should().BeFalse();
    }

    [Fact]
    public void Color_Equals_DifferentG_ReturnsFalse()
    {
        new Color(0, 1, 0).Equals(new Color(0, 2, 0)).Should().BeFalse();
    }

    [Fact]
    public void Color_Equals_DifferentB_ReturnsFalse()
    {
        new Color(0, 0, 1).Equals(new Color(0, 0, 2)).Should().BeFalse();
    }

    #endregion

    // =========================================================================
    // Color.FromHex null guard  (line 228)
    // =========================================================================
    #region Color.FromHex

    [Fact]
    public void Color_FromHex_Null_Throws()
    {
        var act = () => Color.FromHex(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Color_FromHex_WithHash_ParsesCorrectly()
    {
        var c = Color.FromHex("#FF0000");
        c.R.Should().Be(255);
        c.G.Should().Be(0);
        c.B.Should().Be(0);
    }

    #endregion

    // =========================================================================
    // ColorPalette.ExactOrClosest  (lines 19, 37)
    // Line 19: `return exact ?? Closest(...)` — null coalescing mutations
    // Line 37: `FirstOrDefault()` → `First()` — throws when no exact match
    // =========================================================================
    #region ColorPalette.ExactOrClosest

    [Fact]
    public void ColorPalette_ExactOrClosest_UnnamedColorInStandard_DoesNotThrow()
    {
        // Color(100,200,50) is not in Standard palette → Exact returns null via FirstOrDefault
        // With First() mutation → throws InvalidOperationException
        var color = new Color(100, 200, 50);
        var act = () => color.ExactOrClosest(ColorSystem.Standard);
        act.Should().NotThrow();
    }

    [Fact]
    public void ColorPalette_ExactOrClosest_UnnamedColorInLegacy_DoesNotThrow()
    {
        var color = new Color(200, 100, 50);
        var act = () => color.ExactOrClosest(ColorSystem.Legacy);
        act.Should().NotThrow();
    }

    [Fact]
    public void ColorPalette_ExactOrClosest_TrueColor_ReturnsColorItself()
    {
        // TrueColor always returns the color as-is (Exact returns it directly)
        var color = new Color(77, 88, 99);
        var result = color.ExactOrClosest(ColorSystem.TrueColor);
        result.R.Should().Be(77);
        result.G.Should().Be(88);
        result.B.Should().Be(99);
    }

    #endregion

    // =========================================================================
    // ColorTable.GetColor(string): count boundary  (line 36)
    // `if (number > ColorPalette.EightBit.Count - 1)` — >255 guard
    // Mutation >= → returns null for number=255 (grey93), should return valid color
    // =========================================================================
    #region ColorTable boundary

    [Fact]
    public void ColorTable_GetColor_Grey93_ReturnsNonNull()
    {
        // "grey93" maps to number 255 = EightBit.Count-1
        // With >= mutation: 255 >= 255 → returns null → Style.TryParse fails
        Style.TryParse("grey93", out var style).Should().BeTrue();
        style.Foreground.Should().NotBe(Color.Default);
    }

    [Fact]
    public void ColorTable_GetColor_Gray93_ReturnsNonNull()
    {
        Style.TryParse("gray93", out var style).Should().BeTrue();
        style.Foreground.Should().NotBe(Color.Default);
    }

    #endregion

    // =========================================================================
    // AnsiMarkup.Parse  (lines 53, 68)
    // Line 53: `style ??= Style.Plain` — CoalesceAssignment mutation: always assigns Plain
    // Line 68: `link ??= parsed.Link` — CoalesceAssignment mutation: always overwrites link
    // =========================================================================
    #region AnsiMarkup.Parse coalescing assignments

    [Fact]
    public void AnsiMarkup_Parse_NonNullStyle_IsUsedNotReplacedByPlain()
    {
        // Line 53: `style ??= Style.Plain` — mutation changes to `style = Style.Plain` (ignores passed style)
        // Passing a bold style: the text segment should carry Bold decoration, not Plain
        var style = new Style(decoration: Decoration.Bold);
        var segments = AnsiMarkup.Parse("hello", style).ToList();
        segments.Should().HaveCount(1);
        segments[0].Style.Decoration.Should().Be(Decoration.Bold);
    }

    [Fact]
    public void AnsiMarkup_Parse_RedForegroundStyle_IsPreservedOnTextSegment()
    {
        // Additional guard for line 53 — mutation would strip Red foreground
        var style = new Style(foreground: Color.Red);
        var segments = AnsiMarkup.Parse("hello", style).ToList();
        segments[0].Style.Foreground.Should().Be(Color.Red);
    }

    [Fact]
    public void AnsiMarkup_Parse_EachLinkTag_HasItsOwnScope()
    {
        // With the link scope fix, each open/close pair gets its own link.
        // [bold link=first]A[/] → segment with link "first"
        // [link=second]B[/] → segment with link "second"
        // After both close, link is restored to null.
        var segments = AnsiMarkup.Parse("[bold link=first]A[/][link=second]B[/]").ToList();
        segments.Should().HaveCount(2);
        segments[0].Link.Should().NotBeNull();
        segments[0].Link!.Url.Should().Be("first");
        segments[1].Link.Should().NotBeNull();
        segments[1].Link!.Url.Should().Be("second");
    }

    #endregion

    // =========================================================================
    // AnsiWriter.ResetStyle  (line 192)
    // `EndLink()` call in ResetStyle — Statement removal mutation strips link close
    // =========================================================================
    #region AnsiWriter.ResetStyle EndLink

    [Fact]
    public void AnsiWriter_ResetStyle_AfterLink_EmitsLinkCloseSequence()
    {
        // Line 192: Statement removal mutation removes EndLink() from ResetStyle
        // With mutation: resetting style does not close an open link
        // With original: ResetStyle emits SGR(0) AND the OSC 8 link-close sequence
        var (writer, sw) = CreateWriter(ansi: true, links: true);

        writer.BeginLink("http://example.com");
        writer.ResetStyle();

        var output = sw.ToString();
        // OSC 8 link-close: ESC ] 8 ; ; ESC \
        output.Should().Contain("\e]8;;\e\\");
    }

    [Fact]
    public void AnsiWriter_ResetStyle_WithoutPriorLink_DoesNotEmitLinkClose()
    {
        // Verifies EndLink is conditional on _linkCount > 0
        var (writer, sw) = CreateWriter(ansi: true, links: true);

        writer.ResetStyle();

        var output = sw.ToString();
        output.Should().NotContain("\e]8;;\e\\");
    }

    #endregion

    // =========================================================================
    // Color.ToConsoleColor  (line 179)
    // `color.Number.Value >= 16` — Equality mutation: >= → >
    // Color with Number==16 must be quantized to Standard; with > mutation it passes the guard unquantized and throws in switch
    // =========================================================================
    #region Color.ToConsoleColor equality guard

    [Fact]
    public void Color_ToConsoleColor_NullNumberTrueColor_DoesNotThrow()
    {
        // Color.cs:179 Equality mutation: `color.Number == null` → `color.Number != null`
        // With mutation: null-Number color evaluates right side || color.Number.Value >= 16
        //   → color.Number.Value throws NullReferenceException
        // With original: null check is true → quantize → succeeds
        var trueColor = new Color(200, 50, 50); // user-constructed true-color, Number is null
        var act = () => Color.ToConsoleColor(trueColor);
        act.Should().NotThrow();
    }

    [Fact]
    public void Color_ToConsoleColor_Color16_QuantizesToStandardWithoutThrow()
    {
        // Color number 16 is the first "256-color" entry — Number=16, beyond standard range of 0-15.
        // Original: >= 16 catches it → quantize to Standard (Number 0-15) → switch succeeds.
        var color = Color.FromInt32(16);
        var act = () => Color.ToConsoleColor(color);
        act.Should().NotThrow();
    }

    [Fact]
    public void Color_ToConsoleColor_Color15_ReturnsWhite()
    {
        // Boundary: Number==15 is the last standard color — must NOT trigger quantize (already in range).
        var color = Color.FromInt32(15);
        Color.ToConsoleColor(color).Should().Be(ConsoleColor.White);
    }

    #endregion

    // =========================================================================
    // ColorPalette.Closest  (line 79)
    // `OrderBy(x => x.Distance)` — Linq mutation: OrderBy → OrderByDescending returns farthest color
    // =========================================================================
    #region ColorPalette.Closest OrderBy

    [Fact]
    public void ColorPalette_Closest_ReturnsNearestNotFarthest()
    {
        // RGB(255,0,0) is very close to Red (Standard color 9, RGB(255,0,0)) but not named.
        // OrderBy: returns Red (distance ~0); OrderByDescending: returns a distant color like White/Cyan.
        // We use a slightly off red so it doesn't get an exact match:
        var almostRed = new Color(254, 0, 0);
        var result = almostRed.ExactOrClosest(ColorSystem.Standard);
        // The result should be close to red (R high, G+B low), not some random color
        result.R.Should().BeGreaterThan(200);
        result.G.Should().BeLessThan(50);
        result.B.Should().BeLessThan(50);
    }

    #endregion

    // =========================================================================
    // AnsiMarkupTagParser decoration  (line 66)
    // `effectiveDecoration |= decoration.Value` — OrAssignment → ExclusiveOrAssignment mutation
    // |= with same value: Bold|Bold = Bold; ^= with same value: Bold^Bold = None
    // =========================================================================
    #region AnsiMarkupTagParser OrAssignment

    [Fact]
    public void StyleParse_DuplicateDecoration_RetainsDecorationNotCancels()
    {
        // With |=: Bold|=Bold = Bold (idempotent OR)
        // With ^=: Bold^=Bold = None (XOR cancels)
        var style = Style.Parse("bold bold");
        style.Decoration.Should().Be(Decoration.Bold);
    }

    #endregion

    // =========================================================================
    // Constructor null guards
    // =========================================================================
    #region Constructor null guards (continued)

    #endregion

    // =========================================================================
    // AnsiWriter.Write(int) and WriteLine overloads  (NoCoverage)
    // =========================================================================
    #region AnsiWriter Write(int) and WriteLine

    [Fact]
    public void AnsiWriter_WriteInt_EmitsIntegerText()
    {
        var (writer, sw) = CreateWriter();
        writer.Write(42);
        sw.ToString().Should().Be("42");
    }

    [Fact]
    public void AnsiWriter_WriteLineNoArgs_EmitsNewline()
    {
        var (writer, sw) = CreateWriter();
        writer.WriteLine();
        sw.ToString().Should().Be(Environment.NewLine);
    }

    [Fact]
    public void AnsiWriter_WriteLineString_EmitsTextAndNewline()
    {
        var (writer, sw) = CreateWriter();
        writer.WriteLine("hello");
        sw.ToString().Should().Contain("hello");
        sw.ToString().Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void AnsiWriter_WriteLineStringStyle_EmitsTextAndNewline()
    {
        var (writer, sw) = CreateWriter();
        writer.WriteLine("text", new Style(foreground: Color.Red));
        sw.ToString().Should().Contain("text");
        sw.ToString().Should().Contain(Environment.NewLine);
    }

    #endregion

    // =========================================================================
    // AnsiWriter.Style, Decoration, Background, Foreground  (NoCoverage)
    // These methods emit SGR codes — verify the escape sequences.
    // =========================================================================
    #region AnsiWriter Style/Decoration/Background/Foreground

    [Fact]
    public void AnsiWriter_Style_Bold_EmitsBoldSgr()
    {
        var (writer, sw) = CreateWriter();
        writer.Style(new Style(decoration: Decoration.Bold));
        sw.ToString().Should().Contain("\e[1m");
    }

    [Fact]
    public void AnsiWriter_Style_WithLink_EmitsOscAndSgr()
    {
        var (writer, sw) = CreateWriter(ansi: true, links: true);
        writer.Style(new Style(decoration: Decoration.Bold), new Link("http://example.com"));
        sw.ToString().Should().Contain("\e]8;");
        sw.ToString().Should().Contain("\e[1m");
    }

    [Fact]
    public void AnsiWriter_Decoration_Bold_EmitsBoldSgr()
    {
        var (writer, sw) = CreateWriter();
        writer.Decoration(Decoration.Bold);
        sw.ToString().Should().Contain("\e[1m");
    }

    [Fact]
    public void AnsiWriter_Decoration_Italic_EmitsItalicSgr()
    {
        var (writer, sw) = CreateWriter();
        writer.Decoration(Decoration.Italic);
        sw.ToString().Should().Contain("\e[3m");
    }

    [Fact]
    public void AnsiWriter_Background_TrueColorRgb_EmitsBackgroundSgr()
    {
        // Use an unnamed RGB color to force true-color path (named colors use 8-bit encoding).
        var (writer, sw) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Background(new Color(200, 100, 50));
        sw.ToString().Should().Contain("\e[48;2;200;100;50m");
    }

    [Fact]
    public void AnsiWriter_Foreground_TrueColorRgb_EmitsForegroundSgr()
    {
        // Use an unnamed RGB color to force true-color path.
        var (writer, sw) = CreateWriter(colorSystem: ColorSystem.TrueColor);
        writer.Foreground(new Color(10, 20, 200));
        sw.ToString().Should().Contain("\e[38;2;10;20;200m");
    }

    [Fact]
    public void AnsiWriter_NoAnsi_StyleDoesNotEmit()
    {
        var (writer, sw) = CreateWriter(ansi: false);
        writer.Style(new Style(decoration: Decoration.Bold));
        sw.ToString().Should().BeEmpty();
    }

    [Fact]
    public void AnsiWriter_NoAnsi_DecorationDoesNotEmit()
    {
        var (writer, sw) = CreateWriter(ansi: false);
        writer.Decoration(Decoration.Bold);
        sw.ToString().Should().BeEmpty();
    }

    [Fact]
    public void AnsiWriter_NoAnsi_BackgroundDoesNotEmit()
    {
        var (writer, sw) = CreateWriter(ansi: false);
        writer.Background(Color.Red);
        sw.ToString().Should().BeEmpty();
    }

    [Fact]
    public void AnsiWriter_NoAnsi_ForegroundDoesNotEmit()
    {
        var (writer, sw) = CreateWriter(ansi: false);
        writer.Foreground(Color.Red);
        sw.ToString().Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // AnsiWriter cursor and display methods  (NoCoverage)
    // Each method emits a specific ANSI CSI escape sequence.
    // =========================================================================
    #region AnsiWriter cursor and display

    [Fact]
    public void AnsiWriter_CursorPosition_EmitsCupSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.CursorPosition(5, 10);
        sw.ToString().Should().Be("\e[5;10H");
    }

    [Fact]
    public void AnsiWriter_CursorHome_EmitsHomeSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.CursorHome();
        sw.ToString().Should().Be("\e[H");
    }

    [Fact]
    public void AnsiWriter_CursorUp_EmitsCuuSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.CursorUp(3);
        sw.ToString().Should().Be("\e[3A");
    }

    [Fact]
    public void AnsiWriter_CursorDown_EmitsCudSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.CursorDown(2);
        sw.ToString().Should().Be("\e[2B");
    }

    [Fact]
    public void AnsiWriter_CursorRight_EmitsCufSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.CursorRight(4);
        sw.ToString().Should().Be("\e[4C");
    }

    [Fact]
    public void AnsiWriter_CursorLeft_EmitsCubSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.CursorLeft(1);
        sw.ToString().Should().Be("\e[1D");
    }

    [Fact]
    public void AnsiWriter_ShowCursor_EmitsDecPrivateMode25h()
    {
        var (writer, sw) = CreateWriter();
        writer.ShowCursor();
        sw.ToString().Should().Be("\e[?25h");
    }

    [Fact]
    public void AnsiWriter_HideCursor_EmitsDecPrivateMode25l()
    {
        var (writer, sw) = CreateWriter();
        writer.HideCursor();
        sw.ToString().Should().Be("\e[?25l");
    }

    [Fact]
    public void AnsiWriter_SaveCursor_EmitsScosc()
    {
        var (writer, sw) = CreateWriter();
        writer.SaveCursor();
        sw.ToString().Should().Be("\e[s");
    }

    [Fact]
    public void AnsiWriter_RestoreCursor_EmitsScorc()
    {
        var (writer, sw) = CreateWriter();
        writer.RestoreCursor();
        sw.ToString().Should().Be("\e[u");
    }

    [Fact]
    public void AnsiWriter_CursorHorizontalAbsolute_EmitsChaSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.CursorHorizontalAbsolute(7);
        sw.ToString().Should().Be("\e[7G");
    }

    [Fact]
    public void AnsiWriter_EraseInLine_DefaultMode0_EmitsElSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.EraseInLine();
        sw.ToString().Should().Be("\e[0K");
    }

    [Fact]
    public void AnsiWriter_EraseInLine_Mode2_EmitsElMode2()
    {
        var (writer, sw) = CreateWriter();
        writer.EraseInLine(2);
        sw.ToString().Should().Be("\e[2K");
    }

    [Fact]
    public void AnsiWriter_EraseInDisplay_DefaultMode0_EmitsEdSequence()
    {
        var (writer, sw) = CreateWriter();
        writer.EraseInDisplay();
        sw.ToString().Should().Be("\e[0J");
    }

    [Fact]
    public void AnsiWriter_EraseInDisplay_Mode1_EmitsEdMode1()
    {
        var (writer, sw) = CreateWriter();
        writer.EraseInDisplay(1);
        sw.ToString().Should().Be("\e[1J");
    }

    [Fact]
    public void AnsiWriter_ClearScrollback_EmitsCsi3J()
    {
        var (writer, sw) = CreateWriter();
        writer.ClearScrollback();
        sw.ToString().Should().Be("\e[3J");
    }

    [Fact]
    public void AnsiWriter_CursorMethods_NoAnsi_EmitNothing()
    {
        var (writer, sw) = CreateWriter(ansi: false);
        writer.CursorUp(1).CursorDown(1).CursorRight(1).CursorLeft(1);
        writer.ShowCursor().HideCursor().SaveCursor().RestoreCursor();
        writer.EnterAltScreen().ExitAltScreen().ClearScrollback();
        sw.ToString().Should().BeEmpty();
    }

    [Fact]
    public void AnsiWriter_BeginLinkObject_EmitsOscWithUrl()
    {
        var (writer, sw) = CreateWriter(ansi: true, links: true);
        writer.BeginLink(new Link("http://example.com"));
        sw.ToString().Should().Contain("http://example.com");
        sw.ToString().Should().StartWith("\e]8;");
    }

    #endregion

    // =========================================================================
    // AnsiCodeBuilder GetThreeBit (Legacy / 3-bit color)  (NoCoverage)
    // Legacy colors: foreground offset=30, background offset=40.
    // Colors with Number >= 8 must be quantized to the legacy 8-color palette.
    // =========================================================================
    #region AnsiCodeBuilder GetThreeBit

    [Fact]
    public void AnsiWriter_Write_LegacySystem_BlackForeground_Emits30()
    {
        // Color.Black has Number=0 → foreground code = 30+0 = 30
        var (writer, sw) = CreateWriter(colorSystem: ColorSystem.Legacy);
        writer.Write("x", new Style(foreground: Color.Black));
        sw.ToString().Should().Contain("\e[30m");
    }

    [Fact]
    public void AnsiWriter_Write_LegacySystem_BlackBackground_Emits40()
    {
        // Color.Black has Number=0 → background code = 40+0 = 40
        var (writer, sw) = CreateWriter(colorSystem: ColorSystem.Legacy);
        writer.Write("x", new Style(background: Color.Black));
        sw.ToString().Should().Contain("\e[40m");
    }

    [Fact]
    public void AnsiWriter_Write_LegacySystem_GreyForeground_QuantizedNotCode38()
    {
        // Color.Grey has Number=8 (>= 8) so must be quantized to legacy palette.
        // Mutation >= 8 → > 8: Grey(Number=8) would NOT quantize and emit ESC[38m (invalid legacy code).
        // Correct: quantize to nearest legacy color (Silver=7) → ESC[37m.
        var (writer, sw) = CreateWriter(colorSystem: ColorSystem.Legacy);
        writer.Write("x", new Style(foreground: Color.Grey));
        var output = sw.ToString();
        output.Should().NotContain("\e[38m"); // 38 = extended-color intro, NOT a valid 3-bit code
        output.Should().Contain("\e[37m");    // Silver → 30+7=37
    }

    [Fact]
    public void AnsiWriter_Write_LegacySystem_GreyBackground_QuantizedNotCode48()
    {
        var (writer, sw) = CreateWriter(colorSystem: ColorSystem.Legacy);
        writer.Write("x", new Style(background: Color.Grey));
        var output = sw.ToString();
        output.Should().NotContain("\e[48m"); // 48 = extended-color intro
        output.Should().Contain("\e[47m");    // Silver → 40+7=47
    }

    [Fact]
    public void AnsiWriter_Write_LegacySystem_MaroonForeground_Emits31()
    {
        // Color.Maroon has Number=1 → foreground code = 30+1 = 31
        var (writer, sw) = CreateWriter(colorSystem: ColorSystem.Legacy);
        writer.Write("x", new Style(foreground: Color.Maroon));
        sw.ToString().Should().Contain("\e[31m");
    }

    #endregion

    // =========================================================================
    // Color.Blend  (NoCoverage — arithmetic mutations)
    // =========================================================================
    #region Color.Blend

    [Fact]
    public void Color_Blend_Factor0_ReturnsOriginalColor()
    {
        var red = new Color(255, 0, 0);
        var blue = new Color(0, 0, 255);
        var result = red.Blend(blue, 0f);
        result.R.Should().Be(255);
        result.G.Should().Be(0);
        result.B.Should().Be(0);
    }

    [Fact]
    public void Color_Blend_Factor1_ReturnsOtherColor()
    {
        var red = new Color(255, 0, 0);
        var blue = new Color(0, 0, 255);
        var result = red.Blend(blue, 1f);
        result.R.Should().Be(0);
        result.G.Should().Be(0);
        result.B.Should().Be(255);
    }

    [Fact]
    public void Color_Blend_Factor05_ReturnsMidpoint()
    {
        var black = new Color(0, 0, 0);
        var white = new Color(200, 200, 200);
        var result = black.Blend(white, 0.5f);
        result.R.Should().Be(100);
        result.G.Should().Be(100);
        result.B.Should().Be(100);
    }

    [Fact]
    public void Color_Blend_EachComponent_ChangesIndependently()
    {
        // Verify R, G, B components blend independently.
        var start = new Color(100, 50, 200);
        var end = new Color(200, 150, 100);
        var result = start.Blend(end, 1.0f);
        result.R.Should().Be(200);
        result.G.Should().Be(150);
        result.B.Should().Be(100);
    }

    #endregion

    // =========================================================================
    // Color.ToHex  (NoCoverage — string formatting mutations)
    // =========================================================================
    #region Color.ToHex

    [Fact]
    public void Color_ToHex_Red_ReturnsFF0000()
    {
        new Color(255, 0, 0).ToHex().Should().Be("FF0000");
    }

    [Fact]
    public void Color_ToHex_White_ReturnsFFFFFF()
    {
        new Color(255, 255, 255).ToHex().Should().Be("FFFFFF");
    }

    [Fact]
    public void Color_ToHex_Black_Returns000000()
    {
        new Color(0, 0, 0).ToHex().Should().Be("000000");
    }

    [Fact]
    public void Color_ToHex_MixedComponents_CorrectOrder()
    {
        // Ensures R/G/B are emitted in the correct order (not swapped).
        new Color(0x12, 0x34, 0x56).ToHex().Should().Be("123456");
    }

    #endregion

    // =========================================================================
    // Color.ToMarkup and Color.ToString  (NoCoverage)
    // =========================================================================
    #region Color.ToMarkup and ToString

    [Fact]
    public void Color_ToMarkup_Default_ReturnsDefaultString()
    {
        Color.Default.ToMarkup().Should().Be("default");
    }

    [Fact]
    public void Color_ToMarkup_NamedColor_ReturnsName()
    {
        Color.Red.ToMarkup().Should().Be("red");
    }

    [Fact]
    public void Color_ToMarkup_UnnamedTrueColor_ReturnsHexHash()
    {
        new Color(0x12, 0x34, 0x56).ToMarkup().Should().Be("#123456");
    }

    [Fact]
    public void Color_ToString_Default_ReturnsDefaultString()
    {
        Color.Default.ToString().Should().Be("default");
    }

    [Fact]
    public void Color_ToString_NamedColor_ReturnsName()
    {
        Color.Red.ToString().Should().Be("red");
    }

    [Fact]
    public void Color_ToString_UnnamedTrueColor_ReturnsHexAndRgb()
    {
        var color = new Color(18, 52, 86); // 0x12=18, 0x34=52, 0x56=86
        color.ToString().Should().Contain("#123456");
        color.ToString().Should().Contain("RGB=");
    }

    #endregion

    // =========================================================================
    // Color.FromName  (NoCoverage — block removal on FromName)
    // =========================================================================
    #region Color.FromName

    [Fact]
    public void Color_FromName_KnownName_ReturnsColor()
    {
        var color = Color.FromName("red");
        color.Should().NotBeNull();
        color!.Value.Should().Be(Color.Red);
    }

    [Fact]
    public void Color_FromName_UnknownName_ReturnsNull()
    {
        Color.FromName("notacolorname").Should().BeNull();
    }

    [Fact]
    public void Color_FromName_Black_ReturnsBlack()
    {
        var color = Color.FromName("black");
        color.Should().NotBeNull();
        color!.Value.R.Should().Be(0);
        color.Value.G.Should().Be(0);
        color.Value.B.Should().Be(0);
    }

    #endregion

    // =========================================================================
    // Style.GetHashCode  (NoCoverage — arithmetic/bitwise mutations)
    // Uses same FNV-1a pattern as Color. Each component must contribute.
    // =========================================================================
    #region Style.GetHashCode

    [Fact]
    public void Style_GetHashCode_DifferentForeground_ProducesDifferentHash()
    {
        var a = new Style(foreground: new Color(1, 0, 0));
        var b = new Style(foreground: new Color(2, 0, 0));
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Style_GetHashCode_DifferentBackground_ProducesDifferentHash()
    {
        var a = new Style(background: new Color(1, 0, 0));
        var b = new Style(background: new Color(2, 0, 0));
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Style_GetHashCode_DifferentDecoration_ProducesDifferentHash()
    {
        var a = new Style(decoration: Decoration.Bold);
        var b = new Style(decoration: Decoration.Italic);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Style_GetHashCode_SameValues_ProducesSameHash()
    {
        var a = new Style(new Color(10, 20, 30), new Color(40, 50, 60), Decoration.Bold);
        var b = new Style(new Color(10, 20, 30), new Color(40, 50, 60), Decoration.Bold);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Style_GetHashCode_ForegroundVsBackground_ProducesDifferentHash()
    {
        // Ensures foreground and background contribute at distinct positions.
        var a = new Style(foreground: new Color(100, 0, 0), background: Color.Default);
        var b = new Style(foreground: Color.Default, background: new Color(100, 0, 0));
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    #endregion

    // =========================================================================
    // Style implicit operators  (NoCoverage — block removal mutations)
    // =========================================================================
    #region Style implicit operators

    [Fact]
    public void Style_ImplicitFromString_ParsesDecoration()
    {
        Style s = "bold";
        s.Decoration.Should().Be(Decoration.Bold);
    }

    [Fact]
    public void Style_ImplicitFromString_ParsesColor()
    {
        Style s = "red";
        s.Foreground.Should().Be(Color.Red);
    }

    [Fact]
    public void Style_ImplicitFromColor_SetsForeground()
    {
        Style s = Color.Blue;
        s.Foreground.Should().Be(Color.Blue);
        s.Background.Should().Be(Color.Default);
    }

    #endregion

    // =========================================================================
    // StyleExtensions  (NoCoverage — block removal on extension methods)
    // =========================================================================
    #region StyleExtensions

    [Fact]
    public void StyleExtensions_Foreground_SetsForeground()
    {
        var style = Style.Plain.Foreground(Color.Red);
        style.Foreground.Should().Be(Color.Red);
        style.Background.Should().Be(Color.Default);
        style.Decoration.Should().Be(Decoration.None);
    }

    [Fact]
    public void StyleExtensions_Background_SetsBackground()
    {
        var style = Style.Plain.Background(Color.Blue);
        style.Background.Should().Be(Color.Blue);
        style.Foreground.Should().Be(Color.Default);
        style.Decoration.Should().Be(Decoration.None);
    }

    [Fact]
    public void StyleExtensions_Decoration_SetsDecoration()
    {
        var style = Style.Plain.Decoration(Decoration.Italic);
        style.Decoration.Should().Be(Decoration.Italic);
        style.Foreground.Should().Be(Color.Default);
        style.Background.Should().Be(Color.Default);
    }

    [Fact]
    public void StyleExtensions_Foreground_PreservesOtherProperties()
    {
        var original = new Style(background: Color.Green, decoration: Decoration.Bold);
        var modified = original.Foreground(Color.Red);
        modified.Foreground.Should().Be(Color.Red);
        modified.Background.Should().Be(Color.Green);
        modified.Decoration.Should().Be(Decoration.Bold);
    }

    [Fact]
    public void StyleExtensions_Background_PreservesOtherProperties()
    {
        var original = new Style(foreground: Color.Red, decoration: Decoration.Italic);
        var modified = original.Background(Color.Blue);
        modified.Background.Should().Be(Color.Blue);
        modified.Foreground.Should().Be(Color.Red);
        modified.Decoration.Should().Be(Decoration.Italic);
    }

    [Fact]
    public void StyleExtensions_Decoration_PreservesOtherProperties()
    {
        var original = new Style(foreground: Color.Red, background: Color.Green);
        var modified = original.Decoration(Decoration.Underline);
        modified.Decoration.Should().Be(Decoration.Underline);
        modified.Foreground.Should().Be(Color.Red);
        modified.Background.Should().Be(Color.Green);
    }

    #endregion

    // =========================================================================
    // AnsiMarkup.WriteLine  (NoCoverage — statement mutations on lines 39-40)
    // =========================================================================
    #region AnsiMarkup.WriteLine

    [Fact]
    public void AnsiMarkup_WriteLine_WritesMarkupAndNewline()
    {
        var sw = new StringWriter();
        var caps = new AnsiCapabilities { Ansi = true, Links = false, ColorSystem = ColorSystem.TrueColor };
        var writer = new AnsiWriter(sw, caps);
        var markup = new AnsiMarkup(writer);

        markup.WriteLine("[bold]hello[/]");

        var output = sw.ToString();
        output.Should().Contain("hello");
        output.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void AnsiMarkup_WriteLine_CallsWriteThenNewline_BothPresent()
    {
        // If Write(markup) removed → no "hello". If _writer.WriteLine() removed → no newline.
        var sw = new StringWriter();
        var caps = new AnsiCapabilities { Ansi = false, ColorSystem = ColorSystem.NoColors };
        var writer = new AnsiWriter(sw, caps);
        var markup = new AnsiMarkup(writer);

        markup.WriteLine("hello");

        sw.ToString().Should().Be("hello" + Environment.NewLine);
    }

    #endregion

    // =========================================================================
    // AnsiMarkup.Escape(null) and Remove(null/whitespace)  (NoCoverage)
    // =========================================================================
    #region AnsiMarkup.Escape and Remove null paths

    [Fact]
    public void AnsiMarkup_Escape_Null_ReturnsEmpty()
    {
        AnsiMarkup.Escape(null).Should().BeEmpty();
    }

    [Fact]
    public void AnsiMarkup_Remove_Null_ReturnsEmpty()
    {
        AnsiMarkup.Remove(null).Should().BeEmpty();
    }

    [Fact]
    public void AnsiMarkup_Remove_Whitespace_ReturnsEmpty()
    {
        AnsiMarkup.Remove("   ").Should().BeEmpty();
    }

    [Fact]
    public void AnsiMarkup_Remove_EmptyString_ReturnsEmpty()
    {
        AnsiMarkup.Remove(string.Empty).Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // StringBuffer null constructor, Peek/Read at EOF  (NoCoverage)
    // =========================================================================
    #region StringBuffer null/EOF paths

    [Fact]
    public void StringBuffer_NullText_DoesNotThrow_IsEof()
    {
        // Kills `text ??= string.Empty` NullCoalescing: without it, new StringReader(null) throws.
        var buf = new StringBuffer(null!);
        buf.Eof.Should().BeTrue();
        buf.Dispose();
    }

    [Fact]
    public void StringBuffer_PeekAtEof_ReturnsNullChar()
    {
        // Kills block removal of `if (Eof) { return '\0'; }` in Peek:
        // without the guard, _reader.Peek() returns -1 → cast to char = '\uffff', not '\0'.
        using var buf = new StringBuffer("a");
        buf.Read(); // consume 'a' → now at EOF
        buf.Eof.Should().BeTrue();
        buf.Peek().Should().Be('\0');
    }

    [Fact]
    public void StringBuffer_ReadAtEof_ReturnsNullChar()
    {
        // Kills block removal of `if (Eof) { return '\0'; }` in Read.
        using var buf = new StringBuffer("a");
        buf.Read(); // consume 'a'
        buf.Eof.Should().BeTrue();
        buf.Read().Should().Be('\0');
    }

    [Fact]
    public void StringBuffer_PeekNotAtEof_ReturnsNextCharWithoutAdvancing()
    {
        using var buf = new StringBuffer("ab");
        buf.Peek().Should().Be('a');
        buf.Position.Should().Be(0); // Peek does not advance
        buf.Peek().Should().Be('a'); // Same char again
    }

    [Fact]
    public void StringBuffer_ReadNotAtEof_AdvancesPosition()
    {
        using var buf = new StringBuffer("ab");
        buf.Read().Should().Be('a');
        buf.Position.Should().Be(1);
        buf.Read().Should().Be('b');
        buf.Position.Should().Be(2);
        buf.Eof.Should().BeTrue();
    }

    #endregion

    // =========================================================================
    // StringExtensions.RemoveMarkup and ContainsExact  (NoCoverage — block removal)
    // =========================================================================
    #region StringExtensions NoCoverage

    [Fact]
    public void StringExtensions_RemoveMarkup_StripsTags()
    {
        "[bold]hello[/]".RemoveMarkup().Should().Be("hello");
    }

    [Fact]
    public void StringExtensions_RemoveMarkup_NullInput_ReturnsEmpty()
    {
        ((string?)null).RemoveMarkup().Should().BeEmpty();
    }

    [Fact]
    public void StringExtensions_ContainsExact_Present_ReturnsTrue()
    {
        "hello world".ContainsExact("world").Should().BeTrue();
    }

    [Fact]
    public void StringExtensions_ContainsExact_Absent_ReturnsFalse()
    {
        "hello world".ContainsExact("xyz").Should().BeFalse();
    }

    #endregion

    // =========================================================================
    // AnsiMarkupHighlighter — trailing segments after query  (NoCoverage line 87)
    // The else branch (line 87) is reached when a segment starts at or after endIndex,
    // meaning the query was already fully processed in a PREVIOUS segment.
    // Plain text produces ONE segment so the else branch is never reached.
    // Multi-segment markup (e.g. "[bold]prefix query[/] suffix") is required.
    // =========================================================================
    #region AnsiMarkupHighlighter trailing segments

    [Fact]
    public void AnsiMarkupHighlighter_MultiSegment_TrailingSegmentPreserved()
    {
        // "[bold]prefix query[/] suffix" → Segment1: "prefix query" (bold, StartIndex=0, EndIndex=12),
        //                                   Segment2: " suffix"      (plain, StartIndex=12).
        // Query "query" ends at index 12. Segment2.StartIndex == endIndex → 12 < 12 is false
        // → the else branch at line 87 adds Segment2 unchanged.
        // Killing line 87 removes " suffix" from the output.
        var result = AnsiMarkup.Highlight("[bold]prefix query[/] suffix", "query",
            new Style(decoration: Decoration.Underline));
        result.Should().Contain("suffix");
        result.Should().Contain("prefix");
    }

    [Fact]
    public void AnsiMarkupHighlighter_MultiSegment_QueryInFirstSegment_SubsequentSegmentPreserved()
    {
        // Segment1: "find me"  (italic, StartIndex=0, EndIndex=7)
        // Segment2: " and this" (plain, StartIndex=7)
        // Query "find me" ends at 7 → Segment2.StartIndex==7 == endIndex → else branch at line 87.
        var result = AnsiMarkup.Highlight("[italic]find me[/] and this", "find me",
            new Style(decoration: Decoration.Bold));
        result.Should().Contain("and this");
        result.Should().Contain("find me");
    }

    #endregion

    // =========================================================================
    // AnsiMarkupTagParser error message paths  (NoCoverage)
    // =========================================================================
    #region AnsiMarkupTagParser error paths

    [Fact]
    public void AnsiMarkupTagParser_DuplicateLink_ThrowsWithMessage()
    {
        var act = () => Style.Parse("link=http://a.com link=http://b.com");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*link*");
    }

    [Fact]
    public void AnsiMarkupTagParser_DuplicateForeground_ThrowsWithMessage()
    {
        var act = () => Style.Parse("red blue");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*foreground*");
    }

    [Fact]
    public void AnsiMarkupTagParser_DuplicateBackground_ThrowsWithMessage()
    {
        var act = () => Style.Parse("on red on blue");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*background*");
    }

    [Fact]
    public void AnsiMarkupTagParser_NegativeColorNumber_ThrowsWithMessage()
    {
        var act = () => Style.Parse("-1");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*greater than or equal to 0*");
    }

    [Fact]
    public void AnsiMarkupTagParser_ColorNumberAbove255_ThrowsWithMessage()
    {
        var act = () => Style.Parse("256");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*less than or equal to 255*");
    }

    [Fact]
    public void AnsiMarkupTagParser_UnknownBackgroundColor_ThrowsWithMessage()
    {
        var act = () => Style.Parse("on notacolor");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Could not find color*");
    }

    [Fact]
    public void AnsiMarkupTagParser_UnknownForegroundColor_ThrowsWithMessageIncludingStyle()
    {
        var act = () => Style.Parse("notacolororstyle");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*color or style*");
    }

    #endregion

    // =========================================================================
    // AnsiMarkupTagParser hex and RGB color parsing  (NoCoverage lines ~155, ~195)
    // ParseHexColor and ParseRgbColor are reached only when the text starts with
    // '#' or 'rgb'. Need tests that exercise both success and error paths.
    // =========================================================================
    #region AnsiMarkupTagParser hex/rgb color parsing

    [Fact]
    public void AnsiMarkupTagParser_HexColor_6Digit_ParsesCorrectly()
    {
        // Kills String mutation on "#" (line ~155) and exercises ParseHexColor success path.
        var style = Style.Parse("#FF8040");
        style.Foreground.R.Should().Be(0xFF);
        style.Foreground.G.Should().Be(0x80);
        style.Foreground.B.Should().Be(0x40);
    }

    [Fact]
    public void AnsiMarkupTagParser_HexColor_3Digit_ParsesCorrectly()
    {
        // Short hex notation: #F80 → #FF8800
        var style = Style.Parse("#F80");
        style.Foreground.R.Should().Be(0xFF);
        style.Foreground.G.Should().Be(0x88);
        style.Foreground.B.Should().Be(0x00);
    }

    [Fact]
    public void AnsiMarkupTagParser_HexColor_Invalid_ThrowsWithMessage()
    {
        // Kills String mutation on the error message in ParseHexColor.
        var act = () => Style.Parse("#GGGGGG");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hex*");
    }

    [Fact]
    public void AnsiMarkupTagParser_RgbColor_ValidTriple_ParsesCorrectly()
    {
        // Exercises ParseRgbColor success path (lines ~195–217).
        var style = Style.Parse("rgb(200,100,50)");
        style.Foreground.R.Should().Be(200);
        style.Foreground.G.Should().Be(100);
        style.Foreground.B.Should().Be(50);
    }

    [Fact]
    public void AnsiMarkupTagParser_RgbColor_InvalidValues_ThrowsWithMessage()
    {
        // Kills String mutation on the RGB error message (line ~195).
        // Non-numeric component causes FormatException in Convert.ToInt32 → caught → error set → outer throws.
        var act = () => Style.Parse("rgb(abc,0,0)");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RGB*");
    }

    #endregion

    // =========================================================================
    // AnsiWriter Build(Decoration) — Dim/SlowBlink/RapidBlink/Invert/Conceal
    // (NoCoverage lines 635, 650, 655, 660, 665)
    // These yield-return statements in AnsiCodeBuilder.Build(Decoration) are only
    // reached when the decoration flags are set on a rendered style.
    // =========================================================================
    #region AnsiWriter decoration codes — Dim/SlowBlink/RapidBlink/Invert/Conceal

    [Fact]
    public void AnsiWriter_Write_DimDecoration_Emits2()
    {
        // Dim → SGR 2
        var (writer, sw) = CreateWriter();
        writer.Write("x", new Style(decoration: Decoration.Dim));
        sw.ToString().Should().Contain("\e[2m");
    }

    [Fact]
    public void AnsiWriter_Write_SlowBlinkDecoration_Emits5()
    {
        // SlowBlink → SGR 5
        var (writer, sw) = CreateWriter();
        writer.Write("x", new Style(decoration: Decoration.SlowBlink));
        sw.ToString().Should().Contain("\e[5m");
    }

    [Fact]
    public void AnsiWriter_Write_RapidBlinkDecoration_Emits6()
    {
        // RapidBlink → SGR 6
        var (writer, sw) = CreateWriter();
        writer.Write("x", new Style(decoration: Decoration.RapidBlink));
        sw.ToString().Should().Contain("\e[6m");
    }

    [Fact]
    public void AnsiWriter_Write_InvertDecoration_Emits7()
    {
        // Invert → SGR 7
        var (writer, sw) = CreateWriter();
        writer.Write("x", new Style(decoration: Decoration.Invert));
        sw.ToString().Should().Contain("\e[7m");
    }

    [Fact]
    public void AnsiWriter_Write_ConcealDecoration_Emits8()
    {
        // Conceal → SGR 8
        var (writer, sw) = CreateWriter();
        writer.Write("x", new Style(decoration: Decoration.Conceal));
        sw.ToString().Should().Contain("\e[8m");
    }

    #endregion

    // =========================================================================
    // AnsiCodeBuilder.GetThreeBit — null Number path  (AnsiWriter line 695)
    // Mutation: `number == null` → `number != null`. With an RGB color (Number==null)
    // the mutated code skips ExactOrClosest and crashes at number.Value.
    // =========================================================================
    #region AnsiCodeBuilder GetThreeBit — null number path

    [Fact]
    public void AnsiWriter_Write_LegacySystem_RgbColor_QuantizesToLegacyPalette()
    {
        // new Color(r,g,b) has Number==null → must call ExactOrClosest(Legacy).
        // Mutation `!= null` skips quantization → NullReferenceException on number.Value.
        var (writer, sw) = CreateWriter(colorSystem: ColorSystem.Legacy);
        writer.Write("x", new Style(foreground: new Color(200, 100, 50)));
        // Legacy foreground codes are ESC[30m–ESC[37m
        sw.ToString().Should().MatchRegex(@"\x1b\[3[0-7]m");
    }

    #endregion

    // =========================================================================
    // StyleExtensions.Combine(Style?, IEnumerable<Style>)  (NoCoverage lines 226-228)
    // Line 226: `style ?? Style.Plain` — killed by null input.
    // Line 228: `current = current.Combine(item)` — killed by non-empty source.
    // =========================================================================
    #region StyleExtensions.Combine IEnumerable overload

    [Fact]
    public void StyleExtensions_CombineEnumerable_NullStyle_UsesPlain()
    {
        // style==null → `style ?? Style.Plain` → result should equal the source styles applied to Plain
        Style? nullStyle = null;
        var result = StyleExtensions.Combine(nullStyle, new[] { new Style(foreground: Color.Red) });
        result.Foreground.Should().Be(Color.Red);
    }

    [Fact]
    public void StyleExtensions_CombineEnumerable_NonNullStyle_PreservesBaseStyle()
    {
        // Kills NullCoalescing "remove left" mutation on `style ?? Style.Plain`:
        // With the mutation, `style ?? Style.Plain` becomes `Style.Plain`, discarding any non-null input.
        // Here we pass a non-null style with Red fg + empty source → result MUST preserve Red fg.
        var baseStyle = new Style(foreground: Color.Red);
        var result = StyleExtensions.Combine(baseStyle, Array.Empty<Style>());
        result.Foreground.Should().Be(Color.Red);
    }

    [Fact]
    public void StyleExtensions_CombineEnumerable_NonEmptySource_AppliesAll()
    {
        // Kills statement removal on line 228: without it result would remain Style.Plain
        var result = StyleExtensions.Combine(Style.Plain, new[]
        {
            new Style(foreground: Color.Red),
            new Style(background: Color.Blue),
        });
        result.Foreground.Should().Be(Color.Red);
        result.Background.Should().Be(Color.Blue);
    }

    #endregion

    // =========================================================================
    // MarkupTokenizer.ReadText — escaped `]]` and unescaped `]` error path
    // (NoCoverage lines ~280, 283-284 in AnsiMarkup.cs)
    // Line 280: `_reader.Read()` — killed by the `]]` happy-path test.
    // Lines 283-284: `throw InvalidOperationException` — killed by unescaped `]` test.
    // =========================================================================
    #region MarkupTokenizer ReadText escaped/unescaped bracket

    [Fact]
    public void AnsiMarkup_Remove_EscapedDoubleBracket_ReturnsLiteralBracket()
    {
        // "a]]b" → tokenizer reads 'a', sees ']', reads it (line 280), peeks ']' → ok,
        // appends second ']', reads 'b'. Result: "a]b" (no throw).
        AnsiMarkup.Remove("a]]b").Should().Be("a]b");
    }

    [Fact]
    public void AnsiMarkup_Remove_UnescapedBracket_ThrowsInvalidOperation()
    {
        // "a]b" → tokenizer reads 'a', sees ']', reads it, peeks 'b' ≠ ']' → throws (lines 283-284).
        var act = () => AnsiMarkup.Remove("a]b");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unescaped*");
    }

    #endregion
}
