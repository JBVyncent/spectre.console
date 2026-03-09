namespace Spectre.Console.Tests;

/// <summary>
/// Tests for the internal <see cref="SixelDetector"/> class.
/// Each test restores environment variables to their original values so the
/// suite remains hermetic regardless of the host's own terminal configuration.
/// </summary>
public sealed class SixelDetectorTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private sealed class EnvScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _original;

        public EnvScope(string name, string? value)
        {
            _name = name;
            _original = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose() =>
            Environment.SetEnvironmentVariable(_name, _original);
    }

    private static EnvScope SetEnv(string name, string? value) => new(name, value);

    // ── baseline ─────────────────────────────────────────────────────────────

    [Fact]
    public void Returns_False_When_No_Sixel_Vars_Are_Set()
    {
        using var mlterm = SetEnv("MLTERM", null);
        using var termProgram = SetEnv("TERM_PROGRAM", null);
        using var term = SetEnv("TERM", null);

        SixelDetector.Detect().Should().BeFalse();
    }

    // ── MLTERM variable ───────────────────────────────────────────────────────

    [Fact]
    public void Returns_True_When_MLTERM_Variable_Is_Set()
    {
        using var mlterm = SetEnv("MLTERM", "3.9.2");
        using var termProgram = SetEnv("TERM_PROGRAM", null);
        using var term = SetEnv("TERM", null);

        SixelDetector.Detect().Should().BeTrue();
    }

    [Fact]
    public void Returns_True_When_MLTERM_Variable_Is_Empty_String_Yields_False()
    {
        // Empty string == not set for our purposes.
        using var mlterm = SetEnv("MLTERM", string.Empty);
        using var termProgram = SetEnv("TERM_PROGRAM", null);
        using var term = SetEnv("TERM", null);

        SixelDetector.Detect().Should().BeFalse();
    }

    // ── TERM_PROGRAM variable ─────────────────────────────────────────────────

    [Theory]
    [InlineData("WezTerm")]
    [InlineData("mintty")]
    [InlineData("contour")]
    [InlineData("mlterm")]
    public void Returns_True_When_TERM_PROGRAM_Is_Known_Sixel_Terminal(string termProgram)
    {
        using var mlterm = SetEnv("MLTERM", null);
        using var tp = SetEnv("TERM_PROGRAM", termProgram);
        using var term = SetEnv("TERM", null);

        SixelDetector.Detect().Should().BeTrue();
    }

    [Theory]
    [InlineData("WEZTERM")]
    [InlineData("MINTTY")]
    [InlineData("Contour")]
    [InlineData("MLTERM")]
    public void Returns_True_When_TERM_PROGRAM_Comparison_Is_Case_Insensitive(string termProgram)
    {
        using var mlterm = SetEnv("MLTERM", null);
        using var tp = SetEnv("TERM_PROGRAM", termProgram);
        using var term = SetEnv("TERM", null);

        SixelDetector.Detect().Should().BeTrue();
    }

    [Theory]
    [InlineData("iTerm.app")]
    [InlineData("vscode")]
    [InlineData("Hyper")]
    [InlineData("xterm")]
    public void Returns_False_When_TERM_PROGRAM_Is_Not_Sixel_Terminal(string termProgram)
    {
        using var mlterm = SetEnv("MLTERM", null);
        using var tp = SetEnv("TERM_PROGRAM", termProgram);
        using var term = SetEnv("TERM", null);

        SixelDetector.Detect().Should().BeFalse();
    }

    // ── TERM variable ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("foot")]
    [InlineData("foot-extra")]
    public void Returns_True_When_TERM_Is_Foot_Variant(string termValue)
    {
        using var mlterm = SetEnv("MLTERM", null);
        using var termProgram = SetEnv("TERM_PROGRAM", null);
        using var term = SetEnv("TERM", termValue);

        SixelDetector.Detect().Should().BeTrue();
    }

    [Theory]
    [InlineData("FOOT")]
    [InlineData("FOOT-EXTRA")]
    public void Returns_True_When_TERM_Foot_Comparison_Is_Case_Insensitive(string termValue)
    {
        using var mlterm = SetEnv("MLTERM", null);
        using var termProgram = SetEnv("TERM_PROGRAM", null);
        using var term = SetEnv("TERM", termValue);

        SixelDetector.Detect().Should().BeTrue();
    }

    [Theory]
    [InlineData("mlterm")]
    [InlineData("mlterm-256color")]
    [InlineData("mlterm-something")]
    [InlineData("MLTERM")]
    [InlineData("MLTERM-256COLOR")]
    public void Returns_True_When_TERM_Starts_With_mlterm(string termValue)
    {
        using var mlterm = SetEnv("MLTERM", null);
        using var termProgram = SetEnv("TERM_PROGRAM", null);
        using var term = SetEnv("TERM", termValue);

        SixelDetector.Detect().Should().BeTrue();
    }

    [Theory]
    [InlineData("xterm")]
    [InlineData("xterm-256color")]
    [InlineData("screen")]
    [InlineData("tmux")]
    [InlineData("vte")]
    [InlineData("rxvt")]
    public void Returns_False_When_TERM_Is_Not_Sixel_Terminal(string termValue)
    {
        using var mlterm = SetEnv("MLTERM", null);
        using var termProgram = SetEnv("TERM_PROGRAM", null);
        using var term = SetEnv("TERM", termValue);

        SixelDetector.Detect().Should().BeFalse();
    }
}
