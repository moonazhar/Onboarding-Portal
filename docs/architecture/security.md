# Security

> **Status:** ✅ Reviewed by the lead (2026-10-28)
> **Last updated:** 2026-10-28
> **Purpose:** how the portal protects its data and users, and the rules every change must follow. Security is the top priority.

---

## 1. Principles

1. **Deny by default.** Nothing is reachable unless a rule allows it.
2. **The server decides.** The browser is untrusted: anything it sends can be forged, anything it receives can be read.
3. **Least privilege** for users, roles, database logins and service accounts.
4. **Defence in depth.** Network, IP rules, login, MFA, permissions and record checks — no single control is relied on.
5. **Fail closed.** If an auth check errors, the answer is "deny".
6. **Prove it.** Every security-relevant action is logged; every business change is audited.
7. **Use what the framework provides.** Never write our own crypto, password hashing, tokens or session handling.

---

## 2. Data classification

| Class | Examples | Rules |
|---|---|---|
| **Never stored** | Patient data, practice database credentials, passwords in plain text | Must not enter the system — not in fields, notes, attachments, logs or imports |
| **Secret** | Connection strings, keys, SMTP passwords, audit-chain key | Secret store only; never in git, config files, front end or logs |
| **Internal** | Internal notes, staff details, internal comments | Never sent to External users (separate response DTOs) |
| **Shared** | Practice progress, dates, task status | Visible to users whose organisation and permissions allow it |

Open: exactly which fields External users must not see (D-09a — needs the lead's answer).

---

## 3. Authentication

- **ASP.NET Core Identity** with our own React screens, calling our own controllers (`UserManager` / `SignInManager`). (D-02)
- **No `MapIdentityApi`** — it exposes a public `/register` endpoint.
- **Invitation only.** A Super Admin (or a role with `Users.Invite`) creates the account; the user sets a password through a single-use, time-limited link.
- **Cookie authentication**, same origin as the API (BFF pattern): `HttpOnly`, `Secure`, `SameSite=Strict`. JavaScript never sees a token.
- **Passwords** follow NIST SP 800-63B: minimum 12 characters, maximum at least 64, checked against a breached-password list, no forced complexity, no forced rotation. Hashed by Identity's built-in hasher.
- **Generic errors.** Login, invitation and password-reset responses never reveal whether an account exists.
- **Lockout** is short and progressive (e.g. 5 failures → 15 minutes, growing), so attackers can't keep real users locked out.

### Anonymous endpoints (the complete list)

| Endpoint | Why |
|---|---|
| `POST /api/v1/auth/login` | Sign in |
| `POST /api/v1/auth/login/mfa` | Second step of sign in |
| `POST /api/v1/auth/invitations/accept` | Set password from an invitation link |
| `POST /api/v1/auth/password/forgot`, `…/reset` | Password reset |
| `GET /api/v1/auth/antiforgery` | Issue the antiforgery token |
| Static SPA files | The login page must load |

Adding an endpoint to this list requires a security reviewer's approval. A convention test fails for any `[AllowAnonymous]` endpoint not on the list.

---

## 4. MFA

- **TOTP** (authenticator apps) + **recovery codes**, built into Identity. (D-03)
- Uses Identity's **per-user `TwoFactorEnabled`** flag — no extra column, no `RequireMfa` setting. (D-03a)
- The user admin screen shows MFA status per user. Switching MFA off for someone is a **Super Admin action, re-authenticated and audited**.
- Because MFA is per user, production strength depends on accounts having it on. The admin dashboard lists active accounts without MFA.
- Passkeys: open (D-03b).

---

## 5. Sessions

| Setting | Value |
|---|---|
| Idle timeout | 30 minutes (sliding) |
| Absolute lifetime | 12 hours |
| Revocation | Security stamp: password change, role change, deactivation or "sign out everywhere" ends all sessions at once |
| Re-authentication | Required for changes to users, roles, permissions, IP rules and MFA |
| Antiforgery | Required on every state-changing request, on top of `SameSite=Strict` |

---

## 6. Authorization

### 6.1 Model

```text
User ──has many──► Role ──has many──► Permission (fixed list in code)
  │
  └── belongs to one Organisation (Internal = our company, External = partner company)
```

- **Permissions** are constants in code (`Permissions.Calendar.Edit`, `Permissions.Users.Manage`…). Code checks permissions, **never role names**.
- **Roles** are data, managed in the UI by a Super Admin. A new role is a data change, not a code change.
- A user can hold **several roles**; their permissions are the union.
- Draft roles (seed data, 🟡 proposed): Super Admin, Onboarding Manager, Team Lead, Onboarding team, Network team, Database team, Configuration team, Viewer.

### 6.2 Three checks on every request

| Level | Question | Where |
|---|---|---|
| **Organisation** | Does this record belong to an organisation the user may see? | Query filter + handler |
| **Permission** | Does the user have the permission for this action? | `[HasPermission(...)]` on the endpoint |
| **Record** | May this user act on *this* record (e.g. assignee, team lead)? | Handler, before any change |

- A **fallback policy** requires an authenticated user on every endpoint.
- Every endpoint has an explicit permission. A convention test fails if one doesn't.
- The **authorization matrix test** calls every endpoint as every role and checks the expected 2xx/403.
- Hiding a button in the UI is UX, not security.

### 6.3 Super Admin guardrails

- Has every permission, including activating and deactivating users, roles and permissions. (D-09b)
- At least **two active Super Admins** at all times. A Super Admin cannot deactivate themselves or the last other Super Admin.
- Changes to users, roles or access require **re-authentication** and are **audited**.
- Deactivating a user updates their security stamp — their sessions end immediately.
- **Nobody, Super Admin included, can edit or delete the audit trail.**

---

## 7. What the browser may send and receive

Anything the browser receives, the user can read (DevTools). Anything it sends, the user can change. So: (D-14)

| Kind of value | Rule |
|---|---|
| Who / when (user, timestamps, "completed by") | Never accepted from the browser. Taken from the session and `TimeProvider`. |
| Calculated values (status, late/on time, totals, progress %) | Never accepted. Calculated on the server. |
| Fields the user must not set | Not in the request DTO, so they cannot be bound (no mass assignment). |
| Internal-only data | Not sent to users who may not see it. Separate response DTOs per audience; a test checks External responses contain no internal fields. |
| Workflow state (wizard step, import in progress) | Kept on the server; the browser holds an opaque reference ID. |
| A value that must round-trip unchanged (rare) | Protected with ASP.NET Core **Data Protection** (`IDataProtector`); tampering is rejected. |
| Record IDs | `PublicId` only. Visible and fine — access is checked on every request. |
| Secrets | Never. Every `VITE_*` variable is compiled into the public bundle. |

Front-end payload encryption is **dropped** (D-08): it hides nothing from the user. TLS protects data in transit.

---

## 8. Network: IP rules and rate limiting

| Control | How | Notes |
|---|---|---|
| Firewall / IIS IP restrictions | Network level, before the app | First line; blocked traffic never reaches the app |
| **IP allowlist** | Middleware; CIDR ranges in the database, cached, admin screen | Changes are re-authenticated and audited |
| **Automatic blocking** | N failed logins from one IP in M minutes → temporary block, growing, with expiry | Allowlisted office ranges are exempt (shared NAT); account lockout still applies |
| **Rate limiting** | Built-in ASP.NET Core rate limiter, per IP and per user | Strict on auth endpoints |
| **Forwarded headers** | Trusted only from known proxies (`KnownProxies` / `KnownNetworks`) | `X-Forwarded-For` is otherwise spoofable |

IP rules supplement MFA; they never replace it. No CAPTCHA in v1 (D-04); revisit if External users cannot be allowlisted or logs show distributed login attacks.

---

## 9. HTTP security headers

| Header | Value |
|---|---|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` |
| `Content-Security-Policy` | `default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | Deny camera, microphone, geolocation, payment, USB |
| `Cross-Origin-Opener-Policy` | `same-origin` |

- No inline scripts. If a UI library needs inline styles, allow them with a **nonce or hash**, never `'unsafe-inline'` for scripts. Any CSP change needs a security reviewer.
- TLS 1.2+ only, on every hop (including load balancer → server).

---

## 10. Secrets and keys

| Where | Store |
|---|---|
| Local development | `dotnet user-secrets` (outside the repo, per user) |
| Servers | Windows certificate store / server secret store (on-prem) |
| CI | The CI platform's secret store |

- **Never** in `appsettings*.json`, `.env`, source code, the front end, logs or git.
- **Data Protection keys** persisted and encrypted with a certificate, shared by Api and Worker.
- **Audit-chain HMAC key** outside the database, with a key ID for rotation ([database.md §2.2](database.md#22-layer-2--keyed-hash-chain)).
- **gitleaks** runs in CI on every PR. A leaked secret is rotated, not just deleted from history.

---

## 11. Input, files and output

- **Validate on the server, always** (built-in validation). Front-end validation is for UX only.
- **Imports (CSV / xlsx):** size limit, type checked by content (magic bytes), parsed by a proven reader, preview before commit, one transaction. Formulas in cells are read as values, never evaluated.
- **Attachments / evidence:** size limit, allowed types checked by content, random stored names, stored outside the web root, served with `Content-Disposition: attachment`, antivirus scan where available.
- **CSV export:** prefix cells starting with `=`, `+`, `-`, `@` to prevent formula injection.
- **Output:** React escapes by default. No `dangerouslySetInnerHTML`.
- **No outbound calls to user-supplied URLs** (SSRF).

---

## 12. Security logging

Logged as security events (tagged, retained 1 year+):

- Login success and failure (user, IP, user agent), MFA success and failure, lockouts.
- IP blocks and unblocks, allowlist changes.
- 401 / 403 responses.
- User, role, permission and MFA changes.
- Audit-chain verification failures.

Never logged: passwords, MFA codes, tokens, cookies, full request bodies, personal data values.

Alerts on spikes of 401/403/5xx, IP blocks and any audit-chain mismatch.

---

## 13. Threats and controls (OWASP Top 10, 2025)

| # | Risk | Main controls |
|---|---|---|
| A01 | Broken Access Control | Deny by default, three-level checks (§6.2), `PublicId`, authorization matrix test, no SSRF |
| A02 | Security Misconfiguration | Headers (§9), no Swagger or detailed errors in production, config reviewed per environment |
| A03 | Software Supply Chain Failures | [Dependency policy](../standards/tooling-and-dependencies.md), lock files, project-local tooling, `ignore-scripts`, CODEOWNERS on package files |
| A04 | Cryptographic Failures | TLS 1.2+/HSTS, TDE, framework crypto only, keys in secret stores |
| A05 | Injection | EF parameterisation, no string-built SQL, React escaping, server-side validation |
| A06 | Insecure Design | Status state machine in Domain, abuse-case tests, this document reviewed per phase |
| A07 | Authentication Failures | Identity, MFA, lockout, breached-password check, session revocation, rate-limited login |
| A08 | Software / Data Integrity Failures | Hash-chained audit trail + external digests, protected CI, branch protection, reviewed migrations |
| A09 | Logging & Alerting Failures | §12 |
| A10 | Mishandling of Exceptional Conditions | Global exception handler, fail closed, transactions (no partial writes) |

Checklist: **OWASP ASVS 5.0 Level 2**. Before go-live: OWASP ZAP against staging; external pen test (may follow the first release).

---

## 14. Do / Don't

**Do**
- Put `[HasPermission(...)]` on every endpoint and add it to the authorization matrix.
- Check organisation and record access in the handler before reading or changing data.
- Take user and time from `ICurrentUser` and `TimeProvider`.
- Return a different DTO for a different audience.
- Ask a security reviewer before touching auth, middleware, headers, crypto, IP rules or the audit trail.

**Don't**
- Don't trust anything from the browser: IDs, flags, user names, times, roles, calculated values.
- Don't check role names in code.
- Don't add anonymous endpoints outside §3.
- Don't store tokens or sensitive data in `localStorage` / `sessionStorage`.
- Don't write crypto, password hashing or token code yourself; don't use `crypto-js`.
- Don't log secrets or personal data; don't enable detailed errors in production.
- Don't load scripts, fonts or styles from a CDN.
