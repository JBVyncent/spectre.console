namespace Spectre.Console.Tests.Properties;

public sealed class StyleProperties
{
    // Helper: build a Style from raw bytes + decoration bits.
    private static Style MakeStyle(byte rfg, byte gfg, byte bfg, byte rbg, byte gbg, byte bbg, int decoBits)
    {
        var fg = new Color(rfg, gfg, bfg);
        var bg = new Color(rbg, gbg, bbg);
        var deco = (Decoration)(decoBits & 0x1FF);
        return new Style(fg, bg, deco);
    }

    [Property]
    public bool Equality_IsReflexive(byte rfg, byte gfg, byte bfg, byte rbg, byte gbg, byte bbg, int decoBits)
    {
        var style = MakeStyle(rfg, gfg, bfg, rbg, gbg, bbg, decoBits);
        return style.Equals(style);
    }

    [Property]
    public bool Plain_IsLeftIdentity(byte rfg, byte gfg, byte bfg, byte rbg, byte gbg, byte bbg, int decoBits)
    {
        // Style.Plain.Combine(s) == s for any s.
        var s = MakeStyle(rfg, gfg, bfg, rbg, gbg, bbg, decoBits);
        return Style.Plain.Combine(s).Equals(s);
    }

    [Property]
    public bool Plain_IsRightIdentity(byte rfg, byte gfg, byte bfg, byte rbg, byte gbg, byte bbg, int decoBits)
    {
        // s.Combine(Style.Plain) == s for any s.
        var s = MakeStyle(rfg, gfg, bfg, rbg, gbg, bbg, decoBits);
        return s.Combine(Style.Plain).Equals(s);
    }

    [Property]
    public bool Combine_NonDefaultForegroundWins(
        byte rfg1, byte gfg1, byte bfg1,
        byte rfg2, byte gfg2, byte bfg2)
    {
        // When the right-hand style has a non-default foreground, it wins.
        var left = new Style(new Color(rfg1, gfg1, bfg1));
        var right = new Style(new Color(rfg2, gfg2, bfg2));
        return left.Combine(right).Foreground == right.Foreground;
    }

    [Property]
    public bool Combine_NonDefaultBackgroundWins(
        byte rbg1, byte gbg1, byte bbg1,
        byte rbg2, byte gbg2, byte bbg2)
    {
        var left = new Style(null, new Color(rbg1, gbg1, bbg1));
        var right = new Style(null, new Color(rbg2, gbg2, bbg2));
        return left.Combine(right).Background == right.Background;
    }

    [Property]
    public bool Combine_DecorationIsBitwiseOr(byte rfg, byte gfg, byte bfg, int decoA, int decoB)
    {
        var dA = (Decoration)(decoA & 0x1FF);
        var dB = (Decoration)(decoB & 0x1FF);
        var a = new Style(new Color(rfg, gfg, bfg), null, dA);
        var b = new Style(new Color(rfg, gfg, bfg), null, dB);
        return a.Combine(b).Decoration == (dA | dB);
    }

    [Property]
    public bool Combine_DecorationIsAssociative(int decoA, int decoB, int decoC)
    {
        var dA = (Decoration)(decoA & 0x1FF);
        var dB = (Decoration)(decoB & 0x1FF);
        var dC = (Decoration)(decoC & 0x1FF);
        var a = new Style(null, null, dA);
        var b = new Style(null, null, dB);
        var c = new Style(null, null, dC);
        var lhs = a.Combine(b).Combine(c).Decoration;
        var rhs = a.Combine(b.Combine(c)).Decoration;
        return lhs == rhs;
    }

    [Property]
    public bool HashCode_IsConsistentWithEquality(
        byte rfg, byte gfg, byte bfg, byte rbg, byte gbg, byte bbg, int decoBits)
    {
        var a = MakeStyle(rfg, gfg, bfg, rbg, gbg, bbg, decoBits);
        var b = MakeStyle(rfg, gfg, bfg, rbg, gbg, bbg, decoBits);
        if (!a.Equals(b)) return true;
        return a.GetHashCode() == b.GetHashCode();
    }

    [Fact]
    public void Plain_HasDefaultColors()
    {
        Style.Plain.Foreground.Should().Be(Color.Default);
        Style.Plain.Background.Should().Be(Color.Default);
        Style.Plain.Decoration.Should().Be(Decoration.None);
    }

    [Fact]
    public void Combine_WithNullColorDefault_PreservesLeft()
    {
        // When right has default foreground, left wins.
        var left = new Style(Color.Red);
        var right = new Style(null, null, Decoration.Bold);
        left.Combine(right).Foreground.Should().Be(Color.Red);
    }
}
