# Tooling and dependencies

> **Status:** ✅ Reviewed by the lead (2026-10-28) (§1 rule ✅ Decided 2026-10-28)
> **Last updated:** 2026-10-28
> **Purpose:** keep every package and tool inside this repository, and keep the number of packages small.

---

## 1. The rule: nothing global

**No package or tool is installed globally — for the back end or the front end. Everything this project uses lives inside the repository.** Other projects on a developer's machine are never affected, and every developer and the CI agent get exactly the same versions. (Decision D-25, [ADR-0001](../adr/0001-project-local-tooling.md).)

### 1.1 What may be installed on the machine

Only runtimes that cannot be project-local, each pinned by a file in the repo:

| Machine-level | Pinned by | Why it is allowed |
|---|---|---|
| .NET SDK | `global.json` | SDKs install side by side; `global.json` selects the version for this folder only |
| Node.js (with npm) | `package.json` → `engines`, enforced by `.npmrc` `engine-strict=true` | The runtime that runs the front-end tools |
| Git | — | Version control |
| SQL Server 2019+ (incl. LocalDB) | [database.md §1](../architecture/database.md#1-supported-sql-server-versions) | A database server is a machine service |
| PowerShell 5.1 (built in) or 7 | — | Runs `scripts/` |

Personal editors, IDEs and AI tools are the developer's own choice. **The project must never require them.**

### 1.2 Never

| ❌ Don't | ✅ Do instead |
|---|---|
| `npm install -g <tool>` | Add it to `devDependencies` (with approval) and run it with `npm run <script>` |
| `npx <package>` that isn't in `package.json` (downloads and runs code) | `npx --no -- <tool>` — refuses to download anything |
| `dotnet tool install -g <tool>` | `dotnet tool install <tool>` with the local manifest (`.config/dotnet-tools.json`) |
| `dotnet workload install …` | Not needed for this project |
| `choco` / `winget` / `scoop` / `pip` installs for project tools | Ask first; find a project-local option |
| `git config --global …` for project settings | `git config …` (this clone only) — e.g. `core.hooksPath` |
| Editing machine `PATH` or environment variables for the project | Project scripts and npm scripts set what they need |
| Editing `global.json`, `nuget.config`, `.npmrc` or `engines` to fit your machine | Fix your machine, or raise it with the team |

---

## 2. How each ecosystem stays inside the repository

### 2.1 .NET SDK — `global.json`

```json
{
  "sdk": {
    "version": "10.0.403",
    "rollForward": "latestPatch"
  }
}
```

`latestPatch` accepts newer patches of the same feature band (10.0.4xx) and nothing else.

### 2.2 NuGet packages — `backend/nuget.config`

All .NET files (`global.json`, `nuget.config`, `Directory.*.props`, `.config/dotnet-tools.json`) live in `backend/`, so run `dotnet` commands there.

```xml
<configuration>
  <config>
    <!-- Packages are extracted inside the repo, not into %USERPROFILE%\.nuget\packages -->
    <add key="globalPackagesFolder" value=".nuget\packages" />
  </config>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

- `<clear />` ignores any machine-wide or user-wide package sources — the build sees only what this file lists.
- `disabledPackageSources` is cleared too: a user-level config that disables `nuget.org` by name would otherwise break restore.
- Source mapping blocks dependency-confusion attacks.
- `.nuget/` is in `.gitignore`. The first restore downloads into the repo; later restores reuse it.

Build settings in `Directory.Build.props`:

| Setting | Value | Why |
|---|---|---|
| `ManagePackageVersionsCentrally` (in `Directory.Packages.props`) | `true` | Versions set in one file only |
| `RestorePackagesWithLockFile` | `true` | `packages.lock.json` committed per project |
| `NuGetAudit` / `NuGetAuditMode` / `NuGetAuditLevel` | `true` / `all` / `low` | Vulnerability warnings for direct and transitive packages (warnings are errors) |
| `Nullable`, `TreatWarningsAsErrors`, `AnalysisLevel` | `enable`, `true`, `latest-recommended` | See [backend.md](backend.md) |

CI restores with `dotnet restore --locked-mode`: the build fails if the lock file doesn't match.

### 2.3 .NET tools — local tool manifest

```powershell
dotnet new tool-manifest            # once, creates .config/dotnet-tools.json (already done in the repo)
dotnet tool install dotnet-ef       # adds to the manifest — approval required
dotnet tool restore                 # every developer, after clone/pull
dotnet ef migrations list           # runs the project's pinned version
```

### 2.4 npm packages — `package.json` + `.npmrc`

`frontend/.npmrc`:

```ini
# exact versions, no ^ or ~
save-exact=true
# packages can't run install scripts (the main malware route)
ignore-scripts=true
# refuse the wrong Node version
engine-strict=true
fund=false
```

(Comments go on their own line — npm reads anything after `=` as the value.)

- `package-lock.json` is committed. Restore with **`npm ci`** (locally and in CI) — it installs exactly the lock file.
- Every tool is a `devDependency` and is run through an **npm script** (`npm run build`, `npm run lint`, `npm test`), which uses `node_modules/.bin`.
- If a package genuinely needs its install script, it is approved and run explicitly (documented in §6), never by turning `ignore-scripts` off.

### 2.5 Playwright browsers

Playwright normally downloads browsers to a shared folder in the user profile. Ours go inside the project: the npm scripts `e2e:install` and `e2e` set `PLAYWRIGHT_BROWSERS_PATH=0`, which stores browsers in `node_modules`.

### 2.6 Git hooks

Hooks live in `.githooks/` (committed) and are enabled per clone with `git config core.hooksPath .githooks`. No hook manager package (e.g. husky). Hooks give fast feedback only; CI is the gate.

### 2.7 Scripts

- `scripts/*.ps1` — must run on **Windows PowerShell 5.1 and PowerShell 7**, use only built-in cmdlets, and never install anything.
- The same scripts run locally and in CI, so CI stays portable between GitHub Actions and Azure Pipelines (D-05 open).

### 2.8 CI agents

The same rule applies: the agent provides the runtimes (SDK via `global.json`, Node, SQL Server 2019); everything else is restored into the workspace by `dotnet tool restore`, `dotnet restore --locked-mode` and `npm ci`.

---

## 3. Dependency policy

Every package is code we didn't write, can't fully review and must patch forever. **Fewer packages = smaller attack surface.**

**Order of preference**

1. Built into .NET or the browser.
2. Official Microsoft / framework-owner package.
3. Widely used, actively maintained third-party package.
4. Write it ourselves — **except security primitives** (crypto, password hashing, auth protocols, HTML sanitising, file-format parsing), which are never hand-rolled.

**Runtime packages** (shipped to production) get the strictest bar. **Dev-only packages** still run on developer machines and CI, so they need approval too.

---

## 4. Checking a package before proposing it

| Check | Bar |
|---|---|
| Need | Nothing built in or already approved does the job |
| Publisher | Known, verified; for npm, provenance where available |
| Maintenance | Release in the last 12 months; issues answered |
| Usage | Widely used (downloads, dependents) |
| Licence | MIT, Apache-2.0, BSD or MS-PL. No GPL/AGPL, no commercial-only licence |
| Transitive dependencies | Counted and listed; fewer is better |
| Security history | CVEs reviewed; fixed promptly |
| Install scripts | None — or justified |
| Age of version | At least **7 days** old (malicious releases are usually caught within days) |

---

## 5. Adding a package

1. Check §4. Write down the result.
2. Runtime package: write an ADR in `docs/adr/`. Dev-only: a short note in the PR is enough.
3. Add it to §6 of this file in the same PR.
4. Add it to `Directory.Packages.props` (exact version) or `package.json` (exact version via `save-exact`). Pick a version at least 7 days old (§4):
   - npm: `npm install --before=<date 7 days ago> <package>` picks the newest version published before that date.
   - NuGet: check the version's publish date on nuget.org before writing it into `Directory.Packages.props`.
5. Commit the updated lock file.
6. CODEOWNERS approval is required on package files.

**Upgrades:** automated update PRs (Dependabot or Renovate — depends on D-05) with a 7-day cool-down. Major upgrades follow steps 1–6. Security fixes may skip the cool-down after review.

**Monthly:** remove unused packages; review vulnerability reports.

**AI tools never add, remove or upgrade packages on their own.** An AI-suggested package is checked like any other — AI tools sometimes invent package names that attackers then register.

---

## 6. Approved packages

Status: ✅ Decided · 🟡 Proposed. Exact versions live in `backend/Directory.Packages.props` and `frontend/package.json`, not here.

**Approved is not installed.** A package is referenced by a project when the first code needs it. `Directory.Packages.props` lists the version of every approved .NET package, but a listed version is only downloaded once a project references it.

### 6.1 Back end — runtime

| Package | Purpose | Status |
|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | Database access | ✅ |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Login, MFA | ✅ (D-02) |
| `Microsoft.Extensions.Hosting.WindowsServices` | Worker as a Windows Service | ✅ (D-10) |
| `Microsoft.AspNetCore.OpenApi` | OpenAPI document (non-production) | ✅ (D-27) |
| `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore` | Logs, traces, metrics export | ✅ (D-27) |
| `DocumentFormat.OpenXml` | Read `.xlsx` imports | 🟡 (D-12) |
| `MailKit` | Send email | 🟡 (D-16) |

Built in — **no package**: validation, rate limiting, Data Protection, antiforgery, HSTS, forwarded headers, health checks, ProblemDetails, `TimeProvider`, `BackgroundService`, JSON logging, `System.Security.Cryptography`, CSV reading (`Microsoft.VisualBasic.FileIO.TextFieldParser`, part of .NET).

### 6.2 Back end — dev and test only

| Package / tool | Purpose | Status |
|---|---|---|
| `dotnet-ef` (local tool) | Migrations | ✅ |
| `Microsoft.EntityFrameworkCore.Design` | Design-time support for migrations (`PrivateAssets=all`) | ✅ |
| `xunit.v3`, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` | Tests | ✅ (D-19) |
| `Microsoft.AspNetCore.Mvc.Testing` | Integration tests (`WebApplicationFactory`) | ✅ (D-19) |
| `coverlet.collector` | Coverage | ✅ (D-27) |

### 6.3 Front end — runtime

| Package | Purpose | Status |
|---|---|---|
| `react`, `react-dom` | UI | ✅ |
| `react-router` | Routing | ✅ (D-27) |
| `@tanstack/react-query` | Server state, caching | ✅ (D-07) |
| `@tanstack/react-table` | Tables | ✅ (D-07) |
| `@mantine/core`, `@mantine/hooks` | UI components | ✅ (D-07) |
| `@mantine/notifications` | Toasts | ✅ (D-07) |
| `@mantine/dates` (+ `dayjs` peer) | Date pickers, calendar | ✅ (D-27) |
| `@mantine/charts` (+ `recharts`) | Charts and dashboards | ✅ (D-22, D-27) |
| `zod` | Schema validation (optional, zero dependencies) | ✅ (D-27) |

### 6.4 Front end — dev only

`vite`, `@vitejs/plugin-react`, `typescript`, `@types/react`, `@types/react-dom` (type definitions only), `eslint` (+ `typescript-eslint`, `eslint-plugin-react-hooks`), `prettier`, `vitest`, `jsdom`, `@testing-library/react`, `@playwright/test`, `openapi-typescript` (types only, no runtime code), `postcss` + `postcss-preset-mantine` (Mantine styling). All ✅ (D-27).

### 6.5 CI services (not packages)

CodeQL, gitleaks, dependency review / vulnerability scan, OWASP ZAP (staging).

---

## 7. Banned

| Package | Reason / replacement |
|---|---|
| AutoMapper, Mapperly | Hand-written mapping |
| MediatR or any mediator | Plain handler classes |
| FluentValidation | Built-in validation |
| Serilog | Built-in `ILogger` + OpenTelemetry |
| Hangfire, Quartz.NET | `BackgroundService` in the Worker |
| Swashbuckle / Scalar UI in production | OpenAPI document only, non-production |
| Moq, NSubstitute, any mocking library | Hand-written fakes |
| FluentAssertions (commercial licence from v8), Shouldly | xUnit `Assert` |
| Testcontainers, Docker-based tooling | No Docker (D-23) |
| EF Core InMemory provider | Doesn't behave like SQL Server |
| axios | `fetch` |
| lodash, underscore, moment | Built-in JavaScript, `Intl` |
| jQuery, toastr | Mantine notifications |
| crypto-js or any JS crypto library | Web Crypto / server-side only |
| Redux, Zustand, MobX (state libraries) | TanStack Query + React state |
| Create React App, Next.js | Vite SPA served by the API |
| husky, lint-staged, cross-env | `.githooks/` + npm scripts |
| Any second UI component library | Mantine only |
