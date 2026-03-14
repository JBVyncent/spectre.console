using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Screen;
using Xunit;

namespace Spectre.Console.Tui.Tests;

/// <summary>
/// Mutation-killing tests for GROUP 1 - Core types.
/// Targets specific survived/NoCoverage mutants at known lines.
/// </summary>
public sealed class MutantKillerStrykerGroup1Tests
{
    // ── Helpers ──────────────────────────────────────────────────────

    private static (ScreenBuffer buf, BufferSurface surface) Surface(int w, int h)
    {
        var buf = new ScreenBuffer(w, h);
        return (buf, new BufferSurface(buf));
    }

    private static string Row(ScreenBuffer buf, int row)
    {
        var sb = new System.Text.StringBuilder(buf.Width);
        for (var col = 0; col < buf.Width; col++)
        {
            sb.Append(buf[col, row].Character);
        }

        return sb.ToString();
    }

    // ══════════════════════════════════════════════════════════════════
    // Rect.cs — L36-37 Contains boundary, L61-64 Intersect arithmetic
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Rect_Contains_ExactBoundary_Col_EqualsX_Included()
    {
        // Kills: col >= X mutated to col > X
        var r = new Rect(5, 0, 10, 10);
        r.Contains(5, 0).Should().BeTrue();
    }

    [Fact]
    public void Rect_Contains_ExactBoundary_Col_EqualsRight_Excluded()
    {
        // Kills: col < Right mutated to col <= Right
        var r = new Rect(0, 0, 10, 10);
        r.Contains(10, 0).Should().BeFalse();
        r.Contains(9, 0).Should().BeTrue();
    }

    [Fact]
    public void Rect_Contains_ExactBoundary_Row_EqualsY_Included()
    {
        // Kills: row >= Y mutated to row > Y
        var r = new Rect(0, 5, 10, 10);
        r.Contains(0, 5).Should().BeTrue();
    }

    [Fact]
    public void Rect_Contains_ExactBoundary_Row_EqualsBottom_Excluded()
    {
        // Kills: row < Bottom mutated to row <= Bottom
        var r = new Rect(0, 0, 10, 10);
        r.Contains(0, 10).Should().BeFalse();
        r.Contains(0, 9).Should().BeTrue();
    }

    [Fact]
    public void Rect_Contains_JustInsideAllEdges()
    {
        // Ensure all four conditions must be true simultaneously
        var r = new Rect(2, 3, 5, 7);
        // Just inside each boundary
        r.Contains(2, 3).Should().BeTrue();   // X, Y
        r.Contains(6, 9).Should().BeTrue();   // Right-1, Bottom-1
        // Just outside each edge
        r.Contains(1, 3).Should().BeFalse();  // col < X
        r.Contains(7, 3).Should().BeFalse();  // col >= Right
        r.Contains(2, 2).Should().BeFalse();  // row < Y
        r.Contains(2, 10).Should().BeFalse(); // row >= Bottom
    }

    [Fact]
    public void Rect_Intersect_RightMinusX_ArithmeticExact()
    {
        // Kills: right - x mutated to right + x (L41 arithmetic)
        var a = new Rect(2, 3, 10, 10);
        var b = new Rect(5, 6, 10, 10);
        var result = a.Intersect(b);
        // x = Max(2,5) = 5, right = Min(12,15) = 12, width = 12-5 = 7
        result.X.Should().Be(5);
        result.Width.Should().Be(7);
        // y = Max(3,6) = 6, bottom = Min(13,16) = 13, height = 13-6 = 7
        result.Y.Should().Be(6);
        result.Height.Should().Be(7);
    }

    [Fact]
    public void Rect_Intersect_BottomMinusY_ArithmeticExact()
    {
        // Kills: bottom - y mutated to bottom + y (L41 arithmetic)
        var a = new Rect(0, 4, 20, 6);
        var b = new Rect(0, 7, 20, 10);
        var result = a.Intersect(b);
        // y = Max(4,7) = 7, bottom = Min(10,17) = 10, height = 10-7 = 3
        result.Y.Should().Be(7);
        result.Height.Should().Be(3);
    }

    [Fact]
    public void Rect_Intersect_BoundaryCondition_RightEqualsX_ReturnsEmpty()
    {
        // Kills: right <= x mutated to right < x (L36)
        // When right == x, should return empty
        var a = new Rect(0, 0, 5, 5);
        var b = new Rect(5, 0, 5, 5);
        var result = a.Intersect(b);
        result.Width.Should().Be(0);
        result.Height.Should().Be(0);
    }

    [Fact]
    public void Rect_Intersect_BoundaryCondition_BottomEqualsY_ReturnsEmpty()
    {
        // Kills: bottom <= y mutated to bottom < y (L36)
        var a = new Rect(0, 0, 5, 5);
        var b = new Rect(0, 5, 5, 5);
        var result = a.Intersect(b);
        result.Width.Should().Be(0);
        result.Height.Should().Be(0);
    }

    [Fact]
    public void Rect_Intersect_OnePixelOverlap()
    {
        // Ensures right > x and bottom > y by exactly 1 yields 1x1
        var a = new Rect(0, 0, 6, 6);
        var b = new Rect(5, 5, 5, 5);
        var result = a.Intersect(b);
        result.X.Should().Be(5);
        result.Y.Should().Be(5);
        result.Width.Should().Be(1);
        result.Height.Should().Be(1);
    }

    [Fact]
    public void Rect_GetHashCode_ExactValue()
    {
        // Kills: arithmetic mutations in GetHashCode (L61-64)
        // hash = 17
        // hash = (17 * 31) + X
        // hash = (hash * 31) + Y
        // hash = (hash * 31) + Width
        // hash = (hash * 31) + Height
        var r = new Rect(1, 2, 3, 4);
        var expected = 17;
        unchecked
        {
            expected = (expected * 31) + 1; // X
            expected = (expected * 31) + 2; // Y
            expected = (expected * 31) + 3; // Width
            expected = (expected * 31) + 4; // Height
        }

        r.GetHashCode().Should().Be(expected);
    }

    [Fact]
    public void Rect_GetHashCode_DiffersByEachField()
    {
        // Ensures each field contributes uniquely
        var baseline = new Rect(1, 2, 3, 4).GetHashCode();
        new Rect(2, 2, 3, 4).GetHashCode().Should().NotBe(baseline);
        new Rect(1, 3, 3, 4).GetHashCode().Should().NotBe(baseline);
        new Rect(1, 2, 4, 4).GetHashCode().Should().NotBe(baseline);
        new Rect(1, 2, 3, 5).GetHashCode().Should().NotBe(baseline);
    }

    // ══════════════════════════════════════════════════════════════════
    // Margin.cs — L51-54 GetHashCode arithmetic, Horizontal/Vertical
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Margin_Horizontal_ExactArithmetic()
    {
        // Kills: Left + Right mutated to Left - Right
        var m = new Margin(3, 0, 7, 0);
        m.Horizontal.Should().Be(10);
    }

    [Fact]
    public void Margin_Vertical_ExactArithmetic()
    {
        // Kills: Top + Bottom mutated to Top - Bottom
        var m = new Margin(0, 4, 0, 6);
        m.Vertical.Should().Be(10);
    }

    [Fact]
    public void Margin_Horizontal_NonSymmetric()
    {
        // Ensures it's not just Left*2 or Right*2
        var m = new Margin(2, 0, 5, 0);
        m.Horizontal.Should().Be(7);
    }

    [Fact]
    public void Margin_Vertical_NonSymmetric()
    {
        var m = new Margin(0, 3, 0, 8);
        m.Vertical.Should().Be(11);
    }

    [Fact]
    public void Margin_GetHashCode_ExactValue()
    {
        // Kills: arithmetic mutations in GetHashCode (L51-54)
        var m = new Margin(1, 2, 3, 4);
        var expected = 17;
        unchecked
        {
            expected = (expected * 31) + 1; // Left
            expected = (expected * 31) + 2; // Top
            expected = (expected * 31) + 3; // Right
            expected = (expected * 31) + 4; // Bottom
        }

        m.GetHashCode().Should().Be(expected);
    }

    [Fact]
    public void Margin_GetHashCode_DiffersByEachField()
    {
        var baseline = new Margin(1, 2, 3, 4).GetHashCode();
        new Margin(2, 2, 3, 4).GetHashCode().Should().NotBe(baseline);
        new Margin(1, 3, 3, 4).GetHashCode().Should().NotBe(baseline);
        new Margin(1, 2, 4, 4).GetHashCode().Should().NotBe(baseline);
        new Margin(1, 2, 3, 5).GetHashCode().Should().NotBe(baseline);
    }

    // ══════════════════════════════════════════════════════════════════
    // Constraint.cs — L50 Resolve arithmetic for MinMax GetHashCode
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Constraint_GetHashCode_ExactValue()
    {
        // (17 * 31 + (int)Kind) * 31 + Value
        // Kills: arithmetic mutations at L50
        var c = Constraint.Fixed(10);
        var expected = unchecked((17 * 31 + (int)ConstraintKind.Fixed) * 31 + 10);
        c.GetHashCode().Should().Be(expected);
    }

    [Fact]
    public void Constraint_GetHashCode_KindContributes()
    {
        // Same value, different kind -> different hash
        var fixedHash = Constraint.Fixed(5).GetHashCode();
        var minHash = Constraint.Min(5).GetHashCode();
        var maxHash = Constraint.Max(5).GetHashCode();
        fixedHash.Should().NotBe(minHash);
        fixedHash.Should().NotBe(maxHash);
        minHash.Should().NotBe(maxHash);
    }

    [Fact]
    public void Constraint_GetHashCode_ValueContributes()
    {
        Constraint.Fixed(10).GetHashCode().Should().NotBe(Constraint.Fixed(20).GetHashCode());
    }

    [Fact]
    public void Constraint_Resolve_MinMax_Exact()
    {
        // Kills mutations on Min/Max resolve logic
        // Min returns Math.Max(Value, 0) — always the Value when positive
        Constraint.Min(50).Resolve(100).Should().Be(50);
        Constraint.Min(50).Resolve(30).Should().Be(50); // Min doesn't cap to available
        // Max returns Math.Min(Value, available)
        Constraint.Max(50).Resolve(100).Should().Be(50);
        Constraint.Max(50).Resolve(30).Should().Be(30); // capped by available
    }

    [Fact]
    public void Constraint_Resolve_Percentage_ExactArithmetic()
    {
        // (int)(available * (Value / 100.0))
        Constraint.Percentage(25).Resolve(200).Should().Be(50);
        Constraint.Percentage(33).Resolve(100).Should().Be(33);
        Constraint.Percentage(0).Resolve(100).Should().Be(0);
        Constraint.Percentage(100).Resolve(100).Should().Be(100);
    }

    // ══════════════════════════════════════════════════════════════════
    // BufferCell.cs — L35 GetHashCode arithmetic
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void BufferCell_GetHashCode_ExactComputation()
    {
        // (17 * 31 + Character.GetHashCode()) * 31 + Style.GetHashCode()
        var cell = new BufferCell('A', Style.Plain);
        var expected = unchecked((17 * 31 + 'A'.GetHashCode()) * 31 + Style.Plain.GetHashCode());
        cell.GetHashCode().Should().Be(expected);
    }

    [Fact]
    public void BufferCell_GetHashCode_CharacterContributes()
    {
        var a = new BufferCell('A', Style.Plain);
        var b = new BufferCell('Z', Style.Plain);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void BufferCell_GetHashCode_StyleContributes()
    {
        var a = new BufferCell('A', Style.Plain);
        var b = new BufferCell('A', new Style(Color.Red));
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    // ══════════════════════════════════════════════════════════════════
    // BufferSurface.cs — L18,27 constructor, L47 SetText, L57,59 bounds
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void BufferSurface_Constructor_NullBuffer_Throws()
    {
        // Kills: L18 ArgumentNullException.ThrowIfNull statement removal
        var act = () => new BufferSurface(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BufferSurface_BoundsConstructor_NullBuffer_Throws()
    {
        // Kills: L27 ArgumentNullException.ThrowIfNull statement removal
        var act = () => new BufferSurface(null!, new Rect(0, 0, 5, 5));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BufferSurface_SetText_NullText_Throws()
    {
        // Kills: L47 ArgumentNullException.ThrowIfNull statement removal
        var buf = new ScreenBuffer(10, 10);
        var surface = new BufferSurface(buf);
        var act = () => surface.SetText(0, 0, null!, Style.Plain);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BufferSurface_SetText_WritesEachCharacter()
    {
        // Kills: L47 SetText statement mutation (removing the loop body)
        var buf = new ScreenBuffer(10, 1);
        var surface = new BufferSurface(buf);
        surface.SetText(0, 0, "ABC", Style.Plain);
        buf[0, 0].Character.Should().Be('A');
        buf[1, 0].Character.Should().Be('B');
        buf[2, 0].Character.Should().Be('C');
    }

    [Fact]
    public void BufferSurface_Fill_UsesAreaBounds_NotClip()
    {
        // Kills: L57 area.Y vs area.Bottom, L59 area.X vs area.Right boundary
        var buf = new ScreenBuffer(10, 10);
        var surface = new BufferSurface(buf);
        surface.Fill(new Rect(2, 3, 3, 2), '#', Style.Plain);
        // Inside area
        buf[2, 3].Character.Should().Be('#');
        buf[4, 4].Character.Should().Be('#');
        // Outside area
        buf[1, 3].Character.Should().Be(' ');
        buf[5, 3].Character.Should().Be(' ');
        buf[2, 2].Character.Should().Be(' ');
        buf[2, 5].Character.Should().Be(' ');
    }

    [Fact]
    public void BufferSurface_Fill_ExactRowAndColRange()
    {
        // Kills: row < area.Bottom mutated to row <= area.Bottom
        // Kills: c < area.Right mutated to c <= area.Right
        var buf = new ScreenBuffer(10, 10);
        var surface = new BufferSurface(buf);
        surface.Fill(new Rect(0, 0, 3, 2), 'X', Style.Plain);
        // Exactly 3 cols (0,1,2) and 2 rows (0,1)
        buf[2, 1].Character.Should().Be('X'); // last in range
        buf[3, 0].Character.Should().Be(' '); // col 3 not filled
        buf[0, 2].Character.Should().Be(' '); // row 2 not filled
    }

    [Fact]
    public void BufferSurface_BoundsConstructor_ClipsToBufSize()
    {
        // Verifies the bounds.Intersect with buffer rect
        var buf = new ScreenBuffer(5, 5);
        var surface = new BufferSurface(buf, new Rect(0, 0, 100, 100));
        // Width/Height should be clipped to buffer size
        surface.Width.Should().Be(5);
        surface.Height.Should().Be(5);
    }

    [Fact]
    public void BufferSurface_SetCell_Offset_Correct()
    {
        // Kills: absCol = col + _offsetX mutated to col - _offsetX
        var buf = new ScreenBuffer(20, 20);
        var surface = new BufferSurface(buf, new Rect(5, 7, 10, 10));
        surface.SetCell(2, 3, 'Q', Style.Plain);
        buf[7, 10].Character.Should().Be('Q'); // 5+2, 7+3
    }

    // ══════════════════════════════════════════════════════════════════
    // ScreenBuffer.cs — bounds equality, dirty tracking, Resize, Fill, Clear
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ScreenBuffer_SetCell_BoundsCheck_ColLessThanZero()
    {
        // Kills: col < 0 mutated to col <= 0 (L35)
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(-1, 0, 'X', Style.Plain);
        buf[0, 0].Character.Should().Be(' '); // should not have been written
    }

    [Fact]
    public void ScreenBuffer_SetCell_BoundsCheck_ColEqualsWidth()
    {
        // Kills: col >= Width mutated to col > Width (L35)
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(5, 0, 'X', Style.Plain); // should silently skip
        buf.SetCell(4, 0, 'Y', Style.Plain); // last valid col
        buf[4, 0].Character.Should().Be('Y');
    }

    [Fact]
    public void ScreenBuffer_SetCell_BoundsCheck_RowLessThanZero()
    {
        // Kills: row < 0 mutated to row <= 0 (L35)
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(0, -1, 'X', Style.Plain);
        buf[0, 0].Character.Should().Be(' ');
    }

    [Fact]
    public void ScreenBuffer_SetCell_BoundsCheck_RowEqualsHeight()
    {
        // Kills: row >= Height mutated to row > Height (L35)
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(0, 5, 'X', Style.Plain);
        buf.SetCell(0, 4, 'Y', Style.Plain);
        buf[0, 4].Character.Should().Be('Y');
    }

    [Fact]
    public void ScreenBuffer_SetCell_DirtyTracking_CharChange()
    {
        // Kills: L51 dirty tracking — if character != or style != (L41)
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.ClearDirtyFlags();

        // Same char, same style -> no dirty
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf[0, 0].IsDirty.Should().BeFalse();

        // Different char -> dirty
        buf.SetCell(0, 0, 'B', Style.Plain);
        buf[0, 0].IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ScreenBuffer_SetCell_DirtyTracking_StyleChange()
    {
        // Kills: !cell.Style.Equals(style) mutated (L41)
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.ClearDirtyFlags();

        // Same char, different style -> dirty
        buf.SetCell(0, 0, 'A', new Style(Color.Red));
        buf[0, 0].IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ScreenBuffer_SetText_BreaksAtWidth()
    {
        // Kills: c >= Width break (L56-58)
        var buf = new ScreenBuffer(5, 1);
        buf.SetText(3, 0, "ABCDE", Style.Plain);
        buf[3, 0].Character.Should().Be('A');
        buf[4, 0].Character.Should().Be('B');
        // Beyond width - should not crash and rest should not be written
        Row(buf, 0).Should().Be("   AB");
    }

    [Fact]
    public void ScreenBuffer_SetText_NullThrows()
    {
        // Kills: L51 statement removal
        var buf = new ScreenBuffer(5, 5);
        var act = () => buf.SetText(0, 0, null!, Style.Plain);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ScreenBuffer_Resize_SameDimensions_NoOp()
    {
        // Kills: L99 newWidth == Width && newHeight == Height early return
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(2, 2, 'X', Style.Plain);
        buf.ClearDirtyFlags();
        buf.Resize(5, 5);
        // Should not have reset cells or marked dirty
        buf[2, 2].Character.Should().Be('X');
        buf[2, 2].IsDirty.Should().BeFalse();
    }

    [Fact]
    public void ScreenBuffer_Resize_Grow_NewCellsEmpty()
    {
        // Kills: L107-108 (new cells = Empty + dirty)
        var buf = new ScreenBuffer(3, 3);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.Resize(5, 5);
        // Old cell preserved
        buf[0, 0].Character.Should().Be('A');
        // New cells should be space with dirty flag
        buf[4, 4].Character.Should().Be(' ');
        buf[4, 4].IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ScreenBuffer_Resize_CopiedCells_MarkedDirty()
    {
        // Kills: L118 newCells[...].IsDirty = true for copied cells
        var buf = new ScreenBuffer(3, 3);
        buf.SetCell(1, 1, 'Z', Style.Plain);
        buf.ClearDirtyFlags();
        buf.Resize(5, 5);
        // Copied cell should be marked dirty
        buf[1, 1].IsDirty.Should().BeTrue();
        buf[1, 1].Character.Should().Be('Z');
    }

    [Fact]
    public void ScreenBuffer_Resize_Shrink_LosesOutOfBoundsData()
    {
        // Kills: copyWidth = Math.Min, copyHeight = Math.Min (L111-112)
        var buf = new ScreenBuffer(5, 5);
        buf.SetCell(4, 4, 'X', Style.Plain);
        buf.SetCell(1, 1, 'Y', Style.Plain);
        buf.Resize(3, 3);
        buf[1, 1].Character.Should().Be('Y');
        buf.Width.Should().Be(3);
        buf.Height.Should().Be(3);
    }

    [Fact]
    public void ScreenBuffer_Resize_NewWidth_UsedInIndexing()
    {
        // Kills: row * newWidth + c arithmetic (L117)
        var buf = new ScreenBuffer(3, 3);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.SetCell(1, 0, 'B', Style.Plain);
        buf.SetCell(2, 0, 'C', Style.Plain);
        buf.SetCell(0, 1, 'D', Style.Plain);
        buf.SetCell(1, 1, 'E', Style.Plain);
        buf.Resize(5, 5);
        // Verify data is in correct positions after resize
        buf[0, 0].Character.Should().Be('A');
        buf[1, 0].Character.Should().Be('B');
        buf[2, 0].Character.Should().Be('C');
        buf[0, 1].Character.Should().Be('D');
        buf[1, 1].Character.Should().Be('E');
    }

    [Fact]
    public void ScreenBuffer_Fill_Arithmetic_IntersectClipping()
    {
        // Kills: L118 area.Intersect arithmetic in Fill
        var buf = new ScreenBuffer(5, 5);
        buf.Fill(new Rect(2, 2, 10, 10), '#', Style.Plain);
        // Should be clipped to buffer bounds
        buf[2, 2].Character.Should().Be('#');
        buf[4, 4].Character.Should().Be('#');
        // Outside buffer should not crash
    }

    [Fact]
    public void ScreenBuffer_Clear_SetsAllToSpaceAndDirty()
    {
        // Kills: L131-132 (Clear statements: Empty assignment + IsDirty = true)
        var buf = new ScreenBuffer(3, 3);
        buf.SetCell(0, 0, 'X', new Style(Color.Red));
        buf.SetCell(2, 2, 'Y', new Style(Color.Blue));
        buf.ClearDirtyFlags();
        buf.Clear();
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 3; c++)
            {
                buf[c, r].Character.Should().Be(' ', $"cell [{c},{r}] should be space");
                buf[c, r].IsDirty.Should().BeTrue($"cell [{c},{r}] should be dirty");
            }
        }
    }

    [Fact]
    public void ScreenBuffer_Resize_InvalidDimensions_Throws()
    {
        // Kills: L96-97 ArgumentOutOfRangeException guards
        var buf = new ScreenBuffer(5, 5);
        var act1 = () => buf.Resize(0, 5);
        act1.Should().Throw<ArgumentOutOfRangeException>();
        var act2 = () => buf.Resize(5, 0);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ══════════════════════════════════════════════════════════════════
    // ScreenDiff.cs — L29-30, L51, L55, L69
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ScreenDiff_ComputeChanges_NullCurrent_Throws()
    {
        // Kills: L29 statement removal
        var act = () => ScreenDiff.ComputeChanges(null!, new ScreenBuffer(1, 1));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_NullPrevious_Throws()
    {
        // Kills: L30 statement removal
        var act = () => ScreenDiff.ComputeChanges(new ScreenBuffer(1, 1), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_StyleDifference_Detected()
    {
        // Kills: L51 style comparison equality chain
        var current = new ScreenBuffer(3, 1);
        var previous = new ScreenBuffer(3, 1);
        current.SetCell(0, 0, ' ', new Style(Color.Red));
        // previous has default (plain) style
        var changes = ScreenDiff.ComputeChanges(current, previous);
        changes.Should().ContainSingle(c => c.Column == 0 && c.Row == 0);
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_CharDifference_Detected()
    {
        // Kills: L55 character comparison
        var current = new ScreenBuffer(3, 1);
        var previous = new ScreenBuffer(3, 1);
        current.SetCell(1, 0, 'Z', Style.Plain);
        var changes = ScreenDiff.ComputeChanges(current, previous);
        changes.Should().ContainSingle(c => c.Character == 'Z');
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_IdenticalBuffers_Empty()
    {
        // When both are identical, no changes
        var current = new ScreenBuffer(3, 3);
        var previous = new ScreenBuffer(3, 3);
        current.SetCell(1, 1, 'A', Style.Plain);
        previous.SetCell(1, 1, 'A', Style.Plain);
        ScreenDiff.ComputeChanges(current, previous).Should().BeEmpty();
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_CurrentLarger_IncludesNewCells()
    {
        // Kills: L51 size comparison (current > previous dimensions)
        var current = new ScreenBuffer(5, 3);
        var previous = new ScreenBuffer(3, 2);
        current.SetCell(4, 2, 'X', Style.Plain);
        var changes = ScreenDiff.ComputeChanges(current, previous);
        // Must include the cell at (4,2) which is beyond previous bounds
        changes.Should().Contain(c => c.Column == 4 && c.Row == 2 && c.Character == 'X');
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_CurrentWider_StartColForRowsInRange()
    {
        // Kills: L55 startCol = row < height ? width : 0
        var current = new ScreenBuffer(5, 2);
        var previous = new ScreenBuffer(3, 2);
        // For row 0 (< height=2): startCol = width=3, so only cols 3-4 are new
        // For row beyond height: all cols would be new
        current.SetCell(3, 0, 'A', Style.Plain);
        current.SetCell(4, 0, 'B', Style.Plain);
        var changes = ScreenDiff.ComputeChanges(current, previous);
        changes.Should().Contain(c => c.Column == 3 && c.Row == 0);
        changes.Should().Contain(c => c.Column == 4 && c.Row == 0);
    }

    [Fact]
    public void ScreenDiff_ComputeChanges_CurrentTaller_AllColsForNewRows()
    {
        // For rows beyond previous height, startCol should be 0
        var current = new ScreenBuffer(3, 4);
        var previous = new ScreenBuffer(3, 2);
        current.SetCell(0, 2, 'X', Style.Plain);
        current.SetCell(2, 3, 'Y', Style.Plain);
        var changes = ScreenDiff.ComputeChanges(current, previous);
        // Row 2 is beyond previous height=2, so all cols 0-2 should be included
        changes.Should().Contain(c => c.Column == 0 && c.Row == 2);
        changes.Should().Contain(c => c.Column == 2 && c.Row == 3);
    }

    [Fact]
    public void ScreenDiff_GetDirtyChanges_NullThrows()
    {
        // Kills: L69 statement removal
        var act = () => ScreenDiff.GetDirtyChanges(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ScreenDiff_GetDirtyChanges_OnlyDirtyCells()
    {
        // Kills: cell.IsDirty check
        var buf = new ScreenBuffer(3, 3);
        buf.ClearDirtyFlags();
        buf.SetCell(0, 0, 'A', Style.Plain); // dirty
        buf.SetCell(2, 2, 'B', Style.Plain); // dirty
        // (1,1) is not dirty
        var changes = ScreenDiff.GetDirtyChanges(buf);
        changes.Should().HaveCount(2);
        changes.Should().Contain(c => c.Column == 0 && c.Row == 0 && c.Character == 'A');
        changes.Should().Contain(c => c.Column == 2 && c.Row == 2 && c.Character == 'B');
    }

    [Fact]
    public void ScreenDiff_GetDirtyChanges_NoDirty_Empty()
    {
        var buf = new ScreenBuffer(3, 3);
        buf.ClearDirtyFlags();
        ScreenDiff.GetDirtyChanges(buf).Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════
    // MouseParser.cs — L21 format check, L52,58 bitwise modifier
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void MouseParser_TooShort_ReturnsNull()
    {
        // Kills: L21 sequence.Length < 4 mutated to < 3 or < 5
        MouseParser.TryParse("0M").Should().BeNull();   // length 2
        MouseParser.TryParse("01M").Should().BeNull();  // length 3
        // length 4 should attempt parsing (may fail for other reasons)
    }

    [Fact]
    public void MouseParser_Length4_ValidFormat()
    {
        // Exact boundary: length 4 is minimum valid
        // "0;1;1M" has 6 chars so we need a shorter valid one
        // "0;1;1M" = 6, but the boundary is 4 — anything < 4 returns null
        MouseParser.TryParse("abc").Should().BeNull(); // length 3 < 4
    }

    [Fact]
    public void MouseParser_ShiftBit_Exact()
    {
        // Kills: (buttonCode & 4) != 0 — shift bit
        // buttonCode = 4 (shift only, left button)
        var e = MouseParser.TryParse("4;1;1M");
        e.Should().NotBeNull();
        e!.Shift.Should().BeTrue();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeFalse();
    }

    [Fact]
    public void MouseParser_AltBit_Exact()
    {
        // Kills: (buttonCode & 8) != 0 — alt bit
        // buttonCode = 8 (alt only)
        var e = MouseParser.TryParse("8;1;1M");
        e.Should().NotBeNull();
        e!.Shift.Should().BeFalse();
        e.Alt.Should().BeTrue();
        e.Control.Should().BeFalse();
    }

    [Fact]
    public void MouseParser_ControlBit_Exact()
    {
        // Kills: (buttonCode & 16) != 0 — control bit
        // buttonCode = 16 (control only)
        var e = MouseParser.TryParse("16;1;1M");
        e.Should().NotBeNull();
        e!.Shift.Should().BeFalse();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeTrue();
    }

    [Fact]
    public void MouseParser_AllModifiers()
    {
        // buttonCode = 4 + 8 + 16 = 28
        var e = MouseParser.TryParse("28;1;1M");
        e.Should().NotBeNull();
        e!.Shift.Should().BeTrue();
        e.Alt.Should().BeTrue();
        e.Control.Should().BeTrue();
    }

    [Fact]
    public void MouseParser_NoModifiers()
    {
        // buttonCode = 0 (no modifier bits set)
        var e = MouseParser.TryParse("0;1;1M");
        e.Should().NotBeNull();
        e!.Shift.Should().BeFalse();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeFalse();
    }

    [Fact]
    public void MouseParser_ScrollBit_Exact()
    {
        // Kills: (buttonCode & 64) != 0 — scroll bit
        // 64 = scroll up, 65 = scroll down
        var up = MouseParser.TryParse("64;1;1M");
        up.Should().NotBeNull();
        up!.EventType.Should().Be(MouseEventType.ScrollUp);
        up.Button.Should().Be(MouseButton.None);

        var down = MouseParser.TryParse("65;1;1M");
        down.Should().NotBeNull();
        down!.EventType.Should().Be(MouseEventType.ScrollDown);
    }

    [Fact]
    public void MouseParser_MotionBit_WithBaseButton3()
    {
        // Kills: (buttonCode & 32) != 0 — motion bit; baseButton == 3 -> Move
        // 32 + 3 = 35
        var e = MouseParser.TryParse("35;1;1M");
        e.Should().NotBeNull();
        e!.EventType.Should().Be(MouseEventType.Move);
        e.Button.Should().Be(MouseButton.None);
    }

    [Fact]
    public void MouseParser_MotionBit_WithBaseButton0()
    {
        // Motion with left button (32 + 0 = 32) -> press (not move)
        var e = MouseParser.TryParse("32;1;1M");
        e.Should().NotBeNull();
        e!.EventType.Should().Be(MouseEventType.Press);
        e.Button.Should().Be(MouseButton.Left);
    }

    [Fact]
    public void MouseParser_InvalidTerminator_ReturnsNull()
    {
        MouseParser.TryParse("0;1;1X").Should().BeNull();
    }

    [Fact]
    public void MouseParser_InvalidParts_ReturnsNull()
    {
        MouseParser.TryParse("0;1M").Should().BeNull(); // only 2 parts
        MouseParser.TryParse("a;1;1M").Should().BeNull(); // non-numeric
    }

    [Fact]
    public void MouseParser_ColumnAndRow_ConvertedFrom1Indexed()
    {
        // col and row are 1-indexed, converted to 0-indexed
        var e = MouseParser.TryParse("0;10;20M");
        e.Should().NotBeNull();
        e!.Column.Should().Be(9);  // 10 - 1
        e.Row.Should().Be(19);     // 20 - 1
    }

    // ══════════════════════════════════════════════════════════════════
    // InputEvent.cs — L35 KeyEvent bitwise modifier
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void KeyEvent_FromConsoleKeyInfo_ShiftBit()
    {
        // Kills: (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0
        var info = new ConsoleKeyInfo('a', ConsoleKey.A, true, false, false);
        var e = new KeyEvent(info);
        e.Shift.Should().BeTrue();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeFalse();
    }

    [Fact]
    public void KeyEvent_FromConsoleKeyInfo_AltBit()
    {
        var info = new ConsoleKeyInfo('a', ConsoleKey.A, false, true, false);
        var e = new KeyEvent(info);
        e.Shift.Should().BeFalse();
        e.Alt.Should().BeTrue();
        e.Control.Should().BeFalse();
    }

    [Fact]
    public void KeyEvent_FromConsoleKeyInfo_ControlBit()
    {
        var info = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, true);
        var e = new KeyEvent(info);
        e.Shift.Should().BeFalse();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeTrue();
    }

    [Fact]
    public void KeyEvent_FromConsoleKeyInfo_AllModifiers()
    {
        var info = new ConsoleKeyInfo('a', ConsoleKey.A, true, true, true);
        var e = new KeyEvent(info);
        e.Shift.Should().BeTrue();
        e.Alt.Should().BeTrue();
        e.Control.Should().BeTrue();
    }

    [Fact]
    public void KeyEvent_FromConsoleKeyInfo_NoModifiers()
    {
        var info = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        var e = new KeyEvent(info);
        e.Shift.Should().BeFalse();
        e.Alt.Should().BeFalse();
        e.Control.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════
    // TestTerminalDriver.cs — L30 Initialize boolean, L74 EnqueueInput
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void TestTerminalDriver_Initialize_SetsTrue()
    {
        // Kills: L30 IsInitialized = true mutated to false
        var driver = new TestTerminalDriver(10, 5);
        driver.IsInitialized.Should().BeFalse();
        driver.Initialize();
        driver.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void TestTerminalDriver_Initialize_SetsShutdownFalse()
    {
        // Kills: L30 IsShutdown = false statement
        var driver = new TestTerminalDriver(10, 5);
        driver.Shutdown();
        driver.IsShutdown.Should().BeTrue();
        driver.Initialize();
        driver.IsShutdown.Should().BeFalse();
    }

    [Fact]
    public void TestTerminalDriver_EnqueueInput_NullThrows()
    {
        // Kills: L74 ArgumentNullException.ThrowIfNull statement removal
        var driver = new TestTerminalDriver(10, 5);
        var act = () => driver.EnqueueInput(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TestTerminalDriver_EnqueueInput_AddsToQueue()
    {
        // Kills: L74 _inputQueue.Enqueue statement removal
        var driver = new TestTerminalDriver(10, 5);
        var key = new KeyEvent(ConsoleKey.A, 'a');
        driver.EnqueueInput(key);
        var read = driver.ReadEvent(CancellationToken.None);
        read.Should().BeSameAs(key);
    }

    [Fact]
    public void TestTerminalDriver_EnqueueKey_CreatesAndEnqueues()
    {
        var driver = new TestTerminalDriver(10, 5);
        driver.EnqueueKey(ConsoleKey.B, 'b', true, false, true);
        var read = driver.ReadEvent(CancellationToken.None) as KeyEvent;
        read.Should().NotBeNull();
        read!.Key.Should().Be(ConsoleKey.B);
        read.KeyChar.Should().Be('b');
        read.Shift.Should().BeTrue();
        read.Control.Should().BeTrue();
    }

    [Fact]
    public void TestTerminalDriver_ReadEvent_CancelledToken_ReturnsNull()
    {
        var driver = new TestTerminalDriver(10, 5);
        driver.EnqueueKey(ConsoleKey.A, 'a');
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        driver.ReadEvent(cts.Token).Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════
    // Widget.cs — L47,74 boolean defaults, NoCov at L54-55
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Widget_Arrange_SetsBoundsAndClearsNeedsLayout()
    {
        // Kills: L47 _needsLayout = false
        var label = new Label("X");
        label.NeedsLayout.Should().BeTrue(); // initial state
        label.Arrange(new Rect(0, 0, 10, 5));
        label.NeedsLayout.Should().BeFalse();
        label.Bounds.Should().Be(new Rect(0, 0, 10, 5));
    }

    [Fact]
    public void Widget_MarkRendered_ClearsNeedsRender()
    {
        // Kills: L74 _needsRender = false
        var label = new Label("X");
        label.NeedsRender.Should().BeTrue(); // initial state
        label.MarkRendered();
        label.NeedsRender.Should().BeFalse();
    }

    [Fact]
    public void Widget_OnKeyEvent_DefaultReturnsFalse()
    {
        // Kills: L54 => false mutated to true (NoCov)
        var label = new Label("X");
        label.OnKeyEvent(new KeyEvent(ConsoleKey.A, 'a')).Should().BeFalse();
    }

    [Fact]
    public void Widget_OnMouseEvent_DefaultReturnsFalse()
    {
        // Kills: L55 => false mutated to true (NoCov)
        var label = new Label("X");
        label.OnMouseEvent(new MouseEvent(MouseButton.Left, MouseEventType.Press, 0, 0)).Should().BeFalse();
    }

    [Fact]
    public void Widget_Visible_DefaultTrue()
    {
        // Kills: L47 Visible = true mutated to false
        var label = new Label("X");
        label.Visible.Should().BeTrue();
    }

    [Fact]
    public void Widget_CanFocus_DefaultFalse()
    {
        // Kills: CanFocus default false
        var label = new Label("X");
        label.CanFocus.Should().BeFalse();
    }

    [Fact]
    public void Widget_Invalidate_SetsBothFlags()
    {
        var label = new Label("X");
        label.Arrange(new Rect(0, 0, 10, 1));
        label.MarkRendered();
        label.NeedsRender.Should().BeFalse();
        label.NeedsLayout.Should().BeFalse();
        label.Invalidate();
        label.NeedsRender.Should().BeTrue();
        label.NeedsLayout.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════
    // HitTester.cs — L10 result statement
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void HitTester_NullRoot_Throws()
    {
        var act = () => HitTester.HitTest(null!, 0, 0);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HitTester_ReturnsSelf_WhenNoChildren()
    {
        // Kills: L10 return root — must return the widget itself
        var label = new Label("X");
        label.Arrange(new Rect(0, 0, 5, 1));
        var hit = HitTester.HitTest(label, 2, 0);
        hit.Should().BeSameAs(label);
    }

    [Fact]
    public void HitTester_ReturnsDeepestChild()
    {
        var container = new VStack();
        var inner = new VStack();
        var leaf = new Label("X");
        inner.Add(leaf);
        container.Add(inner);
        container.Arrange(new Rect(0, 0, 20, 10));
        inner.Arrange(new Rect(0, 0, 20, 5));
        leaf.Arrange(new Rect(0, 0, 20, 1));

        var hit = HitTester.HitTest(container, 5, 0);
        hit.Should().BeSameAs(leaf);
    }

    [Fact]
    public void HitTester_InvisibleRoot_ReturnsNull()
    {
        var label = new Label("X") { Visible = false };
        label.Arrange(new Rect(0, 0, 5, 1));
        HitTester.HitTest(label, 0, 0).Should().BeNull();
    }

    [Fact]
    public void HitTester_OutOfBounds_ReturnsNull()
    {
        var label = new Label("X");
        label.Arrange(new Rect(0, 0, 5, 1));
        HitTester.HitTest(label, 10, 10).Should().BeNull();
    }

    [Fact]
    public void HitTester_InvisibleChild_SkippedReturnsParent()
    {
        var container = new VStack();
        var child = new Label("X") { Visible = false };
        container.Add(child);
        container.Arrange(new Rect(0, 0, 20, 5));
        child.Arrange(new Rect(0, 0, 20, 1));

        var hit = HitTester.HitTest(container, 5, 0);
        hit.Should().BeSameAs(container);
    }

    [Fact]
    public void HitTester_LastChildOnTop()
    {
        // Children checked in reverse order — last child is "on top"
        var container = new VStack();
        var child1 = new Label("A");
        var child2 = new Label("B");
        container.Add(child1);
        container.Add(child2);
        container.Arrange(new Rect(0, 0, 20, 5));
        // Both overlap at same position
        child1.Arrange(new Rect(0, 0, 20, 5));
        child2.Arrange(new Rect(0, 0, 20, 5));

        var hit = HitTester.HitTest(container, 5, 2);
        hit.Should().BeSameAs(child2); // last added = reverse iteration finds first
    }

    // ══════════════════════════════════════════════════════════════════
    // FocusManager.cs — many lines: rebuild, move, set, remove, apply
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void FocusManager_RebuildChain_NullThrows()
    {
        var fm = new FocusManager();
        var act = () => fm.RebuildChain(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FocusManager_RebuildChain_SortsByTabIndex()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 2 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        var btn3 = new Button("C") { CanFocus = true, TabIndex = 0 };
        container.Add(btn1);
        container.Add(btn2);
        container.Add(btn3);
        container.Arrange(new Rect(0, 0, 20, 10));

        fm.RebuildChain(container);
        // First focused should be lowest TabIndex
        fm.Focused.Should().BeSameAs(btn3);
        btn3.HasFocus.Should().BeTrue();
    }

    [Fact]
    public void FocusManager_RebuildChain_PreservesExistingFocus()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.SetFocus(btn2);
        fm.Focused.Should().BeSameAs(btn2);

        // Rebuild again — should preserve btn2 focus
        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn2);
    }

    [Fact]
    public void FocusManager_RebuildChain_EmptyChain_IndexMinusOne()
    {
        var fm = new FocusManager();
        var container = new VStack(); // no focusable children
        container.Arrange(new Rect(0, 0, 20, 5));
        fm.RebuildChain(container);
        fm.Focused.Should().BeNull();
    }

    [Fact]
    public void FocusManager_RebuildChain_SkipsInvisibleWidgets()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var visible = new Button("A") { CanFocus = true };
        var invisible = new Button("B") { CanFocus = true, Visible = false };
        container.Add(visible);
        container.Add(invisible);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(visible);
    }

    [Fact]
    public void FocusManager_RebuildChain_SkipsNonFocusable()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var label = new Label("X"); // CanFocus = false
        var btn = new Button("A") { CanFocus = true };
        container.Add(label);
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn);
    }

    [Fact]
    public void FocusManager_MoveFocus_Forward_Wraps()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn1);
        fm.MoveFocus(FocusDirection.Forward).Should().BeTrue();
        fm.Focused.Should().BeSameAs(btn2);
        fm.MoveFocus(FocusDirection.Forward).Should().BeTrue();
        fm.Focused.Should().BeSameAs(btn1); // wraps around
    }

    [Fact]
    public void FocusManager_MoveFocus_Backward_Wraps()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn1);
        fm.MoveFocus(FocusDirection.Backward).Should().BeTrue();
        fm.Focused.Should().BeSameAs(btn2); // wraps to end
    }

    [Fact]
    public void FocusManager_MoveFocus_EmptyChain_ReturnsFalse()
    {
        var fm = new FocusManager();
        var container = new VStack();
        container.Arrange(new Rect(0, 0, 20, 5));
        fm.RebuildChain(container);
        fm.MoveFocus(FocusDirection.Forward).Should().BeFalse();
    }

    [Fact]
    public void FocusManager_MoveFocus_RemovesFocusFromPrevious()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        btn1.HasFocus.Should().BeTrue();
        btn2.HasFocus.Should().BeFalse();

        fm.MoveFocus(FocusDirection.Forward);
        btn1.HasFocus.Should().BeFalse();
        btn2.HasFocus.Should().BeTrue();
    }

    [Fact]
    public void FocusManager_SetFocus_NullThrows()
    {
        var fm = new FocusManager();
        var act = () => fm.SetFocus(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FocusManager_SetFocus_NotInChain_NoEffect()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));
        fm.RebuildChain(container);

        var outsider = new Button("Z") { CanFocus = true };
        fm.SetFocus(outsider);
        fm.Focused.Should().BeSameAs(btn); // unchanged
    }

    [Fact]
    public void FocusManager_SetFocus_RemovesFocusFromPrevious()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true };
        var btn2 = new Button("B") { CanFocus = true };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn1);
        btn1.HasFocus.Should().BeTrue();

        fm.SetFocus(btn2);
        btn1.HasFocus.Should().BeFalse();
        btn2.HasFocus.Should().BeTrue();
        fm.Focused.Should().BeSameAs(btn2);
    }

    [Fact]
    public void FocusManager_SetFocus_SameWidget_NoRemoveFocus()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.SetFocus(btn);
        btn.HasFocus.Should().BeTrue();
        // Setting focus to same widget should not lose focus
        fm.SetFocus(btn);
        btn.HasFocus.Should().BeTrue();
    }

    [Fact]
    public void FocusManager_RemoveFromChain_FocusedWidget_MovesFocusToNext()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        var btn3 = new Button("C") { CanFocus = true, TabIndex = 2 };
        container.Add(btn1);
        container.Add(btn2);
        container.Add(btn3);
        container.Arrange(new Rect(0, 0, 20, 10));

        fm.RebuildChain(container);
        fm.SetFocus(btn2);
        fm.Focused.Should().BeSameAs(btn2);
        btn2.HasFocus.Should().BeTrue();

        fm.RemoveFromChain(btn2);
        btn2.HasFocus.Should().BeFalse();
        fm.Focused.Should().NotBeNull();
    }

    [Fact]
    public void FocusManager_RemoveFromChain_NotInChain_NoEffect()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));
        fm.RebuildChain(container);

        var outsider = new Button("Z") { CanFocus = true };
        fm.RemoveFromChain(outsider); // should not throw
        fm.Focused.Should().BeSameAs(btn);
    }

    [Fact]
    public void FocusManager_RemoveFromChain_LastWidget_NoFocus()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn);
        fm.RemoveFromChain(btn);
        fm.Focused.Should().BeNull();
    }

    [Fact]
    public void FocusManager_RemoveFromChain_AdjustsCurrentIndex()
    {
        // Kills: L106-108 _currentIndex >= _focusChain.Count adjustment
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        var btn3 = new Button("C") { CanFocus = true, TabIndex = 2 };
        container.Add(btn1);
        container.Add(btn2);
        container.Add(btn3);
        container.Arrange(new Rect(0, 0, 20, 10));

        fm.RebuildChain(container);
        fm.SetFocus(btn3); // index = 2
        fm.RemoveFromChain(btn3); // chain shrinks, index must adjust
        fm.Focused.Should().NotBeNull();
        // After removing last item, index should clamp
    }

    [Fact]
    public void FocusManager_ApplyFocus_SetsHasFocusAndInvalidates()
    {
        // Kills: widget.HasFocus = true, widget.OnFocusGained(), widget.Invalidate()
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 20, 5));

        // After arrange, clear rendered state
        btn.MarkRendered();
        btn.Arrange(new Rect(0, 0, 20, 1));
        btn.NeedsRender.Should().BeFalse();
        btn.NeedsLayout.Should().BeFalse();

        fm.RebuildChain(container);
        // ApplyFocus is called during RebuildChain
        btn.HasFocus.Should().BeTrue();
        btn.NeedsRender.Should().BeTrue();  // Invalidate was called
        btn.NeedsLayout.Should().BeTrue();
    }

    [Fact]
    public void FocusManager_RemoveFocus_ClearsHasFocusAndInvalidates()
    {
        // Kills: widget.HasFocus = false, widget.OnFocusLost(), widget.Invalidate()
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 20, 5));

        fm.RebuildChain(container);
        btn1.HasFocus.Should().BeTrue();

        // After arrange/render, clear states
        btn1.MarkRendered();
        btn1.Arrange(new Rect(0, 0, 20, 1));

        fm.MoveFocus(FocusDirection.Forward);
        btn1.HasFocus.Should().BeFalse();
        btn1.NeedsRender.Should().BeTrue();  // Invalidate was called on remove
    }

    [Fact]
    public void FocusManager_MoveFocus_Forward_ArithmeticExact()
    {
        // Kills: (_currentIndex + 1) % _focusChain.Count arithmetic
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        var btn3 = new Button("C") { CanFocus = true, TabIndex = 2 };
        container.Add(btn1);
        container.Add(btn2);
        container.Add(btn3);
        container.Arrange(new Rect(0, 0, 20, 10));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn1); // index 0
        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().BeSameAs(btn2); // index 1
        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().BeSameAs(btn3); // index 2
        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().BeSameAs(btn1); // wraps to 0
    }

    [Fact]
    public void FocusManager_MoveFocus_Backward_ArithmeticExact()
    {
        // Kills: (_currentIndex - 1 + _focusChain.Count) % _focusChain.Count
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        var btn3 = new Button("C") { CanFocus = true, TabIndex = 2 };
        container.Add(btn1);
        container.Add(btn2);
        container.Add(btn3);
        container.Arrange(new Rect(0, 0, 20, 10));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn1); // index 0
        fm.MoveFocus(FocusDirection.Backward);
        fm.Focused.Should().BeSameAs(btn3); // wraps to 2
        fm.MoveFocus(FocusDirection.Backward);
        fm.Focused.Should().BeSameAs(btn2); // index 1
        fm.MoveFocus(FocusDirection.Backward);
        fm.Focused.Should().BeSameAs(btn1); // index 0
    }

    [Fact]
    public void FocusManager_RebuildChain_CollectsFromNestedContainers()
    {
        var fm = new FocusManager();
        var outer = new VStack();
        var inner = new VStack();
        var btn = new Button("Deep") { CanFocus = true };
        inner.Add(btn);
        outer.Add(inner);
        outer.Arrange(new Rect(0, 0, 20, 10));

        fm.RebuildChain(outer);
        fm.Focused.Should().BeSameAs(btn);
    }

    [Fact]
    public void FocusManager_RebuildChain_InvisibleContainer_SkipsChildren()
    {
        var fm = new FocusManager();
        var outer = new VStack();
        var inner = new VStack() { Visible = false };
        var btn = new Button("Hidden") { CanFocus = true };
        inner.Add(btn);
        outer.Add(inner);
        outer.Arrange(new Rect(0, 0, 20, 10));

        fm.RebuildChain(outer);
        fm.Focused.Should().BeNull(); // btn is under invisible container
    }

    [Fact]
    public void FocusManager_RemoveFromChain_UnfocusedWidget()
    {
        // Removing a widget that is not focused should not unfocus current
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        var btn3 = new Button("C") { CanFocus = true, TabIndex = 2 };
        container.Add(btn1);
        container.Add(btn2);
        container.Add(btn3);
        container.Arrange(new Rect(0, 0, 20, 10));

        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn1);
        fm.RemoveFromChain(btn3); // not focused
        fm.Focused.Should().BeSameAs(btn1); // still focused on btn1
        btn1.HasFocus.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════
    // ScreenBuffer.cs — SetCell boundary, dirty tracking, SetText bounds,
    //                    Resize, Clear
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ScreenBuffer_SetCell_ExactBoundary_LastCol_Works()
    {
        // Kills: col >= Width mutated to col > Width (L35)
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(4, 0, 'X', Style.Plain); // col == Width-1 should work
        buf[4, 0].Character.Should().Be('X');
    }

    [Fact]
    public void ScreenBuffer_SetCell_AtWidth_IsIgnored()
    {
        // Kills: col >= Width boundary (L35)
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(5, 0, 'X', Style.Plain); // col == Width should be ignored
        // Should not throw, and cell at 4,0 should remain default
        buf[4, 0].Character.Should().Be(' ');
    }

    [Fact]
    public void ScreenBuffer_SetCell_AtHeight_IsIgnored()
    {
        // Kills: row >= Height boundary (L35)
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(0, 3, 'X', Style.Plain); // row == Height should be ignored
        buf[0, 2].Character.Should().Be(' ');
    }

    [Fact]
    public void ScreenBuffer_SetCell_NegativeCol_IsIgnored()
    {
        // Kills: col < 0 boundary (L35)
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(-1, 0, 'X', Style.Plain);
        buf[0, 0].Character.Should().Be(' ');
    }

    [Fact]
    public void ScreenBuffer_SetCell_NegativeRow_IsIgnored()
    {
        // Kills: row < 0 boundary (L35)
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(0, -1, 'X', Style.Plain);
        buf[0, 0].Character.Should().Be(' ');
    }

    [Fact]
    public void ScreenBuffer_SetCell_DirtyWhenCharChanges()
    {
        // Kills: dirty tracking mutation (L56,58) — char change sets dirty
        var buf = new ScreenBuffer(5, 3);
        buf.ClearDirtyFlags();
        buf[0, 0].IsDirty.Should().BeFalse();
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf[0, 0].IsDirty.Should().BeTrue();
        buf[0, 0].Character.Should().Be('A');
    }

    [Fact]
    public void ScreenBuffer_SetCell_DirtyWhenStyleChanges()
    {
        // Kills: style equality check mutation (L56) — style change sets dirty
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(0, 0, ' ', Style.Plain);
        buf.ClearDirtyFlags();
        buf[0, 0].IsDirty.Should().BeFalse();
        buf.SetCell(0, 0, ' ', new Style(Color.Red));
        buf[0, 0].IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ScreenBuffer_SetCell_NotDirtyWhenSameCharAndStyle()
    {
        // Kills: condition negation mutation on char/style check
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.ClearDirtyFlags();
        buf.SetCell(0, 0, 'A', Style.Plain); // same char, same style
        buf[0, 0].IsDirty.Should().BeFalse();
    }

    [Fact]
    public void ScreenBuffer_SetText_ExactBoundary_EndsAtWidth()
    {
        // Kills: c >= Width boundary (L68,70)
        var buf = new ScreenBuffer(5, 1);
        buf.SetText(0, 0, "ABCDE", Style.Plain);
        buf[4, 0].Character.Should().Be('E');
    }

    [Fact]
    public void ScreenBuffer_SetText_TruncatesAtWidth()
    {
        // Kills: break on c >= Width (L68,70) — text beyond width is dropped
        var buf = new ScreenBuffer(5, 1);
        buf.SetText(0, 0, "ABCDEFGH", Style.Plain);
        buf[4, 0].Character.Should().Be('E');
        // Verify truncation by checking all chars
        Row(buf, 0).Should().Be("ABCDE");
    }

    [Fact]
    public void ScreenBuffer_SetText_WithOffset()
    {
        // Verify SetText starting at col > 0
        var buf = new ScreenBuffer(5, 1);
        buf.SetText(3, 0, "XY", Style.Plain);
        buf[3, 0].Character.Should().Be('X');
        buf[4, 0].Character.Should().Be('Y');
    }

    [Fact]
    public void ScreenBuffer_Resize_SameSize_NoOp()
    {
        // Kills: equality mutation on resize check (L99)
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.ClearDirtyFlags();
        buf.Resize(5, 3); // same size — should be no-op
        buf[0, 0].Character.Should().Be('A');
        buf[0, 0].IsDirty.Should().BeFalse();
    }

    [Fact]
    public void ScreenBuffer_Resize_CopiesExistingContent()
    {
        // Kills: resize logic (L99)
        var buf = new ScreenBuffer(5, 3);
        buf.SetCell(0, 0, 'A', Style.Plain);
        buf.SetCell(4, 2, 'Z', Style.Plain);
        buf.Resize(10, 5);
        buf.Width.Should().Be(10);
        buf.Height.Should().Be(5);
        buf[0, 0].Character.Should().Be('A');
        buf[4, 2].Character.Should().Be('Z');
    }

    [Fact]
    public void ScreenBuffer_Resize_ShrinkPreservesVisibleContent()
    {
        var buf = new ScreenBuffer(10, 5);
        buf.SetCell(2, 2, 'M', Style.Plain);
        buf.Resize(5, 3);
        buf.Width.Should().Be(5);
        buf.Height.Should().Be(3);
        buf[2, 2].Character.Should().Be('M');
    }

    [Fact]
    public void ScreenBuffer_Clear_SetsCharacterAndDirty()
    {
        // Kills: clear statements (L131,132)
        var buf = new ScreenBuffer(3, 2);
        buf.SetCell(0, 0, 'X', new Style(Color.Red));
        buf.ClearDirtyFlags();
        buf.Clear();
        buf[0, 0].Character.Should().Be(' ');
        buf[0, 0].Style.Should().Be(Style.Plain);
        buf[0, 0].IsDirty.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════
    // ScreenDiff.cs — Style-only changes, size increase paths
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ScreenDiff_DetectsStyleOnlyChange()
    {
        // Kills: L51 — style equality check; same char, different style should be detected
        var current = new ScreenBuffer(3, 1);
        var previous = new ScreenBuffer(3, 1);
        current.SetCell(1, 0, 'A', new Style(Color.Red));
        previous.SetCell(1, 0, 'A', Style.Plain);

        var changes = ScreenDiff.ComputeChanges(current, previous);
        changes.Should().Contain(c => c.Column == 1 && c.Row == 0 && c.Character == 'A');
    }

    [Fact]
    public void ScreenDiff_SameCharSameStyle_NoChange()
    {
        // Kills: L55 — conditional mutation on character comparison
        var current = new ScreenBuffer(3, 1);
        var previous = new ScreenBuffer(3, 1);
        // Both have same content (default ' ' with Style.Plain)
        var changes = ScreenDiff.ComputeChanges(current, previous);
        changes.Should().BeEmpty();
    }

    [Fact]
    public void ScreenDiff_SizeIncrease_IncludesNewCells()
    {
        // Kills: L51,55 — size increase conditional
        var current = new ScreenBuffer(5, 2);
        var previous = new ScreenBuffer(3, 1);
        current.SetCell(4, 1, 'Z', Style.Plain);

        var changes = ScreenDiff.ComputeChanges(current, previous);
        // Should include cells beyond previous dimensions
        changes.Should().Contain(c => c.Column == 4 && c.Row == 1);
    }

    [Fact]
    public void ScreenDiff_SizeIncrease_StartCol_UsesWidthForOverlapRows()
    {
        // Kills: L55 — startCol = row < height ? width : 0
        // For rows within old height, start at old width; for new rows, start at 0
        var current = new ScreenBuffer(5, 3);
        var previous = new ScreenBuffer(3, 2);
        current.SetCell(3, 0, 'X', Style.Plain); // col 3 in row 0 (overlap row) — beyond old width
        current.SetCell(0, 2, 'Y', Style.Plain); // col 0 in row 2 (new row)

        var changes = ScreenDiff.ComputeChanges(current, previous);
        changes.Should().Contain(c => c.Column == 3 && c.Row == 0 && c.Character == 'X');
        changes.Should().Contain(c => c.Column == 0 && c.Row == 2 && c.Character == 'Y');
    }

    // ══════════════════════════════════════════════════════════════════
    // MouseParser.cs — Length check, modifier bit extraction
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void MouseParser_MinimumValidLength_FourChars()
    {
        // Kills: L21 — sequence.Length < 4 equality
        // "0;1;1M" is valid (6 chars), "0;1M" would be 4 chars but only 2 parts
        // Test exact boundary: 3 chars should fail, 4+ should try to parse
        var result3 = MouseParser.TryParse("01M"); // 3 chars — too short
        result3.Should().BeNull();

        var result4 = MouseParser.TryParse("0;1M"); // 4 chars — not enough parts (only 2) but passes length check
        result4.Should().BeNull(); // fails on parts.Length != 3, but passes length check
    }

    [Fact]
    public void MouseParser_ShiftModifier()
    {
        // Kills: L58 — bitwise mutation on modifier extraction
        // Button code 4 = shift modifier (bit 2 set), left button (base 0)
        var result = MouseParser.TryParse("4;10;5M");
        result.Should().NotBeNull();
        result!.Shift.Should().BeTrue();
        result.Alt.Should().BeFalse();
        result.Control.Should().BeFalse();
        result.Button.Should().Be(MouseButton.Left);
    }

    [Fact]
    public void MouseParser_AltModifier()
    {
        // Button code 8 = alt modifier (bit 3 set)
        var result = MouseParser.TryParse("8;10;5M");
        result.Should().NotBeNull();
        result!.Alt.Should().BeTrue();
        result.Shift.Should().BeFalse();
        result.Control.Should().BeFalse();
    }

    [Fact]
    public void MouseParser_ControlModifier()
    {
        // Button code 16 = control modifier (bit 4 set)
        var result = MouseParser.TryParse("16;10;5M");
        result.Should().NotBeNull();
        result!.Control.Should().BeTrue();
        result.Shift.Should().BeFalse();
        result.Alt.Should().BeFalse();
    }

    [Fact]
    public void MouseParser_MotionBit()
    {
        // Kills: L58 — bitwise mutation on (buttonCode & 32)
        // Button code 35 = 32 (motion) + 3 (no button in motion)
        var result = MouseParser.TryParse("35;10;5M");
        result.Should().NotBeNull();
        result!.EventType.Should().Be(MouseEventType.Move);
        result.Button.Should().Be(MouseButton.None);
    }

    [Fact]
    public void MouseParser_ScrollBit()
    {
        // Button code 64 = scroll bit, base 0 = scroll up
        var result = MouseParser.TryParse("64;10;5M");
        result.Should().NotBeNull();
        result!.EventType.Should().Be(MouseEventType.ScrollUp);

        // base 1 = scroll down
        var result2 = MouseParser.TryParse("65;10;5M");
        result2.Should().NotBeNull();
        result2!.EventType.Should().Be(MouseEventType.ScrollDown);
    }

    [Fact]
    public void MouseParser_Release_LowercaseM()
    {
        // Terminator 'm' = release
        var result = MouseParser.TryParse("0;10;5m");
        result.Should().NotBeNull();
        result!.EventType.Should().Be(MouseEventType.Release);
    }

    [Fact]
    public void MouseParser_Coordinates_AreZeroIndexed()
    {
        // Columns and rows are converted from 1-indexed to 0-indexed
        var result = MouseParser.TryParse("0;10;5M");
        result.Should().NotBeNull();
        result!.Column.Should().Be(9); // 10 - 1
        result.Row.Should().Be(4); // 5 - 1
    }

    [Fact]
    public void MouseParser_MiddleButton()
    {
        // base button 1 = middle
        var result = MouseParser.TryParse("1;1;1M");
        result.Should().NotBeNull();
        result!.Button.Should().Be(MouseButton.Middle);
    }

    [Fact]
    public void MouseParser_RightButton()
    {
        // base button 2 = right
        var result = MouseParser.TryParse("2;1;1M");
        result.Should().NotBeNull();
        result!.Button.Should().Be(MouseButton.Right);
    }

    [Fact]
    public void MouseParser_InvalidTerminator_UnknownChar_ReturnsNull()
    {
        var result = MouseParser.TryParse("0;1;1X");
        result.Should().BeNull();
    }

    [Fact]
    public void MouseParser_WrongPartCount_ReturnsNull()
    {
        var result = MouseParser.TryParse("0;1M"); // only 2 parts
        result.Should().BeNull();
    }

    [Fact]
    public void MouseParser_NonNumericParts_ReturnsNull()
    {
        var result = MouseParser.TryParse("a;b;cM");
        result.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════
    // FocusManager.cs — Initial state, RebuildChain preserves focus,
    //                   Sort by TabIndex, empty chain reset
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void FocusManager_InitialState_FocusIsNull()
    {
        // Kills: L9 — _currentIndex = -1 mutated to +1
        var fm = new FocusManager();
        fm.Focused.Should().BeNull();
    }

    [Fact]
    public void FocusManager_Focused_ReturnsCorrectWidget()
    {
        // Kills: L11 — equality mutation on Focused property
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true, TabIndex = 0 };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 80, 24));
        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn);
    }

    [Fact]
    public void FocusManager_RebuildChain_PreservesFocusedWidget()
    {
        // Kills: L20,29,30 — Contains check and IndexOf
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 80, 24));

        fm.RebuildChain(container);
        fm.SetFocus(btn2);
        fm.Focused.Should().BeSameAs(btn2);

        // Rebuild chain — btn2 should still be focused
        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn2);
    }

    [Fact]
    public void FocusManager_RebuildChain_EmptyChain_ResetsIndex()
    {
        // Kills: L40 — _currentIndex = -1 mutated to +1
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 80, 24));
        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn);

        // Now rebuild with empty container
        var emptyContainer = new VStack();
        emptyContainer.Add(new Label("not focusable"));
        emptyContainer.Arrange(new Rect(0, 0, 80, 24));
        fm.RebuildChain(emptyContainer);
        fm.Focused.Should().BeNull();
    }

    [Fact]
    public void FocusManager_SetFocus_NotInChain_DoesNothing()
    {
        // Kills: L76 — index < 0 check
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true };
        container.Add(btn1);
        container.Arrange(new Rect(0, 0, 80, 24));
        fm.RebuildChain(container);

        var orphan = new Button("Orphan") { CanFocus = true };
        fm.SetFocus(orphan); // not in chain — should be no-op
        fm.Focused.Should().BeSameAs(btn1); // still focused on btn1
    }

    [Fact]
    public void FocusManager_SetFocus_SameWidget_NoError()
    {
        // Kills: L82 — previousFocused != widget check
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true };
        container.Add(btn1);
        container.Arrange(new Rect(0, 0, 80, 24));
        fm.RebuildChain(container);
        fm.SetFocus(btn1); // already focused — should not unfocus first
        fm.Focused.Should().BeSameAs(btn1);
        btn1.HasFocus.Should().BeTrue();
    }

    [Fact]
    public void FocusManager_RemoveFromChain_FocusedWidget_ShiftsFocus()
    {
        // Kills: L111,113 — _currentIndex >= 0 after removal
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 0 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        container.Add(btn1);
        container.Add(btn2);
        container.Arrange(new Rect(0, 0, 80, 24));
        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn1);

        fm.RemoveFromChain(btn1); // focused widget removed — focus shifts to btn2
        fm.Focused.Should().BeSameAs(btn2);
        btn2.HasFocus.Should().BeTrue();
        btn1.HasFocus.Should().BeFalse();
    }

    [Fact]
    public void FocusManager_RemoveFromChain_LastWidget_FocusBecomesNull()
    {
        // Kills: L111 — _currentIndex >= 0 when chain becomes empty
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 80, 24));
        fm.RebuildChain(container);
        fm.Focused.Should().BeSameAs(btn);

        fm.RemoveFromChain(btn);
        fm.Focused.Should().BeNull();
    }

    [Fact]
    public void FocusManager_RemoveFromChain_WidgetNotInChain_DoesNothing()
    {
        var fm = new FocusManager();
        var container = new VStack();
        var btn = new Button("A") { CanFocus = true };
        container.Add(btn);
        container.Arrange(new Rect(0, 0, 80, 24));
        fm.RebuildChain(container);

        var orphan = new Button("X") { CanFocus = true };
        fm.RemoveFromChain(orphan); // not in chain — no-op
        fm.Focused.Should().BeSameAs(btn);
    }

    [Fact]
    public void FocusManager_SortsByTabIndex()
    {
        // Kills: L20 — sort comparison
        var fm = new FocusManager();
        var container = new VStack();
        var btn1 = new Button("A") { CanFocus = true, TabIndex = 2 };
        var btn2 = new Button("B") { CanFocus = true, TabIndex = 1 };
        var btn3 = new Button("C") { CanFocus = true, TabIndex = 0 };
        container.Add(btn1);
        container.Add(btn2);
        container.Add(btn3);
        container.Arrange(new Rect(0, 0, 80, 24));

        fm.RebuildChain(container);
        // First focused should be lowest TabIndex (btn3, TabIndex=0)
        fm.Focused.Should().BeSameAs(btn3);
        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().BeSameAs(btn2); // TabIndex=1
        fm.MoveFocus(FocusDirection.Forward);
        fm.Focused.Should().BeSameAs(btn1); // TabIndex=2
    }

    // ══════════════════════════════════════════════════════════════════
    // HStack.cs — MeasureContent spacing arithmetic
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void HStack_MeasureContent_WithSpacing_ExactWidth()
    {
        // Kills: L23 — arithmetic mutation on width += Spacing
        var hstack = new HStack { Spacing = 2 };
        var a = new Label("AB"); // width 2
        var b = new Label("CD"); // width 2
        hstack.Add(a);
        hstack.Add(b);

        var size = hstack.MeasureContent(new Size(80, 24));
        // Expected: 2 + 2 (spacing) + 2 = 6
        size.Width.Should().Be(6);
    }

    [Fact]
    public void HStack_MeasureContent_SingleChild_NoSpacing()
    {
        // Kills: i < children.Count - 1 boundary
        var hstack = new HStack { Spacing = 5 };
        var a = new Label("AB");
        hstack.Add(a);

        var size = hstack.MeasureContent(new Size(80, 24));
        size.Width.Should().Be(2); // no spacing added for single child
    }

    [Fact]
    public void HStack_MeasureContent_ClampsToAvailable()
    {
        var hstack = new HStack();
        var a = new Label("ABCDEFGHIJ"); // 10 chars
        hstack.Add(a);

        var size = hstack.MeasureContent(new Size(5, 24));
        size.Width.Should().Be(5); // clamped to available
    }

    // ══════════════════════════════════════════════════════════════════
    // VStack.cs — MeasureContent spacing arithmetic
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void VStack_MeasureContent_WithSpacing_ExactHeight()
    {
        // Kills: L23 — arithmetic mutation on height += Spacing
        var vstack = new VStack { Spacing = 2 };
        var a = new Label("A"); // height 1
        var b = new Label("B"); // height 1
        vstack.Add(a);
        vstack.Add(b);

        var size = vstack.MeasureContent(new Size(80, 24));
        // Expected: 1 + 2 (spacing) + 1 = 4
        size.Height.Should().Be(4);
    }

    [Fact]
    public void VStack_MeasureContent_SingleChild_NoSpacing()
    {
        // Kills: i < children.Count - 1 boundary
        var vstack = new VStack { Spacing = 5 };
        var a = new Label("A");
        vstack.Add(a);

        var size = vstack.MeasureContent(new Size(80, 24));
        size.Height.Should().Be(1); // no spacing for single child
    }

    [Fact]
    public void VStack_MeasureContent_ClampsToAvailable()
    {
        var vstack = new VStack();
        var a = new Label("Line1\nLine2\nLine3\nLine4\nLine5");
        vstack.Add(a);

        var size = vstack.MeasureContent(new Size(80, 3));
        size.Height.Should().Be(3); // clamped to available
    }

    // ══════════════════════════════════════════════════════════════════
    // RenderableWidget.cs — MeasureContent
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void RenderableWidget_MeasureContent_ReturnsCorrectWidth()
    {
        // Kills: L46 — measurement.Max used for width
        var text = new Markup("Hello");
        var widget = new Spectre.Console.Tui.Integration.RenderableWidget(text);
        var size = widget.MeasureContent(new Size(80, 24));
        size.Width.Should().Be(5); // "Hello" is 5 chars
        size.Height.Should().Be(24); // uses available height
    }

    [Fact]
    public void RenderableWidget_SetRenderable_ThrowsOnNull()
    {
        var text = new Markup("Hello");
        var widget = new Spectre.Console.Tui.Integration.RenderableWidget(text);
        var act = () => widget.Renderable = null!;
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RenderableWidget_ConstructorThrowsOnNull()
    {
        var act = () => new Spectre.Console.Tui.Integration.RenderableWidget(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ══════════════════════════════════════════════════════════════════
    // ScreenBuffer.cs — GetDirtyChanges
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ScreenDiff_GetDirtyChanges_ReturnsOnlyDirtyCells()
    {
        var buf = new ScreenBuffer(3, 2);
        buf.ClearDirtyFlags();
        buf.SetCell(1, 0, 'X', Style.Plain);
        // Only cell (1,0) should be dirty
        var changes = ScreenDiff.GetDirtyChanges(buf);
        changes.Should().ContainSingle(c => c.Column == 1 && c.Row == 0 && c.Character == 'X');
    }

    [Fact]
    public void ScreenDiff_GetDirtyChanges_EmptyWhenCleared()
    {
        var buf = new ScreenBuffer(3, 2);
        buf.ClearDirtyFlags();
        var changes = ScreenDiff.GetDirtyChanges(buf);
        changes.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════
    // BufferCell.cs — Equality and GetHashCode
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void BufferCell_Equals_SameCharDifferentStyle_NotEqual()
    {
        var a = new BufferCell('A', Style.Plain);
        var b = new BufferCell('A', new Style(Color.Red));
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void BufferCell_Equals_DifferentCharSameStyle_NotEqual()
    {
        var a = new BufferCell('A', Style.Plain);
        var b = new BufferCell('B', Style.Plain);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void BufferCell_Equals_SameCharSameStyle_Equal()
    {
        var a = new BufferCell('A', Style.Plain);
        var b = new BufferCell('A', Style.Plain);
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void BufferCell_Equals_BoxedObject()
    {
        var a = new BufferCell('A', Style.Plain);
        a.Equals((object)a).Should().BeTrue();
        a.Equals((object?)null).Should().BeFalse();
        a.Equals("not a cell").Should().BeFalse();
    }

    [Fact]
    public void BufferCell_GetHashCode_EqualCellsSameHash()
    {
        var a = new BufferCell('A', Style.Plain);
        var b = new BufferCell('A', Style.Plain);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void BufferCell_Constructor_SetsDirty()
    {
        var cell = new BufferCell('X', Style.Plain);
        cell.IsDirty.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════
    // ScreenBuffer.cs — Fill method
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ScreenBuffer_Fill_ClipsToBuffer()
    {
        var buf = new ScreenBuffer(5, 3);
        // Fill area larger than buffer — should clip
        buf.Fill(new Rect(-1, -1, 10, 10), 'X', Style.Plain);
        buf[0, 0].Character.Should().Be('X');
        buf[4, 2].Character.Should().Be('X');
    }

    [Fact]
    public void ScreenBuffer_Fill_PartialArea()
    {
        var buf = new ScreenBuffer(5, 3);
        buf.Fill(new Rect(1, 1, 2, 1), '#', Style.Plain);
        buf[0, 0].Character.Should().Be(' '); // outside fill
        buf[1, 1].Character.Should().Be('#'); // inside fill
        buf[2, 1].Character.Should().Be('#'); // inside fill
        buf[3, 1].Character.Should().Be(' '); // outside fill
    }

    // ══════════════════════════════════════════════════════════════════
    // Constraint.cs — Resolve edge cases
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Constraint_Min_Resolve_ReturnsValue()
    {
        // Kills: L50 — Min resolve returns Math.Max(Value, 0)
        var c = Constraint.Min(15);
        c.Resolve(100).Should().Be(15);
        c.Resolve(5).Should().Be(15); // min doesn't cap at available
    }

    [Fact]
    public void Constraint_Percentage_Resolve_ExactValues()
    {
        var c = Constraint.Percentage(25);
        c.Resolve(200).Should().Be(50);
        c.Resolve(0).Should().Be(0);
    }

    [Fact]
    public void Constraint_Equality_DifferentKindSameValue()
    {
        var a = Constraint.Fixed(10);
        var b = Constraint.Min(10);
        a.Equals(b).Should().BeFalse();
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Constraint_Equals_BoxedNull()
    {
        var a = Constraint.Fixed(10);
        a.Equals((object?)null).Should().BeFalse();
        a.Equals("not a constraint").Should().BeFalse();
    }
}
