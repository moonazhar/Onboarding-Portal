# Back-end standards (C# / ASP.NET Core)

> **Status:** ✅ Reviewed by the lead (2026-10-28)
> **Last updated:** 2026-10-28
> **Purpose:** how back-end code is written, so every feature looks like it was written by one person.

Project boundaries: [architecture/overview.md §6](../architecture/overview.md#6-projects-and-their-boundaries). Security rules: [architecture/security.md](../architecture/security.md). Database rules: [architecture/database.md](../architecture/database.md).

---

## 1. Language and build

| Setting | Value |
|---|---|
| Target | .NET 10 (LTS), latest C# |
| Nullable reference types | Enabled everywhere |
| Warnings | Treated as errors |
| Analyzers | Built-in .NET analyzers, `AnalysisLevel=latest-recommended` |
| Formatting | `.editorconfig`; CI runs `dotnet format --verify-no-changes` |
| Implicit usings | Enabled |
| File-scoped namespaces | Yes |

Suppressing a warning needs a one-line justification next to it (`#pragma warning disable XYZ // reason`) and reviewer approval.

---

## 2. Naming

| Item | Convention | Example |
|---|---|---|
| Namespaces | Match folder path | `OnboardingPortal.Application.Features.Practices.CreatePractice` |
| Classes, records, methods, properties | PascalCase | `CreatePracticeHandler` |
| Interfaces | `I` prefix | `ICurrentUser` |
| Private fields | `_camelCase` | `_db` |
| Locals, parameters | camelCase | `practiceId` |
| Async methods | `Async` suffix | `HandleAsync` |
| Request / response DTOs | `<UseCase>Request` / `<UseCase>Response` | `CreatePracticeRequest` |
| Handlers | `<UseCase>Handler` | `CreatePracticeHandler` |
| Controllers | `<Module>Controller`, plural module | `PracticesController` |
| Permissions | `Permissions.<Module>.<Action>` | `Permissions.Practices.Create` |

Use full words. `practice`, not `prac`; `cancellationToken`, not `ct` in public signatures.

---

## 3. Controllers

- Inherit `ControllerBase`, `[ApiController]`, route under `/api/v1/<module>`.
- Every action has `[HasPermission(...)]` (or is on the anonymous list in [security.md §3](../architecture/security.md#anonymous-endpoints-the-complete-list)).
- An action: bind the request → call **one** handler → map the result to an HTTP response. Nothing else.
- Route parameters are `PublicId` (`Guid`), never `long`.
- Return typed results (`ActionResult<T>`) and declare response types for OpenAPI.

```csharp
[ApiController]
[Route("api/v1/practices")]
public sealed class PracticesController(CreatePracticeHandler create) : ControllerBase
{
    [HttpPost]
    [HasPermission(Permissions.Practices.Create)]
    public async Task<ActionResult<CreatePracticeResponse>> Create(
        CreatePracticeRequest request, CancellationToken cancellationToken)
    {
        var result = await create.HandleAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
```

---

## 4. Handlers

- One public method: `HandleAsync(request, cancellationToken)` returning `Result<T>`.
- Plain classes registered in DI. No mediator.
- Order inside a handler: **validate → load → check access → apply Domain rules → save → map to response.**
- Get the user from `ICurrentUser`, the time from `TimeProvider`.
- One use case = one `SaveChangesAsync`.
- Handlers never call other handlers. Share logic through Domain methods or a small Application service.

---

## 5. Request and response DTOs

- `sealed record` types in the feature folder.
- **Request DTOs contain only fields the user may set.** No `Id`, `CreatedBy`, `Status` (unless it is a requested transition), timestamps or calculated values.
- **Response DTOs are shaped per audience** when Internal and External users see different data (e.g. `PracticeDetailsInternal` / `PracticeDetailsExternal`).
- Never return or bind EF entities.
- Mapping is hand-written (a `ToResponse()` method or a `Select` projection).

---

## 6. Validation

- Built-in: DataAnnotations on request DTOs and `IValidatableObject` for cross-field rules; `[ApiController]` returns 400 ValidationProblemDetails automatically.
- Business rules (e.g. "cannot complete a blocked item") live in **Domain** and return a failed `Result`.
- Always validate on the server, whatever the front end does.
- Set maximum lengths on every string field (matches the database column).

---

## 7. Errors

| Situation | Response |
|---|---|
| Validation failure | 400 ValidationProblemDetails |
| Not signed in | 401 |
| No permission, or record not accessible | 403 — or **404** when revealing existence would leak information (e.g. another organisation's record) |
| Not found | 404 |
| Concurrency conflict (`RowVersion`) | 409 |
| Business rule violated | 422 ProblemDetails with a stable error code |
| Unexpected error | 500 ProblemDetails — generic message + `traceId`, details only in logs |

- Expected failures use `Result<T>`, **not exceptions**.
- Unexpected exceptions are handled once, by the global `IExceptionHandler`.
- Never return stack traces, exception messages or SQL errors to the client.
- Never swallow exceptions (`catch { }`).

---

## 8. Data access

- Handlers use `IAppDbContext` (implemented by the EF `DbContext` in Infrastructure).
- Read-only queries: `AsNoTracking()` and project with `Select` to the response DTO.
- Always pass `CancellationToken`.
- Avoid N+1: load related data in the query, not in a loop.
- Paginate every list endpoint (default and maximum page size set on the server).
- Raw SQL: Infrastructure only, parameterised, SQL Server 2019 syntax ([database.md §12](../architecture/database.md#12-raw-sql)).

---

## 9. Time

- Inject `TimeProvider`; call `GetUtcNow()`.
- Never `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now` in application code (an analyzer or architecture test enforces this).
- Convert to local time only for schedule rules and display, using the practice's time zone.

---

## 10. Logging

- `ILogger<T>` with **message templates**: `logger.LogInformation("Practice {PracticeId} created by {UserId}", practiceId, userId)`. Never string interpolation in log calls.
- Use the right level: `Debug` (diagnostics), `Information` (business events), `Warning` (recoverable problems), `Error` (failed operations), `Critical` (app cannot continue).
- Log **IDs, not values**. Never passwords, tokens, cookies, MFA codes, request bodies or personal data.
- Every log line carries the trace ID automatically — don't add your own correlation scheme.
- Logging is not auditing: business history goes to the audit trail.

---

## 11. Async and cancellation

- Async all the way: no `.Result`, `.Wait()` or `GetAwaiter().GetResult()`.
- Accept and pass `CancellationToken` on every async path.
- No `async void` (except event handlers, which we shouldn't have).
- No `Task.Run` in the Api to "make it faster" or to do background work — use the Worker.

---

## 12. Configuration

- Strongly typed options classes (`IOptions<T>`), validated at startup (`ValidateDataAnnotations().ValidateOnStart()`).
- `appsettings.json` holds non-secret defaults only. Secrets: user secrets locally, secret store on servers.
- New configuration key → update the docs per the [docs map](../README.md#2-docs-map).

---

## 13. Comments

- Code says *what*; comments say *why* — only when it isn't obvious.
- XML doc comments on public Application interfaces and Domain types others build on.
- No commented-out code. No `TODO` without an issue reference.

---

## 14. Do / Don't

**Do**
- Copy the reference feature's structure for every new feature.
- Keep methods short and classes focused; prefer `sealed`.
- Use `Result<T>` for expected failures.
- Use `record` for DTOs and value objects.
- Write the test first when fixing a bug.

**Don't**
- Don't put business rules in controllers, EF configurations or Infrastructure.
- Don't use static mutable state or service locators (`IServiceProvider` in handlers).
- Don't catch exceptions just to log and rethrow.
- Don't use `dynamic`, reflection-heavy tricks or `#if` for environment logic.
- Don't add packages (see [tooling-and-dependencies.md](tooling-and-dependencies.md)).
- Don't use `DateTime.Now`, string-built SQL, or `FromSqlRaw` with input.
