namespace Gallery.Demos;

/// <summary>
/// A self-contained demo that showcases a specific area of Spectre.Console.
/// Each module should demonstrate both standard usage and edge cases.
/// </summary>
public interface IDemoModule
{
    /// <summary>
    /// Short name shown in the gallery menu (e.g., "Tables", "Markup").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// One-line description of what this demo covers.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Run the demo. Implementations should be self-contained and
    /// write all output via AnsiConsole.
    /// </summary>
    void Run();
}
