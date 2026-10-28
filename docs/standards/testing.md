# Testing standards

> **Status:** ✅ Reviewed by the lead (2026-10-28) (project layout ✅ Decided — D-19)
> **Last updated:** 2026-10-28
> **Purpose:** what must be tested, where tests live, and how they run without Docker.

---

## 1. Layout

**One** .NET test project (D-19), organised by folders that mirror the source:

```text
backend/tests/OnboardingPortal.Tests/
├─ Domain/          # pure unit tests — no database, no framework
├─ Application/     # handlers with hand-written fakes
├─ Architecture/    # project boundaries and conventions (reflection)
├─ Integration/     # API + real SQL Server, tagged [Trait("Category", "Integration")]
└─ TestSupport/     # fakes, builders, the test database fixture
```

Front-end tests live inside the web app: `*.test.ts(x)` next to the code, Playwright tests in `frontend/e2e/` (planned).

**Test runner:** the .NET tests run on **VSTest** (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`), so `dotnet test`, `--filter` and `coverlet.collector` work as written here. xUnit v3 4.x defaults to Microsoft.Testing.Platform, so the test project sets `IsTestingPlatformApplication=false` (D-29).

**CA1707 is off in `backend/tests/`** (in `.editorconfig`), because test names use underscores (§5).

---

## 2. Test layers

| Layer | Tool | Covers | Database |
|---|---|---|---|
| Domain | xUnit | State machine, completion rules, late/on-time, time zones | No |
| Application | xUnit + hand-written fakes | Handlers, validation, access checks | No |
| Architecture / conventions | xUnit + reflection | See §4 | No |
| Integration | xUnit + `WebApplicationFactory` | API + auth + database + audit together | Local SQL Server |
| Authorization matrix | Generated integration tests | Every endpoint × every role → expected status | Local SQL Server |
| Front-end unit | Vitest + Testing Library | Components, hooks | No |
| End-to-end | Playwright | Login + MFA, main user journeys | Local SQL Server |
| Security | OWASP ZAP (staging), external pen test | DAST | Staging |

---

## 3. Integration tests without Docker

- They run against the developer's **locally installed SQL Server 2019+** (LocalDB is fine).
- Each test run **creates a uniquely named database** (`OnboardingPortal_Test_<guid>`), applies migrations, and **drops it** at the end.
- The connection string (server part only) comes from an environment variable or user secrets — never from a committed file.
- Tests clean up their own data or use a fresh scope; tests must not depend on run order.
- Default local run skips them: `dotnet test --filter "Category!=Integration"`. **CI runs everything**, on SQL Server 2019.

---

## 4. Convention tests (self-enforcing)

These cover every future endpoint and entity automatically. ✅ = exists (`Architecture/ProjectBoundaryTests.cs`); the rest are added with the code they check.

- ✅ Project references follow the dependency rule, and every project under `backend/src` has a rule.
- ✅ Domain references no packages; Application references only `Microsoft.EntityFrameworkCore`.
- Domain uses no EF Core, ASP.NET or I/O types.
- Every controller action has `[HasPermission]`, or is on the anonymous list.
- Every endpoint appears in the authorization matrix.
- No controller action takes or returns an EF entity.
- No route parameter is an integer ID.
- Every business entity has `PublicId`, `RowVersion` and archive columns.
- No use of `DateTime.Now` / `DateTime.UtcNow` in production code.
- External-audience response DTOs contain no internal-only fields.

---

## 5. Naming

- Test class: `<ClassUnderTest>Tests` — `CreatePracticeHandlerTests`.
- Test method: `<Method>_<Scenario>_<Expected>` — `HandleAsync_WhenUserLacksAccess_ReturnsForbidden`.
- Arrange / Act / Assert, separated by blank lines.

---

## 6. What every PR needs

- Tests for new behaviour and for every changed rule.
- **Bug fix = failing test first**, then the fix.
- For a new endpoint: a happy-path integration test, a 403 test, and an entry in the authorization matrix.
- For a write: an assertion that the audit event was written.
- About **80% coverage on the changed lines** (diff coverage), not on the whole codebase.
- Mutation testing (Stryker) runs occasionally to catch tests that assert nothing.

---

## 7. Do / Don't

**Do**
- Test behaviour through public methods, not private details.
- Use builders in `TestSupport/` for test data; keep tests short and readable.
- Inject `FakeTimeProvider`-style fakes for time (hand-written or `Microsoft.Extensions.TimeProvider.Testing` if approved).
- Assert specific results — status code **and** body, audit row **and** its actor.

**Don't**
- Don't use mocking libraries or the EF InMemory provider (see [banned list](tooling-and-dependencies.md#7-banned)).
- Don't use Docker or Testcontainers.
- Don't share a database between test runs or point tests at a dev or shared database.
- Don't skip, comment out or weaken a failing test to get green CI.
- Don't use `Thread.Sleep` or real delays — control time with the fake time provider.
- Don't write tests that only check "no exception was thrown".
