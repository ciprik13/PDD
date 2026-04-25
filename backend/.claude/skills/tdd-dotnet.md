---
name: tdd-dotnet
description: Use whenever implementing or changing backend behavior. Enforces the red-green-refactor loop with xUnit + FluentAssertions + WebApplicationFactory + Testcontainers. Tests come first, always.
---

# Skill: tdd-dotnet

The integration test is the contract. Write it first, watch it fail, make it pass, refactor while green. Repeat per slice — don't write all tests upfront for a multi-step ticket.

## When to invoke

- Implementing any controller action.
- Adding a use case (handler) in `Application`.
- Adding a domain rule.
- Changing existing behavior.

## The loop

### Step 1: Red
Write the failing test. Where it goes:

| Type of behavior | Test project | Test style |
|---|---|---|
| HTTP behavior, integration with auth/middleware/DB | `tests/AgriCure.Api.IntegrationTests/<Area>/` | `WebApplicationFactory<Program>` + `IClassFixture` |
| Pure use case (handler logic, validator) | `tests/AgriCure.Application.Tests/<Area>/` | xUnit only, mock interfaces |
| Domain invariant (entity/value object) | `tests/AgriCure.Domain.Tests/<Aggregate>/` | xUnit only, no mocks |

Test naming: `MethodOrAction_under_X_returns_Y` — underscores intentional, CA1707 already disabled in `tests/.editorconfig`.

Use FluentAssertions:

```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);
body.Should().BeEquivalentTo(expectedDto);
detections.Should().BeInDescendingOrder(d => d.Timestamp);
act.Should().ThrowExactly<DomainException>().WithMessage("*invalid*");
```

Run the test. It MUST fail (compile error, NotImplementedException, or assertion failure — all valid). If it passes by accident, the test is wrong — strengthen the assertion.

### Step 2: Green
Write the **minimum** code to pass. No bonus features. No "while we're in here" cleanup. If you find yourself writing untested code, stop — go back to red and add a test.

### Step 3: Refactor
Now that you're green, refactor freely:
- Extract methods.
- Rename for clarity.
- Move types into proper folders.
- Run tests after every refactor — they're your safety net.

### Step 4: Loop
Move to the next slice of behavior. Don't accumulate red tests.

## Integration test scaffolding

For PRs #2+, use `Testcontainers.PostgreSql` so each test class gets a clean DB. Sample fixture (after PR #2 lands):

```csharp
public sealed class AgriCureWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
            });
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public new async Task DisposeAsync() => await _postgres.DisposeAsync().AsTask();
}
```

For PR #1 (no DB yet), `WebApplicationFactory<Program>` works directly — see `RootEndpointTests.cs`.

## HARD RULES

- **No production code without a failing test first.** "It's just a tiny method" is not an exception.
- **One test, one behavior.** Don't pile assertions onto a single `[Fact]` for unrelated behaviors.
- **Don't share state between tests.** `IClassFixture` for per-class state, `ICollectionFixture` for cross-class — never `static`.
- **Don't mock what you don't own** unless you have to. Prefer integration tests against real Postgres via Testcontainers.
- **Never disable a test to ship a feature.** If the test reveals a bug, fix the bug. If the test is wrong, fix the test (and explain why in the commit).

## Acceptance checklist

- [ ] Failing test was written, run, and observed failing before the implementation.
- [ ] Test name describes the behavior in plain English (`returns_404_when_detection_missing`).
- [ ] FluentAssertions used for readable failure messages.
- [ ] Testcontainers Postgres for integration tests touching the DB (after PR #2).
- [ ] No `[Fact(Skip = ...)]` left in the codebase.
- [ ] `dotnet test` is green before commit.
