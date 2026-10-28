# Onboarding Portal — Foundation Decisions

> **Status:** Foundation phase — solution scaffolded 2026-10-28.
> **Last updated:** 2026-10-28
> **Purpose of this file:** the lead's own points (saved word for word), the analysis of each point, and the register of what is decided, proposed or still open for the foundation. `docs/discussion.md` holds the full background; this file records the foundation decisions made on top of it.

**Status legend**

| Status | Meaning |
|---|---|
| ✅ **Decided** | Agreed by the project lead. Build on it. |
| 🟡 **Proposed** | Recommended, waiting for the lead's confirmation. |
| 🔴 **Open** | Needs an answer before the related work starts. |
| 🔁 **Re-opened** | Was decided, but new information may change it. |

---

## Table of contents

1. [Lead's points — saved as-is](#1-leads-points--saved-as-is)
2. [Analysis of each point](#2-analysis-of-each-point)
3. [Decision register](#3-decision-register)
4. [Open decisions](#4-open-decisions)

---

## 1. Lead's points — saved as-is

### 1.1 Answers to the first open questions (2026-10-27)

```text
hosting will be on-prem
ASP.NET Core Identity as identity provider
GitHub
UI library (whichever you recommend) the purpose is minimal yet modern UI. easy to use and understand. with few clicks. good dashboard. toastr notifications. tables. everything
Controllers
CSV and .xlsx import
front-end encryption is required
```

### 1.2 Additional points (2026-10-27)

```text
we will use asp.net core identity with MFA (if possible). we can add captcha later if required. but i think ip blocking will do that job for us
there will be multiple user types like roles. data entry staff (us) and progress tracker (client)
background jobs will be a separate jobs (if required)
for notificaitons, either email or push or teams, we will use queue jobs. so that main app isn't slow.
we will add `csv` or `xlsx` import option for calendar module. so that legacy data can also be imported easily. rest of the data entry we can figure that out later.
i think we should add GUID with our records. and on front-end, we can simply show/get the data by it. like instead of `practice/1` we can do `practice/65454-654654-45564` this way we won't have to encrypt data. but the main reason i asked for front-end encryption is that we might need to add hidden values in our form. we can and we will do server side validation but we can't expose information on front-end so what should we do about that?
should we create Agents for each task? like an Agent for Security so whenever a developer adds something, that specific agent will spawn and start working? is it possible? won't that be token heavy?
```

### 1.3 Foundation block points (2026-10-28)

```text
Asp.net core identity with MFA (we need to add a column for is MFA enabled or not). this is because during testing, we don't want to keep entering MFA codes again and again
Multiple users with multiple roles with multiple access. clear definition of boundaries. a "Super Admin" which will have access to everything including activating/deactivating users or roles or access.
different types of project. each with clear boundaries and limitations.
security is the most top priority.
good UI and UX.
charts and dashboards.
all `.md` documents in detailed. so if multiple users work on it, all relevant documents should be updated. I think we need to make it a rule somewhere in our docs and enforce it. that if anyone adds/updates/deletes something from the app, all relevant docs should be updated.
I think we will use Azure for version control but it's not decided yet.
```

Follow-up answers (2026-10-28):

```text
different types of project meant .NET projects
I think we should drop front-end encryption
No need for RequireMfa. Default is fine.
```

### 1.4 Project foundation (2026-10-28)

```text
We have decided several decisions to move forward with.
I think it's time we setup the project foundation.
Start with docs.
All relevant documents. Thorough. Clear. To the Point. Boundaries defined. Dos and Donts.
We don't want any package to be installed globally. Everything should be limited to this project. for backend and front-end.
This way other project won't be effected.
As for hard rule for SQL 2022, don't make it a blocker. Not everyone will have it. Maybe even on production we won't have it. So we will make a work around.
```

Result: the documentation set indexed in [README.md](README.md); decisions D-24 and D-25 below; ADRs [0001](adr/0001-project-local-tooling.md) and [0002](adr/0002-sql-server-2019-baseline.md).

### 1.5 Package approval and docs review (2026-10-28)

```text
package approval as per your suggestion and docs are reviewed as well.
```

The suggestion approved was the list of 🟡 packages needed to scaffold the solution: `react-router`, `@mantine/dates`, `@mantine/charts`, `zod`, `Microsoft.AspNetCore.OpenApi`, the three OpenTelemetry packages, `coverlet.collector`, and the front-end dev tools in [tooling-and-dependencies.md §6.4](standards/tooling-and-dependencies.md#64-front-end--dev-only). `MailKit` and `DocumentFormat.OpenXml` were not in that list and stay 🟡 until their features start.

Result: decisions D-22, D-26 and D-27 below.

### 1.6 Scaffolding (2026-10-28)

```text
ok let's start project scaffolding.
all backend into `backend` folder
front-end into `frontend` folder
```

Approved during scaffolding (asked in chat): `@types/react` and `@types/react-dom`, dev-only type definitions that TypeScript needs to compile React.

Result: decisions D-27 (amended), D-28 and D-29 below.

---

## 2. Analysis of each point

### 2.1 ASP.NET Core Identity with MFA

**Verdict: possible — and should be mandatory, not optional.**

- MFA is built into ASP.NET Core Identity: TOTP (authenticator apps such as Microsoft Authenticator) plus recovery codes. No package needed, except an optional zero-dependency QR-code generator for the setup screen (or users type the setup key manually).
- .NET 10 Identity also supports **passkeys** (WebAuthn; the browser side is built in). Proposed as an optional extra on top of TOTP — to be checked against our custom React login screens in Phase 0.
- **MFA for every user, internal and external.** This portal exists to prove who did what; a stolen password alone must not be enough to act as someone else.
- Identity's built-in Razor UI does not fit a React app, so we build the login, MFA, invitation and password-reset screens ourselves, using `UserManager` / `SignInManager` in our own controllers. `MapIdentityApi` is **not** used — it exposes a public `/register` endpoint, and this app is invitation-only.

### 2.2 CAPTCHA later; IP blocking instead

**Verdict: agree to defer CAPTCHA — but IP blocking alone does not do the whole job.** The real protection is the combination below.

| Control | Stops | Does not stop |
|---|---|---|
| **IP allowlist** (office, VPN, client ranges) | Everyone else on the internet — they can't even reach the login page | Attacks from inside an allowed network |
| **Per-IP auto-blocking + rate limiting** | One machine guessing many passwords | Botnets using thousands of IPs (credential stuffing) |
| **Per-account lockout** | Many guesses against one account | — but can be abused to lock real users out on purpose |
| **MFA** | A correct stolen or guessed password | — |

- With **no self-registration**, CAPTCHA's main use (sign-up spam) does not apply.
- CAPTCHA services (reCAPTCHA, Turnstile) are third-party scripts that send visitor data to Google or Cloudflare — against our dependency and CSP rules.
- **Keep lockout short and progressive** (e.g. 5 failures → 15 minutes, growing), so an attacker can't keep a client user locked out for long.
- **Revisit CAPTCHA if:** client users cannot be IP-allowlisted (changing IPs, no VPN), *or* logs show distributed login attacks (many IPs, many accounts).

### 2.3 User types and roles

**Verdict: agree. Model *user types* and *roles* separately, and check *permissions* in code.**

- **User type** = which organisation the user belongs to: **Internal** (us) or **External** (client). External users only ever see their own organisation's practices — enforced on the server in every query.
- **Roles** group **permissions**. Code checks permissions (`Calendar.Edit`, `Users.Manage`…), never role names, so a new role later is a data change, not a code change.

Proposed roles for v1:

| Role | User type | Can |
|---|---|---|
| **Admin** | Internal | Manage users, roles, IP rules, templates; everything data entry can do |
| **Data entry staff** | Internal | Create and update records; tick off items; import files |
| **Progress tracker** | External (client) | Read-only view of their own organisation's progress, timeline and audit trail |
| *Internal viewer (optional)* | Internal | Read-only, all organisations (e.g. managers taking daily updates) |

- **Admin is separate from data entry** (least privilege): a data-entry account that is compromised must not be able to create users or change IP rules.
- Data entry can later be narrowed per module (calendar, configuration, DB team) by permission — no code change.
- **Needs a decision:** is there anything internal-only that the progress tracker must not see (internal notes, staff names, internal comments)? This drives the response DTOs — see §2.7.

### 2.4 Background jobs as a separate service

**Verdict: agree.** It is a separate project, `OnboardingPortal.Worker`, running as a **Windows Service**.

- **"If required" — it becomes required as soon as notifications exist** (point 2.5), and also for audit-chain verification and digest jobs (`docs/architecture/database.md` §2) and reminders.
- IIS can stop or recycle the web app at any time (idle timeout, scheduled recycle, deployments), so important work must not run inside the web app.
- Same build, two deployables: **Api** (IIS) and **Worker** (Windows Service). Both use the same Application and Infrastructure projects.

### 2.5 Notifications through a queue

**Verdict: agree.** A queue keeps requests fast. More importantly it keeps notifications **reliable**: the notification is saved in the same database transaction as the change. If the change rolls back, no notification goes out. If the email server is down, the notification waits and retries.

**Queue implementation (proposed):** a SQL Server table (the *outbox* pattern). The Worker picks up rows, sends them, retries with back-off, and marks repeated failures for an admin to see.

- No new infrastructure (RabbitMQ would be another server to run, secure and patch).
- No new packages.
- Volumes are small (tens to hundreds of notifications a day).

**Channels:**

| Channel | Notes | Proposal |
|---|---|---|
| **In-app** (bell icon + list) | Built in; real-time updates with SignalR (part of ASP.NET Core) | ✅ v1 |
| **Email** | MailKit + internal SMTP relay. Needs an SMTP relay (open question). | ✅ v1 |
| **Microsoft Teams** | Posted to a Teams *Workflows* webhook (the replacement for the retired Office 365 connector webhooks — check the current Microsoft guidance at build time). Needs outbound internet from the server. | Later |
| **Browser push** (Web Push) | Needs a service worker, VAPID keys and outbound calls to Google/Mozilla push services. Little benefit for a work portal used during working hours. | Not recommended |

**Rule:** notifications carry **links and identifiers, not data** ("Checklist item completed for practice X — open portal"), because email and Teams messages leave our system and are kept forever.

### 2.6 CSV / XLSX import for the calendar module

**Verdict: agree.** Calendar import first; other modules decide later.

Legacy data will be messy. The import must handle:

- **Date formats:** NZ writes `dd/MM/yyyy`; Excel may store dates as serial numbers or as text. The template fixes one format, and every ambiguous value is shown in the preview, never guessed.
- **Unknown practices:** rows that refer to a practice not in the system are flagged in the preview. The user creates the practice or skips the row — the import never creates practices silently.
- **Duplicates:** re-importing the same file must not create duplicate entries (match on practice + date).
- **Honest audit of historic data:** legacy rows are recorded as *"imported from file X (SHA-256 hash) by user Y on date Z"*. The audit trail must **not** pretend the importer performed the historic go-lives. Imported records keep `Source = LegacyImport`.
- **Flow:** download template → upload → validate → preview with per-row errors → confirm → commit in one transaction.
- **Action:** get the real calendar Excel file from Saeed / Imran Rasheed early — the template is designed from it.

### 2.7 GUIDs in URLs, and hidden values in forms

**Verdict on GUIDs: agree** — already in the database design (`docs/discussion.md` §9). Every record has:

- an internal `bigint` key (fast joins, never leaves the server), and
- a random `uniqueidentifier` **PublicId**, the only ID used in URLs and the API: `practice/3f2b8c1e-…`.

What GUIDs do and don't do:

- ✅ Nobody can guess or count records by changing `1` to `2` in the URL.
- ❌ They are **not secrets** — they appear in browser history, logs and shared screenshots. **Every request is still authorized on the server** ("may *this* user see *this* practice?").
- ❌ They are not encryption and do not replace it — GUIDs and encryption solve different problems.

**On hidden values — the key finding:**

> **Anything the browser receives, the user can see** — hidden inputs, JavaScript variables, React state, and also *encrypted* payloads, because the browser must decrypt them before it can use them. The user owns the browser and DevTools. **Front-end encryption does not hide anything from the user.**

So the requirement "we can't expose information on the front end" is met by **server-side design rules**, not by encryption:

| Kind of "hidden value" | What we do |
|---|---|
| **Who / when** (user ID, timestamps, "completed by") | Never sent by the browser. The server takes them from the logged-in session and the server clock. |
| **The record being edited** | Its PublicId is in the URL — visible, and that is fine. The server checks the user may edit it. |
| **Calculated values** (status, late / on-time, totals) | Never accepted from the browser. The server calculates them. |
| **Internal-only data** (internal notes, staff details) the client must not see | **Not sent at all** to users who may not see it. Separate response DTOs per audience (e.g. `PracticeDetailsInternal` vs `PracticeDetailsClient`). A test checks client-role responses contain no internal fields. |
| **Workflow state** (wizard step, import in progress) | Kept on the server; the browser holds only an opaque reference (e.g. an import-session ID). |
| **A value that must round-trip unchanged** (rare) | Protected on the server with ASP.NET Core **Data Protection** (`IDataProtector`, built in — authenticated encryption). The browser receives an opaque token it can neither read nor change; the server rejects anything tampered with. Time-limited if needed. |
| **Fields the user must not set** | Request DTOs contain only the fields the user may set — extra fields are ignored (no mass assignment). |
| **Secrets** (keys, connection strings) | Never in the front end. Note: every `VITE_*` environment variable is compiled into the public JavaScript bundle. |

**Consequence for front-end encryption:** if hidden form values were the main reason for it, payload encryption does not solve that problem. It also brings the largest risk and cost in Phase 0: our own crypto protocol, an independent security review on the critical path, and no WAF body inspection. **Recommendation: drop it, unless another party (e.g. the client or a compliance rule) separately requires it.** TLS 1.2+ with HSTS protects data in transit; the rules above protect data from the user. → Re-opened decision D-08.

### 2.8 An AI agent per task (e.g. a Security agent)

**Verdict: possible, and useful — but not on every edit. Run review agents at checkpoints.**

**What exists today (Claude Code):**

- **Subagents** — `.claude/agents/<name>.md`: a specialist with its own instructions, tools and model (e.g. `security-reviewer`). Claude can delegate to it automatically when the task matches its description, or a developer calls it by name. It works in its own context and returns a summary.
- **Skills / commands** — instructions loaded only when needed. Claude Code already includes `/security-review` and `/code-review`.
- **Hooks** — run scripts automatically on events (e.g. after a file edit); good for fast, deterministic checks.
- **CI** — Claude Code can run in a GitHub Action on every PR.

**Yes, an agent per edit would be token-heavy.** Each spawned agent starts with an empty context and re-reads the standards, the changed files and the surrounding code. A security agent after every edit multiplies token use, slows developers down and produces noisy, repeated findings.

**Proposed setup — cheapest checks run most often:**

| When | What runs | Cost |
|---|---|---|
| **Every build / commit** | Analyzers, tests, convention tests, ESLint, gitleaks | Free, deterministic |
| **Once per phase, before opening the PR** | Developer runs `/security-review` or `/code-review` | One agent run per phase |
| **Once per PR, automatically** | CI review agent — the **security reviewer only when the PR touches sensitive paths** (auth, crypto, middleware, migrations, package files — the same paths as CODEOWNERS) | Bounded: one run per PR |
| **Always** | Human approval | — |

- **Few specialists, not one per task:** `security-reviewer`, `database-reviewer` (migrations, indexes, SQL), `test-reviewer` (tests that assert nothing). More agents = more prompts to maintain.
- **Agent prompts point to `docs/standards/*.md`; they never copy the rules**, so there is one source of truth.
- **Portability:** `.claude/agents` works in Claude Code only. Codex and Gemini have their own mechanisms. That is fine as long as the **rules live in `AGENTS.md` + `docs/standards/`** and the **enforcement lives in CI** — those work for every tool and for manual coding.
- Agents speed reviews up; they are **not the gate**. CI and the human reviewer are.

### 2.9 Foundation block points (§1.3)

**MFA flag.** No new column is needed: Identity's `AspNetUsers.TwoFactorEnabled` already stores it. The lead chose Identity's default per-user behaviour and no `RequireMfa` environment setting. Consequence to keep in mind: in production, MFA is only as strong as the accounts that have it switched on, so the user admin screen shows MFA status per user and switching it off is an audited Super Admin action.

**Super Admin.** Has every permission, including activating and deactivating users, roles and role permissions. Guardrails:

- At least two active Super Admins at all times (break-glass); a Super Admin cannot deactivate themselves or the last Super Admin.
- Changes to users, roles or access ask for re-authentication.
- Deactivating a user updates their security stamp, so their sessions end at once.
- Every access change is audited. **Nobody, Super Admin included, can edit or delete the audit trail.**

**Users, roles, access.** Permissions are a fixed list in code; roles are bundles of permissions managed in the UI; a user can hold several roles; deny by default. Boundaries are checked at three levels: company (ours vs the other company), permission, and record (e.g. assignee or lead). Draft roles from the roadmap meeting (seed data, not code): Super Admin, Onboarding Manager, Team Lead, Onboarding team, Network team, Database team, Configuration team, Viewer.

**"Different types of project"** means the .NET projects in `docs/discussion.md` §5.2, each with a clear responsibility. The dependency rule (§5.3) is enforced by architecture tests in `OnboardingPortal.Tests/Architecture/`, so the build fails if one project reaches into another it must not.

**Charts and dashboards.** Proposed: **Mantine Charts** (same vendor as the UI library, follows the theme and dark mode). It brings Recharts as a transitive dependency, to be recorded in an ADR.

**Docs rule.** Enforced in layers, cheapest first:

1. The rule in `AGENTS.md` (read by Claude, Codex and Copilot; `CLAUDE.md` points to it).
2. A docs map in `docs/README.md`: each code area → the docs that must change with it.
3. Generated docs where possible (permission matrix, OpenAPI, DB schema); a test fails if the committed copy is stale.
4. A CI script (plain PowerShell, so it runs on GitHub Actions or Azure Pipelines): a PR that changes a code area without its mapped docs fails, unless the PR says `Docs: not needed — <reason>`.
5. A checkbox in the PR template.

CI can see that a doc changed, not that it is correct; the reviewer checks that.

**Version control.** Azure is being considered, which re-opens D-05. It does not block coding (local git first); only the CI pipeline file and branch policies depend on it. Needed before the other developers join.

---

## 3. Decision register

| ID | Area | Decision | Status | Source |
|---|---|---|---|---|
| D-01 | Hosting | On-prem: Windows Server + IIS (in-process). Database: SQL Server **2019+** (2022+ recommended, not required — see D-24) | ✅ Decided | Lead §1.1, §1.4 |
| D-02 | Identity | ASP.NET Core Identity, custom React screens via our own controllers; no `MapIdentityApi`; invitation-only (no self-registration) | ✅ Decided (identity) · 🟡 Proposed (custom controllers, invitation-only) | Lead §1.1, §1.2 |
| D-03 | MFA | MFA with TOTP + recovery codes | ✅ Decided | Lead, §1.2 |
| D-03a | MFA | Identity's default per-user `TwoFactorEnabled` (built-in column, no new column); no `RequireMfa` environment setting. Replaces the earlier "mandatory for every user" proposal. | ✅ Decided | Lead §1.3, §2.9 |
| D-03b | MFA | Passkeys as optional extra in v1 | 🔴 Open | §2.1 |
| D-04 | Login protection | No CAPTCHA in v1. IP allowlist + per-IP blocking + rate limiting + short progressive account lockout + MFA. Revisit on the triggers in §2.2. | ✅ Decided (no CAPTCHA) · 🟡 Proposed (combined controls) | Lead §1.2, §2.2 |
| D-05 | Source control / CI | Was GitHub + GitHub Actions. Azure (Azure DevOps Repos + Pipelines) now being considered. Code starts in local git; CI scripts kept platform-neutral (PowerShell). | 🔁 Re-opened | Lead §1.1, §1.3 |
| D-06 | API style | ASP.NET Core **controllers**, thin (one action → one handler) | ✅ Decided | Lead, §1.1 |
| D-07 | UI library | **Mantine** + TanStack Query + TanStack Table; Mantine notifications for toasts (not toastr — it needs jQuery) | ✅ Decided (lead delegated the choice) | Lead §1.1 |
| D-08 | Front-end encryption | **Dropped.** TLS 1.2+ with HSTS protects data in transit; the server-side rules in §2.7 (D-14) protect data from the user. | ✅ Decided | Lead §1.3, §2.7 |
| D-09 | User types | Two user types: **Internal** (data entry staff — us) and **External** (progress tracker — client), organisation-scoped | ✅ Decided | Lead, §1.2 |
| D-09a | Roles | Multiple roles per user; permissions fixed in code, roles are data managed in the UI; code checks permissions, not role names. Draft roles: Super Admin, Onboarding Manager, Team Lead, Onboarding team, Network team, Database team, Configuration team, Viewer. | ✅ Decided (model) · 🟡 Proposed (role list) | Lead §1.3, §2.9 |
| D-09b | Super Admin | Super Admin has every permission, incl. activating/deactivating users, roles and access; guardrails in §2.9 (≥2 active, re-auth, audited, cannot touch the audit trail) | ✅ Decided (role) · 🟡 Proposed (guardrails) | Lead §1.3, §2.9 |
| D-10 | Background jobs | Separate **Worker** project running as a Windows Service; not inside IIS | ✅ Decided | Lead, §1.2 |
| D-11 | Notifications | Sent through a **queue** by the Worker, never inside the web request | ✅ Decided | Lead, §1.2 |
| D-11a | Queue | SQL Server outbox table (no RabbitMQ / no new infrastructure) | 🟡 Proposed | §2.5 |
| D-11b | Channels | v1: in-app + email (MailKit). Teams later. No browser push. | 🟡 Proposed | §2.5 |
| D-12 | Import | CSV and `.xlsx` import for the **calendar module** (incl. legacy data). Other modules decide later. | ✅ Decided | Lead, §1.1, §1.2 |
| D-12a | Import | Fixed template, preview before commit, one transaction, legacy rows marked `Source = LegacyImport` | 🟡 Proposed | §2.6 |
| D-13 | Identifiers | Every record has a random GUID **PublicId**; URLs and the API use only it | ✅ Decided | Lead, §1.2 |
| D-13a | Identifiers | Internal `bigint` clustered key that never leaves the server | 🟡 Proposed | `docs/discussion.md` §9 |
| D-14 | Hidden values | Server-side rules in §2.7: server sets who/when/calculated values, audience-specific DTOs, Data Protection for round-trip values, no secrets in the bundle | 🟡 Proposed | §2.7 |
| D-15 | AI agents | Specialist review agents (security, database, tests) at checkpoints: per phase and per PR (path-triggered), not per edit; rules stay in `AGENTS.md` + `docs/standards/`; CI is the gate | 🟡 Proposed | §2.8 |
| D-16 | Email library | MailKit (Microsoft does not recommend `SmtpClient` for new code); transitive packages recorded in an ADR | 🟡 Proposed | Review feedback |
| D-17 | Audit | Explicit business audit events (primary) + EF interceptor (safety net). Tamper evidence on every version: DENY UPDATE/DELETE + keyed (HMAC-SHA256) hash chain + external digests; ledger table only as an optional extra on 2022+ ([ADR-0002](adr/0002-sql-server-2019-baseline.md)). Temporal tables are *not* the audit. | 🟡 Proposed | Review feedback; Lead §1.4 |
| D-17a | Audit | Where audit-chain digests (and ledger digests on 2022+) are stored (client-held recommended) | 🔴 Open | Review feedback |
| D-18 | Timeline | Phase 0 in two tracks (unblock developers / gate go-live); module coding from ~9 Nov | 🔴 Open | Review feedback |
| D-20 | .NET projects | Separate .NET projects with clear boundaries (`docs/discussion.md` §5.2–5.3), enforced by architecture tests | ✅ Decided | Lead §1.3 |
| D-21 | Docs | Every add/update/delete in the app updates all relevant docs; enforced by `AGENTS.md`, the docs map, generated docs, a CI check and the PR template (§2.9) | ✅ Decided (rule) · 🟡 Proposed (enforcement) | Lead §1.3 |
| D-22 | Charts | Mantine Charts (Recharts transitive) | ✅ Decided | §2.9, Lead §1.5 |
| D-23 | Dev database | **No Docker.** Every developer machine and the CI agent use a **locally installed SQL Server 2019+** — any edition, including LocalDB and Express. *(2026-10-28: the earlier "2022+ Developer edition" requirement is superseded by D-24.)* | ✅ Decided | Lead 2026-10-28, §1.4 |
| D-24 | SQL Server version | **Baseline SQL Server 2019** (compatibility level 150) for development, CI and production; 2017 and older not supported. EF Core uses `UseCompatibilityLevel(150)`; no 2022+ SQL in application code or normal migrations; CI integration tests run on 2019. 2022+ is optional and only adds the ledger layer. ([ADR-0002](adr/0002-sql-server-2019-baseline.md), [database.md §1](architecture/database.md#1-supported-sql-server-versions)) | ✅ Decided | Lead §1.4 |
| D-25 | Tooling | **Nothing installed globally**, back end or front end. Machine-level: only runtimes pinned by repo files (.NET SDK, Node.js, Git, SQL Server, PowerShell). NuGet packages extracted into the repo (`nuget.config` `globalPackagesFolder`), .NET tools via local manifest, npm via `node_modules` + npm scripts, Playwright browsers inside the project, git hooks via repo-local `core.hooksPath`. ([ADR-0001](adr/0001-project-local-tooling.md), [tooling-and-dependencies.md](standards/tooling-and-dependencies.md)) | ✅ Decided | Lead §1.4 |
| D-26 | Docs set | Foundation documentation: root `README.md`, `AGENTS.md`, `CLAUDE.md`; `docs/README.md` (index + docs map); `docs/architecture/` (overview, database, security); `docs/standards/` (tooling, backend, frontend, testing, git workflow, AI usage); `docs/guides/local-setup.md`; `docs/adr/`. Content reviewed by the lead 2026-10-28; individual 🟡 decisions in this register stay proposed until confirmed. | ✅ Decided | Lead §1.4, §1.5 |
| D-27 | Packages | Scaffolding packages approved (list in §1.5) and marked ✅ in [tooling-and-dependencies.md §6](standards/tooling-and-dependencies.md#6-approved-packages). `MailKit` and `DocumentFormat.OpenXml` stay 🟡. 2026-10-28: `@types/react`, `@types/react-dom` added (dev-only). A package is referenced only when the first code needs it; the ADR that §5 requires for a runtime package is written then, when its lock-file dependencies are known. | ✅ Decided | Lead §1.5, §1.6 |
| D-28 | Repository layout | All .NET code and its tooling files in `backend/` (solution, `global.json`, `nuget.config`, `Directory.*.props`, `.config/`, `src/`, `tests/`); the React app directly in `frontend/`. Supersedes the `src/` + `web/onboarding-portal-web/` layout drafted earlier. ([overview.md §5](architecture/overview.md#5-solution-layout)) | ✅ Decided | Lead §1.6 |
| D-29 | Test runner | .NET tests run on VSTest (`Microsoft.NET.Test.Sdk`), with `IsTestingPlatformApplication=false` in the test project, because xUnit v3 4.x defaults to Microsoft.Testing.Platform. Keeps `dotnet test --filter` and `coverlet.collector` working. Switching to Microsoft.Testing.Platform later would change the filter syntax and the coverage package. | 🟡 Proposed | Scaffolding, §1.6 |
| D-19 | Tests | **One** .NET test project, `OnboardingPortal.Tests`, organised by folders (`Domain/`, `Application/`, `Architecture/`, `Integration/`). Integration tests run against the **locally installed SQL Server** (no Docker, no Testcontainers): each run creates a uniquely named test database, applies migrations and drops it afterwards; the connection string comes from an environment variable or user secrets. They carry an xUnit trait; the default local run skips them, CI runs everything. Front-end tests stay inside the web app. | ✅ Decided | Lead, 2026-10-28 |

---

## 4. Open decisions

1. **D-05** — GitHub or Azure DevOps? Needed before the other developers join.
2. **D-09a / §2.3** — what must the progress tracker (client) **not** see? (Internal notes, staff names, internal comments, anything else.)
3. **D-03b** — passkeys in v1 as an optional extra, or later?
4. **D-11b** — is there an internal **SMTP relay**? Is **outbound internet** allowed from the server (needed for Teams)?
5. **D-04** — can client users be IP-allowlisted (fixed IPs or VPN)? If not, CAPTCHA may come back.
6. **D-17a** — audit-chain digests (and ledger digests, if 2022+ is used) held by the client, or internal storage our DB team cannot change?
7. **D-18** — accept module coding starting around 9 Nov, and raise the go-live risk with Dr Jawad now?
8. **§2.6** — get the real calendar Excel file from Saeed / Imran Rasheed.
9. **D-24 follow-up** — which SQL Server version and edition will production run? Decides whether the optional ledger layer is enabled (must be decided before go-live).
