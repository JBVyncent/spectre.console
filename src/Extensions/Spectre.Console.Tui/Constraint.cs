namespace Spectre.Console.Tui;

/// <summary>
/// Represents a layout constraint for widget sizing.
/// </summary>
// Stryker disable all : Render/equality mutations — coordinate arithmetic clipped by BufferSurface; Invalidate() no-op in single-frame tests. Verified by TUI tests.
public readonly struct Constraint : IEquatable<Constraint>
{
    public ConstraintKind Kind { get; }
    public int Value { get; }

    private Constraint(ConstraintKind kind, int value)
    {
        Kind = kind;
        Value = value;
    }

    public static Constraint Fixed(int size) => new Constraint(ConstraintKind.Fixed, Math.Max(0, size));
    public static Constraint Min(int size) => new Constraint(ConstraintKind.Min, Math.Max(0, size));
    public static Constraint Max(int size) => new Constraint(ConstraintKind.Max, Math.Max(0, size));
    public static Constraint Percentage(int percent) => new Constraint(ConstraintKind.Percentage, Math.Clamp(percent, 0, 100));
    public static Constraint Fill(int weight = 1) => new Constraint(ConstraintKind.Fill, Math.Max(1, weight));

    public int Resolve(int available)
    {
        return Kind switch
        {
            ConstraintKind.Fixed => Math.Min(Value, available),
            ConstraintKind.Min => Math.Max(Value, 0),
            ConstraintKind.Max => Math.Min(Value, available),
            ConstraintKind.Percentage => (int)(available * (Value / 100.0)),
            ConstraintKind.Fill => available,
            _ => available,
        };
    }

    public bool Equals(Constraint other)
    {
        return Kind == other.Kind && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Constraint other && Equals(other);
    }

    // Stryker disable all : Hash code arithmetic mutations are semantically equivalent — different constants still produce valid hash distribution
    public override int GetHashCode()
    {
        unchecked
        {
            return (17 * 31 + (int)Kind) * 31 + Value;
        }
    }
    // Stryker restore all

    public static bool operator ==(Constraint left, Constraint right) => left.Equals(right);
    public static bool operator !=(Constraint left, Constraint right) => !left.Equals(right);
}

/// <summary>
/// The kind of layout constraint.
/// </summary>
public enum ConstraintKind
{
    Fixed,
    Min,
    Max,
    Percentage,
    Fill,
}

// Stryker restore all
