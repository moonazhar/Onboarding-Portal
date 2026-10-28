# Using AI tools

> **Status:** ✅ Reviewed by the lead (2026-10-28)
> **Last updated:** 2026-10-28
> **Purpose:** use AI tools (Claude, Codex, ChatGPT, Gemini, Copilot…) safely and so that their output fits this codebase.

---

## 1. Principles

- Every developer may use the AI tool of their choice — or none.
- **The repository enforces the rules, not the tool.** CI doesn't care who wrote the code.
- **You submit it, you own it.** You must understand and be able to explain every line an AI wrote for you.
- AI speeds up writing and first-pass review. **Humans make decisions and approve PRs.**

---

## 2. One rulebook for every tool

| File | Read by |
|---|---|
| [`AGENTS.md`](../../AGENTS.md) | Codex, Copilot, Cursor, Gemini and most agentic tools; humans |
| `CLAUDE.md` | Claude Code — contains only `@AGENTS.md` |
| `docs/standards/*.md`, `docs/architecture/*.md` | Linked from `AGENTS.md`; the detailed rules |

- Rules are written **once**, in `AGENTS.md` and `docs/`. Tool-specific files (`.claude/agents/`, prompts, custom instructions) **point to** these docs; they never copy rules.
- If your tool uses a different rules file, make it point to `AGENTS.md` — don't write new rules in it.

---

## 3. Data rules — never paste into any AI tool

- Production data, database exports or extracts.
- Real practice names, patient data, staff personal data.
- Connection strings, passwords, keys, tokens, certificates.
- Client documents, contracts, the meeting transcripts or files in `docs/data/` and `docs/requirement/` — unless the tool is an approved company business/enterprise plan and the lead agrees.

Use made-up sample data instead. Use company business/enterprise AI plans only.

---

## 4. What AI tools must not do

- Add, remove or upgrade packages, or install anything globally.
- Change authentication, authorization, middleware, IP rules, crypto or the audit trail without a human asking for it and a security reviewer approving it.
- Disable tests, analyzers, warnings or CI checks.
- Undo a decision recorded in `docs/foundation-decisions.md` or `docs/adr/`.
- Invent a new pattern when the reference feature shows one.
- Commit or push without the developer reviewing the diff.

---

## 5. Getting good output

1. **Point at the pattern:** "Follow the pattern in `features/practices` / `Features/Practices`."
2. **Give the rules:** repo-aware tools read `AGENTS.md`; for others, paste the preamble in §7.
3. **Small tasks:** one use case, one component, one migration at a time.
4. **Ask for tests with the code**, then read the tests — AI tests often assert nothing meaningful.
5. **Check every package name** an AI mentions — invented names get registered by attackers.
6. **Review the diff** as if a new colleague wrote it.

Prefer **repo-aware tools** (Claude Code, Codex CLI/IDE, Copilot agent mode) — they see the existing patterns. Browser chat can't, so it needs the preamble.

---

## 6. AI review agents — at checkpoints, not on every edit

Running a review agent after every edit is slow, token-heavy and noisy. Cheapest checks run most often (D-15):

| When | What runs |
|---|---|
| Every build / commit | Analyzers, tests, convention tests, lint, gitleaks — free and deterministic |
| Before opening a PR | The developer runs an AI review (e.g. Claude Code `/code-review`, `/security-review`; Codex `/review`) |
| On each PR (optional) | A CI review agent; the security reviewer only when sensitive paths change (same paths as CODEOWNERS) |
| Always | Human approval |

A few specialists, not one per task: **security**, **database**, **tests**. Their prompts point to `docs/standards/` and `docs/architecture/`.

---

## 7. Preamble for tools that can't see the repo

Paste this at the start of a browser chat session:

```text
You are helping on the Onboarding Portal: ASP.NET Core (.NET 10, controllers) + React/TypeScript
(Vite, Mantine, TanStack Query/Table) + SQL Server 2019+ (EF Core, compatibility level 150).
Rules:
- Clean Architecture: Domain (no dependencies) <- Application (handlers, DTOs) <- Infrastructure
  (EF Core, Identity); Api and Worker are composition roots. Controllers call one handler.
- No new packages. No mediator, mapper, FluentValidation, Serilog, mocking libraries, axios, lodash.
- Every endpoint has a permission attribute; check organisation and record access in the handler.
- User and time come from ICurrentUser and TimeProvider, never from the request or DateTime.Now.
- Separate request/response DTOs; never bind or return EF entities. GUID PublicId in URLs only.
- Parameterised SQL only; nothing newer than SQL Server 2019. Archive, never delete. Audit every write.
- No secrets in code or config; no tokens in localStorage; no dangerouslySetInnerHTML.
- Write tests (xUnit, hand-written fakes; Vitest + Testing Library).
Answer with code that follows these rules, and say if a request conflicts with them.
```

Keep this preamble in sync with `AGENTS.md` (it is a summary, not the source).

---

## 8. Switching tools mid-task

You may move a task from one AI tool to another (e.g. Claude Code → Codex when a usage limit is reached). The rules carry over because they live in the repository. Context does not: one tool's chat history and memory are invisible to the next.

- **Commit before you switch.** Work in progress lives on the branch, not in a chat.
- **Write the hand-off in the repo:** what is done, what is left, and any decision made in the chat. Put it in the PR description or a commit message. A decision goes into `docs/foundation-decisions.md`.
- **Start the new tool with:** "Read `AGENTS.md`, then continue the task on this branch: <what is left>."
- Tool-specific helpers (`.claude/agents/`, Claude Code skills, Codex prompts) are optional extras. Nothing in this project may depend on one tool.
