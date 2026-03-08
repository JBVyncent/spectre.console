namespace Spectre.Console.Tests.Unit;

public sealed class BypassSpinnerTests
{
    [Fact]
    public void Interval_Should_Be_80_Milliseconds()
    {
        var spinner = new BypassSpinner();
        spinner.Interval.ShouldBe(TimeSpan.FromMilliseconds(80));
    }

    [Fact]
    public void IsUnicode_Should_Be_False()
    {
        var spinner = new BypassSpinner();
        spinner.IsUnicode.ShouldBeFalse();
    }

    [Fact]
    public void Frames_Should_Have_Two_Entries()
    {
        var spinner = new BypassSpinner();
        spinner.Frames.Count.ShouldBe(2);
    }

    [Fact]
    public void First_Frame_Should_Be_Dash()
    {
        var spinner = new BypassSpinner();
        spinner.Frames[0].ShouldBe("-");
    }

    [Fact]
    public void Second_Frame_Should_Be_Backslash()
    {
        var spinner = new BypassSpinner();
        spinner.Frames[1].ShouldBe("\\");
    }
}

public sealed class SpinnerColumnTests
{
    private sealed class UnicodeSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => true;
        public override IReadOnlyList<string> Frames => new[] { "*", "+" };
    }

    private sealed class AsciiSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => false;
        public override IReadOnlyList<string> Frames => new[] { "X", "O" };
    }

    private static string Render(SpinnerColumn column, ProgressTask task, TimeSpan deltaTime, bool unicode = true)
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Unicode = unicode;
        var options = RenderOptions.Create(console, console.Profile.Capabilities);
        var output = new TestConsole();
        output.Write(column.Render(options, task, deltaTime));
        return output.Output;
    }

    private static RenderOptions CreateOptions(bool unicode = true)
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Unicode = unicode;
        return RenderOptions.Create(console, console.Profile.Capabilities);
    }

    [Fact]
    public void NoWrap_Should_Be_True()
    {
        var column = new SpinnerColumn();
        column.NoWrap.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Throws_When_Spinner_IsNull()
    {
        Should.Throw<ArgumentNullException>(() => new SpinnerColumn(null!));
    }

    // ── Render: not-started task ─────────────────────────────────────────────

    [Fact]
    public void Should_Render_Space_For_Not_Started_Task()
    {
        var column = new SpinnerColumn();
        var task = new ProgressTask(1, "Foo", 100, autoStart: false);

        var result = Render(column, task, TimeSpan.Zero);

        result.ShouldBe(" ");
    }

    [Fact]
    public void Should_Render_Custom_PendingText_For_Not_Started_Task()
    {
        var column = new SpinnerColumn { PendingText = "..." };
        var task = new ProgressTask(1, "Foo", 100, autoStart: false);

        var result = Render(column, task, TimeSpan.Zero);

        result.ShouldBe("...");
    }

    [Fact]
    public void Should_Apply_PendingStyle_When_Task_Not_Started()
    {
        var column = new SpinnerColumn { PendingText = "?", PendingStyle = Color.Red };
        var task = new ProgressTask(1, "Foo", 100, autoStart: false);

        var console = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        var options = RenderOptions.Create(console, console.Profile.Capabilities);
        var output = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        output.Write(column.Render(options, task, TimeSpan.Zero));

        // Color.Red (index 9) in EightBit mode produces ANSI codes → output is longer than bare text
        output.Output.Length.ShouldBeGreaterThan("?".Length);
    }

    [Fact]
    public void Should_Apply_PendingStyle_Color_Differently_Than_Plain()
    {
        // Kills L105 mutant: PendingStyle ?? Style.Plain → Style.Plain
        var task = new ProgressTask(1, "Foo", 100, autoStart: false);
        var console = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        var options = RenderOptions.Create(console, console.Profile.Capabilities);

        var columnWithColor = new SpinnerColumn { PendingText = "?", PendingStyle = Color.Red };
        var colorOutput = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        colorOutput.Write(columnWithColor.Render(options, task, TimeSpan.Zero));

        var columnPlain = new SpinnerColumn { PendingText = "?", PendingStyle = Style.Plain };
        var plainOutput = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        plainOutput.Write(columnPlain.Render(options, task, TimeSpan.Zero));

        colorOutput.Output.ShouldNotBe(plainOutput.Output);
    }

    // ── Render: finished task ────────────────────────────────────────────────

    [Fact]
    public void Should_Render_Space_For_Finished_Task()
    {
        var column = new SpinnerColumn();
        var task = new ProgressTask(1, "Foo", 100);
        task.Value = 100;

        var result = Render(column, task, TimeSpan.Zero);

        result.ShouldBe(" ");
    }

    [Fact]
    public void Should_Render_Custom_CompletedText_For_Finished_Task()
    {
        var column = new SpinnerColumn { CompletedText = "✓" };
        var task = new ProgressTask(1, "Foo", 100);
        task.Value = 100;

        var result = Render(column, task, TimeSpan.Zero);

        result.ShouldBe("✓");
    }

    [Fact]
    public void Should_Apply_CompletedStyle_When_Task_Finished()
    {
        var column = new SpinnerColumn { CompletedText = "done", CompletedStyle = Color.Green };
        var task = new ProgressTask(1, "Foo", 100);
        task.Value = 100;

        var console = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        var options = RenderOptions.Create(console, console.Profile.Capabilities);
        var output = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        output.Write(column.Render(options, task, TimeSpan.Zero));

        // Color.Green in EightBit mode produces ANSI codes → output is longer than bare text
        output.Output.Length.ShouldBeGreaterThan("done".Length);
    }

    [Fact]
    public void Should_Apply_CompletedStyle_Color_Differently_Than_Plain()
    {
        // Kills L110 mutant: CompletedStyle ?? Style.Plain → Style.Plain
        var task = new ProgressTask(1, "Foo", 100);
        task.Value = 100;
        var console = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        var options = RenderOptions.Create(console, console.Profile.Capabilities);

        var columnWithColor = new SpinnerColumn { CompletedText = "done", CompletedStyle = Color.Green };
        var colorOutput = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        colorOutput.Write(columnWithColor.Render(options, task, TimeSpan.Zero));

        var columnPlain = new SpinnerColumn { CompletedText = "done", CompletedStyle = Style.Plain };
        var plainOutput = new TestConsole().EmitAnsiSequences().Colors(ColorSystem.EightBit);
        plainOutput.Write(columnPlain.Render(options, task, TimeSpan.Zero));

        colorOutput.Output.ShouldNotBe(plainOutput.Output);
    }

    [Fact]
    public void IsFinished_Via_StopTask_Should_Render_CompletedText()
    {
        var column = new SpinnerColumn { CompletedText = "DONE" };
        var task = new ProgressTask(1, "Foo", 100);
        task.StopTask();

        var result = Render(column, task, TimeSpan.Zero);

        result.ShouldBe("DONE");
    }

    // ── Render: active task / frame advancement ──────────────────────────────

    [Fact]
    public void Should_Render_First_Frame_For_Active_Task_With_Zero_Delta()
    {
        var column = new SpinnerColumn();
        var task = new ProgressTask(1, "Foo", 100);

        var result = Render(column, task, TimeSpan.Zero);

        result.ShouldBe("-");
    }

    [Fact]
    public void Should_Advance_To_Second_Frame_When_Interval_Elapsed()
    {
        var column = new SpinnerColumn();
        var task = new ProgressTask(1, "Foo", 100);

        // First render advances to frame index 1 when delta >= 80ms
        var result = Render(column, task, TimeSpan.FromMilliseconds(80));

        result.ShouldBe("\\");
    }

    [Fact]
    public void Should_Not_Advance_Frame_When_Interval_Not_Yet_Elapsed()
    {
        var column = new SpinnerColumn();
        var task = new ProgressTask(1, "Foo", 100);

        var result = Render(column, task, TimeSpan.FromMilliseconds(79));

        result.ShouldBe("-");
    }

    [Fact]
    public void Accumulated_Time_Should_Persist_Between_Renders_And_Advance_Frame()
    {
        var column = new SpinnerColumn();
        var task = new ProgressTask(1, "Foo", 100);
        var options = CreateOptions();

        // First render: accumulated = 40ms, not enough → frame "-"
        var console1 = new TestConsole();
        console1.Write(column.Render(options, task, TimeSpan.FromMilliseconds(40)));
        console1.Output.ShouldBe("-");

        // Second render: accumulated = 80ms, interval reached → advance to frame "\"
        var console2 = new TestConsole();
        console2.Write(column.Render(options, task, TimeSpan.FromMilliseconds(40)));
        console2.Output.ShouldBe("\\");
    }

    [Fact]
    public void Frame_Index_Should_Wrap_Around()
    {
        // BypassSpinner has 2 frames: "-" and "\"; after index 1 it should wrap to "-"
        var column = new SpinnerColumn();
        var task = new ProgressTask(1, "Foo", 100);
        var options = CreateOptions();

        // Advance to frame 1 ("\"): delta = 80ms
        var c1 = new TestConsole();
        c1.Write(column.Render(options, task, TimeSpan.FromMilliseconds(80)));
        c1.Output.ShouldBe("\\");

        // Advance to frame 2 → 2%2 = 0 ("-"): delta = 80ms again
        var c2 = new TestConsole();
        c2.Write(column.Render(options, task, TimeSpan.FromMilliseconds(80)));
        c2.Output.ShouldBe("-");
    }

    [Fact]
    public void Accumulated_Should_Reset_After_Interval_Elapses()
    {
        // After the interval elapses, accumulated resets to 0
        // so a subsequent small delta should NOT advance the frame again
        var column = new SpinnerColumn();
        var task = new ProgressTask(1, "Foo", 100);
        var options = CreateOptions();

        // First render advances index to 1 (accumulated resets to 0)
        var c1 = new TestConsole();
        c1.Write(column.Render(options, task, TimeSpan.FromMilliseconds(80)));
        c1.Output.ShouldBe("\\");

        // Second render with small delta: accumulated = 10, not enough → stays at index 1
        var c2 = new TestConsole();
        c2.Write(column.Render(options, task, TimeSpan.FromMilliseconds(10)));
        c2.Output.ShouldBe("\\");
    }

    // ── ASCII fallback ────────────────────────────────────────────────────────

    [Fact]
    public void Should_Use_Unicode_Spinner_When_Console_Supports_Unicode()
    {
        var column = new SpinnerColumn(new UnicodeSpinner());
        var task = new ProgressTask(1, "Foo", 100);

        // Unicode=true → useAscii=false → uses UnicodeSpinner → frame "*"
        var result = Render(column, task, TimeSpan.Zero, unicode: true);

        result.ShouldBe("*");
    }

    [Fact]
    public void Should_Fall_Back_To_BypassSpinner_When_Console_Does_Not_Support_Unicode()
    {
        var column = new SpinnerColumn(new UnicodeSpinner());
        var task = new ProgressTask(1, "Foo", 100);

        // Unicode=false, IsUnicode=true → useAscii=true → uses BypassSpinner → frame "-"
        var result = Render(column, task, TimeSpan.Zero, unicode: false);

        result.ShouldBe("-");
    }

    [Fact]
    public void Should_Use_Ascii_Spinner_When_Console_Does_Not_Support_Unicode()
    {
        var column = new SpinnerColumn(new AsciiSpinner());
        var task = new ProgressTask(1, "Foo", 100);

        // Unicode=false, IsUnicode=false → useAscii=false → uses AsciiSpinner → frame "X"
        var result = Render(column, task, TimeSpan.Zero, unicode: false);

        result.ShouldBe("X");
    }

    // ── Spinner property setter ───────────────────────────────────────────────

    [Fact]
    public void Setting_Spinner_To_Null_Should_Use_BypassSpinner()
    {
        var column = new SpinnerColumn(new UnicodeSpinner());
        column.Spinner = null!; // setter falls back to BypassSpinner

        var task = new ProgressTask(1, "Foo", 100);
        var result = Render(column, task, TimeSpan.Zero, unicode: true);

        result.ShouldBe("-"); // BypassSpinner first frame
    }

    [Fact]
    public void Setting_Spinner_Should_Reset_MaxWidth()
    {
        var column = new SpinnerColumn();
        var options = CreateOptions();

        // Warm up the cached width with a short-framed spinner
        var initialWidth = column.GetColumnWidth(options)!.Value;

        // Now assign a spinner with much wider frames
        column.Spinner = new WideFrameSpinner();

        var newWidth = column.GetColumnWidth(options)!.Value;
        newWidth.ShouldBeGreaterThan(initialWidth);
    }

    // ── PendingText / CompletedText reset maxWidth ────────────────────────────

    [Fact]
    public void Setting_CompletedText_Should_Reset_MaxWidth()
    {
        var column = new SpinnerColumn();
        var options = CreateOptions();

        var initialWidth = column.GetColumnWidth(options)!.Value;

        column.CompletedText = "VERY WIDE COMPLETED TEXT";
        var newWidth = column.GetColumnWidth(options)!.Value;

        newWidth.ShouldBeGreaterThan(initialWidth);
    }

    [Fact]
    public void Setting_PendingText_Should_Reset_MaxWidth()
    {
        var column = new SpinnerColumn();
        var options = CreateOptions();

        var initialWidth = column.GetColumnWidth(options)!.Value;

        column.PendingText = "VERY WIDE PENDING TEXT";
        var newWidth = column.GetColumnWidth(options)!.Value;

        newWidth.ShouldBeGreaterThan(initialWidth);
    }

    // ── GetColumnWidth ────────────────────────────────────────────────────────

    [Fact]
    public void GetColumnWidth_Should_Return_NonNull_Value()
    {
        var column = new SpinnerColumn();
        var width = column.GetColumnWidth(CreateOptions());
        width.ShouldNotBeNull();
    }

    [Fact]
    public void GetColumnWidth_Should_Return_Positive_Value()
    {
        var column = new SpinnerColumn();
        var width = column.GetColumnWidth(CreateOptions());
        width!.Value.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetColumnWidth_Should_Return_Max_Of_Pending_Completed_And_Frames()
    {
        var column = new SpinnerColumn
        {
            PendingText = "PEND",    // 4 chars
            CompletedText = "COMP",  // 4 chars
        };
        var width = column.GetColumnWidth(CreateOptions());
        width!.Value.ShouldBe(4);
    }

    [Fact]
    public void GetColumnWidth_Should_Return_Width_Of_Ascii_Frames_When_No_Unicode()
    {
        var column = new SpinnerColumn(new UnicodeSpinner());
        var width = column.GetColumnWidth(CreateOptions(unicode: false));
        // Falls back to BypassSpinner whose frames are 1 char each
        width!.Value.ShouldBe(1);
    }

    [Fact]
    public void GetColumnWidth_NonUnicode_Ascii_Spinner_On_NonUnicode_Console_Uses_Spinner_Width()
    {
        // Kills L137 [&&→||] mutant: wide ASCII spinner on non-unicode console should NOT fall back to BypassSpinner
        // Original: useAscii = !false && false = false → uses WideAsciiSpinner (width=4)
        // Mutant ||: useAscii = !false || false = true → uses BypassSpinner (width=1)
        var column = new SpinnerColumn(new WideAsciiSpinner());
        var width = column.GetColumnWidth(CreateOptions(unicode: false));
        width!.Value.ShouldBe(4);
    }

    [Fact]
    public void GetColumnWidth_Wide_Unicode_Spinner_On_Unicode_Console_Uses_Spinner_Width()
    {
        // Kills L137 [LogicalNot] mutant: wide unicode spinner on unicode console should NOT fall back to BypassSpinner
        // Original: useAscii = !true && true = false → uses WideUnicodeSpinner (width=4)
        // Mutant (no !): useAscii = true && true = true → uses BypassSpinner (width=1)
        var column = new SpinnerColumn(new WideUnicodeSpinner());
        var width = column.GetColumnWidth(CreateOptions(unicode: true));
        width!.Value.ShouldBe(4);
    }

    [Fact]
    public void GetColumnWidth_Wide_Unicode_Spinner_On_NonUnicode_Console_Falls_Back_To_BypassSpinner()
    {
        // Kills L138 [Conditional false] mutant: unicode spinner on non-unicode console MUST fall back to BypassSpinner
        // Original: useAscii=true → new BypassSpinner() → width=1
        // Mutant (always use _spinner): uses WideUnicodeSpinner → width=4
        var column = new SpinnerColumn(new WideUnicodeSpinner());
        var width = column.GetColumnWidth(CreateOptions(unicode: false));
        width!.Value.ShouldBe(1);
    }

    [Fact]
    public void GetColumnWidth_Returns_Max_Frame_Width_Not_Min()
    {
        // Kills L144 [Linq Max→Min] mutant: must use Max, not Min, across frame widths
        // MixedWidthSpinner has frames ["-" (1 char), "WIDE" (4 chars)]
        // Original: Max(1, 4) = 4
        // Mutant: Min(1, 4) = 1
        var column = new SpinnerColumn(new MixedWidthSpinner());
        var width = column.GetColumnWidth(CreateOptions(unicode: false));
        width!.Value.ShouldBe(4);
    }

    // ── Style ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Style_Defaults_To_Yellow()
    {
        var column = new SpinnerColumn();
        column.Style.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void Setting_Style_To_Null_Should_Still_Render()
    {
        var column = new SpinnerColumn { Style = null };
        var task = new ProgressTask(1, "Foo", 100);

        Should.NotThrow(() => Render(column, task, TimeSpan.Zero));
    }

    // ── SpinnerColumnExtensions ───────────────────────────────────────────────

    [Fact]
    public void Style_Extension_Returns_Same_Column_Instance()
    {
        var column = new SpinnerColumn();
        var result = column.Style(Color.Red);
        result.ShouldBeSameAs(column);
    }

    [Fact]
    public void Style_Extension_Sets_Style()
    {
        var column = new SpinnerColumn();
        column.Style(Color.Red);
        column.Style.ShouldBe(Color.Red);
    }

    [Fact]
    public void Style_Extension_Throws_When_Column_IsNull()
    {
        Should.Throw<ArgumentNullException>(() => ((SpinnerColumn)null!).Style(Color.Red));
    }

    [Fact]
    public void CompletedText_Extension_Returns_Same_Column_Instance()
    {
        var column = new SpinnerColumn();
        var result = column.CompletedText("done");
        result.ShouldBeSameAs(column);
    }

    [Fact]
    public void CompletedText_Extension_Sets_CompletedText()
    {
        var column = new SpinnerColumn();
        column.CompletedText("done");
        column.CompletedText.ShouldBe("done");
    }

    [Fact]
    public void CompletedText_Extension_Throws_When_Column_IsNull()
    {
        Should.Throw<ArgumentNullException>(() => ((SpinnerColumn)null!).CompletedText("done"));
    }

    [Fact]
    public void CompletedStyle_Extension_Returns_Same_Column_Instance()
    {
        var column = new SpinnerColumn();
        var result = column.CompletedStyle(Color.Green);
        result.ShouldBeSameAs(column);
    }

    [Fact]
    public void CompletedStyle_Extension_Sets_CompletedStyle()
    {
        var column = new SpinnerColumn();
        column.CompletedStyle(Color.Green);
        column.CompletedStyle.ShouldBe(Color.Green);
    }

    [Fact]
    public void CompletedStyle_Extension_Throws_When_Column_IsNull()
    {
        Should.Throw<ArgumentNullException>(() => ((SpinnerColumn)null!).CompletedStyle(Color.Green));
    }

    private sealed class WideFrameSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => false;
        public override IReadOnlyList<string> Frames => new[] { "WIDE_FRAME" };
    }

    private sealed class WideAsciiSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => false;
        public override IReadOnlyList<string> Frames => new[] { "WIDE" };
    }

    private sealed class WideUnicodeSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => true;
        public override IReadOnlyList<string> Frames => new[] { "WIDE" };
    }

    private sealed class MixedWidthSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => false;
        public override IReadOnlyList<string> Frames => new[] { "-", "WIDE" };
    }
}