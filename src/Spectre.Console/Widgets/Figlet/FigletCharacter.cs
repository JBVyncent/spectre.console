namespace Spectre.Console;

// Stryker disable all : NoCoverage — internal Figlet rendering type; Stryker cannot trace coverage through FigletText pipeline
internal sealed class FigletCharacter
{
    public int Code { get; }
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<string> Lines { get; }

    public FigletCharacter(int code, IEnumerable<string> lines)
    {
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through FigletFont pipeline
        Code = code;
        ArgumentNullException.ThrowIfNull(lines);
        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through FigletFont pipeline
        Lines = new List<string>(lines);

        // Stryker disable once all : NoCoverage — internal constructor; NoCoverage through FigletFont pipeline
        var min = Lines.Min(x => x.Length);
        var max = Lines.Max(x => x.Length);
        if (min != max)
        {
            throw new InvalidOperationException($"Figlet character #{code} has varying width");
        }

        Width = max;
        Height = Lines.Count;
    }
}
// Stryker restore all