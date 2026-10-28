# Database

> **Status:** ✅ Reviewed by the lead (2026-10-28) (§1 baseline ✅ Decided 2026-10-28; §2 design 🟡 Proposed)
> **Last updated:** 2026-10-28
> **Purpose:** which SQL Server versions we support, how the schema is built, how the audit trail stays tamper-evident on every version, and how changes reach each database.

---

## 1. Supported SQL Server versions

**SQL Server 2022 is not required.** Not every developer has it, and production may not have it. The application is built to run on **SQL Server 2019** and gets extra protection when 2022+ is available. (Decision D-24, [ADR-0002](../adr/0002-sql-server-2019-baseline.md).)

| Version | Developer machines | CI | Production |
|---|---|---|---|
| SQL Server 2017 or older | ❌ Not supported | ❌ | ❌ |
| **SQL Server 2019** (15.x), latest CU | ✅ Supported — **the baseline** | ✅ Tests must pass here | ✅ Supported |
| SQL Server 2022 (16.x) | ✅ Supported | ✅ | ✅ Recommended (enables the ledger layer, §2.4) |
| SQL Server 2025 (17.x) | ✅ Supported | ✅ | ✅ Supported |

| Edition | Developer machines | Production |
|---|---|---|
| **LocalDB** (2019+) | ✅ Easiest option — no service to manage | ❌ |
| **Express** | ✅ | ❌ Not recommended (10 GB per database limit) |
| **Developer** | ✅ | ❌ Licence forbids production use |
| **Standard / Enterprise** | ✅ | ✅ (TDE is available in Standard from 2019) |

### 1.1 Rules that keep us on the baseline

1. **EF Core targets compatibility level 150** (SQL Server 2019), so it never generates newer SQL:
   ```csharp
   options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(150));
   ```
2. **No SQL Server 2022+ feature** in application code, LINQ that relies on it, raw SQL or normal migrations. Examples of forbidden features: ledger tables, `GREATEST` / `LEAST`, `DATETRUNC`, `IS [NOT] DISTINCT FROM`, `GENERATE_SERIES`, `JSON_OBJECT` / `JSON_ARRAY`, the `json` data type, vector types.
3. **The only exception** is the optional ledger layer (§2.4). It lives in a separate, guarded script — never in an EF migration that must run everywhere.
4. **CI runs integration tests on SQL Server 2019.** If it works on 2019, it works on later versions.
5. Every database we create has a **compatibility level of at least 150**.

### 1.2 Check your version

```sql
SELECT SERVERPROPERTY('ProductVersion')      AS Version,   -- 15.x = 2019, 16.x = 2022, 17.x = 2025
       SERVERPROPERTY('Edition')             AS Edition,
       SERVERPROPERTY('ProductUpdateLevel')  AS CumulativeUpdate;
```

---

## 2. Audit trail integrity on every version

The audit trail is the product. It must show **who did what, when** — and make tampering **detectable**, even by someone with database access. SQL Server 2022's ledger tables do this in the engine, but we cannot rely on 2022. So integrity is built in layers; layers 1–3 work on every supported version.

| Layer | Works on | Stops / detects |
|---|---|---|
| 1. Permissions | All | The app cannot update or delete audit rows, even if it is compromised |
| 2. Keyed hash chain | All | Any changed, deleted or inserted audit row breaks the chain |
| 3. External digests | All | Someone rebuilding the whole chain after tampering |
| 4. Ledger table (optional) | 2022+ | Same as 2–3, enforced by the engine as a second, independent proof |

### 2.1 Layer 1 — permissions

- Audit tables live in the `audit` schema.
- The app login has `INSERT` and `SELECT` on `audit` and **`DENY UPDATE, DELETE`**.
- No application login is `db_owner` or `sa`.

### 2.2 Layer 2 — keyed hash chain

Each audit row stores a hash that covers its own content **and** the previous row's hash:

```text
Hash(n) = HMAC-SHA256( key, Hash(n-1) || CanonicalForm(row n) )
```

- **Keyed (HMAC), not a plain hash.** The key is stored **outside the database** (server certificate store / secret store), so a person with database access alone cannot recompute a valid chain.
- Each row stores `Sequence` (gap-free, increasing), `PreviousHash`, `Hash` and `KeyId` (so the key can be rotated; old rows keep verifying with the old key).
- **Inserts are serialized** with a transaction-scoped application lock (`sp_getapplock` on `audit-chain`). Audit volume is small, so the lock costs nothing noticeable.
- **`CanonicalForm`** is defined in exactly one place in `Infrastructure/Persistence/Audit/`. Changing it breaks verification of old rows, so it needs an ADR.
- Written in the **same transaction** as the business change. If the change rolls back, so does the audit row.

### 2.3 Layer 3 — external digests

A full rebuild of the chain is only possible for someone who has **both** database write access **and** the key. To catch that too:

- The Worker regularly records the **chain head** (latest `Sequence` + `Hash`) somewhere the database team cannot change. Where exactly is open (D-17a — client-held recommended).
- The Worker **verifies the whole chain** on a schedule. A mismatch is a security event and raises an alert.
- An admin screen shows the last verification result.

### 2.4 Layer 4 — ledger table (optional, SQL Server 2022+)

Where the production server is 2022 or later, the audit table can **also** be an append-only ledger table. The application code does not change: it writes the same rows either way.

- Created by a separate script, `scripts/sql/Enable-AuditLedger.sql` (planned), that checks the version first and does nothing on 2019.
- **Decide before go-live.** An existing table cannot be switched to a ledger table; converting later means creating a new table and copying the rows.
- Ledger digests need external storage too (same decision as D-17a).

### 2.5 What the audit trail records

Every audit event: `Sequence`, `OccurredAtUtc` (server clock), `ActorUserId`, `Action`, `EntityType`, `EntityPublicId`, old/new values (changed fields only), `IpAddress`, `CorrelationId`, `Source` (`User`, `System`, `LegacyImport`), chain columns.

Never in the audit trail: passwords, tokens, secrets, patient data.

---

## 3. Schema conventions

| Item | Convention | Example |
|---|---|---|
| Schemas | `app` (business data), `audit`, `auth` (Identity tables), `ops` (outbox, job state) | `app.Practices` |
| Table names | Plural, PascalCase | `Practices`, `TrackedItems` |
| Column names | PascalCase; UTC times end in `Utc` | `GoLiveDate`, `CreatedAtUtc` |
| Booleans | `Is…` / `Has…` | `IsArchived` |
| Strings | `nvarchar(n)` with an explicit length; `nvarchar(max)` only with a reason in the PR | `nvarchar(200)` |
| Enums | Stored as `nvarchar` (readable in audit and reports), max length set | `Status = 'InProgress'` |
| Foreign keys | `<Entity>Id`, always indexed | `PracticeId` |
| Constraints | Named: `PK_`, `FK_`, `UX_` (unique), `IX_`, `CK_`, `DF_` | `UX_Practices_PublicId` |

**Every business table has:**

| Column | Type | Purpose |
|---|---|---|
| `Id` | `bigint IDENTITY`, clustered PK | Internal key — never leaves the server |
| `PublicId` | `uniqueidentifier`, unique index, default `NEWID()` | The only ID in URLs and the API |
| `RowVersion` | `rowversion` | Optimistic concurrency |
| `CreatedAtUtc`, `CreatedByUserId` | `datetime2`, `bigint` | Set by the server |
| `UpdatedAtUtc`, `UpdatedByUserId` | `datetime2`, `bigint` (nullable) | Set by the server |
| `IsArchived`, `ArchivedAtUtc`, `ArchivedByUserId` | `bit`, `datetime2`, `bigint` (nullable) | Soft delete |

---

## 4. Keys and identifiers

- `Id` (`bigint`) for joins and indexes. **Never** in a URL, API payload, log message meant for users, or the front end.
- `PublicId` (random GUID) everywhere outside the server: `/practices/3f2b8c1e-…`.
- GUIDs are **not secrets** — they appear in history, logs and screenshots. Every request is still authorized on the server.
- Don't use GUIDs as clustered keys (index fragmentation).

---

## 5. Concurrency

- Every update sends back the `RowVersion` it read. A mismatch returns **409 Conflict**; the UI asks the user to reload.
- Never "last write wins" silently.

---

## 6. Time

- Store instants as **UTC** in `datetime2` columns ending in `Utc`.
- Store **scheduled local dates/times** (e.g. a practice's planned extract time) as local date/time **plus** the practice's time zone ID; convert with `TimeZoneInfo` (IANA IDs work on Windows in .NET 6+).
- "Late / on time" is calculated against the practice's local schedule, on the server.
- Get "now" from the injected `TimeProvider`. Never `DateTime.Now`, `GETDATE()` or `SYSDATETIME()` in application logic.

---

## 7. Archiving instead of deleting

- No business row is ever deleted. "Delete" in the UI sets `IsArchived = 1` (with who/when) and writes an audit event.
- An EF global query filter hides archived rows by default. Admin views that show archived rows opt out explicitly.
- Unarchiving is allowed where the module allows it, and is audited.
- Audit rows are never archived, updated or deleted.

---

## 8. Temporal tables

- Main business tables can be system-versioned (temporal) to answer "what did this row look like on date X". Available since SQL Server 2016, so fine on the baseline.
- Temporal history is **not** the audit trail. It records *what* changed, not *who* or *why*.

---

## 9. Migrations

| Environment | How changes are applied |
|---|---|
| Local developer database | `dotnet ef database update` (local tool) |
| Integration tests | Each run creates a uniquely named database, applies migrations, drops it afterwards |
| QA, Staging, Production | An **idempotent SQL script** (`dotnet ef migrations script --idempotent`), reviewed in the PR, applied by the pipeline with the migration login |

**Do**
- Name migrations by intent: `AddPracticeGoLiveDate`, not `Update3`.
- Read the generated migration and the generated SQL before committing.
- Make destructive changes in steps: add new → copy data → switch code → remove old (later release).
- Test every migration on SQL Server 2019.

**Don't**
- Don't call `Database.Migrate()` at startup outside local development.
- Don't edit a migration that has been merged to `main`. Add a new one.
- Don't drop columns or tables that hold data in the same release that stops using them.
- Don't put 2022+ SQL in a migration (§1.1).

---

## 10. Logins and permissions

| Login | Used by | Rights |
|---|---|---|
| App login | Api, Worker | Read/write on `app`, `ops`, `auth`; `INSERT, SELECT` on `audit`; `DENY UPDATE, DELETE` on `audit` |
| Migration login | Pipeline only | DDL rights; never used by the running app |
| Reporting login | Reports, read-only queries | `SELECT` only |

- Prefer **Windows authentication** (IIS app pool / service account identity) over SQL passwords.
- Never `sa`, never `db_owner` for the app.
- Developers use their own Windows login on their local database.

---

## 11. Encryption and connections

- Connection strings use `Encrypt=True`. `TrustServerCertificate=True` is allowed **only** on a local developer database.
- **TDE** on QA, Staging and Production (data files and backups).
- Column-level encryption only where a specific field needs it, decided in an ADR.
- Connection strings live in user secrets (local) or the server's secret store — never in `appsettings*.json` or git.

---

## 12. Raw SQL

- Prefer LINQ. Raw SQL only when LINQ cannot express it or is measurably too slow.
- Always parameterised: `FromSql($"… {value}")` / `ExecuteSql($"…")`. Never `FromSqlRaw` / `ExecuteSqlRaw` with concatenated input.
- Raw SQL lives in Infrastructure only, and obeys the 2019 baseline.

---

## 13. Do / Don't

**Do**
- Design for SQL Server 2019; treat 2022+ as a bonus.
- Index every foreign key and every column used in a frequent filter.
- Use `AsNoTracking()` for read-only queries; project to DTOs with `Select`.
- Keep transactions short; one use case = one `SaveChanges`.

**Don't**
- Don't store patient data or practice database credentials. If a step needs a credential, store a reference to a vault secret.
- Don't use the EF Core InMemory provider.
- Don't put business logic in stored procedures, triggers or views.
- Don't use `SELECT *` in raw SQL.
- Don't enable `EnableSensitiveDataLogging` outside local development.
