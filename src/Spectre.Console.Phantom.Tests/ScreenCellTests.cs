using Shouldly;
using Spectre.Console.Phantom;

namespace Spectre.Console.Phantom.Tests;

/// <summary>
/// Unit tests for <see cref="ScreenCell"/>, <see cref="CellColor"/>,
/// and <see cref="CellDecoration"/>.
/// </summary>
public sealed class ScreenCellTests
{
    // ── ScreenCell Defaults ──────────────────────────────────────────

    [Fact]
    public void New_Cell_Should_Have_Default_Values()
    {
        var cell = new ScreenCell();
        cell.Character.ShouldBe(' ');
        cell.Foreground.ShouldBeNull();
        cell.Background.ShouldBeNull();
        cell.Decoration.ShouldBe(CellDecoration.None);
        cell.HyperlinkUrl.ShouldBeNull();
    }

    // ── Reset ────────────────────────────────────────────────────────

    [Fact]
    public void Reset_Should_Restore_All_Defaults()
    {
        var cell = new ScreenCell
        {
            Character = 'X',
            Foreground = CellColor.FromLegacy(31),
            Background = CellColor.FromLegacy(42),
            Decoration = CellDecoration.Bold | CellDecoration.Italic,
            HyperlinkUrl = "https://example.com",
        };

        cell.Reset();

        cell.Character.ShouldBe(' ');
        cell.Foreground.ShouldBeNull();
        cell.Background.ShouldBeNull();
        cell.Decoration.ShouldBe(CellDecoration.None);
        cell.HyperlinkUrl.ShouldBeNull();
    }

    // ── CopyStyleFrom ────────────────────────────────────────────────

    [Fact]
    public void CopyStyleFrom_Should_Copy_All_Style_Properties()
    {
        var source = new ScreenCell
        {
            Character = 'S',
            Foreground = CellColor.FromRgb(255, 0, 0),
            Background = CellColor.FromEightBit(42),
            Decoration = CellDecoration.Underline | CellDecoration.Strikethrough,
            HyperlinkUrl = "https://link.test",
        };

        var target = new ScreenCell { Character = 'T' };
        target.CopyStyleFrom(source);

        // Character should NOT be copied
        target.Character.ShouldBe('T');

        // Style properties should be copied
        target.Foreground!.Value.Mode.ShouldBe(ColorMode.TrueColor);
        target.Foreground!.Value.R.ShouldBe((byte)255);
        target.Background!.Value.Mode.ShouldBe(ColorMode.EightBit);
        target.Background!.Value.Index.ShouldBe(42);
        target.Decoration.HasFlag(CellDecoration.Underline).ShouldBeTrue();
        target.Decoration.HasFlag(CellDecoration.Strikethrough).ShouldBeTrue();
        target.HyperlinkUrl.ShouldBe("https://link.test");
    }

    [Fact]
    public void CopyStyleFrom_Should_Throw_For_Null()
    {
        var cell = new ScreenCell();
        Should.Throw<ArgumentNullException>(() => cell.CopyStyleFrom(null!));
    }

    // ── ToString ─────────────────────────────────────────────────────

    [Fact]
    public void ToString_Should_Return_Character_As_String()
    {
        var cell = new ScreenCell { Character = 'Z' };
        cell.ToString().ShouldBe("Z");
    }

    [Fact]
    public void ToString_Should_Return_Space_For_Default_Cell()
    {
        var cell = new ScreenCell();
        cell.ToString().ShouldBe(" ");
    }

    // ── CellColor Factory Methods ────────────────────────────────────

    [Theory]
    [InlineData(30)]
    [InlineData(37)]
    [InlineData(90)]
    [InlineData(97)]
    [InlineData(40)]
    [InlineData(47)]
    [InlineData(100)]
    [InlineData(107)]
    public void FromLegacy_Should_Set_Legacy_Mode_And_Index(int sgrCode)
    {
        var color = CellColor.FromLegacy(sgrCode);
        color.Mode.ShouldBe(ColorMode.Legacy);
        color.Index.ShouldBe(sgrCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(128)]
    [InlineData(255)]
    public void FromEightBit_Should_Set_EightBit_Mode_And_Index(int index)
    {
        var color = CellColor.FromEightBit(index);
        color.Mode.ShouldBe(ColorMode.EightBit);
        color.Index.ShouldBe(index);
    }

    [Fact]
    public void FromRgb_Should_Set_TrueColor_Mode_And_Components()
    {
        var color = CellColor.FromRgb(100, 150, 200);
        color.Mode.ShouldBe(ColorMode.TrueColor);
        color.R.ShouldBe((byte)100);
        color.G.ShouldBe((byte)150);
        color.B.ShouldBe((byte)200);
    }

    [Fact]
    public void FromRgb_Boundary_Values_Should_Work()
    {
        var min = CellColor.FromRgb(0, 0, 0);
        min.R.ShouldBe((byte)0);
        min.G.ShouldBe((byte)0);
        min.B.ShouldBe((byte)0);

        var max = CellColor.FromRgb(255, 255, 255);
        max.R.ShouldBe((byte)255);
        max.G.ShouldBe((byte)255);
        max.B.ShouldBe((byte)255);
    }

    // ── CellColor Equality ───────────────────────────────────────────

    [Fact]
    public void CellColor_Should_Be_Equal_When_Same_Values()
    {
        var a = CellColor.FromLegacy(31);
        var b = CellColor.FromLegacy(31);
        a.ShouldBe(b);
    }

    [Fact]
    public void CellColor_Should_Not_Be_Equal_When_Different_Values()
    {
        var a = CellColor.FromLegacy(31);
        var b = CellColor.FromLegacy(32);
        a.ShouldNotBe(b);
    }

    [Fact]
    public void CellColor_Should_Not_Be_Equal_Across_Modes()
    {
        var legacy = CellColor.FromLegacy(5);
        var eightBit = CellColor.FromEightBit(5);
        legacy.ShouldNotBe(eightBit);
    }

    [Fact]
    public void CellColor_Rgb_Should_Be_Equal_When_Same_Components()
    {
        var a = CellColor.FromRgb(10, 20, 30);
        var b = CellColor.FromRgb(10, 20, 30);
        a.ShouldBe(b);
    }

    // ── CellDecoration Flags ─────────────────────────────────────────

    [Fact]
    public void CellDecoration_None_Should_Have_No_Flags()
    {
        var d = CellDecoration.None;
        d.HasFlag(CellDecoration.Bold).ShouldBeFalse();
        d.HasFlag(CellDecoration.Italic).ShouldBeFalse();
    }

    [Fact]
    public void CellDecoration_Should_Combine_Multiple_Flags()
    {
        var d = CellDecoration.Bold | CellDecoration.Dim | CellDecoration.Underline;
        d.HasFlag(CellDecoration.Bold).ShouldBeTrue();
        d.HasFlag(CellDecoration.Dim).ShouldBeTrue();
        d.HasFlag(CellDecoration.Underline).ShouldBeTrue();
        d.HasFlag(CellDecoration.Italic).ShouldBeFalse();
    }

    [Fact]
    public void CellDecoration_All_Flags_Should_Be_Distinct()
    {
        var all = new[]
        {
            CellDecoration.Bold,
            CellDecoration.Dim,
            CellDecoration.Italic,
            CellDecoration.Underline,
            CellDecoration.SlowBlink,
            CellDecoration.RapidBlink,
            CellDecoration.Reverse,
            CellDecoration.Conceal,
            CellDecoration.Strikethrough,
        };

        // Each flag should be a unique power of 2
        for (var i = 0; i < all.Length; i++)
        {
            for (var j = i + 1; j < all.Length; j++)
            {
                (all[i] & all[j]).ShouldBe(CellDecoration.None,
                    $"{all[i]} and {all[j]} should be distinct flags");
            }
        }
    }
}
