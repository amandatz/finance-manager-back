# Frontend implementation spec — Login & Auth

> Hand this document to an AI assistant (or developer) implementing the login/auth screens of
> the Finance Manager frontend. It describes exactly what the backend API expects and returns —
> nothing here requires reading the backend source.

## Overview

The backend is a JWT API with **public self-registration** and **refresh tokens**. A user can
belong to more than one household (with an independent role in each); at any moment a session is
scoped to exactly one of them — the **active household** — and switching is explicit.

- Base path for every endpoint below: `/api/v1`
- Auth scheme: `Authorization: Bearer <access token>` header on every authenticated request
- **Access tokens expire 12 hours** after issuance. **Refresh tokens last 30 days** (sliding — every
  successful refresh issues a new 30-day window) and are **single-use**: calling `/auth/refresh`
  returns a brand-new access+refresh pair and immediately invalidates the one you sent. Always
  overwrite both stored tokens after login, register, refresh, switch-household, or join — never
  keep using an old refresh token once you've received a newer one.
- Replaying an already-used refresh token (e.g. two concurrent requests both trying to refresh with
  the same stale token) doesn't just fail that one call — it revokes **every** active token for that
  session, as a reuse-detection measure. See **Token storage & refresh** below for how to avoid
  triggering this by accident.
- All error responses use [RFC 7807 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) —
  see **Error shape** below before writing any error handling.

## Endpoints

### `POST /api/v1/auth/register`

Public — no token required. Creates a brand-new account. There are two mutually exclusive ways to
call it:

**Create a new household (you become its Owner):**
```json
{ "name": "Household Owner", "email": "owner@example.com", "password": "SomePassword123", "inviteCode": null, "householdName": "The Smiths" }
```

**Join an existing household via invite code (you become a Member):**
```json
{ "name": "New Member", "email": "member@example.com", "password": "SomePassword123", "inviteCode": "9T4ALTYR", "householdName": null }
```
`inviteCode` is case-sensitive, 8 characters, from an alphabet that excludes ambiguous
characters (`0`/`O`, `1`/`I`) — safe to uppercase user input before submitting, but don't try to
auto-correct `0`↔`O` or `1`↔`I`, since the alphabet deliberately never uses one of each pair.

Password policy: **minimum 8 characters, no other requirements** (no forced uppercase/digit/symbol).

**Success — `201 Created`:**
```json
{ "accessToken": "eyJhbGciOi...", "refreshToken": "FyoH_R2AHUiuSjm7TrnCEjHgc9YIvZ6CfgMpYv1cIXk" }
```
This logs the new user in immediately — no separate login step needed after registering. Store
both tokens (see **Token storage & refresh**).

**Failures:**
- `400 Bad Request` — invalid/unknown/already-used invite code, or a password under 8 characters.
  Plain Problem Details body (not the validation shape) — check `title`.
- `400 Bad Request` — no `inviteCode` **and** no `householdName`, or a malformed `name`/`email`.
  **Validation error shape** (see below), naming the offending field (`HouseholdName`, `Name`, or
  `Email`).
- `409 Conflict` — email already in use (regardless of whether an invite code was supplied). If a
  user with this email already exists and wants to join another household, they must log in and
  use `POST /households/join` instead of registering again.

---

### `POST /api/v1/auth/login`

Public — no token required.

**Request body:**
```json
{ "email": "owner@example.com", "password": "the-password" }
```

**Success — `200 OK`:**
```json
{ "accessToken": "eyJhbGciOi...", "refreshToken": "FyoH_R2AHUiuSjm7TrnCEjHgc9YIvZ6CfgMpYv1cIXk" }
```
If the user belongs to more than one household, login activates **the one they joined first** —
there's no way to pick a different one at login time. To land somewhere else, log in normally
then call `POST /auth/switch-household`.

**Failure — `401 Unauthorized`:**
A wrong password and an unknown email return the **exact same** Problem Details body on purpose —
do not build UI that tries to distinguish "wrong password" from "no such account." Show one generic
message like "Invalid email or password."

---

### `POST /api/v1/auth/refresh`

Public (identity is proven by possessing a valid refresh token, not a Bearer header).

**Request body:**
```json
{ "refreshToken": "FyoH_R2AHUiuSjm7TrnCEjHgc9YIvZ6CfgMpYv1cIXk" }
```

**Success — `200 OK`:** same shape as login — a brand-new access+refresh pair, scoped to whatever
household/role was active when the refresh token was issued. Overwrite both stored tokens; the
refresh token you just sent is now dead.

**Failure — `401 Unauthorized`:** unknown, expired, or already-used (replayed) token. Treat this as
"the session is gone" — send the user to the login screen. Don't retry automatically.

---

### `POST /api/v1/auth/logout`

Public, same shape as refresh.

**Request body:**
```json
{ "refreshToken": "FyoH_R2AHUiuSjm7TrnCEjHgc9YIvZ6CfgMpYv1cIXk" }
```

**Success — `204 No Content`:** the refresh token is revoked. This is idempotent — calling it again
with the same (now-dead) token, or with a token that was never valid, still returns `204`. This only
kills the one session that owned this refresh token; if the user is logged in elsewhere (another
device, or another household they switched to), those sessions are unaffected.

After calling this, discard both stored tokens client-side. The access token itself isn't revoked
(there's no server-side access-token blocklist) — it simply stops being renewable, and will stop
working on its own within 12 hours.

---

### `POST /api/v1/auth/switch-household`

Requires a valid Bearer token. Re-scopes your session to a different household you already belong
to — use `GET /users/me/households` first to know which household ids are valid for this call.

**Request body:**
```json
{ "householdId": "40bac680-f52e-424e-b9ad-7221e1a27e5d" }
```

**Success — `200 OK`:** same shape as login — a new access+refresh pair scoped to the target
household, with whatever role you hold there (which may differ from your role in the household you
switched away from). Overwrite both stored tokens. Your previous session (for the household you
switched away from) is **not** revoked — it's just not the one these new tokens point to.

**Failures:**
- `404 Not Found` — you don't have a membership in that household id. Plain Problem Details body.
- `401 Unauthorized` — no/invalid/expired access token.

---

### `GET /api/v1/users/me`

Requires a valid Bearer token. Returns the logged-in user's profile **for the currently active
household** — there is no separate "decode the JWT for the user's name" step; call this endpoint
instead.

**Success — `200 OK`:**
```json
{
  "id": "7ed8ff66-1cc8-4f87-9b1a-3f5a27e17e5a",
  "name": "Household Owner",
  "email": "owner@example.com",
  "role": 1,
  "householdId": "40bac680-f52e-424e-b9ad-7221e1a27e5d"
}
```
`role`/`householdId` reflect the **active** household — the one the current access token is scoped
to, not necessarily the only one the user belongs to. After a successful `switch-household` or
`join`, call this again (or just read the new token's claims) to get the refreshed view.

**⚠️ `role` is a number, not a string.** `HouseholdRole` is `Member = 0`, `Owner = 1`. Do not write
`if (user.role === "Owner")` — it will silently always be false. Use `if (user.role === 1)`, or
define a local enum/const map (`{ MEMBER: 0, OWNER: 1 }`) in the frontend and compare against that.

**Failure — `401 Unauthorized`:** no token, or an invalid/expired token. Empty body. Treat this as
"redirect to login" everywhere it can happen, including from endpoints below.

---

### `GET /api/v1/users/me/households` — list your memberships

Requires a valid Bearer token. Returns **every** household the caller belongs to, regardless of
which one is currently active — use this to build a household switcher.

**Success — `200 OK`:**
```json
[
  { "householdId": "40bac680-f52e-424e-b9ad-7221e1a27e5d", "householdName": "The Smiths", "role": 1 },
  { "householdId": "db327e58-c88b-4d9d-9029-6d74d409edb9", "householdName": "Roommates", "role": 0 }
]
```
Same `role` numeric convention as `/me` above. The array has no guaranteed order — sort client-side
if you need a specific presentation order (e.g. alphabetical, or active-household-first by cross-
referencing against `/me`'s `householdId`).

---

### `POST /api/v1/households/join` — join an additional household

Requires a valid Bearer token. For an **already-registered, logged-in** user who wants to join a
second (or third...) household via someone else's invite code — registering again is not an option
once an account exists (register always rejects a taken email with `409`).

**Request body:**
```json
{ "inviteCode": "9T4ALTYR" }
```

**Success — `200 OK`:** new access+refresh pair, scoped to the newly joined household as a Member.
Overwrite both stored tokens — joining also activates the household you just joined. The user's
other memberships are untouched; call `/users/me/households` afterward to see the full list.

**Failures:**
- `400 Bad Request` — invalid/unknown/already-used invite code.
- `409 Conflict` — the caller is already a member of the household that code belongs to.
- `401 Unauthorized` — no/invalid/expired access token.

---

### `POST /api/v1/households/invite-code` — generate an invite code

Requires a valid Bearer token **and** the caller's active role must be Owner (`role === 1`). No
request body. Generates a fresh code for the caller's **active** household (switch households first
if you need a code for a different one).

**Success — `200 OK`:**
```json
{ "inviteCode": "9T4ALTYR" }
```
Generating a new code immediately invalidates any previously issued one for that household — only
the latest code works. There's no way to list or revoke a code without generating a replacement.

**Failures:**
- `403 Forbidden` — caller's active role is Member, not Owner. Empty body; this screen should
  simply not be reachable by a Member in the first place (hide/disable the "invite" UI based on
  `role` from `/me`).
- `401 Unauthorized` — no/invalid/expired access token.

---

### `POST /api/v1/users` — add a household member directly

Requires a valid Bearer token **and** the caller's active role must be Owner. This is a *separate*
mechanism from invite codes — it directly creates a new account as a Member of the caller's active
household in one call, with no invite step. Use this when the Owner wants to set someone up
themselves rather than hand them a code to self-register with.

**Request body:**
```json
{ "name": "Partner Name", "email": "partner@example.com", "password": "TempPassword123" }
```
Same password policy as registration: minimum 8 characters.

**Success — `201 Created`:**
```json
{ "id": "72901dac-fbb0-484e-9269-a1ec71e3fd18", "name": "Partner Name", "email": "partner@example.com" }
```

**Failures:**
- `403 Forbidden` — caller is not an Owner. Empty body.
- `409 Conflict` — email already in use. Problem Details body, `title` describes it.
- `400 Bad Request` — password under 8 characters, or missing/invalid `name`/`email`. This one uses
  the **Validation error shape** (see below), with `errors.Password` or `errors.Name`/`errors.Email`
  naming the field.

---

### `POST /api/v1/users/me/password` — change your own password

Requires a valid Bearer token. Changes the *caller's own* password — there is no admin/reset flow
for one user to change another's password.

**Request body:**
```json
{ "currentPassword": "OldPassword123", "newPassword": "NewPassword456" }
```

**Success — `204 No Content`:** empty body. Existing access/refresh tokens are still valid after
this — there's no forced re-login, but you may choose to prompt one anyway.

**Failures:**
- `400 Bad Request` — either the current password was wrong, or the new password fails the
  8-character minimum. Both cases return a plain Problem Details body (not the validation shape) —
  check `title` for the human-readable reason; there's no machine-readable error code to switch on.
- `401 Unauthorized` — token missing/invalid (not for a wrong current password — that's 400, see above).

## Error shape

Every error response is JSON. There are two shapes:

**Plain Problem Details** (401, 403, 404, 409, 500, and most 400s):
```json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2", "title": "Invalid email or password.", "status": 401, "traceId": "00-..." }
```
Only `title` and `status` are useful for UI. `traceId` is a correlation id for support/debugging —
never shown to the user, and it changes on every request (don't use it as any kind of key).

**Validation Problem Details** (400 on request-shape errors — missing/malformed fields):
```json
{ "title": "One or more validation errors occurred.", "status": 400, "errors": { "Password": ["'Password' must not be empty."] } }
```
`errors` is a dictionary keyed by field name (PascalCase: `Name`, `Email`, `Password`,
`HouseholdName`) with an array of messages per field. Use this to show inline field errors; fall
back to `title` for anything else. Each endpoint's **Failures** section above says which shape to
expect for each `400` — they're not interchangeable: an invalid invite code is the plain shape (it's
a business rule, not a malformed request), while a missing household name is the validation shape.

A `500` can happen (unexpected server error) and always uses the plain shape with a generic
`title` — never retry automatically, just show a generic "something went wrong" message.

## Token storage & refresh

Store **both** the access token and the refresh token — there's no cookie, no CORS-credential
requirement; pick whatever your stack's normal secure-storage pattern is (e.g. an in-memory store
plus `sessionStorage`/`localStorage` for persistence across reloads, or a secure storage API on
mobile). Every authenticated request attaches the access token as:
```
Authorization: Bearer <stored access token>
```

**Refresh is single-use — guard against double-firing it.** Because replaying an already-used
refresh token kills the whole session (not just that one call), the most common way to accidentally
break a logged-in user is firing two refresh calls at once — e.g. two API calls both get a `401` at
nearly the same moment and each independently tries to refresh. Use a single in-flight refresh
promise/mutex so concurrent `401`s share one refresh attempt and all wait on its result, rather than
each calling `/auth/refresh` with the same (soon to be stale) token.

**A typical flow:**
1. On app load, if a refresh token is stored, consider proactively refreshing once rather than
   waiting for the access token to expire mid-session — simpler than decoding `exp` and scheduling
   a timer, and avoids a user-visible failure on the first request after a long idle period.
2. On any `401` from an authenticated endpoint, attempt one refresh (via the shared mutex above),
   retry the original request once with the new access token, and if the refresh itself fails,
   treat it as a full logout — clear stored tokens and send the user to the login screen.
3. After login, register, refresh, switch-household, or join, always overwrite **both** stored
   tokens with the ones just returned — never keep an old refresh token "just in case."

## What is explicitly out of scope (don't build it)

- "Forgot password" / email-based reset — no email infrastructure exists on the backend at all.
- Removing a member, changing someone's role, or leaving a household — no endpoint exists for any
  of these yet.
- Listing or revoking an invite code without generating a replacement — generating a new one is the
  only way to invalidate the old one.
- Rate limiting on invite-code generation or redemption — not implemented server-side.
- A "primary"/default household setting the user can configure — which household activates by
  default at login (the earliest-joined one) is fixed and not user-configurable.
