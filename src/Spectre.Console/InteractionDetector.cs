namespace Spectre.Console;

internal static class InteractionDetector
{
    /// <summary>
    /// Environment variable that forces interactive mode when set to "1" or "true".
    /// Useful for ConPTY hosts, containers, and test harnesses where
    /// <see cref="System.Console.IsInputRedirected"/> reports false negatives.
    /// </summary>
    internal const string ForceInteractiveEnvVar = "SPECTRE_CONSOLE_FORCE_INTERACTIVE";

    public static bool IsInteractive(InteractionSupport interaction)
    {
        if (interaction == InteractionSupport.Yes)
        {
            return true;
        }

        if (interaction == InteractionSupport.No)
        {
            return false;
        }

        // InteractionSupport.Detect — check environment override first
        var envValue = Environment.GetEnvironmentVariable(ForceInteractiveEnvVar);
        if (envValue is "1" or "true" or "True" or "TRUE")
        {
            return true;
        }

        return !System.Console.IsInputRedirected;
    }
}