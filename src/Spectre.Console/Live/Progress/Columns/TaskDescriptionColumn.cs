namespace Spectre.Console;

/// <summary>
/// A column showing the task description.
/// </summary>
public sealed class TaskDescriptionColumn : ProgressColumn
{
    /// <inheritdoc/>
    protected internal override bool NoWrap => true;

    /// <summary>
    /// Gets or sets the alignment of the task description.
    /// </summary>
    public Justify Alignment { get; set; } = Justify.Right;

    /// <inheritdoc/>
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var indent = task.IndentLevel > 0 ? new string(' ', task.IndentLevel * 2) : string.Empty;
        var text = task.Description?.RemoveNewLines()?.Trim();
        return new Markup(indent + (text ?? string.Empty)).Overflow(Overflow.Ellipsis).Justify(Alignment);
    }
}