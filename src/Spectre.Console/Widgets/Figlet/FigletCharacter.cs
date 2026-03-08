namespace Spectre.Console;

internal sealed class FigletCharacter
{
    public int Code { get; }
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<string> Lines { get; }

    public FigletCharacter(int code, IEnumerable<string> lines)
    {
        Code = code;
        ArgumentNullException.ThrowIfNull(lines);
        Lines = new List<string>(lines);

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