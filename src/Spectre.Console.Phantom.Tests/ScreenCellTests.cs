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
        cell.Character.Should().Be(' ');
        cell.Foreground.Should().BeNull();
        cell.Background.Should().BeNull();
        cell.Decoration.Should().Be(CellDecoration.None);
        cell.HyperlinkUrl.Should().BeNull();
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

        cell.Character.Should().Be(' ');
        cell.Foreground.Should().BeNull();
        cell.Background.Should().BeNull();
        cell.Decoration.Should().Be(CellDecoration.None);
        cell.HyperlinkUrl.Should().BeNull();
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
        target.Character.Should().Be('T');

        // Style properties should be copied
        target.Foreground!.Value.Mode.Should().Be(ColorMode.TrueColor);
        target.Foreground!.Value.R.Should().Be((byte)255);
        target.Background!.Value.Mode.Should().Be(ColorMode.EightBit);
        target.Background!.Value.Index.Should().Be(42);
        target.Decoration.HasFlag(CellDecoration.Underline).Should().BeTrue();
        target.Decoration.HasFlag(CellDecoration.Strikethrough).Should().BeTrue();
        target.HyperlinkUrl.Should().Be("https://link.test");
    }

    [Fact]
    public void CopyStyleFrom_Should_Throw_For_Null()
    {
        var cell = new ScreenCell();
        FluentActions.Invoking(() => cell.CopyStyleFrom(null!)).Should().Throw<ArgumentNullException>();
    }

    // ── ToString ─────────────────────────────────────────────────────

    [Fact]
    public void ToString_Should_Return_Character_As_String()
    {
        var cell = new ScreenCell { Character = 'Z' };
        cell.ToString().Should().Be("Z");
    }

    [Fact]
    public void ToString_Should_Return_Space_For_Default_Cell()
    {
        var cell = new ScreenCell();
        cell.ToString().Should().Be(" ");
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
        color.Mode.Should().Be(ColorMode.Legacy);
        color.Index.Should().Be(sgrCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(128)]
    [InlineData(255)]
    public void FromEightBit_Should_Set_EightBit_Mode_And_Index(int index)
    {
        var color = CellColor.FromEightBit(index);
        color.Mode.Should().Be(ColorMode.EightBit);
        color.Index.Should().Be(index);
    }

    [Fact]
    public void FromRgb_Should_Set_TrueColor_Mode_And_Components()
    {
        var color = CellColor.FromRgb(100, 150, 200);
        color.Mode.Should().Be(ColorMode.TrueColor);
        color.R.Should().Be((byte)100);
        color.G.Should().Be((byte)150);
        color.B.Should().Be((byte)200);
    }

    [Fact]
    public void FromRgb_Boundary_Values_Should_Work()
    {
        var min = CellColor.FromRgb(0, 0, 0);
        min.R.Should().Be((byte)0);
        min.G.Should().Be((byte)0);
        min.B.Should().Be((byte)0);

        var max = CellColor.FromRgb(255, 255, 255);
        max.R.Should().Be((byte)255);
        max.G.Should().Be((byte)255);
        max.B.Should().Be((byte)255);
    }

    // ── CellColor Equality ───────────────────────────────────────────

    [Fact]
    public void CellColor_Should_Be_Equal_When_Same_Values()
    {
        var a = CellColor.FromLegacy(31);
        var b = CellColor.FromLegacy(31);
        a.Should().Be(b);
    }

    [Fact]
    public void CellColor_Should_Not_Be_Equal_When_Different_Values()
    {
        var a = CellColor.FromLegacy(31);
        var b = CellColor.FromLegacy(32);
        a.Should().NotBe(b);
    }

    [Fact]
    public void CellColor_Should_Not_Be_Equal_Across_Modes()
    {
        var legacy = CellColor.FromLegacy(5);
        var eightBit = CellColor.FromEightBit(5);
        legacy.Should().NotBe(eightBit);
    }

    [Fact]
    public void CellColor_Rgb_Should_Be_Equal_When_Same_Components()
    {
        var a = CellColor.FromRgb(10, 20, 30);
        var b = CellColor.FromRgb(10, 20, 30);
        a.Should().Be(b);
    }

    // ── CellDecoration Flags ─────────────────────────────────────────

    [Fact]
    public void CellDecoration_None_Should_Have_No_Flags()
    {
        var d = CellDecoration.None;
        d.HasFlag(CellDecoration.Bold).Should().BeFalse();
        d.HasFlag(CellDecoration.Italic).Should().BeFalse();
    }

    [Fact]
    public void CellDecoration_Should_Combine_Multiple_Flags()
    {
        var d = CellDecoration.Bold | CellDecoration.Dim | CellDecoration.Underline;
        d.HasFlag(CellDecoration.Bold).Should().BeTrue();
        d.HasFlag(CellDecoration.Dim).Should().BeTrue();
        d.HasFlag(CellDecoration.Underline).Should().BeTrue();
        d.HasFlag(CellDecoration.Italic).Should().BeFalse();
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
                (all[i] & all[j]).Should().Be(CellDecoration.None,
                    $"{all[i]} and {all[j]} should be distinct flags");
            }
        }
    }
}
