namespace Spectre.Console.Tui;

/// <summary>
/// Represents a rectangular area with position and size.
/// </summary>
public readonly struct Rect : IEquatable<Rect>
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public int Right => X + Width;
    public int Bottom => Y + Height;

    public Rect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
    }

    public bool Contains(int col, int row)
    {
        return col >= X && col < Right && row >= Y && row < Bottom;
    }

    public Rect Intersect(Rect other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);

        if (right <= x || bottom <= y)
        {
            return new Rect(0, 0, 0, 0);
        }

        return new Rect(x, y, right - x, bottom - y);
    }

    public static Rect Empty => new Rect(0, 0, 0, 0);

    public bool Equals(Rect other)
    {
        return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is Rect other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + X;
            hash = (hash * 31) + Y;
            hash = (hash * 31) + Width;
            hash = (hash * 31) + Height;
            return hash;
        }
    }

    public static bool operator ==(Rect left, Rect right) => left.Equals(right);
    public static bool operator !=(Rect left, Rect right) => !left.Equals(right);

    public override string ToString() => $"({X}, {Y}, {Width}x{Height})";
}

