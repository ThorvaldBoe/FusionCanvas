## Context

The normalized catalog already stores archive state on Blueprints, Blueprint Offerings, Options, Option Values, Variants, Placeholders, and Mockup Templates. `CatalogSetupService.ArchiveAsync` currently archives one record only and rejects active references with a generic message. `CatalogSetupViewModel` exposes archive buttons for nested records, but the Blueprint Offering Basics surface has no equivalent lifecycle action. Blueprint/Offering navigation and legacy fulfillment summaries are coordinated by `StoreManagementViewModel` and `ProductSupplierSetupService`.

## Goals / Non-Goals

**Goals:**

- Make archive dependencies inspectable and actionable by name.
- Add a reversible, atomic Offering archive preview and confirmation.
- Archive catalog-owned descendants in one operation while preserving identities and relationships.
- Keep external Item/listing references safe and explicit.
- Refresh selection, counts, archived state, and error messaging consistently.

**Non-Goals:**

- Permanent deletion of records as part of an archive cascade.
- Automatic reassignment or deletion of Items, listing configuration, or other records outside the catalog-owned graph.
- New database tables or migrations.
- Redesign of the existing Blueprint permanent-delete workflow.

## Decisions

1. **Use a catalog-specific preview and confirm contract.** Add a dependency summary/result and an Offering archive-cascade request to the Application catalog boundary. Preview loads the latest snapshot and returns grouped active descendants plus named external blockers; confirmation revalidates the same identities before saving.
2. **Make the mutation atomic at snapshot level.** One application mutation archives the Offering, Options, Option Values, Variants, Placeholders, Mockup Templates, template-color bindings, and source-image records owned by the Offering. Immutable revision history is retained, and links and stable IDs are preserved so restore/history remain meaningful.
3. **Treat external references as blockers.** Item targets, listing configuration, and other non-catalog relationships are not altered by the cascade. Their display names and record types are included in the preview/error so the user can resolve them intentionally.
4. **Keep nested single-record archive safeguards.** Variant and Placeholder archive commands continue to reject unsafe requests, but `GetDependencyError` is changed to resolve concrete dependency labels instead of returning only a generic sentence.
5. **Place the Offering action in Basics with progressive disclosure.** A clearly labeled secondary danger action opens a focused confirmation panel near the existing offering fields. The panel lists counts and names, supports Cancel, and returns to the opened Blueprint/Offering context after completion.

Alternatives rejected: silently cascading from the existing button (unsafe and hard to review); permanent deletion of descendants (irreversible and contrary to archive-first policy); and only adding helper text without an Offering archive action (does not solve the missing lifecycle control).

## Risks / Trade-offs

- **[Risk]** A large Offering may produce a long confirmation list. **Mitigation:** group by record type, show counts with expandable details, and keep names available for blocked records.
- **[Risk]** New normalized catalog descendants may be added later. **Mitigation:** centralize ownership selection in the catalog application service and cover every current descendant collection in integration tests.
- **[Risk]** Legacy and normalized records can be synchronized during load. **Mitigation:** operate on the authoritative normalized graph and refresh both catalog and product summaries after success.
- **[Risk]** Async confirmation can race with another edit. **Mitigation:** reload and revalidate inside the confirmed mutation; return a recoverable stale-context error without partial save.

## Migration Plan

No schema migration. Existing archive flags remain the source of truth. Existing active records are unaffected until a user confirms the new cascade. Restore continues to use stable identities; if restoration of a parent would expose invalid descendants, existing restore validation remains authoritative.

## Open Questions

None for this module. The approved boundary is reversible catalog-owned archival only; external relationships require explicit user resolution.

## Implementation Plan

1. Add application contracts and catalog-service operations for offering archive preview/confirmation and concrete dependency summaries.
2. Implement atomic snapshot cascade and named dependency resolution, including current mockup/template-owned records and cross-record validation.
3. Add `CatalogSetupViewModel` state/commands for preview, confirmation, cancellation, blocked details, and refresh; add the Offering Basics archive action.
4. Update Store Editor markup with progressive disclosure, grouped dependent counts/names, explicit reversible wording, and focus/selection aftermath.
5. Add application tests for preview, blockers, atomic cascade, stable identities, and unrelated-record preservation; add Avalonia headless tests for button ownership, confirmation contents, cancellation, and post-success navigation.
6. Run strict OpenSpec validation, focused tests, and `dotnet test .\\FusionCanvas.sln`.

## Acceptance-to-Verification Map

| Acceptance area | Planned verification |
| --- | --- |
| Offering archive action visibility and disabled states | Avalonia headless view test |
| Preview names/counts and cancellation | View-model and headless tests |
| Atomic catalog-owned cascade and identity preservation | Application/integration tests with in-memory snapshot |
| External blocker details and unchanged relationships | Application test plus headless error-surface test |
| Variant concrete dependency guidance | Application test and existing Variant management headless coverage |
