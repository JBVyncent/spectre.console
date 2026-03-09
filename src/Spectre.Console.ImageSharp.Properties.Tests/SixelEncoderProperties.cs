namespace Spectre.Console.ImageSharp.Tests.Properties;

public sealed class SixelEncoderProperties
{
    // Helper: create a 1×1 image with the given RGBA pixel
    private static Image<Rgba32> OneByOne(byte r, byte g, byte b, byte a = 255)
    {
        var img = new Image<Rgba32>(1, 1);
        img[0, 0] = new Rgba32(r, g, b, a);
        return img;
    }

    [Fact]
    public void Encode_StartsWithDcsIntro()
    {
        using var img = OneByOne(255, 0, 0);
        var result = SixelEncoder.Encode(img);
        result.Should().StartWith("\x1bP");
    }

    [Fact]
    public void Encode_EndsWithStringTerminator()
    {
        using var img = OneByOne(0, 255, 0);
        var result = SixelEncoder.Encode(img);
        result.Should().EndWith("\x1b\\");
    }

    [Fact]
    public void Encode_ReturnsNonEmptyString()
    {
        using var img = OneByOne(0, 0, 255);
        var result = SixelEncoder.Encode(img);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Encode_Throws_WhenMaxColorsIsOne()
    {
        using var img = OneByOne(128, 128, 128);
        var act = () => SixelEncoder.Encode(img, 1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("maxColors");
    }

    [Fact]
    public void Encode_Throws_WhenMaxColorsIsZero()
    {
        using var img = OneByOne(0, 0, 0);
        var act = () => SixelEncoder.Encode(img, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Encode_Throws_ForNullImage()
    {
        var act = () => SixelEncoder.Encode(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Property]
    public bool Encode_AlwaysStartsWithDcsIntroForOpaquePixel(byte r, byte g, byte b)
    {
        using var img = OneByOne(r, g, b, 255);
        var result = SixelEncoder.Encode(img);
        return result.StartsWith("\x1bP", StringComparison.Ordinal);
    }

    [Property]
    public bool Encode_AlwaysEndsWithStringTerminator(byte r, byte g, byte b)
    {
        using var img = OneByOne(r, g, b, 255);
        var result = SixelEncoder.Encode(img);
        return result.EndsWith("\x1b\\", StringComparison.Ordinal);
    }

    [Property]
    public bool Encode_TransparentImageHasSameFrameAsSolid(byte r, byte g, byte b)
    {
        // Fully transparent pixel — no palette entries, but DCS wrapper always present.
        using var transparent = new Image<Rgba32>(1, 1);
        transparent[0, 0] = new Rgba32(r, g, b, 0); // alpha = 0 → transparent
        var result = SixelEncoder.Encode(transparent);
        return result.StartsWith("\x1bP", StringComparison.Ordinal)
            && result.EndsWith("\x1b\\", StringComparison.Ordinal);
    }

    [Property]
    public bool Encode_LargerMaxColors_DoesNotFail(byte r, byte g, byte b, PositiveInt maxColors)
    {
        var colors = Math.Max(2, maxColors.Get % 257); // clamp to [2, 256]
        using var img = OneByOne(r, g, b, 255);
        var result = SixelEncoder.Encode(img, colors);
        return result.Length > 0;
    }

    [Fact]
    public void Encode_2x2SameColor_ProducesDeterministicOutput()
    {
        using var img1 = new Image<Rgba32>(2, 2);
        using var img2 = new Image<Rgba32>(2, 2);
        for (var x = 0; x < 2; x++)
        for (var y = 0; y < 2; y++)
        {
            img1[x, y] = new Rgba32(100, 150, 200, 255);
            img2[x, y] = new Rgba32(100, 150, 200, 255);
        }

        SixelEncoder.Encode(img1).Should().Be(SixelEncoder.Encode(img2));
    }
}
