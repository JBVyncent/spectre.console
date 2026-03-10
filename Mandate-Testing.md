# Testing Mandate — Spectre.Console Fork

Version: 2.1 | Effective: 2026-03-10
Applies to: All production code in `src/` across all assemblies.

---

## 1. Philosophy

Overengineered is underappreciated. Every test is a contract that protects against
regression. If code exists, it is tested. If it cannot be tested, it is refactored
until it can be — or its exclusion is justified and documented inline.

---

## 2. Non-Negotiables

| Target | Threshold | Exceptions |
|--------|-----------|------------|
| Line/Branch Coverage | 100% | None. Untestable code is refactored or excluded with justification. |
| Test Pass Rate | 100% | Zero failing tests in required CI gates on supported TFMs. |
| Flaky Test Policy | Time-boxed quarantine only | Skips are allowed only via Flaky Quarantine policy in §8.2 (issue + owner + expiry). |
| Stryker Mutation Score | 100% | Equivalent mutants are disabled with inline documentation. |
| CI Tiering | Required | Unit, Integration, and Mutation gates run separately with explicit filters. |
| Commit Discipline | One commit per logical change | Tests ship with the code they cover, never separately. |

---

## 3. Technology Stack

| Layer | Tool | Purpose |
|-------|------|---------|
| Framework | xUnit | Test runner. `[Fact]` for deterministic cases, `[Theory]` for parameterized. |
| Assertions | FluentAssertions | `.Should().Be()`, `.Should().Throw<T>()`, `.Should().NotBeEmpty()`. |
| Mocking | Moq / NSubstitute | Only for external dependencies. Prefer real objects when feasible. |
| Mutation | Stryker.NET 4.x | Final validation gate. Run per-assembly via `stryker-config.json`. |
| Test Console | `TestConsole` | Spectre's built-in test harness for rendering verification. |
| Snapshot | Expectation files | `Spectre.Console.Tests/Expectations/` — golden-file comparison for widget rendering. |

### Project conventions

- **Test project naming**: `<Assembly>.Tests` (e.g., `Spectre.Console.Tests`, `Spectre.Console.Ansi.Tests`)
- **Test class naming**: Match the class under test. Use nested classes for organization:
  ```csharp
  public class FooTests
  {
      public class TheConstructors { ... }
      public class TheProperties { ... }
      public class TheFooMethod { ... }
      public class MutationKillers { ... }
  }
  ```
- **Test method naming**: `Method_Condition_ExpectedResult` or descriptive sentence.
  ```csharp
  [Fact]
  public void MaxWidth_Rejects_Zero()
  [Fact]
  public void Render_Should_Scale_Height_Proportionally_When_MaxWidth_Set()
  ```

### Test infrastructure helpers

- **`TestConsole`**: Use `EmitAnsiSequences()` when testing ANSI output. Set
  `Profile.Capabilities` to control feature flags (`Unicode`, `Ansi`, `SupportsSixel`).
- **Image helpers**: Use `CreatePngStream(width, height, color)` factory for ImageSharp tests
  instead of loading files from disk.
- **`GetSegments(console)`**: Extract raw `Segment` output for structural assertions.

---

## 4. Test Design Process

Before writing any test, complete this analysis. It does not need to be written down
as a separate artifact — it informs test design directly.

### 4.1 Path enumeration

Identify every distinct execution path in the code under test:

- Every `if`/`else` branch (including implicit else — the fall-through case)
- Every `switch` case and its default
- Every `try`/`catch`/`finally` path
- Every null-coalescing (`??`) and null-conditional (`?.`) branch
- Every early return / `continue` / `break`
- Every loop boundary (zero iterations, one iteration, many iterations)

### 4.2 Boundary identification

For every input parameter, identify three classes of values:

| Class | Examples |
|-------|---------|
| **Happy path** | Normal, expected values that exercise the primary logic |
| **Edge cases** | `null`, `0`, `1`, `-1`, `int.MaxValue`, empty string, empty collection, single element |
| **Error paths** | Invalid types, out-of-range values, malformed data, disposed objects |

### 4.3 Mutation anticipation

Before writing tests, scan the code for mutation-vulnerable patterns:

| Pattern | Likely Mutation | Defense |
|---------|----------------|---------|
| `if (x > 0)` | `>=`, `<`, `true`, `false` | Test with `x = 0` (boundary) and `x = 1` (just above) |
| `x + 1` | `x - 1`, `x * 1`, `x + 0` | Assert the exact numeric result |
| `return foo ?? bar` | `return foo`, `return bar` | Test with `foo = null` and `foo = non-null` |
| `list.Count - 1` | `list.Count + 1`, `list.Count` | Assert behavior at Count = 0 and Count = 1 |
| String literal in error message | Empty string, different string | Assert message content OR disable with justification |

---

## 5. Test Implementation Standards

### 5.1 Structure

Every test follows Arrange-Act-Assert:

```csharp
[Fact]
public void Encode_Should_Reject_MaxColors_Below_Two()
{
    // Arrange
    var image = CreateTestImage(2, 2);

    // Act
    var act = () => SixelEncoder.Encode(image, maxColors: 1);

    // Assert
    act.Should().Throw<ArgumentOutOfRangeException>()
       .WithParameterName("maxColors");
}
```

- **One assertion concept per test.** Multiple `.Should()` calls are fine if they assert
  the same logical outcome. Do not test unrelated behaviors in one method.
- **No test interdependence.** Tests must pass in any order, in parallel.
- **Unit tests have no file system, network, or clock dependencies.** Use `TestConsole`,
  in-memory streams, and injected `TimeProvider`.
- **Integration tests may use real infrastructure only when necessary.** Tag them with
  `[Trait("Category", "Integration")]`, isolate their side effects, and run them in a
  separate CI tier.

### 5.2 Assertion strength

Assertions must be **specific enough to kill mutations**:

| Weak (mutant survives) | Strong (mutant killed) |
|------------------------|----------------------|
| `result.Should().NotBeNull()` | `result.Should().Be(expectedValue)` |
| `output.Should().NotBeEmpty()` | `output.Should().Contain("expected text")` |
| `act.Should().Throw<Exception>()` | `act.Should().Throw<ArgumentOutOfRangeException>()` |
| `width.Should().BePositive()` | `width.Should().Be(42)` |

### 5.3 Rendering tests

For Spectre.Console widget tests:

- **Snapshot tests**: Use expectation files for complex visual output. Compare against
  golden files in `Expectations/`.
- **Snapshot baseline changes**: Require explicit reviewer approval and a PR note that
  explains the expected visual delta and why the baseline changed.
- **Structural tests**: Use `GetSegments()` or `console.Output` to verify specific
  content without full visual comparison.
- **ANSI tests**: Enable `EmitAnsiSequences()` on `TestConsole` to capture escape codes.
  Assert specific sequences for color, style, and control output.
- **Measurement tests**: Test `Measure()` directly with specific `maxWidth` values.
  Assert both `Min` and `Max` of the returned `Measurement`.

### 5.4 Null guard tests

Every public boundary method with `ArgumentNullException.ThrowIfNull()` gets a
corresponding contract test:

```csharp
[Fact]
public void Foo_Throws_If_Bar_Is_Null()
{
    var act = () => target.Foo(null!);
    act.Should().Throw<ArgumentNullException>()
       .WithParameterName("bar");
}
```

Internal pass-through methods do not need duplicate null-guard tests when boundary
coverage already proves the public contract.

If the null guard is redundant (a downstream call throws the same exception), disable
it with `// Stryker disable once Statement : Equivalent — <downstream> also throws`.

---

## 6. Mutation Testing Protocol

### 6.1 Workflow

1. **Write tests first** — achieve 100% line/branch coverage.
2. **Run Stryker** — `cd src/<Assembly>.Tests && dotnet stryker --config-file stryker-config.json`
3. **Analyze survivors** — parse the JSON report for `Survived` and `Timeout` mutants.
4. **For each survivor, execute exactly one of three actions:**

| Action | When | How |
|--------|------|-----|
| **Kill** | The mutation changes observable behavior | Write a targeted test with a boundary assertion |
| **Refactor** | The code is redundant or unreachable | Remove the dead code. Code that doesn't exist can't be mutated. |
| **Disable** | The mutation is semantically equivalent | Add `// Stryker disable once <type> : <justification>` |

5. **Re-run Stryker** until score = 100%.
6. **Commit** — tests and production code in a single commit.

### 6.2 Stryker config pattern

Each test project has a `stryker-config.json`:

```json
{
  "stryker-config": {
    "project": "<Assembly>.csproj",
    "test-runner": "dotnet test",
    "reporters": ["json", "html", "cleartext"],
    "mutate": [
      "**/*.cs",
      "!**/Generated/**",
      "!../../Spectre.Console/**"
    ]
  }
}
```

Run with: `dotnet stryker --config-file stryker-config.json`
**Never** use `-c` flag (it means concurrency, not config).

### 6.3 Stryker disable rules

#### Placement

- **Method-level coverage**: Place `// Stryker disable all` on the FIRST LINE inside
  the method body (after the opening `{`). Place `// Stryker restore all` on the LAST
  LINE before the closing `}`.
  ```csharp
  private void Foo()
  {
      // Stryker disable all : Foo — justification
      ... code ...
      // Stryker restore all
  }
  ```

- **Single-line disable**: `// Stryker disable once <type> : justification` on the
  line BEFORE the target statement.

- **Class-level coverage**: Place `// Stryker disable all` BEFORE the class declaration
  (not between members inside the class). Place `// Stryker restore all` AFTER the
  closing `}` of the class.

- **Between-member placement does NOT work**: A disable comment between two methods
  inside a class only covers immediately adjacent code — it does NOT cover entire
  method bodies of subsequent methods.

- **Block removal disables**: `// Stryker disable once Block` must be placed BEFORE
  the `if`/`else` keyword, not inside the block body. The Block mutation removes the
  entire block including any disable comments inside it.

#### Justification categories

Every `Stryker disable` comment MUST include a justification. Valid categories:

| Category | Meaning | Example |
|----------|---------|---------|
| `Equivalent` | Mutation produces identical observable behavior | `Equivalent — downstream also throws ArgumentNullException` |
| `NoCoverage` | Code path requires infrastructure not available in tests | `NoCoverage — file-based constructor requires image on disk` |
| `Killed by <TestName>` | Test exists but Stryker can't trace coverage | `Killed by FooTests.Bar — assertion verifies dimensions change` |
| Descriptive reason | Explains why mutation doesn't change behavior | `Arithmetic — scaling mutations produce valid but visually different output` |

#### Metadata for new/touched disables

Every new or modified `Stryker disable` comment MUST include:

- Issue id (e.g., `Issue #1234`)
- Owner (e.g., `@handle` or owning team)
- Expiry date in ISO format (`YYYY-MM-DD`)

Example:
`// Stryker disable once Statement : Equivalent — downstream also throws; Issue #1234; Owner @console-team; Expires 2026-06-30`

#### What is NEVER acceptable

- Blanket `// Stryker disable all` on entire files without per-section justification
- Disabling mutations that ARE distinguishable by tests (write the test instead)
- "Too complex" or "Not important" as justification
- New/touched disable comments without issue, owner, and expiry metadata

### 6.4 NoCoverage vs Survived

| Status | Meaning | Fix |
|--------|---------|-----|
| **NoCoverage** | Mutant generated, but no test executed that code path | Write a test that exercises the path, OR disable with `NoCoverage` justification |
| **Survived** | A test ran but didn't catch the mutation | Strengthen the assertion (more specific value, tighter boundary), OR disable if equivalent |
| **Timeout** | Mutation caused an infinite loop or deadlock | Usually indicates the mutation broke a loop condition — may need a disable if the test can't detect it |

### 6.5 Disable debt audit

Run a periodic disable audit (at least once per release cycle):

- `rg -n "Stryker disable" src` and review all new/changed disable comments.
- Remove or test-fix expired disables; do not silently extend expiry.
- If extension is required, update issue status, owner acknowledgment, and new expiry.

---

## 7. Regression & Impact Analysis

When modifying existing code (bug fixes, enhancements, refactoring):

### 7.1 Before changing code

1. **Read the existing tests** for the code you're modifying.
2. **Run the existing tests** and confirm they pass.
3. **Identify the blast radius**: What other code calls this code? Use `Grep` to find
   all callers/consumers.

### 7.2 After changing code

1. **Update existing tests** if behavior intentionally changed.
2. **Add new tests** for the new behavior or fixed bug.
3. **Run the unit gate** — `dotnet test src/ -c Release --filter "Category!=Integration&Category!=Flaky"`
4. **Run the integration gate** (if affected) — `dotnet test src/ -c Release --filter "Category=Integration&Category!=Flaky"`
5. **Run Stryker** on the affected assembly to verify mutation score remains 100%.
6. **Check for breaking changes**: If a public API signature changed (parameters,
   return type, exception type), flag it explicitly in the commit message.

### 7.3 Side-effect awareness

When fixing bugs, verify these are not silently broken:

- **Rendering output**: Compare before/after for affected widgets
- **Terminal state**: Cursor visibility, color reset, scroll position
- **Resource lifecycle**: Dispose patterns, clone disposal, stream ownership
- **Thread safety**: Lock ordering, shared mutable state
- **Backward compatibility**: Extension method signatures, default parameter values

---

## 8. CI Gates & Completion Checklist

### 8.1 Required CI test tiers

Run and enforce gates in this order:

1. **Unit gate (blocking)** — `dotnet test src/ -c Release --filter "Category!=Integration&Category!=Flaky"`
2. **Integration gate (blocking)** — `dotnet test src/ -c Release --filter "Category=Integration&Category!=Flaky"`
3. **Mutation gate (blocking for affected assemblies)** — `dotnet stryker --config-file stryker-config.json`

### 8.1.1 Evidence artifacts (required)

For each blocking gate, attach evidence in the PR using either:

- CI job link showing pass/fail status
- Command output summary including gate type, TFM(s), and pass/fail result

### 8.2 Flaky quarantine policy (exception path)

A test may be skipped only under quarantine, and only temporarily.
Quarantined tests MUST include all metadata:

- `Trait("Category", "Flaky")`
- Linked issue id (e.g., `Issue #1234`)
- Owner (`@handle` or team)
- Expiry date in ISO format (`YYYY-MM-DD`)

Example:

```csharp
[Trait("Category", "Flaky")]
[Fact(Skip = "Flaky quarantine: Issue #1234; Owner @console-team; Expires 2026-04-15")]
public void Test_Name()
{
    ...
}
```

Rules:

- Quarantined tests do not run in blocking gates.
- Quarantined tests run in a non-blocking scheduled job.
- On or before expiry, either fix and unquarantine or renew with owner approval.

### 8.3 Completion checklist

Before marking any testing work as complete, verify:

- [ ] `dotnet build src/ -c Release` produces 0 warnings, 0 errors
- [ ] Unit gate passes on both `net8.0` and `net10.0` where supported
- [ ] Integration gate passes on both `net8.0` and `net10.0` where supported
- [ ] No new flaky quarantines were added without issue + owner + expiry
- [ ] Stryker mutation score is 100% for the affected assembly
- [ ] PR includes evidence artifacts for unit/integration/mutation gates (CI links or command summaries with TFM + result)
- [ ] Every new or touched `Stryker disable` comment has justification + issue + owner + expiry
- [ ] Snapshot baseline changes include explicit reviewer approval and PR delta note
- [ ] New tests are committed alongside the code they test
- [ ] Test class follows project naming and organization conventions
- [ ] Unit tests do not depend on file system, network, real clock, or execution order

---

## Appendix: Mutation Types Reference

| Stryker Mutation | What It Does | How to Kill It |
|-----------------|--------------|----------------|
| **Arithmetic** | `+` → `-`, `*` → `/`, etc. | Assert exact numeric result |
| **Equality** | `==` → `!=`, `<` → `<=` | Test the boundary value |
| **Boolean** | `true` → `false`, `&&` → `\|\|` | Test both branches |
| **String** | `"text"` → `""` | Assert string content (or disable if error message) |
| **Statement** | Removes entire statement | Verify side effect of the removed statement |
| **Block** | Removes entire `if` block | Verify the condition's effect on output |
| **NullCoalescing** | `a ?? b` → `a`, `a ?? b` → `b` | Test with `a = null` and `a = non-null` |
| **Negate** | `!condition` → `condition` | Test both true and false evaluation |
| **Initializer** | `= value` → `= default` | Assert initial state before any mutation |
| **Linq** | `.First()` → `.Last()`, etc. | Assert specific element/ordering |
