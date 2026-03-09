namespace Spectre.Console;

// Stryker disable all : IsStandardOut/IsStandardError compare against Console.Out/Console.Error which are
// redirected by xUnit and other test runners — equality and boolean mutations are unobservable in standard
// test environments. Catch blocks protect against rare IO exceptions that cannot be triggered in unit tests.
internal static class TextWriterExtensions
{
    public static bool IsStandardOut(this TextWriter writer)
    {
        try
        {
            return writer == System.Console.Out;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsStandardError(this TextWriter writer)
    {
        try
        {
            return writer == System.Console.Error;
        }
        catch
        {
            return false;
        }
    }
}
// Stryker restore all