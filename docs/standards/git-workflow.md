# Git workflow

> **Status:** ✅ Reviewed by the lead (2026-10-28) — git host still open (D-05: GitHub or Azure DevOps)
> **Last updated:** 2026-10-28
> **Purpose:** how changes move from a developer's machine to `main`, and the gates they pass.

---

## 1. Branching

- **Trunk-based.** `main` is always releasable.
- Short-lived branches from `main`, merged back by pull request — aim for **less than 2 days** per branch.
- No long-lived `develop` or release branches.
- Releases are **tags** on `main` (`v1.0.0`, semantic versioning).

| Branch type | Name | Example |
|---|---|---|
| Feature | `feat/<short-description>` | `feat/practice-list` |
| Fix | `fix/<short-description>` | `fix/golive-holiday-check` |
| Docs | `docs/<short-description>` | `docs/local-setup` |
| Chore / tooling | `chore/<short-description>` | `chore/ci-docs-check` |

Lower case, hyphens, no personal names.

---

## 2. Commits

**Conventional Commits:** `<type>(<optional scope>): <summary>`

| Type | Use for |
|---|---|
| `feat` | New behaviour |
| `fix` | Bug fix |
| `docs` | Docs only |
| `test` | Tests only |
| `refactor` | Code change with no behaviour change |
| `chore` | Build, tooling, CI, dependencies |
| `security` | Security fix or hardening |

Examples: `feat(calendar): warn when go-live falls on an NZ public holiday` · `fix(auth): end sessions when a user is deactivated`

- Summary in the imperative, under ~72 characters.
- Body explains *why* when it isn't obvious.
- One logical change per commit.

---

## 3. Pull requests

- **Small:** aim for less than ~400 changed lines (generated files excluded). CI warns above that.
- One purpose per PR. Don't mix a refactor with a feature.
- Title follows the commit format. The description says **what, why, how tested**, and links the requirement or issue.
- Fill in the PR template (Definition of Done — [AGENTS.md §4](../../AGENTS.md#4-definition-of-done-every-pr)).
- Include `Docs: not needed — <reason>` if no doc changes are needed (see [docs rule](../README.md#3-the-docs-rule)).
- Screenshots for UI changes.
- **Squash merge** into `main`; the squash message follows the commit format.

---

## 4. Review

- **At least one human approval** on every PR. AI review helps; it never replaces the human.
- **"You submit it, you own it."** The author can explain every line, whoever or whatever wrote it.
- **CODEOWNERS** approval required for:

| Paths | Why |
|---|---|
| `Directory.Packages.props`, `**/package.json`, `**/package-lock.json`, `nuget.config`, `.npmrc`, `.config/dotnet-tools.json` | Dependencies |
| `backend/src/**/Identity/**`, `backend/src/**/Security/**`, `backend/src/**/Middleware/**`, `Permissions.cs` | Security |
| `backend/src/**/Persistence/Migrations/**`, `backend/src/**/Persistence/Audit/**` | Database and audit |
| CI pipeline files, `.githooks/**`, `scripts/**`, `CODEOWNERS` | Build integrity |
| `AGENTS.md`, `docs/foundation-decisions.md`, `docs/adr/**` | Rules and decisions |

- Agree the approach **before** large changes (short call or ADR); the code review then checks it matches.

---

## 5. CI gates (every PR)

| Gate | Fails when |
|---|---|
| Restore | Lock files don't match (`dotnet restore --locked-mode`, `npm ci`) |
| Build | Any warning (warnings are errors) |
| Format / lint | `dotnet format --verify-no-changes`, ESLint or Prettier report anything |
| Tests | Any test fails — **including integration tests on SQL Server 2019** |
| Diff coverage | Changed lines below ~80% |
| Generated files | OpenAPI types or other generated docs are stale |
| Docs check | A mapped code area changed without its doc (`scripts/Test-DocsUpdated.ps1`) |
| Secrets | gitleaks finds a secret |
| Dependencies | Known vulnerable package (NuGet audit, `npm audit`, dependency review) |
| Static analysis | CodeQL finds an issue |
| AI review | Optional, path-triggered for sensitive paths (D-15) — advisory, not a gate |

CI logic lives in `scripts/*.ps1` so the pipeline file is thin and portable between GitHub Actions and Azure Pipelines.

**Branch protection on `main`:** no direct pushes, no force pushes, PR required, all gates green, at least one approval, CODEOWNERS approval where it applies, branch up to date before merge.

---

## 6. Local hooks

- Enabled per clone: `git config core.hooksPath .githooks` (never `--global`).
- `pre-commit`: format check on staged files. `commit-msg`: Conventional Commits format.
- Hooks are for fast feedback and can be skipped. **CI is the gate.**

---

## 7. Never commit

- Secrets, connection strings, `.env` files, certificates or keys.
- `bin/`, `obj/`, `.nuget/`, `node_modules/`, build output, test results.
- Real practice data, client documents, database exports.
- IDE-specific settings (except shared `.editorconfig` and agreed `.vscode/extensions.json`).

Line endings are fixed by `.gitattributes` (`* text=auto`), so Windows and other systems don't fight over them.

**If a secret is committed:** tell the lead immediately, **rotate the secret**, then remove it from history. Removing it from history alone is not enough.

---

## 8. Do / Don't

**Do**
- Pull `main` often; rebase your branch before opening the PR.
- Keep PRs small and focused; merge daily where possible.
- Write the PR description for a reviewer who has no context.

**Don't**
- Don't push to `main` directly or force-push shared branches.
- Don't skip hooks or CI checks to save time.
- Don't merge your own PR without an approval.
- Don't leave branches open for more than a few days.
