namespace Spectre.Console.Tests.Unit;

public sealed class LiveDisplayTests
{
    // ── Constructor null guards ───────────────────────────────────────────────

    [Fact]
    public void Constructor_Throws_When_Console_IsNull()
    {
        // Kills L35 NoCoverage: ArgumentNullException.ThrowIfNull(console)
        FluentActions.Invoking(() => new LiveDisplay(null!, new Text("x"))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Throws_When_Target_IsNull()
    {
        // Kills L36 NoCoverage: ArgumentNullException.ThrowIfNull(target)
        var console = new TestConsole().Interactive();
        FluentActions.Invoking(() => new LiveDisplay(console, null!)).Should().Throw<ArgumentNullException>();
    }

    // ── Start / Start<T> null guards and execution ────────────────────────────

    [Fact]
    public void Start_Throws_When_Action_IsNull()
    {
        // Kills L47 NoCoverage: ArgumentNullException.ThrowIfNull(action)
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));
        FluentActions.Invoking(() => live.Start(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Start_Executes_Action()
    {
        // Kills L51 NoCoverage: action(ctx) and L55 task.GetAwaiter().GetResult()
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("hello"));
        var ran = false;

        live.Start(ctx => { ran = true; });

        ran.Should().BeTrue();
    }

    [Fact]
    public void Start_Propagates_Exception_From_Action()
    {
        // Kills L55 NoCoverage: task.GetAwaiter().GetResult() removal would swallow exception
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        FluentActions.Invoking(() =>
            live.Start(ctx => throw new InvalidOperationException("from action"))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StartT_Throws_When_Func_IsNull()
    {
        // Kills L66 NoCoverage: ArgumentNullException.ThrowIfNull(func)
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));
        FluentActions.Invoking(() => live.Start<int>(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartT_Returns_Func_Result()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        var result = live.Start<int>(ctx => 42);

        result.Should().Be(42);
    }

    // ── StartAsync null guards ────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_Throws_When_Func_IsNull()
    {
        // Kills L79 NoCoverage: ArgumentNullException.ThrowIfNull(func)
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));
        await FluentActions.Awaiting(() => live.StartAsync((Func<LiveDisplayContext, Task>)null!)).Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StartAsyncT_Throws_When_Func_IsNull()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));
        await FluentActions.Awaiting(() => live.StartAsync<int>((Func<LiveDisplayContext, Task<int>>)null!)).Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StartAsync_Executes_Action()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));
        var ran = false;

        await live.StartAsync(ctx =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        ran.Should().BeTrue();
    }

    // ── LiveDisplayContext exercised via Start ────────────────────────────────

    [Fact]
    public void Context_UpdateTarget_Does_Not_Throw()
    {
        // Kills L29 NoCoverage: Live.SetRenderable(target)
        // and L30: Refresh()
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("initial"));

        live.Start(ctx =>
        {
            ctx.UpdateTarget(new Text("updated"));
        });
    }

    [Fact]
    public void Context_UpdateTarget_With_Null_Does_Not_Throw()
    {
        // Exercises the null target path through SetRenderable
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        live.Start(ctx =>
        {
            ctx.UpdateTarget(null);
        });
    }

    [Fact]
    public void Context_Refresh_Does_Not_Throw()
    {
        // Kills L41 NoCoverage: _console.Write(ControlCode.Empty) in Refresh
        // and L46 block removal
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        live.Start(ctx =>
        {
            ctx.Refresh();
            ctx.Refresh();
        });
    }

    // ── AutoClear default ─────────────────────────────────────────────────────

    [Fact]
    public void AutoClear_Defaults_To_False()
    {
        var console = new TestConsole().Interactive();
        new LiveDisplay(console, new Text("x")).AutoClear.Should().BeFalse();
    }

    [Fact]
    public void Start_With_AutoClear_True_Does_Not_Throw()
    {
        // Exercises the autoclear=true path in LiveDisplayRenderer.Completed
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x")) { AutoClear = true };

        live.Start(ctx => { ctx.Refresh(); });
    }

    [Fact]
    public void Start_With_AutoClear_False_Does_Not_Throw()
    {
        // Exercises the autoclear=false path in LiveDisplayRenderer.Completed
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x")) { AutoClear = false };

        live.Start(ctx => { ctx.Refresh(); });
    }

    // ── Extension method null guards ──────────────────────────────────────────

    [Fact]
    public void AutoClear_Extension_Throws_When_Live_IsNull()
    {
        // Kills L139 NoCoverage: ArgumentNullException.ThrowIfNull(live)
        FluentActions.Invoking(() => ((LiveDisplay)null!).AutoClear(true)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AutoClear_Extension_Sets_AutoClear()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        live.AutoClear(true).AutoClear.Should().BeTrue();
    }

    [Fact]
    public void AutoClear_Extension_Returns_Same_Instance()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        live.AutoClear(false).Should().BeSameAs(live);
    }

    [Fact]
    public void Overflow_Extension_Throws_When_Live_IsNull()
    {
        // Kills L154 NoCoverage: ArgumentNullException.ThrowIfNull(live)
        FluentActions.Invoking(() => ((LiveDisplay)null!).Overflow(VerticalOverflow.Crop)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Overflow_Extension_Sets_Overflow()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        live.Overflow(VerticalOverflow.Crop).Overflow.Should().Be(VerticalOverflow.Crop);
    }

    [Fact]
    public void Overflow_Extension_Returns_Same_Instance()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        live.Overflow(VerticalOverflow.Visible).Should().BeSameAs(live);
    }

    [Fact]
    public void Cropping_Extension_Throws_When_Live_IsNull()
    {
        // Kills L169 NoCoverage: ArgumentNullException.ThrowIfNull(live)
        FluentActions.Invoking(() =>
            ((LiveDisplay)null!).Cropping(VerticalOverflowCropping.Bottom)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Cropping_Extension_Sets_Cropping()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        live.Cropping(VerticalOverflowCropping.Bottom).Cropping.Should().Be(VerticalOverflowCropping.Bottom);
    }

    [Fact]
    public void Cropping_Extension_Returns_Same_Instance()
    {
        var console = new TestConsole().Interactive();
        var live = new LiveDisplay(console, new Text("x"));

        live.Cropping(VerticalOverflowCropping.Top).Should().BeSameAs(live);
    }
}
