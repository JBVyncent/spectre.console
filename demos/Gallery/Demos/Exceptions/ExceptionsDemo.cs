using Spectre.Console;

namespace Gallery.Demos.Exceptions;

public sealed class ExceptionsDemo : IDemoModule
{
    public string Name => "Exceptions";
    public string Description => "Rich exception rendering with syntax highlighting";

    public void Run()
    {
        AnsiConsole.MarkupLine("[bold underline blue]Exception Rendering[/]");
        AnsiConsole.MarkupLine("[grey]Exceptions displayed with syntax highlighting and formatting.[/]");
        AnsiConsole.WriteLine();

        // Basic exception
        try
        {
            ThrowNestedException();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
        }

        AnsiConsole.WriteLine();

        // Exception with custom formatting
        AnsiConsole.MarkupLine("[bold underline blue]Custom Exception Formatting[/]");
        AnsiConsole.WriteLine();

        try
        {
            ThrowDetailedException();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, new ExceptionSettings
            {
                Format = ExceptionFormats.ShortenPaths
                    | ExceptionFormats.ShortenTypes
                    | ExceptionFormats.ShowLinks,
                Style = new ExceptionStyle
                {
                    Exception = new Style(Color.Red),
                    Message = new Style(Color.White),
                    Method = new Style(Color.Yellow),
                    Path = new Style(Color.Grey),
                    LineNumber = new Style(Color.Cyan1),
                },
            });
        }

        AnsiConsole.WriteLine();

        // Note about Bug #11 fix:
        // ExceptionFormatter no longer throws NullReferenceException
        // when running in PublishTrimmed/AOT scenarios where
        // reflection metadata may be stripped.
        AnsiConsole.MarkupLine("[grey]Note: Exception formatting is safe under PublishTrimmed/AOT.[/]");
    }

    private static void ThrowNestedException()
    {
        try
        {
            try
            {
                var dict = new Dictionary<string, int>();
                _ = dict["missing_key"];
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to process data", ex);
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Operation failed", ex);
        }
    }

    private static void ThrowDetailedException()
    {
        try
        {
            int[] numbers = { 1, 2, 3 };
            _ = numbers[10];
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Array access out of bounds while processing batch", ex);
        }
    }
}
