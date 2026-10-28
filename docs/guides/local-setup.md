# Local setup

> **Status:** ✅ Reviewed by the lead (2026-10-28). Solution scaffolded 2026-10-28; steps marked *planned* start working when that code exists.
> **Last updated:** 2026-10-28
> **Purpose:** get a working development machine without installing anything globally.

---

## 1. The principle

Your machine needs only a handful of **runtimes**. Every package and tool the project uses is installed **inside the repository** and restored with one command. Nothing this project does changes another project on your machine. Full rule: [tooling-and-dependencies.md](../standards/tooling-and-dependencies.md).

---

## 2. What your machine needs

| Runtime | Version | Check | Notes |
|---|---|---|---|
| **.NET SDK** | Pinned in `global.json` (10.0.4xx band) | `dotnet --version` | Install side by side with other SDKs; `global.json` picks the right one for this folder only |
| **Node.js** | Major pinned in `package.json` → `engines` (24 LTS) | `node --version` | npm comes with it. If you need several Node versions, use a version manager — but that is your choice, not a project requirement |
| **Git** | Any recent | `git --version` | |
| **SQL Server** | **2019 or later**, any dev edition | see §3 | 2022 is **not** required |
| **PowerShell** | Windows PowerShell 5.1 (built into Windows) or PowerShell 7 | `$PSVersionTable.PSVersion` | Project scripts work on both |
| Editor / IDE | Your choice | — | VS Code, Visual Studio or Rider. Not a project dependency |
| AI tool | Your choice | — | Follow [ai-usage.md](../standards/ai-usage.md) |

You do **not** need: Docker, global npm packages, global .NET tools, .NET workloads, IIS (for development).

---

## 3. SQL Server — pick one

| Option | Effort | When to choose |
|---|---|---|
| **LocalDB 2019+** | Lowest — no service, starts on demand | You don't have SQL Server, or want the simplest setup. Comes with Visual Studio, or install "LocalDB" from the SQL Server Express installer |
| **Existing SQL Server 2019+ instance** | None | You already have one (Developer, Express or Standard) |
| **SQL Server 2022 Developer** | Medium | You want to test the optional ledger layer ([database.md §2.4](../architecture/database.md#24-layer-4--ledger-table-optional-sql-server-2022)) |

SQL Server 2017 or older won't work. If you have **2019 RTM**, install the latest cumulative update.

Check the version: [database.md §1.2](../architecture/database.md#12-check-your-version).

Example connection strings (local only — they go into **user secrets**, never into a file in the repo):

```text
LocalDB:          Server=(localdb)\MSSQLLocalDB;Database=OnboardingPortal_Dev;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True
Default instance: Server=localhost;Database=OnboardingPortal_Dev;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True
Named instance:   Server=localhost\SQL2022;Database=OnboardingPortal_Dev;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True
```

`TrustServerCertificate=True` is allowed for a local database only.

---

## 4. First-time setup

Back-end commands run in `backend/` (where `global.json` and `nuget.config` live). Front-end commands run in `frontend/`.

```powershell
# 1. Get the code
git clone <repository-url> Onboarding-Portal
cd Onboarding-Portal

# 2. Enable the project's git hooks (this clone only — never --global) — planned, once .githooks/ exists
git config core.hooksPath .githooks

# 3. Back end: restore project-local .NET tools (dotnet-ef …) and NuGet packages into backend/.nuget
cd backend
dotnet tool restore
dotnet restore

# 4. Set your local connection string (stored outside the repo, for your user only)
dotnet user-secrets set "ConnectionStrings:Default" "<your connection string>" --project src/OnboardingPortal.Api
dotnet user-secrets set "ConnectionStrings:Default" "<your connection string>" --project src/OnboardingPortal.Worker

# 5. Create your local database — planned, once the first migration exists
dotnet ef database update --project src/OnboardingPortal.Infrastructure --startup-project src/OnboardingPortal.Api

# 6. Front end
cd ../frontend
npm ci
cd ..

# 7. (Optional) trust the ASP.NET Core development HTTPS certificate — once per Windows user
dotnet dev-certs https --trust
```

The local admin account and seed data are created by `scripts/Initialize-DevData.ps1` (planned). Test credentials it creates are written to your user secrets, not to the repo.

---

## 5. Daily commands

**In `backend/`:**

| Task | Command |
|---|---|
| Build (warnings are errors) | `dotnet build` |
| Run the API (https://localhost:7180) | `dotnet run --project src/OnboardingPortal.Api` |
| Run the Worker | `dotnet run --project src/OnboardingPortal.Worker` |
| Fast tests (no database) | `dotnet test --filter "Category!=Integration"` |
| All tests (needs your SQL Server) | `dotnet test` |
| Format check | `dotnet format --verify-no-changes` |
| Add a migration | `dotnet ef migrations add <Name> --project src/OnboardingPortal.Infrastructure --startup-project src/OnboardingPortal.Api` |
| Reset local database | `dotnet ef database drop --force` then `dotnet ef database update` (same `--project` flags) |
| After pulling changes | `dotnet tool restore`, `dotnet restore` |

**In `frontend/`:**

| Task | Command |
|---|---|
| Dev server (http://localhost:5173, proxies `/api` to the API) | `npm run dev` |
| Type check | `npm run typecheck` |
| Lint / format check | `npm run lint`, `npm run format:check` |
| Fix formatting | `npm run format` |
| Tests | `npm test` |
| Production build (to `frontend/dist`) | `npm run build` |
| End-to-end tests (planned) | `npm run e2e` (first time: `npm run e2e:install` — browsers go inside the project) |
| Run a tool directly | `npx --no -- <tool>` (the `--` matters: without it npm 11 runs `npm` itself) |
| After pulling changes | `npm ci` |

Ports are defined in `backend/src/OnboardingPortal.Api/Properties/launchSettings.json` and `frontend/vite.config.ts`.

---

## 6. What stays inside the repository

| Thing | Location | In git? |
|---|---|---|
| NuGet packages | `backend/.nuget/packages/` | No (ignored) |
| .NET tools | Restored via `backend/.config/dotnet-tools.json` into `backend/.nuget/packages/` | Manifest yes, binaries no |
| npm packages | `frontend/node_modules/` | No |
| Playwright browsers | Inside `node_modules` | No |
| Git hooks | `.githooks/` | Yes |
| Your secrets | `%APPDATA%\Microsoft\UserSecrets\` (per user, outside the repo) | Never |

Deleting the repository folder removes everything the project installed (except your user secrets and the trusted dev certificate).

---

## 7. Troubleshooting

| Problem | Fix |
|---|---|
| `dotnet` uses the wrong SDK / "SDK not found" | Install the SDK band named in `global.json`. Don't edit `global.json` to match your machine |
| `npm ci` fails with an engine error | Use the Node major in `package.json` → `engines` |
| `dotnet ef` not found | Run `dotnet tool restore` in `backend/`. Don't install it globally |
| Restore fails with "Package Source Mapping is enabled, but no source mapped…" | Run the command in `backend/` so `backend/nuget.config` applies. It already ignores sources your machine config disables |
| Cannot connect to LocalDB | `sqllocaldb info` lists instances; `sqllocaldb start MSSQLLocalDB` starts it |
| Certificate error connecting to SQL Server | Local only: add `TrustServerCertificate=True` |
| Browser warns about the HTTPS certificate | Run `dotnet dev-certs https --trust` once |
| Integration tests fail on SQL Server 2017 | Not supported — use LocalDB 2019+ |

---

## 8. Don't

- Don't install anything globally to "make it work". Ask, then fix the project so it works locally for everyone.
- Don't commit `.nuget/`, `node_modules/`, `bin/`, `obj/`, `.env` files or anything with a connection string.
- Don't change `global.json`, `nuget.config`, `.npmrc` or `engines` for your own machine.
- Don't use `npm install` to restore — use `npm ci` (it respects the lock file exactly). `npm install <package>` is only for an approved package change.
- Don't point your local app at a shared or production database.
