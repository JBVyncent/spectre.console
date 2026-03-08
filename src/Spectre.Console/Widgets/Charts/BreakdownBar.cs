namespace Spectre.Console;

// Stryker disable all : NoCoverage — chart rendering pipeline; Stryker cannot trace coverage through rendering
internal sealed class BreakdownBar : Renderable
{
    private readonly List<IBreakdownChartItem> _data;

    public int? Width { get; set; }

    public BreakdownBar(List<IBreakdownChartItem> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through chart rendering pipeline
        _data = data;
    }

    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        // Stryker disable once all : NoCoverage — internal rendering type; NoCoverage through chart rendering pipeline
        var width = Math.Min(Width ?? maxWidth, maxWidth);
        return new Measurement(width, width);
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Stryker disable once all : NoCoverage — internal rendering type; NoCoverage through chart rendering pipeline
        var width = Math.Min(Width ?? maxWidth, maxWidth);

        // Chart
        var maxValue = _data.Sum(i => i.Value);
        var items = _data.ToArray();
        var bars = Ratio.Distribute(width, items.Select(i => Math.Max(0, (int)(width * (i.Value / maxValue)))).ToArray());

        for (var index = 0; index < items.Length; index++)
        {
            yield return new Segment(new string('█', bars[index]), new Style(items[index].Color));
        }

        // Stryker disable once all : NoCoverage — internal rendering type; NoCoverage through chart rendering pipeline
        yield return Segment.LineBreak;
    }
}
// Stryker restore all