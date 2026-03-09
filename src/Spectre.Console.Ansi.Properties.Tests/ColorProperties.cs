namespace Spectre.Console.Ansi.Tests.Properties;

public sealed class ColorProperties
{
    [Property]
    public bool Equality_IsReflexive(byte r, byte g, byte b)
    {
        var a = new Color(r, g, b);
        var b2 = new Color(r, g, b);
        return a.Equals(b2);
    }

    [Property]
    public bool Equality_IsSymmetric(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var a = new Color(r1, g1, b1);
        var b = new Color(r2, g2, b2);
        return (a == b) == (b == a);
    }

    [Property]
    public bool Components_ArePreserved(byte r, byte g, byte b)
    {
        var color = new Color(r, g, b);
        return color.R == r && color.G == g && color.B == b;
    }

    [Property]
    public bool HashCode_IsConsistentWithEquality(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var a = new Color(r1, g1, b1);
        var b = new Color(r2, g2, b2);
        if (a != b) return true;
        return a.GetHashCode() == b.GetHashCode();
    }

    [Property]
    public bool HexRoundTrip(byte r, byte g, byte b)
    {
        var original = new Color(r, g, b);
        var parsed = Color.FromHex(original.ToHex());
        return original == parsed;
    }

    [Property]
    public bool ToHex_ProducesSixUppercaseHexDigits(byte r, byte g, byte b)
    {
        var hex = new Color(r, g, b).ToHex();
        return hex.Length == 6 && hex.All(c => "0123456789ABCDEF".Contains(c));
    }

    [Property]
    public bool BlendAtZero_ReturnsOriginalColor(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var a = new Color(r1, g1, b1);
        var b = new Color(r2, g2, b2);
        return a.Blend(b, 0f) == a;
    }

    [Property]
    public bool BlendAtOne_ReturnsOtherColor(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var a = new Color(r1, g1, b1);
        var other = new Color(r2, g2, b2);
        return a.Blend(other, 1f) == other;
    }

    [Property]
    public bool BlendComponents_AreWithinBounds(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var a = new Color(r1, g1, b1);
        var b = new Color(r2, g2, b2);
        var blended = a.Blend(b, 0.5f);
        // Blended components should be between the two colors (+1 for float rounding tolerance).
        var rOk = blended.R >= Math.Min(r1, r2) && blended.R <= Math.Max(r1, r2) + 1;
        var gOk = blended.G >= Math.Min(g1, g2) && blended.G <= Math.Max(g1, g2) + 1;
        var bOk = blended.B >= Math.Min(b1, b2) && blended.B <= Math.Max(b1, b2) + 1;
        return rOk && gOk && bOk;
    }

    [Property]
    public bool ConstructedColor_IsDistinctFromDefault(byte r, byte g, byte b)
    {
        return new Color(r, g, b) != Color.Default;
    }

    [Property]
    public bool TryFromHex_SucceedsForValidHex(byte r, byte g, byte b)
    {
        var hex = new Color(r, g, b).ToHex();
        var result = Color.TryFromHex(hex, out var parsed);
        return result && parsed == new Color(r, g, b);
    }

    [Fact]
    public void Default_EqualsItself()
    {
        Color.Default.Should().Be(Color.Default);
    }

    [Fact]
    public void ConstructedZero_IsNotSameAsDefault()
    {
        new Color(0, 0, 0).Should().NotBe(Color.Default);
    }

    [Fact]
    public void FromHex_SupportsHashPrefix()
    {
        Color.FromHex("#FF8000").Should().Be(Color.FromHex("FF8000"));
    }

    [Fact]
    public void FromHex_ExpandsThreeDigitCodes()
    {
        Color.FromHex("F8A").Should().Be(Color.FromHex("FF88AA"));
    }
}
