# Architecture overview

> **Status:** ✅ Reviewed by the lead (2026-10-28)
> **Last updated:** 2026-10-28
> **Purpose:** what the system is, how it is split, and the boundaries every part must respect.

---

## 1. What the system is — and is not

| The portal **does** | The portal **does not** |
|---|---|
| Track onboarding work per practice: plan, owner, actual start/finish, who did it | Perform configuration, run migrations or touch practice databases |
| Show a go-live calendar, task progress, dashboards | Replace Jira after go-live |
| Keep a tamper-evident audit trail of every change | Replace Microsoft Teams for client chat and file exchange |
| Import legacy data from CSV / `.xlsx` | Store patient data or practice database credentials |
| Notify people (in-app, email) through a queue | Send data in notifications — only links and identifiers |

Detailed module scope is defined from requirements (see [discussion.md §2](../discussion.md#2-requirements)). The **practice record is the root**: everything else links to a practice.

---

## 2. Users and their boundaries

| User type | Who | Sees |
|---|---|---|
| **Internal** | Our company's staff | Everything their permissions allow, across all organisations |
| **External** | Staff of the one partner company | Only records of their own organisation, and only fields meant for them |

- Users come from **two companies only** (as of 2026-10-28). Practice staff do not log in.
- Accounts are created by invitation. There is **no self-registration**.
- What a user may do is decided by **permissions**, grouped into **roles**. A user can hold several roles. Details: [security.md §6](security.md#6-authorization).

---

## 3. System context

```text
                    ┌──────────────────────────── On-prem network ───────────────────────────┐
                    │                                                                         │
 Browser ──HTTPS──► │  IIS ─► OnboardingPortal.Api  ──────────►  SQL Server 2019+             │
 (React SPA)        │         (serves the SPA + /api)              ▲                          │
                    │                                              │                          │
                    │         OnboardingPortal.Worker ─────────────┘                          │
                    │         (Windows Service) ──► SMTP relay (email)                        │
                    │                           ──► Teams webhook (later, needs outbound)     │
                    └─────────────────────────────────────────────────────────────────────────┘
```

- The browser talks **only** to the Api, on **one origin**. No CORS.
- Only the Api and the Worker talk to the database.
- External systems are reached **only by the Worker**, never inside a web request.

---

## 4. Deployables

| Deployable | Runs as | Does | Never does |
|---|---|---|---|
| **Api** | IIS, in-process | Serves the built React app and `/api/v1/*`; handles login; reads and writes data | Long-running work, sending email, calling external systems |
| **Worker** | Windows Service | Sends queued notifications (outbox), reminders, audit-chain verification, scheduled jobs | Serves HTTP, hosts UI |
| **Database** | SQL Server 2019+ | Stores data, audit trail, outbox queue | Runs business logic in stored procedures |

The same build output is promoted through every environment (Dev → QA → Staging → Production). Configuration differs; code does not.

---

## 5. Solution layout

```text
Onboarding-Portal/
├─ backend/                        # everything .NET; run dotnet commands from here
│  ├─ OnboardingPortal.slnx
│  ├─ global.json  nuget.config  Directory.Build.props  Directory.Packages.props
│  ├─ .config/dotnet-tools.json
│  ├─ src/
│  │  ├─ OnboardingPortal.Domain/
│  │  ├─ OnboardingPortal.Application/
│  │  ├─ OnboardingPortal.Infrastructure/
│  │  │   ├─ Persistence/      # DbContext, EF configurations, migrations, audit
│  │  │   ├─ Identity/         # ASP.NET Core Identity setup, stores
│  │  │   ├─ Security/         # IP rules, Data Protection, audit-chain keys
│  │  │   ├─ Notifications/    # outbox writer, email sender
│  │  │   └─ Import/           # CSV / xlsx readers
│  │  ├─ OnboardingPortal.Api/
│  │  └─ OnboardingPortal.Worker/
│  └─ tests/
│     └─ OnboardingPortal.Tests/   # Domain/ Application/ Architecture/ Integration/
├─ frontend/                       # React + TypeScript + Vite; run npm commands from here
├─ scripts/                        # PowerShell, used locally and in CI (planned)
└─ docs/
```

The folders under `Infrastructure/` are the target; each is created when its first code arrives. (Layout: D-28.)

Start with **one** Infrastructure project split by folders. Split only when there is a concrete reason, recorded in an ADR.

---

## 6. Projects and their boundaries

| Project | Responsibility | May reference | Must never reference | Contains | Never contains |
|---|---|---|---|---|---|
| **Domain** | Business rules | Nothing (only the .NET base library) | Any other project, any NuGet package | Entities, value objects, enums, status state machine, domain events, `Result<T>`, guards | EF Core, ASP.NET, `HttpContext`, logging, I/O, `DateTime.Now` |
| **Application** | Use cases | Domain; `Microsoft.EntityFrameworkCore` core package only (for `DbSet<T>` and async LINQ — see note) | Infrastructure, Api, Worker, the SQL Server EF provider, ASP.NET | Handlers, request/response DTOs, validation, permission constants, interfaces (`IAppDbContext`, `ICurrentUser`, `IEmailQueue`…) | Raw SQL, HTTP, file system, provider-specific EF code |
| **Infrastructure** | Talks to the outside world | Application, Domain | Api, Worker | `DbContext`, migrations, Identity stores, audit interceptor and chain, email sender, file import readers | Controllers, business rules, endpoint logic |
| **Api** | HTTP host, composition root | Application, Infrastructure, Domain | Worker | Controllers, middleware, auth configuration, DI registration, `Program.cs`, built SPA in `wwwroot` | Business rules, SQL, direct `DbContext` use in controllers |
| **Worker** | Background host, composition root | Application, Infrastructure, Domain | Api | `BackgroundService` jobs, DI registration | HTTP endpoints, UI, business rules |
| **Tests** | All .NET tests | Every project above | — | Unit, architecture, integration tests | Production code |

### Dependency rule

```text
   Api ────┐          ┌──── Worker
           ▼          ▼
        Infrastructure
               │
               ▼
          Application
               │
               ▼
            Domain
```

Arrows point to what a project may use. Nothing points up.

**Note on EF Core in Application:** handlers query through `IAppDbContext`, which exposes `DbSet<T>`. That needs the EF Core *core* package — a deliberate trade-off that avoids a generic repository layer. The SQL Server provider, migrations and raw SQL stay in Infrastructure.

**Enforced by:**
1. Project references — Domain physically cannot use EF Core.
2. Architecture tests in `backend/tests/OnboardingPortal.Tests/Architecture/` (reflection, no package) — the build fails if a type uses a forbidden namespace.

---

## 7. Request flow (Api)

Every request passes the same pipeline. The order is defined once, in `Program.cs`.

1. **Forwarded headers** — only from known proxies.
2. **Exception handler** — any error becomes a ProblemDetails response with `traceId`; no stack traces.
3. **HTTPS + HSTS**, **security headers** (CSP and others — [security.md §9](security.md#9-http-security-headers)).
4. **IP filter** — allowlist and automatic blocks.
5. **Rate limiting** — per IP and per user; strict on login.
6. **Authentication** — Identity cookie.
7. **Authorization** — fallback policy: authenticated user required; endpoint permission checked.
8. **Antiforgery** — on every state-changing request.
9. **Controller action** — binds the request DTO, calls **one handler**.
10. **Handler** — validates, checks record access, applies Domain rules, saves through `IAppDbContext`.
11. **SaveChanges** — data change + audit event + outbox message in **one transaction**.
12. **Response DTO** — shaped for the caller's audience.

---

## 8. Cross-cutting rules

| Concern | Rule | Details |
|---|---|---|
| Identifiers | `bigint` key internally; GUID `PublicId` outside | [database.md §4](database.md#4-keys-and-identifiers) |
| Time | UTC everywhere; `TimeProvider` injected; local time only for display and schedule rules | [database.md §6](database.md#6-time) |
| Audit | Explicit business audit events + EF interceptor safety net, hash-chained | [database.md §2](database.md#2-audit-trail-integrity-on-every-version) |
| Deletion | Archive only | [database.md §7](database.md#7-archiving-instead-of-deleting) |
| Errors | ProblemDetails + `traceId`; UI shows the reference | [backend.md](../standards/backend.md#7-errors) |
| Logging | Structured `ILogger`, OpenTelemetry export | [backend.md](../standards/backend.md#10-logging) |
| Notifications | Written to the outbox table in the same transaction; the Worker sends them | §8.1 |
| Background work | Worker only; never `Task.Run` or timers in the Api | §8.1 |

### 8.1 Worker jobs

| Job | Does | Status |
|---|---|---|
| Outbox dispatcher | Sends queued notifications, retries with back-off, flags repeated failures | 🟡 Proposed (D-11a) |
| Audit-chain verifier | Recomputes the audit hash chain, publishes the digest, alerts on mismatch | 🟡 Proposed (D-17) |
| Reminders | Due / overdue reminders | Planned |

Add a row here whenever a job is added or removed.

---

## 9. Feature pattern

All modules share one idea: **an item with a plan, an owner and an actual outcome** (a *tracked item*). Each module adds its own fields and views on top of one engine.

Every feature has the same shape in every layer:

```text
Application/Features/<Module>/<UseCase>/
    <UseCase>Request.cs        # input DTO — only fields the user may set
    <UseCase>Response.cs       # output DTO — per audience if needed
    <UseCase>Handler.cs        # the use case
Api/Controllers/<Module>Controller.cs      # one action → one handler
frontend/src/features/<module>/   # pages, components, queries
backend/tests/OnboardingPortal.Tests/<Layer>/<Module>/
```

**Reference feature:** the first feature built end-to-end — **Practices** (create, list, view the practice record) — is the pattern every later feature copies. Until it is merged, no other module starts.

---

## 10. Do / Don't

**Do**
- Keep business rules in Domain, use cases in Application, plumbing in Infrastructure.
- Add an interface in Application when a handler needs something from outside; implement it in Infrastructure.
- Keep each controller action to binding + one handler call + result mapping.
- Keep the two hosts (Api, Worker) thin: they only wire things up.

**Don't**
- Don't create "Utilities", "Common", "Shared" or "Helpers" projects.
- Don't wrap EF Core in a generic repository or unit of work.
- Don't add microservices, message brokers, event sourcing or separate read databases.
- Don't call external systems (SMTP, Teams, HTTP APIs) from the Api.
- Don't reference Api from Worker or Worker from Api.
- Don't put logic in stored procedures or triggers.
