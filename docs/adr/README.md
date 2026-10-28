# Architecture Decision Records

> **Last updated:** 2026-10-28
> **Purpose:** record *why* a significant decision was made, so nobody (human or AI) undoes it by accident.

The decision **register** (one line per decision, D-xx) is [../foundation-decisions.md](../foundation-decisions.md). An ADR is the full reasoning behind one of those lines.

---

## When to write an ADR

- Adding a **runtime** package.
- Adding or removing a project, a deployable, or an external system.
- Changing the database baseline, the audit-trail design, or the authentication / authorization model.
- Any decision that would be expensive to reverse, or that someone is likely to question later.

Small, easily reversed choices don't need one — a line in the register or the PR is enough.

## How

1. Copy [template.md](template.md) to `NNNN-short-title.md` (next free number, four digits).
2. Status starts as **Proposed**. The project lead changes it to **Accepted**.
3. Add a row to the index below and to the register in `foundation-decisions.md`.
4. **Never rewrite an accepted ADR.** To change a decision, write a new ADR and set the old one to **Superseded by NNNN**.

## Index

| ADR | Title | Status | Register |
|---|---|---|---|
| [0001](0001-project-local-tooling.md) | Project-local tooling — nothing installed globally | Accepted | D-25 |
| [0002](0002-sql-server-2019-baseline.md) | SQL Server 2019 baseline; 2022 optional | Accepted (baseline) · Proposed (audit design) | D-24, D-17 |
