# Onboarding Portal — Discussion & Decisions

> **Status:** Analysis phase — no code written yet.
> **Last updated:** 2026-10-28
> **Purpose of this file:** the single record of what we discussed, what we decided, and why. Update the *Decision log* (section 17) whenever something is agreed or changed.
> **Note (2026-10-28):** this file is background and reasoning. The current rules live in `docs/architecture/` and `docs/standards/`, and the decision register in `docs/foundation-decisions.md`. Where they differ, those files win.

---

## Table of contents

1. [Background — why this app exists](#1-background--why-this-app-exists)
2. [Requirements](#2-requirements)
3. [Original ask vs. meeting — comparison](#3-original-ask-vs-meeting--comparison)
4. [Open questions](#4-open-questions)
5. [Architecture](#5-architecture)
6. [Domain model](#6-domain-model)
7. [Dependency policy — minimal packages](#7-dependency-policy--minimal-packages)
8. [Backend approach](#8-backend-approach)
9. [Database](#9-database)
10. [Security](#10-security)
11. [Logging & observability](#11-logging--observability)
12. [Front-end](#12-front-end)
13. [Testing strategy](#13-testing-strategy)
14. [Team practices — AI-assisted and manual development](#14-team-practices--ai-assisted-and-manual-development)
15. [Git & CI/CD](#15-git--cicd)
16. [Timeline](#16-timeline)
17. [Decision log](#17-decision-log)
18. [Avoid list](#18-avoid-list)

---

## 1. Background — why this app exists

Source: meeting recording `Call with Azhar and 5 others.vtt` (repo root), led by Dr Jawad Ahmed. The transcript is auto-transcribed Urdu in Hindi script with no speaker labels; some names are garbled (e.g. "इंडसइंड" ≈ *Indici*, "वालेंशिया" ≈ *Valentia*).

- A new Valentia / Indici team will oversee practice migrations in **New Zealand**. Our team keeps doing the work, but the client's oversight increases.
- Today, database people run scripts on production data manually and nothing proves afterwards what was done. The client's position: *the production data is no longer yours — give us full accountability: what was done, how, why, when, and by whom.*
- Without visibility the client will not continue with us. With visibility, they will.
- Volume: from next year **at least one practice goes live every week**. Later: **Ireland** (~100 practices) and **Australia**.
- Currently tracked in an **Excel sheet** (go-live calendar, maintained by Saeed / Imran Rasheed) and **Microsoft Teams Forms** (database logistics).

**The audit trail is the product.** Consequences:

1. Every write creates an immutable activity record. *Who* = the authenticated user (never a value from the request body). *When* = the server's UTC clock.
2. Nothing is hard-deleted — only archived.
3. History must be tamper-evident.
4. It is a small line-of-business app (tens to low hundreds of users) → **modular monolith**, not microservices.

---

## 2. Requirements

### 2.1 The five questions (for every tracked item)

1. **What** has to be done?
2. **When** does it have to be done?
3. **Who** has to do it?
4. **Who** did it?
5. **When** was it finished (and when was it started)?

### 2.2 Modules

| # | Module | What it does | Requirement source |
|---|---|---|---|
| 1 | **Go-live calendar** | Replaces the yearly Excel schedule (which practice goes live which week/Friday). Includes **resource management** (who is allocated where). Published online. | Saeed, Imran Rasheed |
| 2 | **Configuration checklist** | Per-practice list of configuration items (e.g. 40 documents, 20 forms, templates, mail-merges). **Imported from Excel.** People tick items off; their name and the time appear. Supports the client's ISO process. | Configuration / data-entry team |
| 3 | **Database logistics** | Per practice: planned date/time the practice provides its database (1st and **2nd extract**), when it was downloaded, who downloaded it, and whether it was **early / on time / late**. Shown on the calendar. Currently in Microsoft Teams Forms (copy those fields). | Teams Forms, DB team |
| 4 | **Database migration tracking** | Every migration step (conversion, merging, profile, etc.) with planned / started / completed / by whom. An existing table already logs **which stored procedure ran and when** — candidate for automatic capture. | Umar Rizwan |

Also from the original ask: **which modules are required** for each practice and by when (tracked, not configured).

### 2.3 Scope boundaries

- **Tracking and visibility only.** Configuration work (e.g. enabling modules) is *not* performed through the portal.
- **Scope ends at go-live.** Post-go-live issues are handled in **Jira**.
- **One system** containing calendar and all modules.
- **External users:** the client (Valentia) must be able to see progress. The app is not internal-only.
- **Time zones:** work happens at night in Pakistan for NZ practices. All times stored in UTC; displayed in the viewer's / practice's time zone; "late / on time" calculated against the practice's local schedule.

### 2.4 Non-functional requirements

- Production-grade security from the first line of code.
- Data protected in transit (and the requested front-end encryption — see §10.1).
- Strong login, IP restriction and IP blocking.
- OWASP Top 10 and beyond.
- Logging good enough to debug production.
- Everything in **git**.
- Minimal third-party dependencies (see §7).

### 2.5 Delivery expectations from the meeting

- 1–2 days of analysis, then report problems and a plan.
- **Running by end of November 2026**, maturing in December, then handed to an owner (possibly the client).
- Daily updates to Jahangir.
- People to involve: Umar Rizwan, Saeed, Imran Rasheed (calendar only), Bushra, Ehsanullah, configuration team.

---

## 3. Original ask vs. meeting — comparison

| Topic | Original ask | Meeting (Dr Jawad) | Verdict |
|---|---|---|---|
| Who did what and when | ✅ | ✅ (the five questions) | Same |
| Which practice | ✅ | ✅ | Same |
| Who is doing / who did DB migration | ✅ | ✅ — but step-by-step per migration type | Same, more granular |
| When the whole process completed | ✅ | ✅ (until go-live) | Same |
| Required modules | ✅ | Only tracked; enabling modules is **not** done in the portal | Track only |
| Go-live calendar + resource management | ❌ | ✅ first deliverable | **Added** |
| Configuration checklist imported from Excel | ❌ | ✅ | **Added** |
| DB handover timing (1st/2nd extract, late/on time) | ❌ | ✅ | **Added** |
| Auto-capture from stored-procedure log table | ❌ | Mentioned | **Added (to investigate)** |
| Client (external) visibility | ❌ | ✅ — the whole point | **Added — major architectural impact** |
| Security (encryption, IP rules, OWASP) | ✅ | Not mentioned | Ours — even more important with external users |
| Deadline | — | End of November 2026 | **Added** |

---

## 4. Open questions

1. **Hosting:** on-prem IIS / Windows Server, Azure, or other cloud? (Decides WAF, secret vault, managed identity.)
2. **Client access model:** how do Valentia users log in? Our accounts for them, or their own identity provider? What can each client role see?
3. **Our identity provider:** Microsoft 365 / Entra ID, on-prem Active Directory, or app-managed accounts?
4. **Compliance:** HIPAA / NZ Health Information Privacy Code / ISO requirements? Rule: the portal stores **metadata only, never patient data**.
5. **Front-end encryption:** who requires it, and what exactly does the requirement say? (See §10.1.)
6. **Git host and CI:** GitHub, Azure DevOps, or GitLab?
7. **Excel import format:** can users export CSV, or must we read `.xlsx`? (Affects one dependency — see §7.)
8. **Stored-procedure log table:** where it lives, what it contains, and whether we can read it.
9. **Network:** fixed office IPs, VPN, or remote with changing IPs? Where do client users connect from?
10. **Notifications:** email, Microsoft Teams, or none for v1?

---

## 5. Architecture

### 5.1 Shape

A **modular monolith** with Clean Architecture layering: one API, one database, one React front-end, one repository.

Rejected: microservices, event sourcing, separate read databases — over-engineering for this size.

### 5.2 Solution layout

> Superseded by `docs/architecture/overview.md` §5 (adds the Worker project). Kept for history.

```text
OnboardingPortal/
├─ OnboardingPortal.slnx              # .NET 10 XML solution format
├─ Directory.Build.props              # nullable, warnings-as-errors, analyzers
├─ Directory.Packages.props           # central package versions (CODEOWNERS-protected)
├─ global.json                        # pin the SDK
├─ .editorconfig  .gitignore  .gitattributes
├─ AGENTS.md                          # rules for AI tools and humans (see §14)
├─ CLAUDE.md                          # imports AGENTS.md
├─ src/
│  ├─ OnboardingPortal.Domain/          # entities, value objects, enums, state machine, domain events — ZERO dependencies
│  ├─ OnboardingPortal.Application/     # use cases, DTOs, validation, interfaces (ICurrentUser, IEncryptionService…)
│  ├─ OnboardingPortal.Infrastructure/
│  │   ├─ Persistence/                  # DbContext, EF configurations, migrations, audit interceptor
│  │   ├─ Security/                     # encryption implementation, key handling, IP rule store
│  │   ├─ Identity/
│  │   └─ Import/                       # Excel/CSV import
│  └─ OnboardingPortal.Api/             # host: endpoints, middleware, auth config, logging, DI composition root
├─ tests/
│  └─ OnboardingPortal.Tests/           # ONE project (D-19): Domain/, Application/, Architecture/, Integration/ (locally installed SQL Server, throwaway test DB per run, trait-tagged)
├─ web/
│  └─ onboarding-portal-web/            # React + TypeScript + Vite
├─ docs/
│  ├─ discussion.md                     # this file
│  ├─ adr/                              # Architecture Decision Records
│  └─ threat-model.md
└─ .github/ (or pipelines/)
```

### 5.3 Dependency rule

```text
Api ──► Application ──► Domain
 │            ▲
 └──► Infrastructure (implements Application interfaces)
```

- Domain depends on nothing.
- Application depends only on Domain.
- Infrastructure implements Application's interfaces.
- Api composes everything (composition root).
- Enforced by **project references** (Domain physically cannot reference EF Core) plus a few reflection-based tests — no extra package needed.

### 5.4 Answers to the original structure questions

| Question | Answer |
|---|---|
| One global `.sln` with multiple projects? | **Yes.** One solution (`.slnx`), one repository (monorepo with `src/`, `tests/`, `web/`). |
| Separate DAL? | **Yes**, as `Infrastructure/Persistence`. |
| Separate Encryption project? | **No** (unless reused by other solutions — then a NuGet package in its own repo). Application defines `IEncryptionService`; Infrastructure implements it. |
| Separate Utilities project (incl. logging)? | **No.** A "Utilities/Common" project becomes a junk drawer everything depends on. Logging is already abstracted by `ILogger<T>`; configure it in Api. Put shared primitives (`Result<T>`, guards) in Domain. |
| Domain / Application / Infrastructure? | **Yes.** |
| Separate test projects? | **No** (decided 2026-10-28, D-19) — one `OnboardingPortal.Tests` project, organised by folders; integration tests tagged with a trait. |

Start with **one** Infrastructure project split by folders. Splitting later is cheap; merging later is painful.

---

## 6. Domain model

### 6.1 Key insight — one tracking engine, four modules

All four modules are the same thing underneath: **an item with a plan, an owner, and an actual outcome**. Build one generic *tracked item* engine (planned vs. actual, assignee, status, audit) and let each module add its own fields and views. Less code, consistent behaviour, faster delivery.

### 6.2 Entities

| Entity | Purpose |
|---|---|
| **Organisation** | Our company, the client (Valentia), later others. Scopes what external users can see. |
| **Region** | NZ / Ireland / Australia, with default time zone. |
| **Practice** | The practice being onboarded (region, time zone, identifiers). |
| **Onboarding** | One onboarding run per practice: target go-live date, status, created by/at, completed by/at (set automatically). |
| **CalendarEntry** | Go-live dates, DB extract dates, other milestones — the calendar is a *view* over dated items. |
| **ResourceAllocation** | Person ↔ onboarding ↔ role ↔ date range. |
| **Module / OnboardingModule** | Module catalogue; which modules a practice needs and by when. |
| **ChecklistTemplate → TemplateItem** | Versioned standard lists (configuration items, migration steps). |
| **TrackedItem** | The generic engine: category (Configuration / DbLogistics / Migration / Other), assignee, planned start/due, actual start/finish, started by, completed by, verified by, status, notes, evidence. Instantiated from a **snapshot** of a template version. |
| **DbHandover** | Extends a tracked item: extract number (1st/2nd), planned date-time (practice local), received at, downloaded by/at, variance (early / on time / late). |
| **MigrationStep** | Extends a tracked item: migration type (conversion, merging, profile…), optional link to stored-procedure log records. |
| **Comment / Attachment** | Discussion and evidence. |
| **ActivityLog** | Append-only: actor, action, entity, old/new values, UTC time, IP, correlation ID. |
| **User / Role** | See §10.3. |

### 6.3 Rules

- **Status state machine:** `NotStarted → InProgress → (Blocked) → Completed → Verified`. Reopening requires a reason (logged).
- **Template snapshotting:** changing a template never rewrites past onboardings.
- **Completion:** `Onboarding.CompletedAt` is set by the system when the last required item completes.
- **Time:** stored in UTC; local times converted using the practice's time zone.

---

## 7. Dependency policy — minimal packages

Every package is code we did not write, cannot fully review, and must patch forever. Supply-chain attacks are now an OWASP Top 10 category, and 2025 saw npm malware campaigns that stole credentials from developer machines and CI through install scripts. **Fewer packages = smaller attack surface.**

### 7.1 Rules

1. **Order of preference:**
   1. Built into .NET / the browser.
   2. Official Microsoft / framework-owner packages.
   3. A widely used, actively maintained third-party package.
   4. Write it ourselves.
2. **Never hand-roll security primitives** — cryptography, password hashing, authentication protocols, HTML sanitising, file-format parsing. Minimising packages must not mean writing our own crypto.
3. **Runtime packages** (shipped to production) get the strictest bar. **Dev-only tools** are lower risk but still a risk (they run on developer machines and CI).
4. **Adding a runtime package requires:**
   - An entry in the Decision log / an ADR.
   - Approval from CODEOWNERS (`Directory.Packages.props`, `package.json` are protected).
   - A check of maintenance activity, download count, licence, transitive dependency count and CVE history.
5. **Lock and pin:**
   - .NET: Central Package Management with exact versions; `packages.lock.json` with locked restore in CI.
   - npm: committed `package-lock.json`; `npm ci` in CI; `.npmrc` with `save-exact=true` and `ignore-scripts=true`.
   - Automated update PRs (Dependabot / Renovate) with a **cool-down** (don't adopt versions younger than ~7 days — malicious releases are usually caught within days).
6. **Monthly review:** remove unused packages; check vulnerability reports.

### 7.2 Backend — runtime (target list)

| Need | Choice | Publisher |
|---|---|---|
| Database access | `Microsoft.EntityFrameworkCore.SqlServer` | Microsoft |
| Login | ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) **or** `Microsoft.Identity.Web` if we use Entra ID | Microsoft |
| API description (dev/staging only) | `Microsoft.AspNetCore.OpenApi` | Microsoft |
| Telemetry / log export | OpenTelemetry (`OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`) | CNCF (official) |
| Excel import — **only if CSV is not acceptable** | `DocumentFormat.OpenXml` | Microsoft |

**Built in — no package needed:** rate limiting, Data Protection, antiforgery, CORS, HSTS, forwarded headers, health checks, ProblemDetails / `IExceptionHandler`, validation (DataAnnotations + .NET 10 built-in validation), `BackgroundService` for reminders, `TimeProvider`, JSON console logging, `System.Security.Cryptography` (AES-GCM, ECDH, HKDF).

**Removed from the earlier proposal** (and why):

| Dropped | Replaced by |
|---|---|
| FluentValidation | Built-in validation |
| Mapperly / AutoMapper | Hand-written mapping |
| MediatR / any mediator | Plain handler classes |
| Hangfire / Quartz.NET | `BackgroundService` (reminders are simple) |
| Asp.Versioning | `/api/v1` route group |
| Scalar / Swagger UI | OpenAPI document only, non-production |
| Serilog | Built-in `ILogger` + OpenTelemetry export (Seq accepts OTLP) |
| NetArchTest | Project references + reflection tests |

### 7.3 Front-end — runtime (target list)

| Need | Choice |
|---|---|
| Core | `react`, `react-dom` |
| Routing | `react-router` |
| Server state / caching | `@tanstack/react-query` (removes a lot of hand-written, bug-prone fetch code) |
| UI components | **One** component library (Mantine or MUI — pick the one with fewer dependencies) |
| Calendar view | The UI library's date components; add **FullCalendar** (MIT core) only if a month/week scheduling view is required |
| Schema validation (optional) | `zod` (no dependencies) |

**Not used:** axios (use `fetch`), lodash, moment, state-management libraries, form libraries unless forms prove painful.

### 7.4 Dev-only and CI-only tools (not shipped)

- **.NET tests:** xUnit, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, `Microsoft.AspNetCore.Mvc.Testing`. No Testcontainers: there is no Docker (foundation-decisions D-23); integration tests use the locally installed SQL Server. Use xUnit's built-in `Assert` and hand-written fakes — no mocking or assertion libraries.
- **Front-end:** Vite, TypeScript, ESLint, Prettier, Vitest, Testing Library, Playwright, `openapi-typescript` (generates types only; zero runtime code).
- **CI services:** CodeQL, gitleaks, OWASP ZAP, optionally SonarQube; Stryker (mutation testing) run occasionally.

---

## 8. Backend approach

| Concern | Use | Avoid |
|---|---|---|
| Runtime | .NET 10 LTS, nullable enabled | Non-LTS versions in production |
| API style | Minimal APIs with route groups **or** controllers — pick one, stay consistent | Mixing styles |
| Data access | EF Core `DbContext` behind an Application interface | Generic Repository + Unit of Work wrappers over EF |
| Raw SQL | `FromSqlInterpolated` (parameterised) | `FromSqlRaw` with string concatenation |
| Use cases | Plain handler classes | Mediator libraries |
| Mapping | Hand-written | AutoMapper |
| Validation | Built-in, server-side, always | Trusting front-end validation |
| Errors | `IExceptionHandler` → ProblemDetails with `traceId` | Stack traces in responses |
| API docs | Built-in OpenAPI, non-production or behind auth | Public Swagger in production |
| Background work | `BackgroundService` | Timers in controllers |
| Time | Injected `TimeProvider`, UTC | `DateTime.Now` |
| Versioning | `/api/v1` route group | Breaking changes without a version |

**Audit trail — two mechanisms, same transaction as the change:**

1. **EF Core `SaveChanges` interceptor** captures every insert/update automatically (entity, old/new values, user, time, IP, correlation ID). Nobody can forget to audit.
2. **Domain events** add business meaning (e.g. `StepCompleted("Data Migration", by: user)`) for the human-readable timeline.

---

## 9. Database

- **SQL Server 2019 or later** (decided 2026-10-28, foundation-decisions D-24 — 2022 is optional, not required). Tamper evidence for the audit trail comes from a keyed hash chain + external digests that work on every version; on 2022+ the audit table can **also** be an append-only ledger table. Full design: `docs/architecture/database.md` §2.
- **Temporal (system-versioned) tables** on main entities for "what did this row look like on date X".
- **Keys:** `bigint IDENTITY` clustered primary key internally + `uniqueidentifier PublicId` exposed by the API. (Sequential IDs in URLs invite IDOR; random GUIDs as clustered keys fragment indexes; SQL Server sorts GUIDs in an unusual byte order, so even v7 GUIDs are not sequential there.)
- **Concurrency:** `rowversion` column for optimistic concurrency.
- **Time:** `datetime2` / `datetimeoffset`, UTC.
- **Soft delete:** `IsArchived` + EF global query filter. No hard deletes.
- **Least privilege — three logins:**
  - App login: read/write on its schema, `DENY UPDATE, DELETE` on audit tables.
  - Migration login: DDL rights, pipeline only.
  - Reporting login: read-only.
  - Never `sa` or `db_owner`. Prefer Windows auth / managed identity over SQL passwords.
- **Encryption:**
  - Connection: `Encrypt=True; TrustServerCertificate=False` (or `Strict`).
  - **TDE** for data files and backups.
  - Column-level encryption only where genuinely needed.
- **Migrations:** EF Core code-first, **never `Database.Migrate()` at startup in production**. Generate idempotent scripts (`dotnet ef migrations script --idempotent`) or migration bundles, review them in the PR, and apply them from the pipeline with the migration login.
- **Never store practice database credentials or patient data** in this portal. If a step needs a credential, store a reference to a vault secret.

---

## 10. Security

### 10.1 Reality check on "front-end encryption"

> **Decision 2026-10-28: dropped** (`docs/foundation-decisions.md` D-08). Kept below for the reasoning. The "never" list and the browser-storage rule at the end of this section still apply.

What protects data between browser and API is **TLS** (TLS 1.2+/1.3, HSTS with preload, strong ciphers). Extra encryption inside the browser on top of TLS:

- **Does not protect against:** XSS (attacker script runs in the page and can use the key), someone who breaks TLS (they can replace the JavaScript, encryption included), the user (DevTools always shows decrypted data), a compromised server.
- **Helps only with:** intermediaries that terminate TLS and see plaintext (corporate SSL inspection, a load balancer or WAF that logs bodies).
- **Costs:** the WAF can no longer inspect payloads; debugging gets much harder; more crypto code = more bugs.

**Recommendation:**

1. Find out where the requirement comes from.
2. Fix the real issues first: TLS end-to-end (including load balancer → server), nothing sensitive in browser storage, column encryption at rest.
3. **If still mandated**, implement as a transparent layer (a `fetch` wrapper on the front-end + ASP.NET middleware), so feature code never touches it:
   - ECDH P-256 key agreement at login → HKDF → AES-256-GCM session key, using the **Web Crypto API** and .NET `System.Security.Cryptography` (no third-party crypto).
   - Non-extractable `CryptoKey` kept in memory only.
   - Random 96-bit IV per message; timestamp + nonce + request path in the additional authenticated data to block replays.
   - Switchable per environment.
   - **Never:** `crypto-js`, keys in JavaScript, AES-CBC without a MAC, RSA-encrypting whole payloads, custom algorithms, base64 as "encryption".

"Encryption on the front-end" also means: **no tokens or sensitive data in `localStorage` / `sessionStorage`.** Keep state in memory.

### 10.2 Authentication

- **BFF pattern with cookies:** React served from the **same origin** as the API; authentication via **HttpOnly, Secure, SameSite=Strict cookie**. JavaScript never sees a token; no CORS needed.
- **Avoid:** JWTs in localStorage, long-lived JWTs without revocation, implicit flow.
- **Identity provider (decide in §4):**
  - Staff on Microsoft 365 → **Entra ID via OpenID Connect** (SSO, MFA, conditional access incl. IP-based named locations, automatic offboarding).
  - Otherwise → **ASP.NET Core Identity** with mandatory MFA (TOTP or passkeys), built-in PBKDF2 hashing, progressive lockout.
  - Never build our own password hashing, token format or session system.
- **Rules:**
  - Passwords per NIST SP 800-63B: minimum 12–15 characters, check against breached-password lists, no forced complexity or rotation.
  - Generic login errors (no user enumeration).
  - Every login success/failure logged as a security event (IP, user agent).
  - Sessions: 15–30 min idle timeout, 8–12 h absolute, server-side revocation (security stamp), re-authentication for sensitive actions.
  - Antiforgery tokens on top of SameSite cookies.

### 10.3 Authorization

- **Deny by default** — a fallback policy requires an authenticated user on every endpoint.
- **Roles (draft):** Admin, Onboarding Manager, Implementer, DBA, Configuration, Calendar-only (e.g. resource planner), **Client Viewer** (external, read-only).
- **Organisation scoping:** client users see only their organisation's practices.
- **Resource-based checks:** "can *this* user complete *this* item?" (assignee or manager). Never trust an ID from the client on its own.
- **Separate request DTOs** per action — never bind entities (mass assignment).
- Hiding a button in the UI is UX only; the server enforces everything.
- **Authorization matrix test:** every endpoint × every role → expected 200/403.

### 10.4 IP restriction and blocking

1. **Network / WAF first** — firewall, IIS IP restrictions, or a cloud WAF. Blocked traffic never reaches the app.
2. **Application middleware:**
   - **Allowlist:** CIDR ranges (offices, VPN, client ranges) stored in DB, cached, managed via an admin screen. Changes audited and require re-authentication.
   - **Automatic blocking:** N failed logins from one IP in M minutes → temporary block with exponential backoff and expiry, plus per-account lockout.
   - **Rate limiting:** built-in ASP.NET Core rate limiter, partitioned by IP and user; strict on `/login`.
3. **Pitfalls:**
   - `X-Forwarded-For` is spoofable — trust forwarded headers only from known proxies (`KnownProxies` / `KnownNetworks`).
   - Shared office NAT — exempt allowlisted ranges from auto-blocking (keep account lockout).
   - Remote workers with changing IPs → VPN with a fixed egress IP.
   - IP rules supplement MFA; they never replace it.

### 10.5 Secrets and keys

- Production: Azure Key Vault, or HashiCorp Vault / Windows certificate store on-prem. Local: `dotnet user-secrets`.
- **Never** in `appsettings.json` or git.
- Persist **ASP.NET Data Protection** keys, encrypted with a certificate.
- **gitleaks** in pre-commit and CI.

### 10.6 OWASP Top 10 (2025) → controls

| # | Risk | Controls |
|---|---|---|
| A01 | Broken Access Control (incl. SSRF) | Deny-by-default, resource-based authorization, organisation scoping, public GUIDs, authorization matrix tests, no outbound calls to user-supplied URLs |
| A02 | Security Misconfiguration | Security headers (strict CSP, `frame-ancestors 'none'`, nosniff, Referrer-Policy, Permissions-Policy), no Swagger / detailed errors in production, per-environment config review |
| A03 | Software Supply Chain Failures | §7 dependency policy, lock files, update cool-down, vulnerability scanning, SBOM, no CDN scripts |
| A04 | Cryptographic Failures | TLS 1.2+/HSTS, TDE, AES-GCM only, keys in a vault, no custom crypto |
| A05 | Injection | EF parameterisation, no string-built SQL, React escaping, no `dangerouslySetInnerHTML`, server-side validation |
| A06 | Insecure Design | Threat model (STRIDE), state machine in Domain, abuse-case tests |
| A07 | Authentication Failures | BFF cookies, MFA, lockout, breached-password check, session revocation, rate-limited login |
| A08 | Software / Data Integrity Failures | Ledger audit table, protected CI/CD, branch protection, reviewed migrations |
| A09 | Logging & Alerting Failures | Security events + alerts on 401/403/5xx spikes and IP blocks (§11) |
| A10 | Mishandling of Exceptional Conditions | Global exception handler, fail closed (auth error ⇒ deny), transactions (no partial writes) |

**Beyond the Top 10:** use **OWASP ASVS 5.0 Level 2** as the real checklist. File uploads: size limits, type check by magic bytes, random names, stored outside web root, antivirus scan. Before go-live: OWASP ZAP against staging, then an external pen test (may follow the first release).

---

## 11. Logging & observability

### 11.1 Three separate streams

| Stream | Purpose | Where | Retention |
|---|---|---|---|
| Application logs | Debugging | Built-in `ILogger` → OpenTelemetry → **Seq** (or Grafana / App Insights) | 30–90 days |
| Security events | Logins, failures, IP blocks, 403s, permission changes | Same pipeline, tagged; forward to SIEM if available | 1 year+ |
| Audit trail | Business history | SQL Server `audit` schema, hash-chained (also a ledger table on 2022+) | Permanent |

### 11.2 What makes production debuggable

- **Structured logs** (`"Item {ItemId} completed by {UserId}"`), never string concatenation.
- **Correlation ID:** W3C trace ID on every request, in every log line and the response header; the **UI shows it in error messages** ("Reference: 7f3a…") so support can find the exact request.
- **OpenTelemetry traces and metrics** — shows which SQL query made a request slow.
- **Log levels changeable at runtime** via configuration reload — no redeploy.
- **Front-end errors** sent by a React error boundary to a rate-limited `/api/client-logs` endpoint. Source maps never public.
- **Health checks** (`/health/live`, `/health/ready`), not publicly exposed.
- **Never log:** passwords, tokens, cookies, full request bodies, personal data. Log IDs, not values. Never enable EF `EnableSensitiveDataLogging` in production.

---

## 12. Front-end

| Concern | Use | Avoid |
|---|---|---|
| Tooling | Vite + TypeScript `strict` | Create React App (deprecated) |
| Framework | SPA served by the API (same origin) | Next.js (adds an unneeded Node server) |
| Routing | `react-router` | |
| Server state | `@tanstack/react-query` | Hand-rolled fetch + `useEffect` caching |
| HTTP | Native `fetch` wrapper | axios |
| UI | One component library | Mixing libraries |
| API types | Generated from OpenAPI (`openapi-typescript`, dev-only) | Hand-written duplicate DTOs |
| Structure | Feature folders (`features/calendar`, `features/checklists`, `features/db-logistics`, `features/migrations`) | Folders by file type |

**Security:** no tokens in storage, strict CSP (no inline scripts), no `dangerouslySetInnerHTML`, no CDN scripts.

**Key screens:** go-live calendar · practice list with status · onboarding detail with **timeline** · my assigned items · overdue / late dashboard · checklist import · templates admin · users / roles / IP rules admin · client read-only view.

---

## 13. Testing strategy

| Layer | Tool | Covers |
|---|---|---|
| Domain unit tests | xUnit | State machine, completion rules, late/on-time calculation, time zones |
| Application tests | xUnit + hand-written fakes | Handlers, validation, authorization rules |
| Integration tests | `WebApplicationFactory` + locally installed SQL Server 2019+ (a uniquely named test DB per run, dropped afterwards) | API + DB + auth + audit together |
| Convention / architecture tests | Reflection (no package) | Layer rules; every endpoint has a policy; every endpoint is in the authorization matrix; every request DTO is validated; every entity is audited |
| Authorization matrix | Generated integration tests | Every endpoint × every role |
| Front-end | Vitest + Testing Library | Components, hooks |
| End-to-end | Playwright | Login + MFA, create onboarding, import checklist, complete item, client view |
| Security | OWASP ZAP in CI; external pen test | DAST |

**Avoid:** EF Core InMemory provider (does not behave like SQL Server), mocking libraries, commercial assertion libraries.

---

## 14. Team practices — AI-assisted and manual development

**Principle: don't rely on people or AI to remember rules — make the repository enforce them.** CI doesn't care who (or what) wrote the code.

### 14.1 One rulebook every tool reads

- **`AGENTS.md`** (repo root; read by Codex, Cursor, Copilot and others): architecture rules, folder layout, naming, the avoid list (§18), security rules, dependency policy (§7), Definition of Done.
- **`CLAUDE.md`** contains just `@AGENTS.md` — one source of truth.
- **Browser ChatGPT is the weak spot** — it can't see the repo or its patterns. Prefer repo-aware tools (Claude Code, Codex CLI/IDE, Copilot agent mode). If chat is used, paste the standard preamble from `docs/standards/ai-usage.md` §7.
- **Data rule:** never paste production data, practice names, connection strings or client documents into any AI tool. Use company business/enterprise AI plans only.
- **AI must not add packages.** Any new dependency follows §7.

### 14.2 Reference implementation

AI copies existing patterns very well and invents good ones poorly. Build **one feature end-to-end properly** first (endpoint → handler → validation → audit → tests → React page). Every task then starts with *"follow the pattern in `features/<reference>`."* Shared scaffolding prompts live in the repo (e.g. `.claude/commands/new-feature.md`).

### 14.3 Automated gates

| Gate | Catches |
|---|---|
| `.editorconfig` + `dotnet format --verify-no-changes`; ESLint + Prettier; TypeScript `strict` | Style drift |
| Analyzers with **warnings as errors** | Bugs, null issues, insecure APIs |
| Convention / architecture tests (§13) | Wrong layering; forgotten authorization / validation / audit |
| CodeQL, vulnerability scans, gitleaks | Security issues, vulnerable packages, leaked secrets |
| CODEOWNERS on `Directory.Packages.props` / `package.json` | Unapproved dependencies (including AI-hallucinated packages) |
| OpenAPI diff + generated-client freshness check | Front-end / back-end drift |

Pre-commit hooks give fast feedback but can be skipped — **CI on the PR is the real gate**.

**Branch protection on `main`:**
- No direct pushes.
- PR required, with green CI and at least one human approval.
- CODEOWNERS for auth, crypto, middleware, migrations and package files.

### 14.4 Keeping tests current

1. **Diff coverage, not total coverage:** require ≈80% coverage on the lines changed in each PR. New code can't merge untested; nobody has to backfill legacy.
2. **Self-enforcing convention tests** (§13) automatically cover every future endpoint and entity.
3. **Mutation testing** (Stryker) weekly — exposes AI-written tests that execute code but assert nothing meaningful.
4. **Every bug fix starts with a failing test** that reproduces it.
5. **PR template — Definition of Done:**
   - Tests added or updated.
   - Migration reviewed.
   - No secrets.
   - Audit entries verified.
   - No new packages (or ADR attached).
   - Screenshots for UI changes.

### 14.5 Review culture

- **"You submit it, you own it."** The author must explain every line, whoever generated it.
- **Small PRs** (aim < ~400 changed lines). CI warns on oversized PRs.
- **AI does the first review pass** (e.g. Claude Code `/code-review`, Copilot review); **human approval is mandatory**.
- **ADRs** in `docs/adr/` so the next person's AI doesn't undo a decision.
- **Review decisions before code:** agree the approach in an ADR or a short call first; then the code review only checks it matches.

---

## 15. Git & CI/CD

- **Branching:** trunk-based — short-lived feature branches → PR → `main`. Conventional Commits; semantic version tags.
- **CI on every PR:**
  - Build (warnings as errors).
  - Unit + integration tests with diff coverage.
  - Lint / format.
  - CodeQL.
  - Dependency vulnerability scan.
  - gitleaks.
  - Front-end build + tests.
- **CD:**
  - Dev → QA → Staging (prod-like) → Production.
  - Manual approval for production.
  - The same build artifact is promoted through every environment (never rebuilt per environment).
  - DB scripts applied by the pipeline.
  - ZAP against staging.

---

## 16. Timeline

### 16.1 Revised view (2026-10-27)

Agreed observation: with AI-assisted development, **writing code is no longer the bottleneck**. The foundation can be generated in hours, and the four modules can realistically be built in **about two weeks**. The time goes into:

1. **Human review and decisions at the foundation level** (approach, tools, conventions) — the true critical path.
2. **Requirement gathering**, which depends on the availability of Saeed, Imran Rasheed, Umar Rizwan and the configuration team.
3. **Infrastructure and access** (server, database, domain, TLS certificate, firewall, client accounts) — depends on other people; **request on day 1**.
4. **Real data** — the actual Excel sheets, Teams Forms and stored-procedure log are always messier than expected.
5. **User acceptance** by the people who will use it.

### 16.2 Plan

| When | Work | Notes |
|---|---|---|
| **Tue 27 – Wed 28 Oct** | Answer open questions (§4); raise infrastructure requests; generate foundation (hours) | Requirement gathering starts in parallel — **one owner per module** |
| **Wed 28 – Fri 30 Oct** | **Human review of the foundation** (the real bottleneck); write `AGENTS.md`, CI gates, PR template; merge the reference feature | Report the analysis + plan to Dr Jawad (he asked for this after 1–2 days) |
| **Mon 2 – Fri 13 Nov** | Four modules built in parallel, each person owning one; small PRs merged daily | A module whose requirements are late slips alone, not the others |
| **Mon 16 – Fri 20 Nov** | Integration, import of real Excel / Forms data, UAT with real users, fixes; staging → production | |
| **Mon 23 – Mon 30 Nov** | **Buffer** — late requirements, client feedback, ZAP scan, hardening | Target: live by 30 Nov |
| **December** | Maturing, client feedback, deferred items, handover preparation | |

### 16.3 Deferred until after the November release (unless mandated)

- Custom payload encryption (§10.1) — only if the requirement is confirmed.
- Admin UI for IP rules (configuration-based rules first).
- Notifications (email / Teams).
- Automatic capture from the stored-procedure log table (manual entry first).
- External pen test.

**Not deferrable:** HTTPS, MFA login, deny-by-default authorization, organisation scoping for client users, audit trail, structured logging.

---

## 17. Decision log

| Date | Decision | Status |
|---|---|---|
| 2026-10-27 | Stack: .NET API, React front-end, SQL Server, git | Agreed |
| 2026-10-27 | Modular monolith, Clean Architecture (Domain / Application / Infrastructure / Api + tests), single repo | Proposed |
| 2026-10-27 | No separate Encryption or Utilities projects | Proposed |
| 2026-10-27 | Scope: tracking & visibility only, until go-live; Jira afterwards | Agreed (meeting) |
| 2026-10-27 | Four modules: calendar, configuration checklist, DB logistics, DB migration tracking | Agreed (meeting) |
| 2026-10-27 | Client (external) users get read-only, organisation-scoped access | Proposed |
| 2026-10-27 | BFF cookie authentication; no tokens in browser storage | Proposed |
| 2026-10-27 | Audit log as SQL Server append-only ledger table | Proposed |
| 2026-10-27 | **Minimal dependency policy** (§7) — built-in / Microsoft first; new runtime packages need approval | Agreed |
| 2026-10-27 | Development estimate ~2 weeks; foundation review is the critical path; live by 30 Nov | Agreed |
| 2026-10-27 | `AGENTS.md` + CI gates + diff coverage for AI / manual team consistency | Proposed |
| 2026-10-28 | Front-end encryption dropped; TLS + server-side rules instead (foundation-decisions D-08) | Agreed |
| 2026-10-28 | MFA: Identity's default per-user `TwoFactorEnabled`; no `RequireMfa` setting (D-03a) | Agreed |
| 2026-10-28 | Super Admin role; multiple roles per user; permissions in code, roles as data (D-09a, D-09b) | Agreed |
| 2026-10-28 | Docs-update rule for every change, enforced in CI (D-21) | Agreed |
| 2026-10-28 | SQL Server 2019 baseline; 2022 optional (ledger as an extra layer only); audit tamper evidence via keyed hash chain (D-24, D-17, ADR-0002) | Agreed (baseline) |
| 2026-10-28 | Nothing installed globally — all packages and tools project-local (D-25, ADR-0001) | Agreed |
| 2026-10-28 | Foundation documentation set written (D-26) — see `docs/README.md` | Agreed (structure) |
| — | Git host re-opened (GitHub or Azure DevOps, D-05); production SQL Server version and edition (D-24 follow-up); client login model | **Open** |
| — | Hosting (on-prem) and identity provider (ASP.NET Core Identity) — decided, see foundation-decisions D-01, D-02 | Agreed |

---

## 18. Avoid list

- Microservices, event sourcing, separate read databases.
- "Utilities" / "Common" projects.
- Generic repository over EF Core.
- AutoMapper, MediatR, and any package that the built-in framework already covers.
- Hand-rolled authentication, password hashing, tokens or cryptography; `crypto-js`; keys in JavaScript.
- JWTs in `localStorage`.
- `sa` / `db_owner` connections.
- `Database.Migrate()` at startup in production.
- Secrets in config files or git.
- Sequential integer IDs in URLs.
- Public Swagger; stack traces in responses.
- Logging personal data or tokens.
- Trusting `X-Forwarded-For` blindly.
- Hard deletes.
- Storing practice database credentials or patient data in the portal.
- EF Core InMemory provider for tests.
- Create React App; CDN-loaded scripts.
- Client-side-only validation or authorization.
- Pasting production or client data into AI tools.
- Adding packages without approval.
