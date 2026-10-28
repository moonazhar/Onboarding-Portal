# Onboarding Portal

Tracks the onboarding of practices onto the practice management system: **what** must be done, **when**, **who** must do it, **who** did it and **when** it finished. Every change is recorded in a tamper-evident audit trail. The audit trail is the product.

> **Status:** Foundation phase — solution scaffolded (empty back-end projects, front-end app shell). No features yet.
> **Last updated:** 2026-10-28

---

## Stack

| Part | Technology |
|---|---|
| API | ASP.NET Core on .NET 10 (LTS), controllers |
| Background jobs | .NET Worker running as a Windows Service |
| Front end | React + TypeScript + Vite, Mantine UI |
| Database | SQL Server **2019 or later** (2022+ recommended, not required) |
| Hosting | On-prem: Windows Server + IIS |
| Source control | git (host not decided yet: GitHub or Azure DevOps) |

---

## Start here

| You want to… | Read |
|---|---|
| Set up your machine and run the app | [docs/guides/local-setup.md](docs/guides/local-setup.md) |
| Know the rules (for humans and AI tools) | [AGENTS.md](AGENTS.md) |
| Understand how the system is built | [docs/architecture/overview.md](docs/architecture/overview.md) |
| Find any document | [docs/README.md](docs/README.md) |
| See what is decided and what is open | [docs/foundation-decisions.md](docs/foundation-decisions.md) |
| Understand why the app exists | [docs/discussion.md](docs/discussion.md) |

---

## Golden rules

1. **Nothing is installed globally.** Every package and tool lives inside this repository. Other projects on your machine are never affected. → [tooling-and-dependencies.md](docs/standards/tooling-and-dependencies.md)
2. **No new package without approval.** Built into .NET or the browser first. → [tooling-and-dependencies.md](docs/standards/tooling-and-dependencies.md#5-adding-a-package)
3. **Security first.** Deny by default; the server decides everything. → [security.md](docs/architecture/security.md)
4. **Nothing is hard-deleted, and the audit trail is never edited.** → [database.md](docs/architecture/database.md)
5. **Works on SQL Server 2019.** No SQL feature newer than 2019 in application code. → [database.md](docs/architecture/database.md#1-supported-sql-server-versions)
6. **Change the code, change the docs** — in the same pull request. → [docs/README.md](docs/README.md#3-the-docs-rule)
7. **No patient data, no practice database credentials** — ever.

---

## Repository layout

```text
Onboarding-Portal/
├─ AGENTS.md                    # rules for humans and AI tools
├─ CLAUDE.md                    # imports AGENTS.md
├─ README.md                    # this file
├─ .editorconfig  .gitattributes  .gitignore
├─ backend/                     # everything .NET
│  ├─ OnboardingPortal.slnx     # .NET solution
│  ├─ global.json               # pins the .NET SDK
│  ├─ nuget.config              # package sources + project-local package folder
│  ├─ Directory.Build.props     # shared build settings
│  ├─ Directory.Packages.props  # the only place package versions are set
│  ├─ .config/dotnet-tools.json # project-local .NET tools (e.g. dotnet-ef)
│  ├─ src/                      # .NET projects
│  └─ tests/                    # the one .NET test project
├─ frontend/                    # React app (Vite)
├─ .githooks/                   # git hooks, enabled per clone (planned)
├─ scripts/                     # PowerShell scripts used locally and in CI (planned)
└─ docs/                        # all documentation
```

Details and boundaries of each project: [docs/architecture/overview.md](docs/architecture/overview.md#5-solution-layout).
