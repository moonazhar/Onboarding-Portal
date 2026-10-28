# 0002 — SQL Server 2019 baseline; 2022 optional

- **Status:** Accepted (baseline) · Proposed (audit-integrity design)
- **Date:** 2026-10-28
- **Decided by:** Project lead (baseline); design proposed for review
- **Register:** D-24 (baseline), D-17 (audit design), supersedes the "2022+" part of D-01 and D-23

## Context

Earlier decisions required SQL Server 2022+ everywhere, mainly for **ledger tables** (engine-level tamper evidence for the audit trail). The project lead: *"As for the hard rule for SQL 2022, don't make it a blocker. Not everyone will have it. Maybe even on production we won't have it. So we will make a workaround."*

The current dev machine runs SQL Server 2019. Production's version is not confirmed.

## Decision

1. **Baseline: SQL Server 2019** (compatibility level 150), any edition for development including LocalDB and Express; Standard or Enterprise for production. 2017 and older are not supported.
2. EF Core is configured with `UseCompatibilityLevel(150)`. No 2022+ SQL in application code or normal migrations. CI runs integration tests on 2019.
3. **Audit tamper evidence works on every supported version**, in layers:
   1. Permissions: the app login is denied `UPDATE` / `DELETE` on the `audit` schema.
   2. Keyed hash chain: each audit row stores `HMAC-SHA256(key, previousHash || canonicalRow)`; the key lives outside the database; inserts are serialized with `sp_getapplock`.
   3. External digests: the Worker verifies the chain on a schedule and records the chain head where the database team cannot change it (location open — D-17a).
   4. Optional: where the server is 2022+, the audit table is **also** created as an append-only ledger table by a separate, version-guarded script.

Details: [database.md §1–2](../architecture/database.md).

## Consequences

- **Good:** nobody is blocked by their SQL Server version; production can run on 2019; tamper evidence does not depend on one engine feature; on 2022+ there are two independent proofs.
- **Bad / cost:** we own the hash-chain code (small, but security-critical — it gets a security review and tests for tampering scenarios); the HMAC key must be managed and rotated; we can't use convenient 2022 T-SQL functions.
- **Follow-ups:** the ledger layer must be decided before go-live (a table can't be converted to ledger later without copying data); D-17a (digest location) still open; update D-01, D-17, D-23.

## Alternatives considered

| Option | Why not |
|---|---|
| Require 2022+ everywhere | Blocks developers and possibly production — rejected by the lead |
| Audit table with permissions only | A DBA or anyone with elevated access could change history undetected |
| Plain (unkeyed) hash chain | Anyone with write access can recompute the whole chain |
| Triggers that block updates/deletes | Can be disabled by anyone with `ALTER`; gives no evidence after the fact |
| Separate audit database / external log service | More infrastructure to run and secure; still needs integrity checks |
