namespace Spectre.Console.Tests.Unit;

[ExpectationPath("Live/Status")]
public sealed class StatusTests
{
    private sealed class DummySpinner1 : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => true;
        public override IReadOnlyList<string> Frames => new[] { "*" };
    }

    private sealed class DummySpinner2 : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
        public override bool IsUnicode => true;
        public override IReadOnlyList<string> Frames => new[] { "-" };
    }

    [Fact]
    [Expectation("Render")]
    public Task Should_Render_Status_Correctly()
    {
        // Given
        var console = new TestConsole()
            .Colors(ColorSystem.TrueColor)
            .Width(10)
            .Interactive()
            .EmitAnsiSequences();

        var status = new Status(console)
        {
            AutoRefresh = false,
            Spinner = new DummySpinner1(),
        };

        // When
        status.Start("foo", ctx =>
        {
            ctx.Refresh();
            ctx.Spinner(new DummySpinner2());
            ctx.Status("bar");
            ctx.Refresh();
            ctx.Spinner(new DummySpinner1());
            ctx.Status("baz");
        });

        // Then
        return Verifier.Verify(console.Output);
    }

    // ── Status constructor ────────────────────────────────────────────────────

    [Fact]
    public void Constructor_Throws_When_Console_IsNull()
    {
        Should.Throw<ArgumentNullException>(() => new Status(null!));
    }

    [Fact]
    public void AutoRefresh_Defaults_To_True()
    {
        // Kills L24 mutant: AutoRefresh = true → AutoRefresh = false
        var console = new TestConsole().Interactive();
        new Status(console).AutoRefresh.ShouldBeTrue();
    }

    // ── Status.Start overloads ────────────────────────────────────────────────

    [Fact]
    public void Start_Propagates_Exceptions_From_Action()
    {
        // Kills L48 mutant: task.GetAwaiter().GetResult() removed
        // Without GetResult(), the exception from the async task is never observed
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        Should.Throw<InvalidOperationException>(() =>
            status.Start("test", ctx => throw new InvalidOperationException("from action")));
    }

    [Fact]
    public void Start_Executes_Action()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };
        var ran = false;

        status.Start("test", ctx => { ran = true; });

        ran.ShouldBeTrue();
    }

    [Fact]
    public void StartT_Returns_Func_Result()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        var result = status.Start("test", ctx => 42);

        result.ShouldBe(42);
    }

    [Fact]
    public async Task StartAsync_Executes_Action()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };
        var ran = false;

        await status.StartAsync("test", async ctx =>
        {
            ran = true;
            await Task.CompletedTask;
        });

        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_Throws_When_Action_IsNull()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);

        await Should.ThrowAsync<ArgumentNullException>(
            () => status.StartAsync("test", null!));
    }

    [Fact]
    public async Task StartAsyncT_Returns_Func_Result()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        var result = await status.StartAsync("test", async ctx =>
        {
            await Task.CompletedTask;
            return 99;
        });

        result.ShouldBe(99);
    }

    [Fact]
    public async Task StartAsyncT_Throws_When_Func_IsNull()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);

        await Should.ThrowAsync<ArgumentNullException>(
            () => status.StartAsync<int>("test", null!));
    }

    [Fact]
    public void Start_Throws_When_Action_IsNull()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        Should.Throw<ArgumentNullException>(
            () => status.Start("test", (Action<StatusContext>)null!));
    }

    [Fact]
    public void StartT_Throws_When_Func_IsNull()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        Should.Throw<ArgumentNullException>(
            () => status.Start<int>("test", (Func<StatusContext, int>)null!));
    }

    // ── SpinnerStyle is applied to SpinnerColumn ─────────────────────────────

    [Fact]
    public void Start_Applies_Custom_SpinnerStyle_To_SpinnerColumn()
    {
        // Kills L93 mutant: object initializer { Style = SpinnerStyle ?? Style.Plain } removed
        // Without initializer, SpinnerColumn defaults to Color.Yellow (producing ANSI codes)
        // With SpinnerStyle = Style.Plain, the spinner should render with NO color codes
        var console = new TestConsole()
            .Colors(ColorSystem.EightBit)
            .Width(20)
            .Interactive()
            .EmitAnsiSequences();

        var status = new Status(console)
        {
            AutoRefresh = false,
            Spinner = new DummySpinner1(), // frames = ["*"]
            SpinnerStyle = Style.Plain,    // no color
        };

        status.Start("foo", ctx => ctx.Refresh());

        // With Style.Plain the spinner emits no color codes; mutant uses Color.Yellow → [38;5;11m
        console.Output.ShouldNotContain("[38;5;11m");
    }

    // ── Null Spinner falls back to BypassSpinner ─────────────────────────────

    [Fact]
    public void Should_Use_BypassSpinner_When_Spinner_Property_IsNull()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { Spinner = null, AutoRefresh = false };
        var ran = false;

        // Should NOT throw even when Spinner is null
        status.Start("test", ctx => { ran = true; });

        ran.ShouldBeTrue();
    }

    // ── Null SpinnerStyle falls back to Style.Plain ───────────────────────────

    [Fact]
    public void Should_Use_StylePlain_When_SpinnerStyle_IsNull()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { SpinnerStyle = null, AutoRefresh = false };
        var ran = false;

        // Should NOT throw even when SpinnerStyle is null
        status.Start("test", ctx => { ran = true; });

        ran.ShouldBeTrue();
    }
}

public sealed class StatusContextMutationTests
{
    // ── SetStatus null guard ───────────────────────────────────────────────────

    [Fact]
    public void SetStatus_Throws_When_Null()
    {
        // Kills L59 Survived: ArgumentNullException.ThrowIfNull(status)
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        Should.Throw<ArgumentNullException>(() =>
            status.Start("test", ctx => { ctx.Status = null!; }));
    }

    [Fact]
    public void SetStatus_Updates_Description()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };
        var captured = string.Empty;

        status.Start("initial", ctx =>
        {
            ctx.Status = "updated";
            captured = ctx.Status;
        });

        captured.ShouldBe("updated");
    }

    // ── SetSpinner null guard ──────────────────────────────────────────────────

    [Fact]
    public void SetSpinner_Throws_When_Null()
    {
        // Kills L66 Survived: ArgumentNullException.ThrowIfNull(spinner)
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        Should.Throw<ArgumentNullException>(() =>
            status.Start("test", ctx => { ctx.Spinner = null!; }));
    }

    [Fact]
    public void SetSpinner_Updates_Spinner()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };
        var spinner = Spinner.Known.Default;

        status.Start("test", ctx =>
        {
            ctx.Spinner = spinner;
            ctx.Spinner.ShouldBeSameAs(spinner);
        });
    }

    // ── SpinnerStyle property round-trip ──────────────────────────────────────

    [Fact]
    public void SpinnerStyle_Property_Gets_And_Sets()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        status.Start("test", ctx =>
        {
            ctx.SpinnerStyle = Color.Red;
            ctx.SpinnerStyle.ShouldBe(Color.Red);
        });
    }

    // ── Extension method null guards ──────────────────────────────────────────

    [Fact]
    public void StatusExtension_Throws_When_Context_IsNull()
    {
        // Kills L85 Survived: ArgumentNullException.ThrowIfNull(context)
        Should.Throw<ArgumentNullException>(
            () => ((StatusContext)null!).Status("test"));
    }

    [Fact]
    public void StatusExtension_Sets_Status_And_Returns_Same_Instance()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        status.Start("initial", ctx =>
        {
            var returned = ctx.Status("new status");
            returned.ShouldBeSameAs(ctx);
            ctx.Status.ShouldBe("new status");
        });
    }

    [Fact]
    public void SpinnerExtension_Throws_When_Context_IsNull()
    {
        // Kills L99 Survived: ArgumentNullException.ThrowIfNull(context)
        Should.Throw<ArgumentNullException>(
            () => ((StatusContext)null!).Spinner(Spinner.Known.Default));
    }

    [Fact]
    public void SpinnerStyleExtension_Throws_When_Context_IsNull()
    {
        // Kills L113 NoCoverage: ArgumentNullException.ThrowIfNull(context)
        Should.Throw<ArgumentNullException>(
            () => ((StatusContext)null!).SpinnerStyle(Color.Red));
    }

    [Fact]
    public void SpinnerStyleExtension_Sets_SpinnerStyle_And_Returns_Same_Instance()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        status.Start("test", ctx =>
        {
            var returned = ctx.SpinnerStyle(Color.Blue);
            returned.ShouldBeSameAs(ctx);
            ctx.SpinnerStyle.ShouldBe(Color.Blue);
        });
    }
}

public sealed class StatusExtensionsTests
{
    // ── AutoRefresh extension ─────────────────────────────────────────────────

    [Fact]
    public void AutoRefresh_Extension_Returns_Same_Status_Instance()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);

        var result = status.AutoRefresh(false);

        result.ShouldBeSameAs(status);
    }

    [Fact]
    public void AutoRefresh_Extension_Sets_AutoRefresh_To_False()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);

        status.AutoRefresh(false);

        status.AutoRefresh.ShouldBeFalse();
    }

    [Fact]
    public void AutoRefresh_Extension_Sets_AutoRefresh_To_True()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console) { AutoRefresh = false };

        status.AutoRefresh(true);

        status.AutoRefresh.ShouldBeTrue();
    }

    [Fact]
    public void AutoRefresh_Extension_Throws_When_Status_IsNull()
    {
        Should.Throw<ArgumentNullException>(() => ((Status)null!).AutoRefresh(true));
    }

    // ── Spinner extension ─────────────────────────────────────────────────────

    [Fact]
    public void Spinner_Extension_Returns_Same_Status_Instance()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);
        var spinner = Spinner.Known.Default;

        var result = status.Spinner(spinner);

        result.ShouldBeSameAs(status);
    }

    [Fact]
    public void Spinner_Extension_Sets_Spinner()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);
        var spinner = Spinner.Known.Default;

        status.Spinner(spinner);

        status.Spinner.ShouldBeSameAs(spinner);
    }

    [Fact]
    public void Spinner_Extension_Throws_When_Status_IsNull()
    {
        Should.Throw<ArgumentNullException>(() => ((Status)null!).Spinner(Spinner.Known.Default));
    }

    // ── SpinnerStyle extension ────────────────────────────────────────────────

    [Fact]
    public void SpinnerStyle_Extension_Returns_Same_Status_Instance()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);

        var result = status.SpinnerStyle(Color.Red);

        result.ShouldBeSameAs(status);
    }

    [Fact]
    public void SpinnerStyle_Extension_Sets_SpinnerStyle()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);

        status.SpinnerStyle(Color.Red);

        status.SpinnerStyle.ShouldBe(Color.Red);
    }

    [Fact]
    public void SpinnerStyle_Extension_Throws_When_Status_IsNull()
    {
        Should.Throw<ArgumentNullException>(() => ((Status)null!).SpinnerStyle(Color.Red));
    }

    [Fact]
    public void SpinnerStyle_Extension_Accepts_Null_Style()
    {
        var console = new TestConsole().Interactive();
        var status = new Status(console);

        status.SpinnerStyle(null);

        status.SpinnerStyle.ShouldBeNull();
    }
}

public sealed class StatusContextExtensionsTests
{
    [Fact]
    public void Status_Extension_Throws_When_Context_IsNull()
    {
        Should.Throw<ArgumentNullException>(
            () => StatusContextExtensions.Status(null!, "test"));
    }

    [Fact]
    public void Spinner_Extension_Throws_When_Context_IsNull()
    {
        Should.Throw<ArgumentNullException>(
            () => StatusContextExtensions.Spinner(null!, Spinner.Known.Default));
    }

    [Fact]
    public void SpinnerStyle_Extension_Throws_When_Context_IsNull()
    {
        Should.Throw<ArgumentNullException>(
            () => StatusContextExtensions.SpinnerStyle(null!, Color.Red));
    }
}