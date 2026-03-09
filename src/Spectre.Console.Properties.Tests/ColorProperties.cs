namespace Spectre.Console.Tests.Properties;

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
    public bool Equality_IsTransitive(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var a = new Color(r1, g1, b1);
        var b = new Color(r1, g1, b1);
        var c = new Color(r2, g2, b2);
        if (a != c) return true;
        return b == c;
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
    public bool ToHex_ProducesSixHexDigits(byte r, byte g, byte b)
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
    public bool ConstructedColor_IsDistinctFromDefault(byte r, byte g, byte b)
    {
        // The public Color(r,g,b) constructor never produces a color that equals Color.Default.
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
        // Even (0,0,0) constructed via public ctor is distinct from Color.Default.
        new Color(0, 0, 0).Should().NotBe(Color.Default);
    }

    [Fact]
    public void TryFromHex_ReturnsFalseForInvalidHex()
    {
        Color.TryFromHex("ZZZZZZ", out _).Should().BeFalse();
    }

    [Fact]
    public void FromHex_SupportsHashPrefix()
    {
        Color.FromHex("#FF8000").Should().Be(Color.FromHex("FF8000"));
    }

    [Fact]
    public void FromHex_ExpandsShortHexCodes()
    {
        Color.FromHex("F8A").Should().Be(Color.FromHex("FF88AA"));
    }
}
