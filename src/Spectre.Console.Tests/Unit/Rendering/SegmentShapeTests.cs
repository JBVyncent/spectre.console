namespace Spectre.Console.Tests.Unit;

public sealed class SegmentShapeTests
{
    [Fact]
    public void Calculate_Should_Return_Zero_Size_When_No_Lines()
    {
        // Given
        var capabilities = new TestCapabilities();
        var options = new RenderOptions(capabilities, new Size(80, 24));
        var lines = new List<SegmentLine>();

        // When
        var shape = SegmentShape.Calculate(options, lines);

        // Then
        shape.Width.Should().Be(0);
        shape.Height.Should().Be(0);
    }
}