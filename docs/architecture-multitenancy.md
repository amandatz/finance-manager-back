# Multi-tenant isolation

## The tenant

The tenant in this system is the **Household**. Every household-scoped entity (today:
`FinancialAccount`, `CreditCard`) belongs to exactly one household and must never be visible,
readable, or writable by a request authenticated against a different household.

## How the per-request tenant is resolved

Every JWT access token already carries a `household_id` claim, minted by
`JwtTokenGenerator.GenerateAccessToken` on login, register, refresh, and
`POST /api/v1/auth/switch-household`. This claim is the single source of truth for "which
household is this request acting as" - there is no separate session store, no request header,
no query-string parameter.

`ICurrentHouseholdAccessor` (`FinanceManager.Application.Abstractions`) exposes this as a
single `Guid HouseholdId { get; }` property. Its implementation,
`HttpContextCurrentHouseholdAccessor` (`FinanceManager.Infrastructure.Identity`), reads the
claim off `IHttpContextAccessor.HttpContext.User` and throws `InvalidOperationException` if
it's missing or unparseable - which should never happen in practice, since every
household-scoped endpoint requires `[Authorize]`. It is registered **scoped** (not singleton),
because it depends on the per-request `HttpContext`.

## How entities opt in

1. Inherit `FinanceManager.Domain.Common.HouseholdScopedEntity` (which implements the marker
   interface `IHouseholdScoped`), or implement `IHouseholdScoped` directly if a different base
   class is unavoidable. Entities that additionally need multi-owner support (see below)
   inherit `HouseholdScopedOwnedEntity` instead, which itself derives from
   `HouseholdScopedEntity`.
2. Pass `householdId` to the base constructor once, at construction time. `HouseholdId` is
   never updated afterwards - an entity never changes household.

That's it. Nothing else is required: `FinanceManagerDbContext.OnModelCreating` reflects over
every entity type in the model, and for every type assignable to `IHouseholdScoped`, applies a
global EF Core query filter (`HasQueryFilter`) restricting `HouseholdId` to the current
request's household, resolved live from `ICurrentHouseholdAccessor` on every query (the filter
expression closes over the DbContext instance, not a materialized value captured at
model-build time, so it always reflects whichever household the in-flight request belongs to).

This is why task 007 (`Transaction`, `Category`) gets isolation "for free": the reflection
loop in `OnModelCreating` has no entity-type-specific code in it at all - it only asks "does
this CLR type implement `IHouseholdScoped`". Making a new entity inherit
`HouseholdScopedEntity`/`HouseholdScopedOwnedEntity` is the entire integration surface.

### Marker interface vs. base-class-only

`IHouseholdScoped` exists as a separate interface (not just "is `HouseholdScopedEntity`")
because:
- EF Core's `HasQueryFilter` wiring needs a uniform way to find every entity requiring
  isolation. Reflecting over "implements an interface" is exactly as easy as reflecting over
  "is assignable to a base class", so this is not an added cost.
- It documents the actual invariant ("any type implementing this must be tenant-isolated")
  independently of the inheritance hierarchy, and leaves room for a future entity that, for
  some other reason, cannot inherit `HouseholdScopedEntity` (e.g. it needs a different base
  class) but can still implement the interface directly.

## Ownership (N:N, not `OwnerUserId` + `Visibility`)

`FinancialAccount` and `CreditCard` both inherit `HouseholdScopedOwnedEntity`, which adds an
`OwnerUserIds` collection (backed by a private `List<Guid>`, persisted as a Postgres `uuid[]`
column via EF Core's primitive collection support - see `HouseholdScopedOwnedEntityConfigurationExtensions`).
An entity with exactly one owner behaves like what used to be called "Personal"; 2+ owners
means "shared" between exactly those people. There is no separate `Visibility` enum: every
member of the entity's household can **see** it (enforced by the household query filter
above), but only an **owner** can mutate or delete it (enforced in the application service,
e.g. `FinancialAccountsService.GetOwnedOrThrowAsync`). The domain enforces "at least one
owner, no duplicates" via `HouseholdScopedOwnedEntity.AddOwner`/`RemoveOwner`
(`RemoveOwner` throws `BusinessRuleViolationException` if it would leave zero owners).

A household member who is not an owner attempting `PUT`/`DELETE` gets the exact same 404
(`NotFoundException`) as a genuine cross-household caller would. The codebase has no
precedent anywhere for mapping a domain exception to HTTP 403 (confirmed by inspecting
`DomainExceptionHandler`), so "exists but isn't yours" is deliberately indistinguishable from
"doesn't exist" - consistent with the project's existing "don't leak existence" pattern (see
`NotFoundException.For`).

## Proof of isolation

`tests/FinanceManager.IntegrationTests/CrossHouseholdIsolationTests.cs` is the primary proof:
two independent households (registered with no invite code, so each gets its own household -
an invite code would join the *same* household) assert that list/get/update/delete across
households always 404s, and that create/round-trip correctly records ownership.

`tests/FinanceManager.IntegrationTests/FinancialAccounts/UpdateTests.cs` and
`tests/FinanceManager.IntegrationTests/FinancialAccounts/DeleteTests.cs` (plus their
`CreditCards` equivalents) additionally prove the *same-household, non-owner* case: a second
member of the *same* household can `GET`/list an account they don't own, but gets 404 on
`PUT`/`DELETE`.
