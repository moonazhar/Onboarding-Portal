# Documentation index

> **Last updated:** 2026-10-28
> **Purpose:** find any document, know which docs must change with which code, and how to write docs here.

---

## 1. Index

| Document | Answers | Status |
|---|---|---|
| [../README.md](../README.md) | What is this, where do I start? | ✅ Reviewed (2026-10-28) |
| [../AGENTS.md](../AGENTS.md) | What are the hard rules? (humans and AI tools) | ✅ Reviewed (2026-10-28) |
| **Background and decisions** | | |
| [discussion.md](discussion.md) | Why the app exists, requirements, original reasoning | Reference |
| [foundation-decisions.md](foundation-decisions.md) | What is decided, proposed or open (register D-xx) | Living |
| [adr/](adr/README.md) | Why a significant decision was made (one file per decision) | Living |
| **Architecture** | | |
| [architecture/overview.md](architecture/overview.md) | System shape, projects and their boundaries, request flow | ✅ Reviewed (2026-10-28) |
| [architecture/database.md](architecture/database.md) | SQL Server versions, schema conventions, audit integrity, migrations | ✅ Reviewed (2026-10-28) |
| [architecture/security.md](architecture/security.md) | Login, permissions, IP rules, secrets, threats and controls | ✅ Reviewed (2026-10-28) |
| **Standards (how we write code)** | | |
| [standards/tooling-and-dependencies.md](standards/tooling-and-dependencies.md) | Project-local tooling, adding packages, approved list | ✅ Reviewed (2026-10-28) |
| [standards/backend.md](standards/backend.md) | C# / ASP.NET Core rules | ✅ Reviewed (2026-10-28) |
| [standards/frontend.md](standards/frontend.md) | React / TypeScript rules, UI and UX | ✅ Reviewed (2026-10-28) |
| [standards/testing.md](standards/testing.md) | What to test, where, how | ✅ Reviewed (2026-10-28) |
| [standards/git-workflow.md](standards/git-workflow.md) | Branches, commits, PRs, CI gates | ✅ Reviewed (2026-10-28) |
| [standards/ai-usage.md](standards/ai-usage.md) | Using AI tools safely and consistently | ✅ Reviewed (2026-10-28) |
| **Guides** | | |
| [guides/local-setup.md](guides/local-setup.md) | Set up a machine and run the app | ✅ Reviewed (2026-10-28) |

**Planned** (written when the related code exists): domain model, module docs (one per module), deployment and operations runbook, generated permission matrix, generated database schema.

**Source material** (not edited): `docs/data/`, `docs/requirement/`, and the `.vtt` meeting transcripts in the repository root.

---

## 2. Docs map

Which docs must change when you change a code area. CI uses this table (see §3).

| When you change… | Paths (target) | Update |
|---|---|---|
| Projects or project references | `*.csproj`, `OnboardingPortal.slnx` | [architecture/overview.md](architecture/overview.md) §5–6 |
| Packages (add, remove, major upgrade) | `Directory.Packages.props`, `frontend/package.json` | [standards/tooling-and-dependencies.md](standards/tooling-and-dependencies.md) §6, ADR if runtime |
| Tools, SDK or Node version, scripts | `.config/dotnet-tools.json`, `global.json`, `nuget.config`, `.npmrc`, `scripts/` | [standards/tooling-and-dependencies.md](standards/tooling-and-dependencies.md), [guides/local-setup.md](guides/local-setup.md) |
| Schema, migrations, SQL conventions | `backend/src/**/Persistence/**` | [architecture/database.md](architecture/database.md) if a convention or SQL feature changes; generated schema (planned) |
| Login, permissions, roles, middleware, IP rules, headers, crypto | `backend/src/**/Identity/**`, `backend/src/**/Security/**`, `backend/src/**/Middleware/**`, `Permissions.cs` | [architecture/security.md](architecture/security.md); permission matrix (planned, generated) |
| API endpoints | `backend/src/OnboardingPortal.Api/Controllers/**` | OpenAPI document (generated); module doc (planned) |
| Configuration keys | `appsettings*.json`, options classes | [guides/local-setup.md](guides/local-setup.md) if developers must set it; deployment runbook (planned) |
| Worker jobs | `backend/src/OnboardingPortal.Worker/**` | [architecture/overview.md](architecture/overview.md) §8 |
| Front-end structure, shared components | `frontend/src/app/**`, `frontend/src/shared/**` | [standards/frontend.md](standards/frontend.md) |
| Test layout, test helpers | `backend/tests/**` | [standards/testing.md](standards/testing.md) |
| CI, hooks, PR template, CODEOWNERS | `.github/**` or `pipelines/**`, `.githooks/**` | [standards/git-workflow.md](standards/git-workflow.md) |
| Any decision made or changed | — | [foundation-decisions.md](foundation-decisions.md) + ADR if significant |
| Rules for AI tools | — | [../AGENTS.md](../AGENTS.md) |

---

## 3. The docs rule

**Anyone who adds, changes or removes something in the app updates every relevant doc in the same pull request.** (Decision D-21.)

How it is enforced, cheapest first:

1. **`AGENTS.md`** states the rule — every AI tool reads it.
2. **The docs map** above says which doc belongs to which code.
3. **Generated docs** (OpenAPI, permission matrix, schema) are produced from code; a test fails if the committed copy is stale.
4. **CI check** (`scripts/Test-DocsUpdated.ps1`, planned): a PR that changes a mapped code area without touching its doc fails, unless the PR description contains `Docs: not needed — <reason>`.
5. **PR template** checkbox.

CI can see *that* a doc changed, not that it is *correct*. The reviewer checks correctness.

---

## 4. How to write docs here

**Do**
- Start with a header: title, `Status`, `Last updated` (absolute date, `YYYY-MM-DD`), one-line purpose.
- Write short sentences in plain English. Many readers are not native speakers.
- Prefer tables and lists to paragraphs. Use **Do / Don't** sections for rules.
- State the rule, then the reason in one line.
- Use absolute dates ("2026-11-09"), never "next week".
- Link to the one place a rule lives. **Never copy a rule** into a second doc — copies drift.
- Mark anything not yet agreed: 🟡 Proposed, 🔴 Open (same legend as `foundation-decisions.md`).
- Use relative links so they work on any git host.

**Don't**
- Don't write docs for code that doesn't exist, except as clearly marked *target* or *planned*.
- Don't put secrets, real connection strings, real practice names or personal data in docs.
- Don't rewrite history in decision records — add a new entry that supersedes the old one.
- Don't edit the source material in `docs/data/` or `docs/requirement/`.

**Status legend**

| Status | Meaning |
|---|---|
| ✅ Decided | Agreed by the project lead. Build on it. |
| 🟡 Proposed / Draft for review | Recommended, waiting for the lead's confirmation. |
| 🔴 Open | Needs an answer before the related work starts. |
| 🔁 Re-opened | Was decided, but new information may change it. |
