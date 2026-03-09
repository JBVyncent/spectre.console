namespace Spectre.Console.Ansi.Tests.Properties;

public sealed class StyleProperties
{
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
        var s = MakeStyle(rfg, gfg, bfg, rbg, gbg, bbg, decoBits);
        return Style.Plain.Combine(s).Equals(s);
    }

    [Property]
    public bool Plain_IsRightIdentity(byte rfg, byte gfg, byte bfg, byte rbg, byte gbg, byte bbg, int decoBits)
    {
        var s = MakeStyle(rfg, gfg, bfg, rbg, gbg, bbg, decoBits);
        return s.Combine(Style.Plain).Equals(s);
    }

    [Property]
    public bool Combine_NonDefaultForegroundWins(
        byte rfg1, byte gfg1, byte bfg1,
        byte rfg2, byte gfg2, byte bfg2)
    {
        var left = new Style(new Color(rfg1, gfg1, bfg1));
        var right = new Style(new Color(rfg2, gfg2, bfg2));
        return left.Combine(right).Foreground == right.Foreground;
    }

    [Property]
    public bool Combine_DecorationIsBitwiseOr(int decoA, int decoB)
    {
        var dA = (Decoration)(decoA & 0x1FF);
        var dB = (Decoration)(decoB & 0x1FF);
        var a = new Style(null, null, dA);
        var b = new Style(null, null, dB);
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
        return a.Combine(b).Combine(c).Decoration == a.Combine(b.Combine(c)).Decoration;
    }

    [Property]
    public bool Combine_DecorationIsCommutative(int decoA, int decoB)
    {
        var dA = (Decoration)(decoA & 0x1FF);
        var dB = (Decoration)(decoB & 0x1FF);
        var a = new Style(null, null, dA);
        var b = new Style(null, null, dB);
        return a.Combine(b).Decoration == b.Combine(a).Decoration;
    }

    [Fact]
    public void Plain_HasDefaultColors()
    {
        Style.Plain.Foreground.Should().Be(Color.Default);
        Style.Plain.Background.Should().Be(Color.Default);
        Style.Plain.Decoration.Should().Be(Decoration.None);
    }
}
