# AGENTS.md — Rules for everyone working on this repository

This file is read by AI coding tools (Claude Code, Codex, Copilot, Gemini, Cursor…) **and** by humans. It is the rulebook. Detailed rules live in `docs/`; this file lists the hard rules and points to the details. If a rule here conflicts with any other file, **this file wins** — then fix the other file.

`CLAUDE.md` only imports this file. Put rules here, never there.

---

## 1. Read before you change anything

| Before working on… | Read |
|---|---|
| Anything | This file, [docs/README.md](docs/README.md) |
| Back end (C#) | [docs/standards/backend.md](docs/standards/backend.md), [docs/architecture/overview.md](docs/architecture/overview.md) |
| Front end (React) | [docs/standards/frontend.md](docs/standards/frontend.md) |
| Database, migrations, SQL | [docs/architecture/database.md](docs/architecture/database.md) |
| Login, permissions, middleware, anything security | [docs/architecture/security.md](docs/architecture/security.md) |
| Packages, tools, scripts | [docs/standards/tooling-and-dependencies.md](docs/standards/tooling-and-dependencies.md) |
| Tests | [docs/standards/testing.md](docs/standards/testing.md) |
| Commits, branches, PRs | [docs/standards/git-workflow.md](docs/standards/git-workflow.md) |
| Using an AI tool | [docs/standards/ai-usage.md](docs/standards/ai-usage.md) |

Decided vs. open questions: [docs/foundation-decisions.md](docs/foundation-decisions.md). **Do not undo a decision** recorded there or in `docs/adr/`. If you think one is wrong, say so — don't code around it.

---

## 2. Hard rules — MUST

### Scope
- The portal **tracks and shows** onboarding work. It never performs the configuration or the migration itself.
- Scope ends at go-live. Post-go-live work is in Jira.

### Tooling
- Install packages and tools **inside this repository only**: NuGet via `nuget.config` (project-local package folder), .NET tools via `.config/dotnet-tools.json`, npm via `package.json`.
- Run front-end tools through **npm scripts** (`npm run …`) or `npx --no -- <tool>`.
- Use the .NET SDK pinned in `global.json` and the Node.js major pinned in `package.json` → `engines`.

### Architecture
- Respect project boundaries: `Domain` ← `Application` ← `Infrastructure`, composed by `Api` and `Worker`. See [overview.md §6](docs/architecture/overview.md#6-projects-and-their-boundaries).
- Controllers stay thin: one action calls one handler.
- New feature? **Copy the reference feature's pattern** (named in [overview.md §9](docs/architecture/overview.md#9-feature-pattern)). Don't invent a new one.

### Security
- Every endpoint is authorized by **permission** (never by role name). Anonymous endpoints are listed in [security.md](docs/architecture/security.md) and nowhere else.
- *Who* and *when* come from the server (logged-in user, server UTC clock) — **never** from the request.
- Check record access on the server for every request ("may *this* user see/change *this* record?").
- Use separate request and response DTOs. Never bind or return EF entities.
- Response DTOs contain only what *that* audience may see.
- Parameterised SQL only.
- Log IDs, not values. Never log passwords, tokens, cookies or request bodies.

### Data and database
- Application code and migrations work on **SQL Server 2019** (compatibility level 150). Nothing newer. See [database.md §1](docs/architecture/database.md#1-supported-sql-server-versions).
- Every record has a `bigint` key (never leaves the server) and a GUID `PublicId` (the only ID in URLs and the API).
- Archive, never delete. Every write produces an audit event in the **same transaction**.
- Store times as UTC (`…Utc` columns). Use the injected `TimeProvider`.

### Tests
- Every change ships with tests. Every bug fix starts with a failing test.
- One .NET test project: `backend/tests/OnboardingPortal.Tests`.

### Docs
- **Change the code, change the docs, in the same PR.** The docs map in [docs/README.md](docs/README.md#2-docs-map) says which doc goes with which code.
- Record new or changed decisions in [docs/foundation-decisions.md](docs/foundation-decisions.md) (and an ADR for significant ones).

---

## 3. Hard rules — MUST NOT

- ❌ `npm install -g`, `dotnet tool install -g`, `dotnet workload install`, global `pip`/`choco`/`winget` installs for this project, or `git config --global` changes required by this project.
- ❌ Add, remove or upgrade a package without approval and an entry in [tooling-and-dependencies.md §6](docs/standards/tooling-and-dependencies.md#6-approved-packages). **AI tools never add packages on their own.**
- ❌ Use a SQL Server 2022+ feature in application code or normal migrations (ledger tables, `GREATEST`/`LEAST`, `DATETRUNC`, `IS [NOT] DISTINCT FROM`, JSON type…). The optional ledger layer is the only exception and lives in its own guarded script.
- ❌ Hard-delete business data. Update or delete audit rows. Anyone — Super Admin included.
- ❌ Put secrets in code, `appsettings*.json`, `.env` files, front-end code or git.
- ❌ Put tokens or sensitive data in `localStorage` / `sessionStorage`.
- ❌ `dangerouslySetInnerHTML`, string-built SQL, `FromSqlRaw` with concatenation.
- ❌ `DateTime.Now` / `DateTime.UtcNow` in application code (use `TimeProvider`).
- ❌ `Database.Migrate()` at startup outside local development.
- ❌ Sequential IDs in URLs or API payloads.
- ❌ Patient data or practice database credentials anywhere in the portal.
- ❌ Paste production data, client documents, connection strings or practice names into an AI tool.
- ❌ "Utilities" / "Common" projects, generic repositories over EF Core, mediator or mapper libraries.
- ❌ Disable a test, analyzer, warning or CI check to make a build pass.

---

## 4. Definition of Done (every PR)

- [ ] Builds with zero warnings (warnings are errors).
- [ ] Tests added or updated; all tests pass, including integration tests.
- [ ] New endpoints have a permission and appear in the authorization matrix test.
- [ ] Writes produce audit events; nothing hard-deleted.
- [ ] Migration (if any) reviewed and works on SQL Server 2019.
- [ ] No new package — or the approval and docs entry are in the PR.
- [ ] No secrets, no personal data in logs.
- [ ] Docs updated per the docs map — or the PR says `Docs: not needed — <reason>`.
- [ ] Screenshots for UI changes.

---

## 5. When unsure — stop and ask

Ask a human before you:
- add a package or tool, or change a build/CI setting;
- touch authentication, authorization, middleware, IP rules, cryptography or the audit trail;
- change the database schema in a way that rewrites or drops data;
- go beyond the task you were given.

A wrong guess in these areas costs more than the question.
