namespace Spectre.Console;

internal sealed class BypassSpinner : Spinner
{
    public override TimeSpan Interval => TimeSpan.FromMilliseconds(80);
    public override bool IsUnicode => false;
    public override IReadOnlyList<string> Frames => new[] { "-", "\\" };
}