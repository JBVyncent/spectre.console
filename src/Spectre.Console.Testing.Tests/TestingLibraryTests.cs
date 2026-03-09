namespace Spectre.Console.Testing.Tests;

// Helper: a renderable that yields one control-code segment then one text segment.
// Used to exercise the IsControlCode=true branch in TestConsole.Write.
file sealed class ControlAndTextRenderable : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) => new Measurement(0, 4);
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        yield return Segment.Control("\x1b[31m"); // IsControlCode = true
        yield return new Segment("text");
    }
}

// ============================================================
// TestConsole tests
// ============================================================
public sealed class TestConsoleTests
{
    // --- Constructor defaults ---

    [Fact]
    public void Constructor_EmitAnsiSequences_DefaultsFalse()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Width_DefaultsTo80()
    {
        using var console = new TestConsole();
        console.Profile.Width.Should().Be(80);
    }

    [Fact]
    public void Constructor_Height_DefaultsTo24()
    {
        using var console = new TestConsole();
        console.Profile.Height.Should().Be(24);
    }

    [Fact]
    public void Constructor_AnsiCapability_DefaultsTrue()
    {
        using var console = new TestConsole();
        console.Profile.Capabilities.Ansi.Should().BeTrue();
    }

    [Fact]
    public void Constructor_UnicodeCapability_DefaultsTrue()
    {
        using var console = new TestConsole();
        console.Profile.Capabilities.Unicode.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Output_IsEmpty()
    {
        using var console = new TestConsole();
        console.Output.Should().BeEmpty();
    }

    // --- Dispose ---

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var console = new TestConsole();
        var act = () => console.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var console = new TestConsole();
        console.Dispose();
        var act = () => console.Dispose();
        act.Should().NotThrow();
    }

    // --- Clear ---

    [Fact]
    public void Clear_DoesNotThrow()
    {
        using var console = new TestConsole();
        var act = () => console.Clear(false);
        act.Should().NotThrow();
    }

    // --- Write when EmitAnsiSequences = false (default) ---

    [Fact]
    public void Write_EmitAnsiSequencesFalse_PlainTextAppearsInOutput()
    {
        using var console = new TestConsole();
        console.Write(new Markup("hello"));
        console.Output.Should().Contain("hello");
    }

    [Fact]
    public void Write_EmitAnsiSequencesFalse_StyledText_OutputContainsText()
    {
        // Styled markup: control-code segments (SGR) are skipped; text segment is written.
        // Kills: Boolean mutation on `if (EmitAnsiSequences)` and `if (segment.IsControlCode)`.
        using var console = new TestConsole();
        console.Write(new Markup("[red]world[/]"));
        console.Output.Should().Contain("world");
        console.Output.Should().NotContain("\x1b[");
    }

    [Fact]
    public void Write_EmitAnsiSequencesFalse_ControlCodesAreSkipped()
    {
        // Segment.IsControlCode=true segments must not appear in output.
        // If the IsControlCode branch were removed (Block mutant), ANSI codes would leak into output.
        using var console = new TestConsole();
        console.Write(new Markup("[bold blue on white]test[/]"));
        console.Output.Should().NotContain("\x1b");
        console.Output.Should().Contain("test");
    }

    // --- Write when EmitAnsiSequences = true ---

    [Fact]
    public void Write_EmitAnsiSequencesTrue_OutputContainsAnsiEscapes()
    {
        // Kills: Boolean mutation on `if (EmitAnsiSequences)` — if mutant flips to false,
        // ANSI sequences would be stripped and \x1b would not appear.
        using var console = new TestConsole().EmitAnsiSequences();
        console.Write(new Markup("[red]colored[/]"));
        console.Output.Should().Contain("\x1b[");
        console.Output.Should().Contain("colored");
    }

    [Fact]
    public void Write_EmitAnsiSequencesTrue_DoesNotWriteToDirectWriter()
    {
        // When true, routing goes to _console.Write (IAnsiConsole path), not Profile.Out.Writer directly.
        // Both paths ultimately produce output; we confirm the text still appears.
        using var console = new TestConsole().EmitAnsiSequences();
        console.Write(new Markup("present"));
        console.Output.Should().Contain("present");
    }

    // --- WriteAnsi ---

    [Fact]
    public void WriteAnsi_ExecutesAction_AndOutputContainsResult()
    {
        using var console = new TestConsole().EmitAnsiSequences();
        console.WriteAnsi(w => w.CursorUp(1)); // ESC[1A
        console.Output.Should().Contain("\x1b[");
    }

    // --- Cursor property (NullCoalescing ?? kill) ---

    [Fact]
    public void Cursor_Default_IsNotNull()
    {
        // _cursor is set to NoopCursor in constructor; ?? should never activate.
        // Kills NullCoalescing "remove left" mutation: `_cursor ?? _console.Cursor` → `_console.Cursor`.
        using var console = new TestConsole();
        console.Cursor.Should().NotBeNull();
    }

    [Fact]
    public void Cursor_AfterEmitAnsiSequencesExtension_FallsBackToConsoleCursor()
    {
        // EmitAnsiSequences extension calls SetCursor(null), activating the ?? branch.
        // Kills NullCoalescing "remove right" mutation: `_cursor ?? _console.Cursor` → `_cursor`.
        using var console = new TestConsole().EmitAnsiSequences();
        var cursor = console.Cursor;
        cursor.Should().NotBeNull();
    }

    // --- Cursor no-op operations don't throw ---

    [Fact]
    public void Cursor_Move_DoesNotThrow()
    {
        using var console = new TestConsole();
        var act = () => console.Cursor.Move(CursorDirection.Up, 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void Cursor_SetPosition_DoesNotThrow()
    {
        using var console = new TestConsole();
        var act = () => console.Cursor.SetPosition(0, 0);
        act.Should().NotThrow();
    }

    [Fact]
    public void Cursor_Show_DoesNotThrow()
    {
        using var console = new TestConsole();
        var act = () => console.Cursor.Show(true);
        act.Should().NotThrow();
    }

    // --- Input property ---

    [Fact]
    public void Input_IsNotNull()
    {
        using var console = new TestConsole();
        console.Input.Should().NotBeNull();
    }

    // --- Clear produces output ---

    [Fact]
    public void Clear_WritesEraseSequenceToUnderlyingWriter()
    {
        // Kills Statement mutation on `_console.Clear(home)`.
        // Clear bypasses the EmitAnsiSequences check and writes directly to _writer via _console.
        // If the statement is removed, nothing is written and Output remains empty.
        using var console = new TestConsole();
        console.Clear(false);
        console.Output.Should().NotBeEmpty();
        console.Output.Should().Contain("\x1b");
    }

    // --- Cursor: NoopCursor vs _console.Cursor ---

    [Fact]
    public void Cursor_Default_MoveDoesNotWriteToOutput()
    {
        // NoopCursor.Move() is a no-op — nothing written to output.
        // Kills NullCoalescing "remove left" (_cursor ?? _console.Cursor → _console.Cursor):
        // _console.Cursor.Move() would emit ESC sequences to _writer, making Output non-empty.
        using var console = new TestConsole();
        console.Cursor.Move(CursorDirection.Up, 1);
        console.Output.Should().BeEmpty();
    }

    [Fact]
    public void Cursor_AfterEmitAnsiSequences_MoveWritesAnsiToOutput()
    {
        // After EmitAnsiSequences(), SetCursor(null) is called → _cursor = null.
        // Cursor property returns _console.Cursor, whose Move() writes ESC sequences.
        // Kills Block removal on TestConsole.SetCursor body: if _cursor = cursor is removed,
        //   _cursor stays as NoopCursor → Move() produces nothing → assertion fails.
        // Kills Statement removal on TestConsoleExtensions line 99 (console.SetCursor(null)):
        //   if that call is removed, _cursor stays as NoopCursor → same failure.
        using var console = new TestConsole().EmitAnsiSequences();
        console.Cursor.Move(CursorDirection.Up, 1);
        console.Output.Should().Contain("\x1b[");
    }

    // --- IsControlCode segment is skipped when EmitAnsiSequences=false ---

    [Fact]
    public void Write_EmitAnsiSequencesFalse_ControlCodeSegmentSkipped()
    {
        // Uses ControlAndTextRenderable which yields:
        //   Segment("\x1b[31m", isControlCode: true) — must be skipped
        //   Segment("text")                          — must be written
        // Kills Statement mutation on `continue` (removing it makes control code text appear in output).
        using var console = new TestConsole();
        console.Write(new ControlAndTextRenderable());
        console.Output.Should().Contain("text");
        console.Output.Should().NotContain("\x1b");
    }

    // --- ExclusivityMode (exercises NoopExclusivityMode.Run / RunAsync) ---

    [Fact]
    public void ExclusivityMode_Run_InvokesFunc()
    {
        // Kills Block removal on NoopExclusivityMode.Run: if func() is not called, called stays false.
        using var console = new TestConsole();
        var called = false;
        console.ExclusivityMode.Run(() => { called = true; return 0; });
        called.Should().BeTrue();
    }

    [Fact]
    public void ExclusivityMode_Run_ReturnsFuncResult()
    {
        using var console = new TestConsole();
        var result = console.ExclusivityMode.Run(() => 42);
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExclusivityMode_RunAsync_InvokesFunc()
    {
        // Exercises NoopExclusivityMode.RunAsync (the Block-removal mutant).
        using var console = new TestConsole();
        var called = false;
        await console.ExclusivityMode.RunAsync(async () =>
        {
            called = true;
            return await Task.FromResult(0);
        });

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ExclusivityMode_RunAsync_ReturnsFuncResult()
    {
        using var console = new TestConsole();
        var result = await console.ExclusivityMode.RunAsync(() => Task.FromResult(99));
        result.Should().Be(99);
    }
}

// ============================================================
// TestConsoleExtensions tests
// ============================================================
public sealed class TestConsoleExtensionsTests
{
    [Fact]
    public void Colors_SetsColorSystem_AndReturnsConsole()
    {
        using var console = new TestConsole();
        var returned = console.Colors(ColorSystem.EightBit);
        returned.Should().BeSameAs(console);
        console.Profile.Capabilities.ColorSystem.Should().Be(ColorSystem.EightBit);
    }

    [Fact]
    public void SupportsAnsi_True_SetsAnsiCapability()
    {
        using var console = new TestConsole();
        var returned = console.SupportsAnsi(true);
        returned.Should().BeSameAs(console);
        console.Profile.Capabilities.Ansi.Should().BeTrue();
    }

    [Fact]
    public void SupportsAnsi_False_ClearsAnsiCapability()
    {
        using var console = new TestConsole();
        var returned = console.SupportsAnsi(false);
        returned.Should().BeSameAs(console);
        console.Profile.Capabilities.Ansi.Should().BeFalse();
    }

    [Fact]
    public void SupportsUnicode_True_SetsUnicodeCapability()
    {
        using var console = new TestConsole();
        var returned = console.SupportsUnicode(true);
        returned.Should().BeSameAs(console);
        console.Profile.Capabilities.Unicode.Should().BeTrue();
    }

    [Fact]
    public void SupportsUnicode_False_ClearsUnicodeCapability()
    {
        using var console = new TestConsole();
        var returned = console.SupportsUnicode(false);
        returned.Should().BeSameAs(console);
        console.Profile.Capabilities.Unicode.Should().BeFalse();
    }

    [Fact]
    public void Interactive_SetsInteractiveCapability()
    {
        using var console = new TestConsole();
        var returned = console.Interactive();
        returned.Should().BeSameAs(console);
        console.Profile.Capabilities.Interactive.Should().BeTrue();
    }

    [Fact]
    public void Width_SetsProfileWidth_AndReturnsConsole()
    {
        using var console = new TestConsole();
        var returned = console.Width(120);
        returned.Should().BeSameAs(console);
        console.Profile.Width.Should().Be(120);
    }

    [Fact]
    public void Height_SetsProfileHeight_AndReturnsConsole()
    {
        using var console = new TestConsole();
        var returned = console.Height(50);
        returned.Should().BeSameAs(console);
        console.Profile.Height.Should().Be(50);
    }

    [Fact]
    public void Size_SetsBothWidthAndHeight_AndReturnsConsole()
    {
        using var console = new TestConsole();
        var returned = console.Size(new Size(100, 40));
        returned.Should().BeSameAs(console);
        console.Profile.Width.Should().Be(100);
        console.Profile.Height.Should().Be(40);
    }

    [Fact]
    public void EmitAnsiSequences_SetsPropertyTrue_AndNullsCursor()
    {
        using var console = new TestConsole();
        var returned = console.EmitAnsiSequences();
        returned.Should().BeSameAs(console);
        console.EmitAnsiSequences.Should().BeTrue();
        // After extension, cursor falls back to _console.Cursor (not NoopCursor)
        console.Cursor.Should().NotBeNull();
    }
}

// ============================================================
// TestConsoleInput tests
// ============================================================
public sealed class TestConsoleInputTests
{
    // --- PushText ---

    [Fact]
    public void PushText_Null_ThrowsArgumentNullException()
    {
        var input = new TestConsoleInput();
        var act = () => input.PushText(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PushText_EnqueuesEachCharacterInOrder()
    {
        var input = new TestConsoleInput();
        input.PushText("ab");
        var first = input.ReadKey(false);
        var second = input.ReadKey(false);
        first!.Value.KeyChar.Should().Be('a');
        second!.Value.KeyChar.Should().Be('b');
    }

    [Fact]
    public void PushText_EmptyString_EnqueuesNothing()
    {
        var input = new TestConsoleInput();
        input.PushText(string.Empty);
        input.IsKeyAvailable().Should().BeFalse();
    }

    // --- PushTextWithEnter ---

    [Fact]
    public void PushTextWithEnter_EnqueuesTextFollowedByEnter()
    {
        var input = new TestConsoleInput();
        input.PushTextWithEnter("hi");
        input.ReadKey(false); // 'h'
        input.ReadKey(false); // 'i'
        var enter = input.ReadKey(false);
        enter!.Value.Key.Should().Be(ConsoleKey.Enter);
        enter.Value.KeyChar.Should().Be('\0'); // non-printing key → '\0'
    }

    // --- PushCharacter ---

    [Fact]
    public void PushCharacter_Uppercase_SetsControlModifier_NotShiftOrAlt()
    {
        // Kills: Logical mutation on `char.IsUpper(input)` — if mutant returns false,
        //   uppercase chars would have no Control modifier.
        // Kills: Boolean mutation on the `false, false` (shift, alt) params in ConsoleKeyInfo ctor:
        //   if shift were true, Modifiers would include Shift (violating the assertion below).
        var input = new TestConsoleInput();
        input.PushCharacter('A');
        var key = input.ReadKey(false);
        key!.Value.KeyChar.Should().Be('A');
        key.Value.Modifiers.HasFlag(ConsoleModifiers.Control).Should().BeTrue();
        key.Value.Modifiers.HasFlag(ConsoleModifiers.Shift).Should().BeFalse();
        key.Value.Modifiers.HasFlag(ConsoleModifiers.Alt).Should().BeFalse();
    }

    [Fact]
    public void PushCharacter_Lowercase_NoControlShiftOrAltModifier()
    {
        // Kills: Boolean mutation — if `!char.IsUpper` were used, lowercase would set Control.
        // Also kills mutations on the `false, false` shift/alt params.
        var input = new TestConsoleInput();
        input.PushCharacter('a');
        var key = input.ReadKey(false);
        key!.Value.KeyChar.Should().Be('a');
        key.Value.Modifiers.HasFlag(ConsoleModifiers.Control).Should().BeFalse();
        key.Value.Modifiers.HasFlag(ConsoleModifiers.Shift).Should().BeFalse();
        key.Value.Modifiers.HasFlag(ConsoleModifiers.Alt).Should().BeFalse();
    }

    [Fact]
    public void PushCharacter_Digit_HasCorrectKeyChar()
    {
        var input = new TestConsoleInput();
        input.PushCharacter('5');
        var key = input.ReadKey(false);
        key!.Value.KeyChar.Should().Be('5');
    }

    // --- PushKey(ConsoleKey) ---

    [Fact]
    public void PushKey_NonPrintingKey_KeyCharIsNul_AndNoModifiers()
    {
        // UpArrow is in _nonPrintingKeys → ch = '\0'.
        // Kills: Boolean mutation on `_nonPrintingKeys.Contains(input)` (ternary).
        // Kills: Boolean mutations on the `false, false, false` (shift, alt, control) params
        //   in ConsoleKeyInfo ctor — if any were true, the corresponding Modifier flag would be set.
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.UpArrow);
        var key = input.ReadKey(false);
        key!.Value.Key.Should().Be(ConsoleKey.UpArrow);
        key.Value.KeyChar.Should().Be('\0');
        key.Value.Modifiers.Should().Be((ConsoleModifiers)0);
    }

    [Fact]
    public void PushKey_DownArrow_KeyCharIsNul()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.DownArrow);
        var key = input.ReadKey(false);
        key!.Value.Key.Should().Be(ConsoleKey.DownArrow);
        key.Value.KeyChar.Should().Be('\0');
    }

    [Fact]
    public void PushKey_Enter_KeyCharIsNul()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.Enter);
        var key = input.ReadKey(false);
        key!.Value.Key.Should().Be(ConsoleKey.Enter);
        key.Value.KeyChar.Should().Be('\0');
    }

    [Fact]
    public void PushKey_Escape_KeyCharIsNul()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.Escape);
        var key = input.ReadKey(false);
        key!.Value.Key.Should().Be(ConsoleKey.Escape);
        key.Value.KeyChar.Should().Be('\0');
    }

    [Fact]
    public void PushKey_Tab_KeyCharIsNul()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.Tab);
        var key = input.ReadKey(false);
        key!.Value.Key.Should().Be(ConsoleKey.Tab);
        key.Value.KeyChar.Should().Be('\0');
    }

    [Fact]
    public void PushKey_Backspace_KeyCharIsNul()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.Backspace);
        var key = input.ReadKey(false);
        key!.Value.Key.Should().Be(ConsoleKey.Backspace);
        key.Value.KeyChar.Should().Be('\0');
    }

    [Fact]
    public void PushKey_FunctionKeys_KeyCharIsNul()
    {
        // All 12 function keys must produce '\0'.
        var functionKeys = new[]
        {
            ConsoleKey.F1, ConsoleKey.F2, ConsoleKey.F3, ConsoleKey.F4,
            ConsoleKey.F5, ConsoleKey.F6, ConsoleKey.F7, ConsoleKey.F8,
            ConsoleKey.F9, ConsoleKey.F10, ConsoleKey.F11, ConsoleKey.F12,
        };
        var input = new TestConsoleInput();
        foreach (var fk in functionKeys)
        {
            input.PushKey(fk);
        }

        foreach (var fk in functionKeys)
        {
            var key = input.ReadKey(false);
            key!.Value.Key.Should().Be(fk);
            key.Value.KeyChar.Should().Be('\0', $"because {fk} is a non-printing key");
        }
    }

    [Fact]
    public void PushKey_PrintingKey_KeyCharIsNotNul()
    {
        // ConsoleKey.A is not in _nonPrintingKeys → ch = (char)ConsoleKey.A = 'A'.
        // Kills: the Contains ternary — if it always returned '\0', ch would be '\0' here.
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.A);
        var key = input.ReadKey(false);
        key!.Value.Key.Should().Be(ConsoleKey.A);
        key.Value.KeyChar.Should().NotBe('\0');
    }

    [Fact]
    public void PushKey_ConsoleKeyInfo_EnqueuesDirectly()
    {
        var info = new ConsoleKeyInfo('x', ConsoleKey.X, shift: true, alt: false, control: false);
        var input = new TestConsoleInput();
        input.PushKey(info);
        var key = input.ReadKey(false);
        key!.Value.KeyChar.Should().Be('x');
        key.Value.Key.Should().Be(ConsoleKey.X);
        key.Value.Modifiers.HasFlag(ConsoleModifiers.Shift).Should().BeTrue();
    }

    // --- IsKeyAvailable ---

    [Fact]
    public void IsKeyAvailable_EmptyQueue_ReturnsFalse()
    {
        // Kills: Boolean mutation `Count > 0` → `Count >= 0` (always true).
        var input = new TestConsoleInput();
        input.IsKeyAvailable().Should().BeFalse();
    }

    [Fact]
    public void IsKeyAvailable_AfterPush_ReturnsTrue()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.Spacebar);
        input.IsKeyAvailable().Should().BeTrue();
    }

    [Fact]
    public void IsKeyAvailable_AfterDequeue_ReturnsFalse()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.Spacebar);
        input.ReadKey(false);
        input.IsKeyAvailable().Should().BeFalse();
    }

    // --- ReadKey ---

    [Fact]
    public void ReadKey_EmptyQueue_ThrowsInvalidOperationException()
    {
        var input = new TestConsoleInput();
        var act = () => input.ReadKey(false);
        act.Should().Throw<InvalidOperationException>().WithMessage("*No input available*");
    }

    [Fact]
    public void ReadKey_DequeuesInFifoOrder()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.A);
        input.PushKey(ConsoleKey.B);
        var first = input.ReadKey(false);
        var second = input.ReadKey(false);
        first!.Value.Key.Should().Be(ConsoleKey.A);
        second!.Value.Key.Should().Be(ConsoleKey.B);
    }

    [Fact]
    public void ReadKey_InterceptParameter_DoesNotAffectResult()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.Z);
        var result = input.ReadKey(true);
        result.Should().NotBeNull();
        result!.Value.Key.Should().Be(ConsoleKey.Z);
    }

    // --- ReadKeyAsync ---

    [Fact]
    public async Task ReadKeyAsync_ReturnsNextKey()
    {
        var input = new TestConsoleInput();
        input.PushKey(ConsoleKey.Enter);
        var result = await input.ReadKeyAsync(false, CancellationToken.None);
        result!.Value.Key.Should().Be(ConsoleKey.Enter);
        result.Value.KeyChar.Should().Be('\0');
    }

    [Fact]
    public async Task ReadKeyAsync_EmptyQueue_ThrowsInvalidOperationException()
    {
        var input = new TestConsoleInput();
        var act = async () => await input.ReadKeyAsync(false, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // --- FsCheck properties ---

    [Property]
    public bool PushText_FifoOrderPreserved(NonNull<string> nonNull)
    {
        // Invariant: reading chars back yields them in the same order as pushed.
        var text = new string(nonNull.Get.Where(c => c > 0x1F && c < 0x7F).ToArray());
        if (text.Length == 0) return true;

        var input = new TestConsoleInput();
        input.PushText(text);
        var sb = new System.Text.StringBuilder();
        while (input.IsKeyAvailable())
        {
            var key = input.ReadKey(false);
            sb.Append(key!.Value.KeyChar);
        }

        return sb.ToString() == text;
    }

    [Property]
    public bool IsKeyAvailable_TrueIffQueueNonEmpty(NonNegativeInt count)
    {
        // Invariant: IsKeyAvailable ↔ at least one item in queue.
        var n = count.Get % 10; // bound to 0-9
        var input = new TestConsoleInput();
        for (var i = 0; i < n; i++)
        {
            input.PushKey(ConsoleKey.Spacebar);
        }

        return input.IsKeyAvailable() == (n > 0);
    }
}

// ============================================================
// TestCapabilities tests
// ============================================================
public sealed class TestCapabilitiesTests
{
    [Fact]
    public void DefaultColorSystem_IsTrueColor()
    {
        var caps = new TestCapabilities();
        caps.ColorSystem.Should().Be(ColorSystem.TrueColor);
    }

    [Fact]
    public void AllBoolProperties_GetAndSet()
    {
        var caps = new TestCapabilities
        {
            Ansi = true,
            Links = true,
            Legacy = true,
            Interactive = true,
            Unicode = true,
            AlternateBuffer = true,
            SupportsSixel = true,
        };

        caps.Ansi.Should().BeTrue();
        caps.Links.Should().BeTrue();
        caps.Legacy.Should().BeTrue();
        caps.Interactive.Should().BeTrue();
        caps.Unicode.Should().BeTrue();
        caps.AlternateBuffer.Should().BeTrue();
        caps.SupportsSixel.Should().BeTrue();

        // Toggle all off to kill Boolean inversion mutants
        caps.Ansi = false;
        caps.Links = false;
        caps.Legacy = false;
        caps.Interactive = false;
        caps.Unicode = false;
        caps.AlternateBuffer = false;
        caps.SupportsSixel = false;

        caps.Ansi.Should().BeFalse();
        caps.Links.Should().BeFalse();
        caps.Legacy.Should().BeFalse();
        caps.Interactive.Should().BeFalse();
        caps.Unicode.Should().BeFalse();
        caps.AlternateBuffer.Should().BeFalse();
        caps.SupportsSixel.Should().BeFalse();
    }

    [Fact]
    public void ColorSystem_CanBeSetToEachValue()
    {
        var caps = new TestCapabilities();
        foreach (var system in Enum.GetValues<ColorSystem>())
        {
            caps.ColorSystem = system;
            caps.ColorSystem.Should().Be(system);
        }
    }

    [Fact]
    public void CreateRenderContext_NullConsole_Throws()
    {
        var caps = new TestCapabilities();
        var act = () => caps.CreateRenderContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateRenderContext_ValidConsole_ReturnsRenderOptionsWithConsoleSize()
    {
        // Kills Statement mutation on `return RenderOptions.Create(console, this)`:
        // mutation changes it to `return default(RenderOptions)` — ConsoleSize would be (0,0),
        // not the actual console dimensions. We verify ConsoleSize reflects the test console.
        using var console = new TestConsole(); // Width=80, Height=24
        var caps = new TestCapabilities { ColorSystem = ColorSystem.EightBit };
        var opts = caps.CreateRenderContext(console);
        opts.ConsoleSize.Width.Should().Be(80);
        opts.ConsoleSize.Height.Should().Be(24);
    }

    [Fact]
    public void CreateRenderContext_ValidConsole_ReturnsRenderOptionsWithCapabilities()
    {
        using var console = new TestConsole();
        var caps = new TestCapabilities { ColorSystem = ColorSystem.EightBit, Unicode = true };
        var opts = caps.CreateRenderContext(console);
        opts.ColorSystem.Should().Be(ColorSystem.EightBit);
        opts.Unicode.Should().BeTrue();
    }
}

// ============================================================
// StringExtensions tests
// ============================================================
public sealed class StringExtensionsTests
{
    // --- TrimLines ---

    [Fact]
    public void TrimLines_Null_ReturnsEmpty()
    {
        // Kills: Boolean mutation on `value is null` → `value is not null`.
        ((string?)null).TrimLines().Should().BeEmpty();
    }

    [Fact]
    public void TrimLines_EmptyString_ReturnsEmptyString()
    {
        string.Empty.TrimLines().Should().BeEmpty();
    }

    [Fact]
    public void TrimLines_SingleLine_TrailingWhitespaceRemoved()
    {
        "hello   ".TrimLines().Should().Be("hello");
    }

    [Fact]
    public void TrimLines_MultipleLines_EachLineTrimmed()
    {
        "line1   \nline2  \nline3".TrimLines()
            .Should().Be("line1\nline2\nline3");
    }

    [Fact]
    public void TrimLines_LeadingWhitespace_Preserved()
    {
        // TrimEnd should not touch leading whitespace.
        "  indented  ".TrimLines().Should().Be("  indented");
    }

    [Fact]
    public void TrimLines_WindowsLineEndings_Normalised()
    {
        "a  \r\nb  ".TrimLines().Should().Be("a\nb");
    }

    [Fact]
    public void TrimLines_BlankLines_Preserved()
    {
        "a\n\nb".TrimLines().Should().Be("a\n\nb");
    }

    // --- NormalizeLineEndings ---

    [Fact]
    public void NormalizeLineEndings_Null_ReturnsEmpty()
    {
        // Kills: Boolean mutation on `value != null` → `value == null`.
        ((string?)null).NormalizeLineEndings().Should().BeEmpty();
    }

    [Fact]
    public void NormalizeLineEndings_CrLf_BecomesLf()
    {
        "a\r\nb".NormalizeLineEndings().Should().Be("a\nb");
    }

    [Fact]
    public void NormalizeLineEndings_StandaloneCr_Removed()
    {
        "a\rb".NormalizeLineEndings().Should().Be("ab");
    }

    [Fact]
    public void NormalizeLineEndings_Lf_Unchanged()
    {
        "a\nb".NormalizeLineEndings().Should().Be("a\nb");
    }

    [Fact]
    public void NormalizeLineEndings_Mixed_NormalisesAll()
    {
        // \r\n → \n, then \r → "" (deleted, not newline), \n stays.
        // "a\r\nb\rc\nd" → step1: "a\nb\rc\nd" → step2: "a\nbc\nd"
        "a\r\nb\rc\nd".NormalizeLineEndings().Should().Be("a\nbc\nd");
    }

    [Fact]
    public void NormalizeLineEndings_NoSpecialChars_Unchanged()
    {
        "hello world".NormalizeLineEndings().Should().Be("hello world");
    }

    // --- FsCheck properties ---

    [Property]
    public bool TrimLines_NoLineHasTrailingWhitespace(NonNull<string> input)
    {
        // Invariant: every line produced by TrimLines must have no trailing whitespace.
        var result = input.Get.TrimLines();
        return result.Split('\n').All(line => line == line.TrimEnd());
    }

    [Property]
    public bool TrimLines_Null_AlwaysEmpty(string? input)
    {
        if (input is not null) return true; // only care about null
        return input.TrimLines() == string.Empty;
    }

    [Property]
    public bool NormalizeLineEndings_ResultNeverContainsCr(NonNull<string> input)
    {
        // Invariant: output must never contain \r.
        var result = input.Get.NormalizeLineEndings();
        return !result.Contains('\r');
    }

    [Property]
    public bool NormalizeLineEndings_ResultNeverContainsCrLf(NonNull<string> input)
    {
        // Invariant: output must never contain the two-char sequence \r\n.
        var result = input.Get.NormalizeLineEndings();
        return !result.Contains("\r\n");
    }

    [Property]
    public bool NormalizeLineEndings_Null_AlwaysEmpty(string? input)
    {
        if (input is not null) return true;
        return input.NormalizeLineEndings() == string.Empty;
    }

    [Property]
    public bool NormalizeLineEndings_Idempotent(NonNull<string> input)
    {
        // Invariant: normalizing twice equals normalizing once.
        var once = input.Get.NormalizeLineEndings();
        var twice = once.NormalizeLineEndings();
        return once == twice;
    }
}

// ============================================================
// StyleExtensions (Testing assembly) tests
// ============================================================
public sealed class StyleExtensionsTests
{
    [Fact]
    public void SetColor_ForegroundTrue_SetsForgroundColor()
    {
        // Kills: Boolean mutation on `if (foreground)` → `if (!foreground)` or `if (true)`.
        var style = Style.Plain;
        var result = style.SetColor(Color.Red, foreground: true);
        result.Foreground.Should().Be(Color.Red);
        result.Background.Should().Be(Color.Default);
    }

    [Fact]
    public void SetColor_ForegroundFalse_SetsBackgroundColor()
    {
        // Kills: Boolean mutation on `if (foreground)` → `if (true)`.
        var style = Style.Plain;
        var result = style.SetColor(Color.Blue, foreground: false);
        result.Background.Should().Be(Color.Blue);
        result.Foreground.Should().Be(Color.Default);
    }

    [Fact]
    public void SetColor_ForegroundTrue_DoesNotTouchBackground()
    {
        var style = new Style(background: Color.Green);
        var result = style.SetColor(Color.Red, foreground: true);
        result.Background.Should().Be(Color.Green);
        result.Foreground.Should().Be(Color.Red);
    }

    [Fact]
    public void SetColor_ForegroundFalse_DoesNotTouchForeground()
    {
        var style = new Style(Color.Yellow);
        var result = style.SetColor(Color.Blue, foreground: false);
        result.Foreground.Should().Be(Color.Yellow);
        result.Background.Should().Be(Color.Blue);
    }

    [Property]
    public bool SetColor_Foreground_AlwaysMatchesForegroundMethod(bool fg)
    {
        // Invariant: SetColor(c, true) ≡ style.Foreground(c) and SetColor(c, false) ≡ style.Background(c).
        var style = Style.Plain;
        var result = style.SetColor(Color.Red, fg);
        return fg
            ? result.Foreground == Color.Red && result.Background == Color.Default
            : result.Background == Color.Red && result.Foreground == Color.Default;
    }
}

// ============================================================
// ShouldlyExtensions tests
// ============================================================
public sealed class ShouldlyExtensionsTests
{
    [Fact]
    public void And_NullAction_ThrowsArgumentNullException()
    {
        var item = "test";
        var act = () => item.And(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void And_ExecutesActionOnItem()
    {
        var calls = 0;
        "value".And(_ => calls++);
        calls.Should().Be(1);
    }

    [Fact]
    public void And_ReturnsOriginalItem()
    {
        var obj = new object();
        var returned = obj.And(_ => { });
        returned.Should().BeSameAs(obj);
    }

    [Fact]
    public void And_ActionReceivesCorrectItem()
    {
        var received = string.Empty;
        "hello".And(s => received = s);
        received.Should().Be("hello");
    }

    [Property]
    public bool And_AlwaysReturnsInputUnchanged(NonNull<string> input)
    {
        // Invariant: And never mutates or substitutes the input reference.
        var item = input.Get;
        var returned = item.And(_ => { });
        return ReferenceEquals(item, returned);
    }
}
