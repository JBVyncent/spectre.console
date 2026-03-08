namespace Spectre.Console.Phantom;

/// <summary>
/// Represents a single cell in the terminal screen buffer.
/// Contains the character and its visual style.
/// </summary>
public sealed class ScreenCell
{
    /// <summary>
    /// The character displayed in this cell.
    /// </summary>
    public char Character { get; set; } = ' ';

    /// <summary>
    /// Foreground color as an SGR code, or null for default.
    /// </summary>
    public CellColor? Foreground { get; set; }

    /// <summary>
    /// Background color as an SGR code, or null for default.
    /// </summary>
    public CellColor? Background { get; set; }

    /// <summary>
    /// Active text decorations.
    /// </summary>
    public CellDecoration Decoration { get; set; }

    /// <summary>
    /// Active hyperlink URL, or null if none.
    /// </summary>
    public string? HyperlinkUrl { get; set; }

    /// <summary>
    /// Reset this cell to default state.
    /// </summary>
    public void Reset()
    {
        Character = ' ';
        Foreground = null;
        Background = null;
        Decoration = CellDecoration.None;
        HyperlinkUrl = null;
    }

    /// <summary>
    /// Copy style (but not character) from another cell.
    /// </summary>
    public void CopyStyleFrom(ScreenCell other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Foreground = other.Foreground;
        Background = other.Background;
        Decoration = other.Decoration;
        HyperlinkUrl = other.HyperlinkUrl;
    }

    public override string ToString()
    {
        return Character.ToString();
    }
}

/// <summary>
/// Represents a color value that can be default, 4-bit, 8-bit, or 24-bit RGB.
/// </summary>
public readonly record struct CellColor
{
    /// <summary>
    /// The color mode.
    /// </summary>
    public ColorMode Mode { get; init; }

    /// <summary>
    /// For 4-bit: the SGR code (30-37, 40-47, 90-97, 100-107).
    /// For 8-bit: the palette index (0-255).
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// For 24-bit RGB: the red component.
    /// </summary>
    public byte R { get; init; }

    /// <summary>
    /// For 24-bit RGB: the green component.
    /// </summary>
    public byte G { get; init; }

    /// <summary>
    /// For 24-bit RGB: the blue component.
    /// </summary>
    public byte B { get; init; }

    public static CellColor FromLegacy(int sgrCode) =>
        new() { Mode = ColorMode.Legacy, Index = sgrCode };

    public static CellColor FromEightBit(int index) =>
        new() { Mode = ColorMode.EightBit, Index = index };

    public static CellColor FromRgb(byte r, byte g, byte b) =>
        new() { Mode = ColorMode.TrueColor, R = r, G = g, B = b };
}

public enum ColorMode
{
    Legacy,
    EightBit,
    TrueColor,
}

[Flags]
public enum CellDecoration
{
    None = 0,
    Bold = 1 << 0,
    Dim = 1 << 1,
    Italic = 1 << 2,
    Underline = 1 << 3,
    SlowBlink = 1 << 4,
    RapidBlink = 1 << 5,
    Reverse = 1 << 6,
    Conceal = 1 << 7,
    Strikethrough = 1 << 8,
}
