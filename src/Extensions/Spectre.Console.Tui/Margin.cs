namespace Spectre.Console.Tui;

/// <summary>
/// Represents spacing around a widget (margin or padding).
/// </summary>
public readonly struct Margin : IEquatable<Margin>
{
    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public int Horizontal => Left + Right;
    public int Vertical => Top + Bottom;

    public Margin(int uniform)
        : this(uniform, uniform, uniform, uniform)
    {
    }

    public Margin(int horizontal, int vertical)
        : this(horizontal, vertical, horizontal, vertical)
    {
    }

    public Margin(int left, int top, int right, int bottom)
    {
        Left = Math.Max(0, left);
        Top = Math.Max(0, top);
        Right = Math.Max(0, right);
        Bottom = Math.Max(0, bottom);
    }

    public static Margin None => new Margin(0);

    public bool Equals(Margin other)
    {
        return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
    }

    public override bool Equals(object? obj)
    {
        return obj is Margin other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + Left;
            hash = (hash * 31) + Top;
            hash = (hash * 31) + Right;
            hash = (hash * 31) + Bottom;
            return hash;
        }
    }

    public static bool operator ==(Margin left, Margin right) => left.Equals(right);
    public static bool operator !=(Margin left, Margin right) => !left.Equals(right);
}

