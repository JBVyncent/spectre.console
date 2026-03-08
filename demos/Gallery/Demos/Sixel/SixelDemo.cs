using Gallery.Demos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;

namespace Gallery.Demos.Sixel;

/// <summary>
/// Demonstrates Sixel image rendering (hardware pixel-perfect images in the terminal)
/// with automatic fallback to block-character rendering on terminals that don't support Sixel.
/// </summary>
public sealed class SixelDemo : IDemoModule
{
    public string Name => "Sixel";
    public string Description => "Pixel-perfect image rendering with Sixel (+ block-char fallback)";

    public void Run()
    {
        // ── Capability detection ────────────────────────────────────────────────

        var supportsSixel = AnsiConsole.Profile.Capabilities.SupportsSixel;

        AnsiConsole.Write(new Rule("[bold]Terminal Sixel Capability[/]").RuleStyle(Style.Parse("grey")));
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[grey]Capability[/]");
        table.AddColumn("[grey]Value[/]");
        table.AddRow("Sixel support detected",
            supportsSixel
                ? "[green bold]Yes — images will render as true pixels[/]"
                : "[yellow]No — falling back to block characters (▀ / ▄)[/]");
        table.AddRow("Max palette colors", $"{SixelEncoder.DefaultMaxColors} (default)");
        table.AddRow("How to override",
            "[grey]AnsiConsole.Profile.Capabilities.SupportsSixel = true[/]");
        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();

        // ── Build a small gradient image in memory ──────────────────────────────

        AnsiConsole.Write(new Rule("[bold]Gradient Image[/]").RuleStyle(Style.Parse("grey")));
        AnsiConsole.MarkupLine("[grey]A 32×16 gradient rendered via SixelImage.[/]");
        if (supportsSixel)
        {
            AnsiConsole.MarkupLine("[grey]Your terminal supports Sixel → rendered as true pixels.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Your terminal does not support Sixel → block-character fallback.[/]");
        }

        AnsiConsole.WriteLine();

        using var gradientImage = BuildGradientImage(32, 16);
        using var gradientStream = new MemoryStream();
        gradientImage.SaveAsPng(gradientStream);
        gradientStream.Position = 0;

        AnsiConsole.Write(
            new SixelImage(gradientStream)
                .MaxWidth(32));

        AnsiConsole.WriteLine();

        // ── Demonstrate MaxColors throttling ────────────────────────────────────

        AnsiConsole.Write(new Rule("[bold]Palette Size Demo[/]").RuleStyle(Style.Parse("grey")));
        AnsiConsole.MarkupLine(
            "[grey]Same gradient at 4 colors (harsh banding) vs 16 colors vs 256 colors.[/]");
        AnsiConsole.WriteLine();

        foreach (var maxColors in new[] { 4, 16, 256 })
        {
            AnsiConsole.MarkupLine($"  [cyan]MaxColors = {maxColors}[/]");

            using var stream = new MemoryStream();
            gradientImage.SaveAsPng(stream);
            stream.Position = 0;

            AnsiConsole.Write(
                new SixelImage(stream)
                    .MaxWidth(32)
                    .MaxColors(maxColors));

            AnsiConsole.WriteLine();
        }

        // ── Builder / fluent API tour ───────────────────────────────────────────

        AnsiConsole.Write(new Rule("[bold]Fluent API[/]").RuleStyle(Style.Parse("grey")));
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[grey]// Chain extension methods for terse configuration:[/]");
        AnsiConsole.MarkupLine("[cyan]new SixelImage(\"photo.png\")[/]");
        AnsiConsole.MarkupLine("[cyan]    .MaxWidth(80)[/]");
        AnsiConsole.MarkupLine("[cyan]    .MaxColors(128)[/]");
        AnsiConsole.MarkupLine("[cyan]    .UseResampler(KnownResamplers.Lanczos3);[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Sixel Demo complete[/]").RuleStyle(Style.Parse("green")));
    }

    // ── helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a horizontal red-to-blue gradient image, with a green-to-white vertical gradient.
    /// </summary>
    private static Image<Rgba32> BuildGradientImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var r = (byte)(255 * x / Math.Max(1, width - 1));
                var g = (byte)(200 * y / Math.Max(1, height - 1));
                var b = (byte)(255 - r);
                image[x, y] = new Rgba32(r, g, b, 255);
            }
        }

        return image;
    }
}
