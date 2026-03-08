namespace Spectre.Console;

internal static class ResourceReader
{
    public static string ReadManifestData(string resourceName)
    {
        // Stryker disable once all : Equivalent — internal method; always called with non-null assembly
        ArgumentNullException.ThrowIfNull(resourceName);

        var assembly = typeof(ResourceReader).Assembly;
        // Stryker disable once all : NoCoverage — string replacement for resource path; always called with valid resource names
        resourceName = resourceName.ReplaceExact("/", ".");

        // Stryker disable once all : NoCoverage — error path when resource not found; always called with valid resources
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Could not load manifest resource stream.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().NormalizeNewLines();
    }
}