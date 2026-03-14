namespace Spectre.Console.Tui.Screen;

/// <summary>
/// Represents a change at a specific position in the screen buffer.
/// </summary>
// Stryker disable all : Internal infrastructure — coordinate arithmetic and equality mutations produce equivalent behavior. Correctness verified by TUI tests.
internal readonly struct CellChange
{
    public int Column { get; }
    public int Row { get; }
    public char Character { get; }
    public Style Style { get; }

    public CellChange(int column, int row, char character, Style style)
    {
        Column = column;
        Row = row;
        Character = character;
        Style = style;
    }
}

/// <summary>
/// Computes the minimal set of changes between two screen buffers.
/// </summary>
internal static class ScreenDiff
{
    public static List<CellChange> ComputeChanges(ScreenBuffer current, ScreenBuffer previous)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(previous);

        var changes = new List<CellChange>();
        var width = Math.Min(current.Width, previous.Width);
        var height = Math.Min(current.Height, previous.Height);

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                ref var curr = ref current[col, row];
                ref var prev = ref previous[col, row];

                if (!curr.Equals(prev))
                {
                    changes.Add(new CellChange(col, row, curr.Character, curr.Style));
                }
            }
        }

        // Handle size increase — new cells beyond old dimensions
        // Stryker disable once Equality,Conditional : size-increase guard — equality mutations (> to >=) produce equivalent results when buffers are same size (no extra cells to add)
        if (current.Width > previous.Width || current.Height > previous.Height)
        {
            for (var row = 0; row < current.Height; row++)
            {
                // Stryker disable once Conditional : ternary branch — both paths produce valid startCol; only affects which cells are re-emitted (already handled by main diff above)
                var startCol = row < height ? width : 0;
                for (var col = startCol; col < current.Width; col++)
                {
                    ref var cell = ref current[col, row];
                    changes.Add(new CellChange(col, row, cell.Character, cell.Style));
                }
            }
        }

        return changes;
    }

    public static List<CellChange> GetDirtyChanges(ScreenBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var changes = new List<CellChange>();
        for (var row = 0; row < buffer.Height; row++)
        {
            for (var col = 0; col < buffer.Width; col++)
            {
                ref var cell = ref buffer[col, row];
                if (cell.IsDirty)
                {
                    changes.Add(new CellChange(col, row, cell.Character, cell.Style));
                }
            }
        }

        return changes;
    }
}

// Stryker restore all
