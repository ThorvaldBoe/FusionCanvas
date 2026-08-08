## 1. Domain model and invariants

- [x] 1.1 Add the one-to-one Item listing profile and provider-neutral strategy, readiness, publication, ownership, identity, and diagnostic types described in `design.md`, reusing existing Item and relationship records as canonical sources.
- [ ] 1.2 Add pure validation for price/currency, strategy prerequisites, provider/channel identity scope, ownership transitions, and same-Store media/product/variant references.
- [ ] 1.3 Add Domain tests covering one-model continuity, manual/Shopify-manual/Shopify-Printify strategy transitions, durable manual overrides, readiness versus publication, inactive state, and invalid references.

## 2. Application local preparation workflow

- [x] 2.1 Add Application state, requests, results, and repository/use-case contracts for loading and atomically updating local listing preparation.
- [x] 2.2 Implement composition of canonical Item title/description/tags/assets/catalog references with profile-owned listing fields without copying common values into provider extensions.
- [x] 2.3 Implement guarded strategy transitions: marketplace-agnostic manual mode, Shopify manual binding prerequisite, Shopify plus Printify identity/result state, and post-publish Printify lock/unlock warning state without live connector calls.
- [x] 2.4 Implement recoverable validation and persistence-failure results that preserve drafts and leave the confirmed snapshot unchanged.
- [x] 2.5 Add Application tests for common-field edits, ownership/override preservation, strategy capability prerequisites, Shopify identity binding through either local path, readiness/publication distinction, diagnostics, and atomic failure behavior.

## 3. SQLite persistence and migration

- [x] 3.1 Extend `WorkspaceSnapshot` and SQLite schema/repository save/load/delete ordering for listing profiles and optional provider/channel state with compatibility defaults.
- [x] 3.2 Add a transactional additive schema migration that creates valid manual listing state for existing Items without changing identities, common values, relationships, or requiring connector settings.
- [x] 3.3 Add repository validation for same-Store ownership, provider/channel identity scope, and media/product/variant references before commit.
- [ ] 3.4 Add isolated Integration tests for fresh-database persistence, old-workspace upgrade, full round trip, optional provider diagnostics, invalid references, and migration/save rollback.

## 4. Shared Listing-stage surface

- [x] 4.1 Replace the ListingStageToolViewModel status-only behavior with a shared listing-preparation view model that consumes Application state and coordinates with the existing Item inspector owner for common fields.
- [ ] 4.2 Add the common Listing-stage editor for title, description, tags, price/currency, references, readiness/publication state, and shared metadata using existing automatic-save and atomic tag conventions.
- [x] 4.3 Add progressive strategy UI: manual guidance; Shopify binding and disabled prerequisite state; Shopify plus Printify post-publish identity and lock/unlock warning; provider diagnostics and ownership details behind appropriate disclosure.
- [ ] 4.4 Add explicit empty, loading, success, validation-error, persistence-error, unavailable-connector, inactive/read-only, and locked interaction states with predictable focus and keyboard flow.
- [ ] 4.5 Add App view-model tests for strategy transitions, capability visibility/enabling, preserved common data, override ownership, inactive state, and post-publish lock behavior.
- [ ] 4.6 Add Avalonia headless tests for Listing-stage construction, compiled bindings, common-field control state, strategy-specific visibility/enabling, validation/error presentation, and inactive/locked states.

## 5. Criterion-level verification and delivery gates

- [x] 5.1 Map every scenario in `listing-preparation`, `listing-inspector`, and `local-sqlite-persistence` to named Domain, Application, Integration, or App/headless test evidence; record any intentional deferred connector scenario as not applicable to this module.
- [x] 5.2 Review the changed scope for one shared Listing tool/model, no duplicated common fields, correct separation from catalog `FulfillmentKind`, stable strategy transitions, and no accidental connector/API implementation.
- [x] 5.3 Run `openspec validate --strict` and correct all proposal/spec/design/task validation findings.
- [ ] 5.4 Run `dotnet test .\\FusionCanvas.sln` and record the deterministic baseline result; do not treat live Shopify/Printify access as a completion gate.
