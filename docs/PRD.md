# Finance Manager — Product Requirements Document

**Status:** Draft
**Owner:** Product/Engineering
**Last updated:** 2026-10-13
**Version:** 0.4

---

## 1. Overview

Finance Manager is a personal finance platform for households that lets each
member consolidate their money across every account they hold —
bank accounts, brokerages, benefits cards, and cash — record what they earn and
spend, organize it into categories and budgets, and understand where the
household's money goes over time.

Anyone can sign up. Registering creates a brand-new household with that person
as its **Owner**; a second person joins that same household (as a **Member**)
using an invite code the Owner generates, instead of creating their own. A
person isn't limited to one household either — the same login can be Owner of
their own household and a Member of someone else's (a partner's, a parent's),
switching which one is "active" without logging in again. Some accounts are
personal and visible only to their owner; others are shared with the household
(e.g. joint checking). An Owner manages their household's structure (accounts,
categories, budgets); Members log transactions day to day. See §5 and §6.7 for
the detailed model.

This document describes the product as a whole: who it is for, what problems it
solves, the feature set, and the requirements that guide the backend currently
under development.

### 1.1 Current state

The backend is in early development. A clean-architecture .NET solution exists with
the first domain concept — **Financial Accounts** — being built out. Everything
else in this document is planned scope.

---

## 2. Problem statement

People's money is scattered. A typical person has a checking account, a savings
account, one or more investment accounts, a meal/benefit card, and cash in their
wallet. Each lives in a separate app with its own view, so no single place answers
the questions that actually matter:

- How much money do I have in total, right now?
- Where did my money go last month?
- Am I spending more than I earn?
- Am I on track against the budget I set for myself?

Spreadsheets can answer these but demand constant manual upkeep and break easily.
Bank apps only see their own account. Finance Manager exists to be the single,
trustworthy ledger the user controls.

---

## 3. Goals & non-goals

### 3.1 Goals

- Let anyone **sign up and start a household** of their own, in control of their own data from the first click.
- Let a second household member **join via an invite code**, without the Owner typing their credentials for them.
- Give each household member **one consolidated view** of their own and shared accounts and balances.
- Let household members **record income and expenses** quickly and reliably.
- Let the household **categorize and budget** so spending is intentional, not accidental.
- Provide **reports and insights** that answer the questions in §2, both per-person and household-wide.
- Keep financial data **private where it should be, shared where it should be, and under the household's control.**

### 3.2 Non-goals (for now)

- Automatic bank sync / Open Finance aggregation. Entry is manual in v1.
- Email-based invites, password reset, or any flow requiring sending email. Joining a household is done entirely via invite code, copy/pasted or shared by the Owner through any channel they like — the backend never sends email.
- Tax filing, invoicing, or full accounting (double-entry ledger, depreciation, etc.).
- Native mobile apps. The backend serves an API; clients come later.
- Investment performance analytics (returns, allocation). Brokerages are tracked
  as balances only in v1.

---

## 4. Target users

| Persona | Description | Primary need |
|---|---|---|
| **The household owner** | Manages the household's structure — accounts, categories, budgets — and wants a full household picture. | Consolidation + reports + control |
| **The household member** | Logs their own income/expenses and sees shared accounts, but doesn't restructure the household. | Fast, reliable entry |
| **The budgeter** | Has income/expense targets (personal or household) and wants to stay within them. | Budgets + alerts |

Primary market: households in Brazil managing personal finances together
(currency, benefit cards, and institution types reflect this). Each household is
independent and private from every other; the platform hosts many of them.

---

## 5. Key concepts (domain model)

| Concept | Meaning | Status |
|---|---|---|
| **Household** | A self-contained group of Users and their Financial Accounts. The platform hosts many, each fully private from every other. | Built, but currently only created via a one-time seed — self-registration (HH-6) is planned |
| **User** | A person with their own login, who can belong to more than one household, each with its own independent role. One household is "active" for a session at a time. | Built (single-household); multi-household + active-session switching is planned |
| **HouseholdMembership** | The link between a User and a Household, carrying that user's role (`Owner`/`Member`) *in that household specifically* — the same user can be `Owner` of one and `Member` of another. | Planned |
| **HouseholdInvite** | A code an Owner generates that lets one person join that Owner's household as a Member — either by registering fresh, or (if already a user elsewhere) by redeeming it as an added membership. | Planned |
| **Financial Account** | A place money is held: bank, brokerage, benefit, cash, other. Has a name, type, optional institution, an owning User, and a visibility (`Personal` or `Shared`). | In progress — ownership/visibility not yet modeled |
| **Transaction** | A single money movement — income or expense — against an account, with amount, date, description, recorded by a User. | Planned |
| **Category** | A label for transactions (e.g. Groceries, Salary, Rent), optionally hierarchical. Household-wide, not per-user. | Planned |
| **Budget** | A spending target for a category over a period (e.g. R$800/month on Groceries). Can be personal (one user) or household-wide. | Planned |
| **Transfer** | Movement of money between two accounts the same user (or household, if both shared) controls (not income/expense). | Planned |
| **Report** | A derived view: balances, cash flow, spending by category, budget progress — scoped to one user or the whole household. | Planned |

Existing account types: `Bank`, `Brokerage`, `Benefit`, `Cash`, `Other`.

**Visibility rule:** a `Personal` account is visible to its owning User and to the
household Owner. A `Shared` account is visible to every User in the household, and
any member can record transactions against it, subject to role permissions in §6.7.
Only the household Owner sees everything; a Member sees their own Personal accounts
plus all Shared accounts, but not another Member's Personal accounts.

---

## 6. Features & requirements

Requirements use **MoSCoW** priority: **Must**, **Should**, **Could**.

### 6.1 Financial Accounts (in progress)

| ID | Requirement | Priority |
|---|---|---|
| FA-1 | User can create an account with a name, type, optional institution name, and visibility (Personal/Shared). | Must |
| FA-2 | User can view a list of accounts they can see: their own Personal accounts, all Shared household accounts, and — if they are the household Owner — every Member's Personal accounts too. | Must |
| FA-3 | User can view a single account's details, if they can see it per FA-2. | Must |
| FA-4 | Account Owner (the user who created it) or household Owner can update an account's name, type, institution, and visibility. | Must |
| FA-5 | Account Owner or household Owner can delete (or archive) an account. | Must |
| FA-6 | Each account exposes a current balance derived from its transactions. | Should |
| FA-7 | Deleting an account with transactions is prevented or requires confirmation. | Should |
| FA-8 | Changing a Personal account to Shared (or back) is an explicit, deliberate action — not a side effect of another edit. | Should |

### 6.2 Transactions

| ID | Requirement | Priority |
|---|---|---|
| TX-1 | User can record an income or expense against an account with amount, date, and description. | Must |
| TX-2 | User can assign a category to a transaction. | Must |
| TX-3 | User can edit and delete transactions. | Must |
| TX-4 | User can list/filter transactions by account, category, date range, and type. | Must |
| TX-5 | Recording a transaction updates the owning account's balance. | Must |
| TX-6 | User can record a transfer between two of their own accounts. | Should |
| TX-7 | User can attach a note or receipt reference to a transaction. | Could |
| TX-8 | User can set up recurring transactions (e.g. monthly rent). | Could |

### 6.3 Categories

| ID | Requirement | Priority |
|---|---|---|
| CAT-1 | User can create, rename, and delete categories. | Must |
| CAT-2 | Categories are typed as income or expense. | Should |
| CAT-3 | Categories can be nested (parent/child). | Could |
| CAT-4 | System seeds a sensible default set of categories for new users. | Should |

### 6.4 Budgets

| ID | Requirement | Priority |
|---|---|---|
| BUD-1 | User can set a spending budget for a category over a period (monthly to start). | Must |
| BUD-2 | User can see actual spend vs. budget for the current period. | Must |
| BUD-3 | User is alerted when spending approaches or exceeds a budget. | Should |
| BUD-4 | Budgets roll over / regenerate automatically each period. | Could |

### 6.5 Reports & insights

| ID | Requirement | Priority |
|---|---|---|
| RPT-1 | Net worth / total balance across all accounts at a point in time. | Must |
| RPT-2 | Cash flow (income vs. expense) over a chosen period. | Must |
| RPT-3 | Spending breakdown by category for a period. | Must |
| RPT-4 | Trend of balances and spending over time. | Should |
| RPT-5 | Exportable report (CSV) for a period. | Could |

### 6.6 Accounts & security (platform)

| ID | Requirement | Priority |
|---|---|---|
| SEC-1 | A user authenticates before accessing any financial data. | Must |
| SEC-2 | A user can only read their own Personal data and the household's Shared data; the household Owner can additionally read every Member's Personal data (see §6.7 for role rules on writes). | Must |
| SEC-3 | Monetary amounts are stored with exact precision (no floating-point money). | Must |
| SEC-4 | All amounts carry a currency; the user has a default currency. | Should |
| SEC-5 | Sensitive data is protected in transit (HTTPS) and at rest. | Must |

### 6.7 Household & roles

| ID | Requirement | Priority |
|---|---|---|
| HH-1 | A User can belong to more than one Household, with an independent role (`Owner` or `Member`) in each — e.g. Owner of their own household and a Member of someone else's at the same time. At any moment, a session is scoped to exactly one of the user's households ("active"); switching is explicit (see HH-10). | Must |
| HH-2 | An Owner can create, edit, and delete Shared accounts, categories, and household-wide budgets — within whichever household they are an Owner of and currently active in. | Must |
| HH-3 | A Member can record and edit transactions on any account they can see (own Personal + household Shared), and manage their own Personal accounts and budgets — within the currently active household. | Must |
| HH-4 | A Member cannot view, edit, or delete another Member's Personal accounts, transactions, or budgets. Only that household's Owner can view every Member's Personal data within it; write access to another member's Personal data is never granted, even to the Owner, unless explicitly stated otherwise (see §11). | Must |
| HH-5 | A Member cannot restructure Shared accounts, categories, or household-wide budgets (create/edit/delete) — only that household's Owner can. | Should |
| HH-6 | A person can register a new account at any time. Registering with no invite code creates a brand-new Household with that person as its Owner, active immediately. Registering with a valid invite code instead joins the Household that code belongs to, as a Member, active immediately. | Must |
| HH-7 | Household-wide reports (net worth, cash flow) aggregate across Shared accounts and, at the viewer's choice, their own Personal accounts — scoped to the currently active household only. | Should |
| HH-8 | An Owner can generate an invite code for their household and share it through any channel they choose (the app never sends it). | Must |
| HH-9 | An invite code identifies which household it joins and nothing else — it does not grant any access by itself; it only shapes what registering or joining with it does (see HH-6, HH-11). | Must |
| HH-10 | An already-logged-in user can switch their active household to any other household they belong to, without re-entering credentials. | Must |
| HH-11 | An already-logged-in user can redeem a valid invite code to gain a Member membership in an additional household, without creating a new account. | Must |

---

## 7. User stories (representative)

- *As a user, I want to add all my accounts so I can see my total balance in one place.*
- *As a user, I want to log an expense in a few seconds so I actually keep my records up to date.*
- *As a user, I want to tag expenses by category so I can see where my money goes.*
- *As a user, I want to set a monthly grocery budget so I stop overspending.*
- *As a user, I want a monthly summary of income vs. expense so I know if I'm saving.*
- *As a user, I want to move money between my accounts without it counting as income or expense.*
- *As a household owner, I want to create a shared checking account so both of us can see and log against it.*
- *As a household member, I want to log an expense on the shared grocery account without needing the owner's permission.*
- *As a household owner, I want to see our combined net worth, while each member can still keep some accounts private.*

---

## 8. Technical context

The product is delivered as a backend API; UI clients consume it.

- **Platform:** .NET 10 / ASP.NET Core
- **Architecture:** Clean architecture — `Domain`, `Application`, `Infrastructure`, `Api`
- **Persistence:** PostgreSQL 18 via Entity Framework Core (Npgsql)
- **API docs:** OpenAPI + Scalar reference UI
- **Local dev:** PostgreSQL via Docker Compose

Architectural principles to uphold:

- Domain entities own their invariants (rich model, private setters, no anemic DTOs leaking into the domain).
- Money is represented with a precise decimal + currency, never a float/double.
- Each feature is organized vertically (requests, responses, repository, controller).

---

## 9. Success metrics

| Metric | Why it matters |
|---|---|
| Weekly active logging (transactions recorded/week) | The product only works if records stay current. |
| % of users with ≥2 accounts connected | Measures the consolidation value prop. |
| % of users with ≥1 active budget | Measures the budgeting value prop. |
| Retention at 4 weeks | Whether the product becomes a habit. |
| Time-to-record a transaction | Entry friction is the main churn risk. |

---

## 10. Milestones (proposed)

| Phase | Scope |
|---|---|
| **M1 — Foundations** | Financial Accounts CRUD + balances. Auth. (current) |
| **M2 — The ledger** | Transactions + categories. The core logging loop. |
| **M3 — Intentional spending** | Budgets + spending-by-category report. |
| **M4 — Insight** | Cash flow, net worth, trends, transfers. |
| **M5 — Polish** | Recurring transactions, exports, alerts. |

---

## 11. Open questions

- Is auth self-hosted (ASP.NET Identity) or delegated to an external provider?
- Single currency per user, or multi-currency with conversion?
- Do we soft-delete (archive) accounts and transactions, or hard-delete?
- Are transfers modeled as a pair of linked transactions or a first-class entity?
- Which locale/currency formatting rules do we commit to for v1 (BRL only)?
- The Owner can *view* every Member's Personal accounts/transactions (confirmed).
  Can the Owner also *edit or delete* another Member's Personal data, or is write
  access still restricted to the owning Member only?
- Can budgets mix scopes — e.g. a household budget for "Groceries" alongside a
  member's personal budget for "Hobbies" — or is scope fixed per category?
- Invite code lifecycle: single-use (consumed on redemption) or reusable until the
  Owner regenerates it? Does it expire? Can an Owner have more than one active code
  at a time (e.g. to track who used which)?
- When a user has multiple households, how does login pick which one becomes
  "active" by default (most recently active? the first one created? must the user
  choose explicitly even at login)?

---

*This is a living document. Update it as scope and decisions evolve.*
