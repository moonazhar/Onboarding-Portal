# 0001 — Project-local tooling: nothing installed globally

- **Status:** Accepted
- **Date:** 2026-10-28
- **Decided by:** Project lead
- **Register:** D-25

## Context

The project lead: *"We don't want any package to be installed globally. Everything should be limited to this project, for backend and front-end. This way other projects won't be affected."*

Developers work on several projects on the same machine. Globally installed tools (npm `-g`, global .NET tools, workloads) cause version clashes between projects, differ between machines, and are invisible in the repository — so a build that works on one machine fails on another. Install-time scripts of global npm packages are also a known malware route onto developer machines.

## Decision

1. No package or tool is installed globally for this project. The only machine-level installs are runtimes, each pinned by a file in the repo: .NET SDK (`global.json`), Node.js (`engines` + `engine-strict`), Git, SQL Server 2019+ (incl. LocalDB), PowerShell.
2. **NuGet:** a repository `nuget.config` clears all machine/user sources, lists nuget.org only with package source mapping, and sets `globalPackagesFolder` to `.nuget/packages` inside the repo.
3. **.NET tools:** local tool manifest (`.config/dotnet-tools.json`), restored with `dotnet tool restore`.
4. **npm:** packages in the project's `node_modules`; tools run via npm scripts or `npx --no --`; `.npmrc` with `save-exact`, `ignore-scripts`, `engine-strict`; restore with `npm ci`.
5. **Playwright browsers** stored inside `node_modules` (`PLAYWRIGHT_BROWSERS_PATH=0`).
6. **Git hooks** via repo-local `core.hooksPath`, no hook manager package.
7. CI agents follow the same rule.

Details: [tooling-and-dependencies.md](../standards/tooling-and-dependencies.md).

## Consequences

- **Good:** other projects are never affected; every machine and CI use identical versions; deleting the folder removes everything; the full toolchain is visible and reviewable in git.
- **Bad / cost:** packages are downloaded per repository (more disk space, slower first restore); developers must run `dotnet tool restore` / `npm ci` after cloning; IDEs must respect `nuget.config` (Visual Studio, Rider and VS Code do).
- **Follow-ups:** `.gitignore` must exclude `.nuget/`; local-setup guide and CI scripts use only project-local commands.

## Alternatives considered

| Option | Why not |
|---|---|
| Allow global tools, document versions | Clashes between projects; drift between machines; exactly what the lead wants to avoid |
| Docker dev containers | No Docker (D-23) |
| Keep NuGet's default user-wide package folder | Packages would be shared with other projects; the lead asked for everything to stay in this project |
