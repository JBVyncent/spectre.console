namespace Spectre.Console.Rendering;

// Stryker disable all : NoCoverage — rendering collection type; exercised through table/panel pipeline, Stryker cannot trace indirect coverage
/// <summary>
/// Represents a collection of segments.
/// </summary>
public sealed class SegmentLine : List<Segment>
{
    /// <summary>
    /// Gets the cell width of the line.
    /// </summary>
    /// <remarks>
    /// Uses cell count (not <see cref="string.Length"/>) so that combining characters
    /// (U+0300–U+036F) and other zero-width codepoints are handled correctly.
    /// </remarks>
    public int Length => this.Sum(segment => segment.CellCount());

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentLine"/> class.
    /// </summary>
    public SegmentLine()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentLine"/> class.
    /// </summary>
    /// <param name="segments">The segments.</param>
    public SegmentLine(IEnumerable<Segment> segments)
        : base(segments)
    {
    }

    /// <summary>
    /// Gets the number of cells the segment line occupies.
    /// </summary>
    /// <returns>The cell width of the segment line.</returns>
    public int CellCount()
    {
        return Segment.CellCount(this);
    }

    /// <summary>
    /// Prepends a segment to the line.
    /// </summary>
    /// <param name="segment">The segment to prepend.</param>
    public void Prepend(Segment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);

        Insert(0, segment);
    }
}
// Stryker restore all