# Front-end standards (React / TypeScript)

> **Status:** ✅ Reviewed by the lead (2026-10-28)
> **Last updated:** 2026-10-28
> **Purpose:** how front-end code is written, and what a good screen looks like in this portal.

Security rules: [architecture/security.md](../architecture/security.md). Packages: [tooling-and-dependencies.md §6.3](tooling-and-dependencies.md#63-front-end--runtime).

---

## 1. Tooling

| Concern | Use |
|---|---|
| Build / dev server | Vite (dev server proxies `/api` to the local API — same origin, no CORS) |
| Language | TypeScript, `strict: true`, `noUncheckedIndexedAccess: true` |
| UI | **Mantine** only (D-07) |
| Server state | TanStack Query |
| Tables | TanStack Table rendered with Mantine components |
| Routing | `react-router` |
| Lint / format | ESLint + Prettier; CI fails on any error |
| Tests | Vitest + Testing Library; Playwright for end-to-end |
| API types | Generated from the OpenAPI document with `openapi-typescript` — never hand-written |

Production build: `npm run build` writes to `frontend/dist`. For deployment that output is copied into the Api's `wwwroot` and served by the Api (copy step and SPA hosting: planned, with the security pipeline).

---

## 2. Folder structure (target — `app/` exists today)

```text
frontend/src/
├─ app/              # app shell: providers, router, layout, error boundary, theme
├─ api/
│  ├─ client.ts      # the one fetch wrapper (antiforgery header, errors, traceId)
│  └─ schema.d.ts    # GENERATED from OpenAPI — do not edit
├─ auth/             # login, MFA, current user, permission helpers
├─ features/
│  ├─ practices/     # one folder per module: pages/, components/, queries.ts, types.ts
│  ├─ calendar/
│  └─ …
├─ shared/           # components and hooks used by 2+ features (not a dumping ground)
└─ main.tsx
```

- Code used by one feature stays in that feature.
- Move something to `shared/` only when a second feature needs it.
- Features don't import from each other. Shared needs go to `shared/`.

---

## 3. Naming

| Item | Convention | Example |
|---|---|---|
| Components and their files | PascalCase | `PracticeList.tsx` |
| Hooks | `use` + PascalCase | `usePractices` |
| Other files | camelCase | `queries.ts` |
| Types | PascalCase, no `I` prefix | `PracticeSummary` |
| Constants | `UPPER_SNAKE_CASE` for true constants | `PAGE_SIZE` |
| Query keys | Array, feature first | `['practices', 'list', filters]` |

---

## 4. Talking to the API

- **All** requests go through `api/client.ts` (native `fetch`). No `fetch` calls in components.
- The client: sends cookies (same origin), adds the antiforgery header to every non-GET request, parses ProblemDetails, and keeps the `traceId` from errors.
- All server data goes through **TanStack Query** (`useQuery` / `useMutation`) in the feature's `queries.ts`. No `useEffect` + `fetch` + `useState` for server data.
- After a mutation, invalidate the related query keys.
- A 401 response sends the user to the login page. A 403 shows "You don't have access" — never a blank screen.
- Regenerate `schema.d.ts` when the API changes (`npm run api:types`); CI fails if it is stale.

---

## 5. Authentication and permissions in the SPA

- The SPA never sees a token. The session is an HttpOnly cookie.
- After login, the SPA loads `GET /api/v1/me` (user, organisation, permissions) and keeps it in memory (TanStack Query).
- Use permissions to **hide or disable** buttons and menu items. This is UX only — the server enforces every rule.
- Check **permissions**, not role names: `can('Practices.Create')`.

---

## 6. Forms

- Mantine form components; React state or Mantine's `useForm`. No extra form library.
- Send only the fields the user may set.
- Validate on the client for fast feedback (optionally with `zod`), but expect and display server validation errors (400 ValidationProblemDetails) next to the fields.
- Disable the submit button while saving. Prevent double submits.
- Warn before leaving a form with unsaved changes.

---

## 7. UI and UX rules

The goal is **minimal, modern, easy to understand, few clicks.**

| Rule | Detail |
|---|---|
| Three states for every data view | **Loading** (skeleton), **empty** (helpful message + next action), **error** (message + retry + reference) |
| Errors show a reference | "Something went wrong. Reference: 7f3a…" using the `traceId`, so support can find the request |
| Notifications | Mantine notifications: success briefly, errors stay until dismissed |
| Tables | Server-side paging, sorting and filtering; sticky header; empty state; row click opens details |
| Few clicks | Common actions visible on the page, not hidden in menus; sensible defaults |
| Destructive actions | Confirmation dialog that names the item ("Archive practice *X*?"); the UI says **Archive**, not Delete |
| Dates | Show in the user's or practice's time zone, and say which; format with `Intl.DateTimeFormat` |
| Consistency | Use the theme's spacing, colours and typography. No hard-coded colours or pixel sizes in components |
| Status colours | Defined once in the theme (e.g. Reserved / Confirmed / Live) |

**Accessibility (WCAG 2.1 AA):** every input has a label, everything works with the keyboard, focus is visible, colour is never the only signal, contrast meets AA.

---

## 8. Security

- No tokens or sensitive data in `localStorage`, `sessionStorage` or IndexedDB. Small UI preferences (e.g. theme) are fine.
- No `dangerouslySetInnerHTML`. No `eval`, `new Function`, or inline event strings.
- No scripts, fonts or styles from a CDN — everything is bundled.
- No secrets in `VITE_*` variables — they are public.
- No inline `<script>` in `index.html` (CSP blocks it).
- Source maps are not published to production.
- Don't build URLs from user input for navigation outside the app.

---

## 9. TypeScript

- No `any`. Use `unknown` and narrow it.
- No non-null assertions (`!`) unless the reason is obvious from the line above.
- Types for API data come from `schema.d.ts`; derive feature types from them.
- Prefer `type` aliases and union types; avoid enums (use string unions).

---

## 10. Do / Don't

**Do**
- Copy the reference feature (`features/practices`) for new features.
- Keep components small; move logic into hooks.
- Handle loading, empty and error states in every view.
- Write a test for each component with logic, and a Playwright test for each main user journey.

**Don't**
- Don't add a second UI library or a state-management library.
- Don't use axios, lodash, moment or jQuery.
- Don't call `fetch` from components.
- Don't edit `api/schema.d.ts` by hand.
- Don't trust the client: hiding a button is not security.
