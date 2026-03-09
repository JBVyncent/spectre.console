using Spectre.Console.Phantom;

namespace Spectre.Console.Phantom.Tests;

/// <summary>
/// Assertion helpers for Phantom terminal testing.
/// These provide expressive, purpose-built assertions for screen buffer state,
/// cell styling, cursor position, and sequence history validation.
/// </summary>
public static class PhantomAssertions
{
    // ── Row/Text Assertions ──────────────────────────────────────────

    /// <summary>
    /// Assert that a row contains the specified text anywhere in the row.
    /// </summary>
    public static void AssertRowContains(this ScreenBuffer buffer, int row, string expected)
    {
        var rowText = buffer.GetRowText(row);
        rowText.Should().Contain(expected);
    }

    /// <summary>
    /// Assert that a row exactly equals the specified text (trailing spaces trimmed).
    /// </summary>
    public static void AssertRowEquals(this ScreenBuffer buffer, int row, string expected)
    {
        buffer.GetRowText(row).Should().Be(expected);
    }

    /// <summary>
    /// Assert that a row starts with the specified text.
    /// </summary>
    public static void AssertRowStartsWith(this ScreenBuffer buffer, int row, string expected)
    {
        buffer.GetRowText(row).Should().StartWith(expected);
    }

    /// <summary>
    /// Assert that a row is empty (all spaces).
    /// </summary>
    public static void AssertRowEmpty(this ScreenBuffer buffer, int row)
    {
        buffer.GetRowText(row).Should().BeEmpty();
    }

    /// <summary>
    /// Assert that the screen contains the specified text anywhere.
    /// </summary>
    public static void AssertContainsText(this ScreenBuffer buffer, string text)
    {
        buffer.ContainsText(text).Should().BeTrue(
            $"Screen should contain \"{text}\" but it was not found.\nScreen content:\n{buffer.GetText()}");
    }

    /// <summary>
    /// Assert that the screen does NOT contain the specified text.
    /// </summary>
    public static void AssertNotContainsText(this ScreenBuffer buffer, string text)
    {
        buffer.ContainsText(text).Should().BeFalse(
            $"Screen should NOT contain \"{text}\" but it was found.");
    }

    /// <summary>
    /// Assert that text is found at the specified position.
    /// </summary>
    public static void AssertTextAt(this ScreenBuffer buffer, int row, int col, string expected)
    {
        for (var i = 0; i < expected.Length; i++)
        {
            buffer[row, col + i].Character.Should().Be(expected[i],
                $"Expected '{expected[i]}' at ({row}, {col + i}) but found '{buffer[row, col + i].Character}'");
        }
    }

    // ── Cell Style Assertions ────────────────────────────────────────

    /// <summary>
    /// Assert that a cell has the specified foreground color mode.
    /// </summary>
    public static void AssertCellForeground(this ScreenBuffer buffer, int row, int col, ColorMode mode, int index)
    {
        var cell = buffer[row, col];
        cell.Foreground.Should().NotBeNull($"Cell ({row}, {col}) should have a foreground color");
        cell.Foreground!.Value.Mode.Should().Be(mode);
        cell.Foreground!.Value.Index.Should().Be(index);
    }

    /// <summary>
    /// Assert that a cell has the specified RGB foreground color.
    /// </summary>
    public static void AssertCellForegroundRgb(this ScreenBuffer buffer, int row, int col, byte r, byte g, byte b)
    {
        var cell = buffer[row, col];
        cell.Foreground.Should().NotBeNull($"Cell ({row}, {col}) should have a foreground color");
        cell.Foreground!.Value.Mode.Should().Be(ColorMode.TrueColor);
        cell.Foreground!.Value.R.Should().Be(r);
        cell.Foreground!.Value.G.Should().Be(g);
        cell.Foreground!.Value.B.Should().Be(b);
    }

    /// <summary>
    /// Assert that a cell has the specified background color mode.
    /// </summary>
    public static void AssertCellBackground(this ScreenBuffer buffer, int row, int col, ColorMode mode, int index)
    {
        var cell = buffer[row, col];
        cell.Background.Should().NotBeNull($"Cell ({row}, {col}) should have a background color");
        cell.Background!.Value.Mode.Should().Be(mode);
        cell.Background!.Value.Index.Should().Be(index);
    }

    /// <summary>
    /// Assert that a cell has the specified RGB background color.
    /// </summary>
    public static void AssertCellBackgroundRgb(this ScreenBuffer buffer, int row, int col, byte r, byte g, byte b)
    {
        var cell = buffer[row, col];
        cell.Background.Should().NotBeNull($"Cell ({row}, {col}) should have a background color");
        cell.Background!.Value.Mode.Should().Be(ColorMode.TrueColor);
        cell.Background!.Value.R.Should().Be(r);
        cell.Background!.Value.G.Should().Be(g);
        cell.Background!.Value.B.Should().Be(b);
    }

    /// <summary>
    /// Assert that a cell has no foreground color (default).
    /// </summary>
    public static void AssertCellDefaultForeground(this ScreenBuffer buffer, int row, int col)
    {
        buffer[row, col].Foreground.Should().BeNull(
            $"Cell ({row}, {col}) should have default foreground (null)");
    }

    /// <summary>
    /// Assert that a cell has no background color (default).
    /// </summary>
    public static void AssertCellDefaultBackground(this ScreenBuffer buffer, int row, int col)
    {
        buffer[row, col].Background.Should().BeNull(
            $"Cell ({row}, {col}) should have default background (null)");
    }

    /// <summary>
    /// Assert that a cell has the specified decoration flags set.
    /// </summary>
    public static void AssertCellDecoration(this ScreenBuffer buffer, int row, int col, CellDecoration expected)
    {
        var actual = buffer[row, col].Decoration;
        actual.HasFlag(expected).Should().BeTrue(
            $"Cell ({row}, {col}) should have decoration {expected} but has {actual}");
    }

    /// <summary>
    /// Assert that a cell has no decorations.
    /// </summary>
    public static void AssertCellNoDecoration(this ScreenBuffer buffer, int row, int col)
    {
        buffer[row, col].Decoration.Should().Be(CellDecoration.None,
            $"Cell ({row}, {col}) should have no decoration");
    }

    /// <summary>
    /// Assert that a cell has the specified hyperlink URL.
    /// </summary>
    public static void AssertCellHyperlink(this ScreenBuffer buffer, int row, int col, string expectedUrl)
    {
        buffer[row, col].HyperlinkUrl.Should().Be(expectedUrl,
            $"Cell ({row}, {col}) should have hyperlink \"{expectedUrl}\"");
    }

    /// <summary>
    /// Assert that a cell has no hyperlink.
    /// </summary>
    public static void AssertCellNoHyperlink(this ScreenBuffer buffer, int row, int col)
    {
        buffer[row, col].HyperlinkUrl.Should().BeNull(
            $"Cell ({row}, {col}) should have no hyperlink");
    }

    /// <summary>
    /// Assert that a specific character exists at the given position.
    /// </summary>
    public static void AssertCharAt(this ScreenBuffer buffer, int row, int col, char expected)
    {
        buffer[row, col].Character.Should().Be(expected,
            $"Expected '{expected}' at ({row}, {col}) but found '{buffer[row, col].Character}'");
    }

    // ── PhantomTerminal Assertions ───────────────────────────────────

    /// <summary>
    /// Assert cursor is at the specified position.
    /// </summary>
    public static void AssertCursorAt(this PhantomTerminal terminal, int row, int col)
    {
        terminal.CursorRow.Should().Be(row, $"Expected cursor row {row}");
        terminal.CursorCol.Should().Be(col, $"Expected cursor col {col}");
    }

    /// <summary>
    /// Assert cursor visibility state.
    /// </summary>
    public static void AssertCursorVisible(this PhantomTerminal terminal, bool visible)
    {
        terminal.CursorVisible.Should().Be(visible);
    }

    /// <summary>
    /// Assert the terminal is (or is not) on the alternate screen.
    /// </summary>
    public static void AssertAlternateScreen(this PhantomTerminal terminal, bool expected)
    {
        terminal.IsAlternateScreen.Should().Be(expected);
    }

    /// <summary>
    /// Assert the sequence history contains a specific sequence type.
    /// </summary>
    public static void AssertHistoryContains<T>(this PhantomTerminal terminal) where T : AnsiSequence
    {
        terminal.SequenceHistory.OfType<T>().Should().NotBeEmpty(
            $"Sequence history should contain {typeof(T).Name}");
    }

    /// <summary>
    /// Assert the sequence history contains exactly N instances of a sequence type.
    /// </summary>
    public static void AssertHistoryCount<T>(this PhantomTerminal terminal, int expected) where T : AnsiSequence
    {
        terminal.SequenceHistory.OfType<T>().Count().Should().Be(expected,
            $"Expected {expected} instances of {typeof(T).Name} in history");
    }
}
