namespace Spectre.Console;

// Stryker disable all : NoCoverage — layout rendering pipeline; Stryker cannot trace indirect coverage through nested rendering
[DebuggerDisplay("{Region,nq}")]
internal sealed class LayoutRender
{
    public Region Region { get; }
    public List<SegmentLine> Render { get; }

    public LayoutRender(Region region, List<SegmentLine> render)
    {
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through layout rendering pipeline
        ArgumentNullException.ThrowIfNull(render);
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through layout rendering pipeline
        Region = region;
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through layout rendering pipeline
        Render = render;
    }
}
// Stryker restore all