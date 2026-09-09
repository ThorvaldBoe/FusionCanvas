## Context

Issue #322 extends the Store Editor's existing local Printify configuration. The repository already has Store-scoped Printify credentials and shop selection, a normalized local catalog model, catalog setup services, and a Catalog & mockups surface. Printify's catalog API exposes a blueprint list, blueprint details, provider offerings, provider variants, option data, and printable placeholders; the local model represents these as Blueprint, BlueprintOffering, typed Options/Values, OfferingVariants, and OfferingPlaceholders.

This is occasional Store administration. The primary workspace must remain unchanged; the import action belongs beside New Blueprint in the focused Store Editor. Retrieval and preview are read-only. Only explicit confirmation starts a transactional local upsert.

## Goals / Non-Goals

**Goals:**

- Retrieve Printify catalog data through a dedicated application port with safe, bounded HTTP parsing.
- Let users select Blueprints before any local mutation.
- Normalize provider data into the existing Store-scoped catalog and preserve provider identities for repeat imports.
- Make repeated imports update in place and preserve local-only records.
- Cover provider, application, persistence, view-model, and headless UI behavior deterministically.

**Non-Goals:**

- Creating or selecting Printify mockup templates, downloading catalog images, importing artwork, or generating products.
- Publishing, product synchronization, orders, Shopify APIs, or a generalized marketplace catalog framework.
- Deleting local records that disappear from a provider response.
- Changing the existing domain terminology or redesigning the catalog editor beyond the import entry point and selection surface.

## Decisions

### Provider boundary and API shape

Add a focused `IPrintifyCatalogClient` application-facing port and an Integration implementation under `Integration/Stores/Printify`. The client receives a Store credential scope and returns safe immutable catalog DTOs containing provider IDs, titles, option values, sellable variants, print areas/placeholders, dimensions, decoration method, and variant applicability. It never returns or logs the token. Use the existing Printify base URL and HTTP safety conventions from the credential verifier: HTTPS-only fixed origin, bearer header scoped to the request, bounded response, no redirects, timeout, cancellation, and typed status mapping.

Prefer the documented V1 catalog endpoints because they expose the complete blueprint/provider/variant/placeholder shape required by the current local model. Use one catalog retrieval orchestration that may make the required detail/provider/variant requests internally; do not expose a multi-step network sequence to the UI.

### Selection and import orchestration

Add an application import service with two phases:

```text
Store context + credentials
        │
        ▼
read-only catalog retrieval
        │
        ▼
Blueprint selection preview
        │ explicit confirmation
        ▼
validate + normalize selected payloads
        │
        ▼
one atomic Store catalog upsert
```

The preview returns only selectable Blueprint summaries and enough provider/record counts to support confirmation. The confirmed request carries stable provider Blueprint IDs, not display labels. The application validates the persisted Store strategy, saved shop, active/non-draft state, and context generation before publishing results.

### Identity and idempotency

Add nullable provider identity fields or the smallest existing metadata-boundary extension needed for Blueprint, offering, option/value, variant, and placeholder records. Identity is scoped by Store and record kind; titles are editable display data and never keys. The importer resolves existing records by provider identity, updates them in place, creates missing records, preserves local IDs and valid Item/template relationships, and leaves provider-missing local records untouched. Duplicate identities in one payload are a validation failure.

The importer produces an in-memory normalized import plan first. It validates ownership, dimensions, option membership, variant references, and placeholder compatibility before handing one snapshot/update request to the existing catalog persistence service. This prevents partial imports and keeps persistence transaction ownership below the UI.

### Mapping Printify data to the local model

- One selected Printify blueprint becomes one local `Blueprint`.
- Each Print Provider offering becomes a local `BlueprintOffering` with stable provider identity.
- Printify option names/types become `OptionKind.Color`, `OptionKind.Size`, or `Other`; provider option IDs and value IDs are retained.
- Each provider variant becomes an `OfferingVariant`; enabled/available state maps to sellable lifecycle data without inventing unavailable local records.
- Each print-area placeholder becomes an `OfferingPlaceholder`/design area with provider position, decoration method, physical width/height, and the exact compatible variant IDs.
- Mockup templates and source images are not created from provider image URLs.

If one provider response cannot express a local invariant, reject the selected Blueprint's import before commit and report a safe, actionable validation message. Do not silently coerce malformed provider data.

### UX and interaction states

The action remains in the Catalog & mockups tab. The selection surface is a compact focused dialog or panel with an initial loading state, searchable/scrollable Blueprint list, empty state, selection count, explicit Import selected and Cancel actions, and an error state with Retry. The invoking Store Editor retains its selected Blueprint/offering and local drafts. On success, refresh persisted catalog state and select the first imported Blueprint only if the current selection was empty; otherwise preserve selection. On cancel/error, return focus to Import from Printify and preserve local drafts.

The action is disabled with inline guidance for unsaved, archived, non-Printify, missing-shop, or unavailable-credential contexts. Keyboard users can reach the list, toggle selection, confirm, and cancel without pointer-only controls. Late responses are discarded using cancellation plus a captured Store/context generation.

### Alternatives considered

- **Import directly when opening the Store Editor:** rejected because it mutates local data without consent and makes provider failures part of ordinary editor loading.
- **Use provider titles as local keys:** rejected because titles can change and collisions across providers are possible.
- **Replace the local catalog with the provider snapshot:** rejected because local-first data and local-only records must survive provider changes.
- **Download provider mockup images during catalog import:** rejected because issue 322 explicitly excludes local-file-dependent mockup setup and image ownership.
- **Build a generic marketplace import framework now:** rejected because only Printify is in scope and the existing code has a focused Store Printify boundary.

## Risks / Trade-offs

- [Risk] Catalog retrieval can require many provider requests and be slow. → Mitigation: explicit busy state, cancellation, bounded concurrency/sequencing, and no UI mutation until the complete selected payload validates.
- [Risk] Printify response shapes or option types evolve. → Mitigation: strict DTO parsing, typed malformed-response results, fixture coverage, and no silent coercion.
- [Risk] Imported provider identities could collide with legacy local records. → Mitigation: Store-scoped record-kind identity matching and an explicit migration/repair path for records without identities.
- [Risk] A failed persistence write could leave a partial catalog. → Mitigation: build a validated plan and commit through one existing SQLite transaction.
- [Risk] A late response could be applied to another Store. → Mitigation: cancellation plus Store ID, workspace ID, and context-generation checks before publication.

## Migration Plan

No destructive schema migration is intended. Add the smallest backward-compatible SQLite migration for provider identity fields/indexes, defaulting existing local records to no provider identity. Existing manually configured catalogs remain usable. On rollback, the import action disappears while imported records remain readable by the current catalog model; the application must retain compatibility with the added nullable columns.

## Open Questions

None for implementation. The following decisions are fixed for this module: V1 catalog response shape, explicit Blueprint confirmation, Store-scoped provider identity, idempotent upsert, preservation rather than deletion of provider-missing local records, no image/mockup import, and no external request for Manual or Shopify + Manual Stores.

## Implementation Plan

1. Add Printify catalog DTOs, typed failure results, and `IPrintifyCatalogClient` under Application; implement bounded V1 HTTP retrieval and safe JSON parsing under Integration with fake-HTTP tests.
2. Extend catalog import contracts and `CatalogSetupService` or a focused `PrintifyCatalogImportService` to validate Store context, build a normalized upsert plan, and delegate atomic persistence.
3. Add provider identity fields/mappings and the ordered SQLite migration, uniqueness validation, snapshot round-trip, transaction rollback, and workspace-package compatibility checks.
4. Implement idempotent merge logic for Blueprint/provider/option/value/variant/placeholder identities, preserving local-only records and valid existing relationships.
5. Add a Store Editor import state/view model and selection surface; bind strategy/credential/shop guards, loading/error/cancel states, keyboard focus, selection, confirmation, and post-import refresh using compiled bindings and automation IDs.
6. Add focused Domain/Application/Integration tests, Store Editor view-model tests, and Avalonia headless tests for visibility, selection, focus, busy/error states, context changes, and no-mutation cancellation.
7. Run strict OpenSpec validation, solution build/test, and scoped QA; fill `verification.md` with criterion-level evidence before apply/archive.

## Acceptance-to-Verification Mapping

| Capability | Acceptance scenarios | Planned verification |
|---|---|---|
| printify-catalog-import / retrieval and guards | saved Store, unsupported Store | Application tests with fake client; Integration request fixtures |
| printify-catalog-import / selection and confirmation | subset confirmation, cancel | View-model tests plus Avalonia headless selection/cancel tests |
| printify-catalog-import / idempotent mapping | repeat import, changed provider data | Application merge tests and SQLite round-trip/uniqueness tests |
| printify-catalog-import / safe validation | valid mapping, malformed payload | DTO/parser fixtures, import-plan validation tests, transaction rollback test |
| printify-catalog-import / recoverable failures | provider failure, cancellation | typed-result tests, view-model busy/error/cancellation race tests |
| store-management / gated action and safety | Printify/manual visibility, local draft/context transitions | Store Editor view-model and Avalonia headless tests |
| product-supplier-setup / provider identity and compatibility | provider offering mapping, variant subset areas | Domain/application mapping tests and persistence round-trip |
| local-sqlite-persistence / atomic/idempotent storage | confirmed import, reopen, duplicate payload | isolated SQLite migration, transaction, reload, and uniqueness tests |
