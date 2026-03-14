namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Represents a single cell in the screen buffer.
/// </summary>
internal struct BufferCell : IEquatable<BufferCell>
{
    public char Character { get; set; }
    public Style Style { get; set; }
    public bool IsDirty { get; set; }

    public BufferCell(char character, Style style)
    {
        Character = character;
        Style = style;
        IsDirty = true;
    }

    public static BufferCell Empty => new BufferCell(' ', Style.Plain);

    public bool Equals(BufferCell other)
    {
        return Character == other.Character && Style.Equals(other.Style);
    }

    public override bool Equals(object? obj)
    {
        return obj is BufferCell other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (17 * 31 + Character.GetHashCode()) * 31 + Style.GetHashCode();
        }
    }
}

