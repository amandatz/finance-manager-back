# Frontend implementation spec — Login & Auth

> Hand this document to an AI assistant (or developer) implementing the login/auth screens of
> the Finance Manager frontend. It describes exactly what the backend API expects and returns —
> nothing here requires reading the backend source.

## Overview

The backend is a stateless JWT API. There is **no cookie session**, **no refresh token**, and
**no public self-registration** — this is a single-household app with exactly one seeded Owner
account; the Owner adds every other household member through the app itself, not a signup form.

- Base path for every endpoint below: `/api/v1`
- Auth scheme: `Authorization: Bearer <token>` header on every request after login
- Access tokens expire **12 hours** after issuance, with no way to refresh — when a token expires,
  the frontend must send the user back to the login screen. There is no silent-refresh flow to build.
- All error responses use [RFC 7807 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) —
  see **Error shape** below before writing any error handling.

## Endpoints

### `POST /api/v1/auth/login`

Public — no token required. This is the only auth entry point; there is no register endpoint.

**Request body:**
```json
{ "email": "owner@example.com", "password": "the-password" }
```

**Success — `200 OK`:**
```json
{ "accessToken": "eyJhbGciOi..." }
```
Store this token (see **Token storage** below). There is no other field in the success response —
no refresh token, no expiry timestamp. If you need the expiry, decode the JWT client-side (its
`exp` claim is a standard Unix timestamp).

**Failure — `401 Unauthorized`:**
A wrong password and an unknown email return the **exact same** Problem Details body on purpose —
do not build UI that tries to distinguish "wrong password" from "no such account." Show one generic
message like "Invalid email or password."

---

### `GET /api/v1/users/me`

Requires a valid Bearer token. Use this right after login (and/or on app load, if a token is
already stored) to get the logged-in user's profile — there is no separate "decode the JWT for
the user's name" step; call this endpoint instead.

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

**⚠️ `role` is a number, not a string.** `HouseholdRole` is `Member = 0`, `Owner = 1`. Do not write
`if (user.role === "Owner")` — it will silently always be false. Use `if (user.role === 1)`, or
define a local enum/const map (`{ MEMBER: 0, OWNER: 1 }`) in the frontend and compare against that.

**Failure — `401 Unauthorized`:** no token, or an invalid/expired token. Empty body. Treat this as
"redirect to login" everywhere it can happen, including from endpoints below.

---

### `POST /api/v1/users` — add a household member

Requires a valid Bearer token **and** the caller's role must be Owner (`role === 1`). There is no
separate "invite" flow — the Owner directly sets the new member's name, email, and a temporary
password in one call.

**Request body:**
```json
{ "name": "Partner Name", "email": "partner@example.com", "password": "TempPassword123" }
```
Password policy: **minimum 8 characters, no other requirements** (no forced uppercase/digit/symbol).

**Success — `201 Created`:**
```json
{ "id": "72901dac-fbb0-484e-9269-a1ec71e3fd18", "name": "Partner Name", "email": "partner@example.com" }
```

**Failures:**
- `403 Forbidden` — caller is not an Owner. Empty body; this screen should simply not be reachable
  by a Member in the first place (hide/disable the "add member" UI based on `role` from `/me`).
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

**Success — `204 No Content`:** empty body. After this, the user's existing token is still valid
until it naturally expires — there's no forced re-login, but you may choose to prompt one anyway.

**Failures:**
- `400 Bad Request` — either the current password was wrong, or the new password fails the
  8-character minimum. Both cases return a plain Problem Details body (not the validation shape) —
  check `title` for the human-readable reason; there's no machine-readable error code to switch on.
- `401 Unauthorized` — token missing/invalid (not for a wrong current password — that's 400, see above).

## Error shape

Every error response is JSON. There are two shapes:

**Plain Problem Details** (401, 403, 404, 409, 500, and the two 400s on change-password):
```json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2", "title": "Invalid email or password.", "status": 401, "traceId": "00-..." }
```
Only `title` and `status` are useful for UI. `traceId` is a correlation id for support/debugging —
never shown to the user, and it changes on every request (don't use it as any kind of key).

**Validation Problem Details** (400 on login/create-member field errors):
```json
{ "title": "One or more validation errors occurred.", "status": 400, "errors": { "Password": ["'Password' must not be empty."] } }
```
`errors` is a dictionary keyed by field name (PascalCase: `Name`, `Email`, `Password`) with an array
of messages per field. Use this to show inline field errors; fall back to `title` for anything else.

A `500` can happen (unexpected server error) and always uses the plain shape with a generic
`title` — never retry automatically, just show a generic "something went wrong" message.

## Token storage

No guidance is enforced server-side (there's no cookie, no CORS-credential requirement) — pick
whatever your frontend stack's normal secure-storage pattern is (e.g. an in-memory store plus
`sessionStorage`/`localStorage` for persistence across reloads, or a secure storage API on mobile).
Whatever you choose, every authenticated request must attach it as:
```
Authorization: Bearer <stored token>
```

## What is explicitly out of scope (don't build it)

- Public sign-up / registration screen — does not exist, will 404.
- "Forgot password" / email-based reset — no email infrastructure exists on the backend at all.
- Refresh tokens / silent re-auth / "remember me" beyond normal token storage — not supported.
- Removing a member or changing someone's role — no endpoint exists yet.
- Logout endpoint — there is none; "logging out" is purely a frontend action (discard the stored token).
