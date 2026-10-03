## Context

The normalized catalog stores Store-owned `PrintProvider` records and connects fixed `BlueprintOffering` records through `PrintProviderId`. Printify imports currently match providers by external ID, with a legacy same-name fallback only when the existing provider has no external ID. The Store Editor then groups providers by external identity whenever one exists. This allows two persisted records named `SwiftPOD` with different external IDs to remain visible as duplicate picker entries.

The module must repair both newly imported and stale persisted data while preserving offering relationships and the existing compatibility projection. The primary user workflow is occasional Store catalog setup: a creator edits a fixed-provider offering and selects a provider. The repair should be automatic during catalog loading/import; no permanent workspace area or new management dialog is warranted.

## Goals / Non-Goals

**Goals:**

- Establish one active Store-owned Print Provider per trimmed, case-insensitive provider name.
- Consolidate same-name records with different Printify IDs into one deterministic local provider identity.
- Reassign every Store-owned `BlueprintOffering` that references a superseded provider.
- Preserve superseded records as archived history and preserve all observed external IDs as metadata aliases.
- Repair stale records on Catalog setup load, before the picker projection is built.
- Keep Printify shop-product and offering identities idempotent and Store-scoped.
- Prevent manual creation of an active same-name duplicate.
- Verify application behavior, picker output, persistence round-trip, and the full solution baseline.

**Non-Goals:**

- No change to Provider Network identity or Printify Choice behavior.
- No merge of different provider names merely because their display labels are similar beyond trimming and case-insensitive comparison.
- No deletion of provider records or unrelated catalog records.
- No change to Printify retrieval, credentials, shop selection, or remote API contracts.
- No new provider-management dialog or user-facing merge wizard.

## Decisions

### Provider identity is Store-scoped and name-normalized

For this module, `NormalizeProviderName` is `Trim()` followed by ordinal case-insensitive comparison. The normalized name is the local reconciliation key for Print Providers within one Store. The local `PrintProvider.Id` remains the relationship key used by offerings; the display name is not written into offering relationships.

This resolves the product decision that two records such as `SwiftPOD` with external IDs `9` and `23` represent one provider. Store scoping prevents a provider in one Store from affecting another Store.

### Consolidation keeps one deterministic survivor

When a Store contains multiple records for one normalized name, choose the survivor deterministically:

1. Prefer an active record over archived records.
2. Within that set, choose the earliest `CreatedAt`.
3. Break ties by ascending `Id`.

Reassign every `BlueprintOffering` in the Store whose `PrintProviderId` points to a non-survivor record. Archive every non-survivor record, preserving its ID, timestamps, and metadata. The survivor is unarchived when a current import or active catalog repair requires it.

This keeps local identity stable across repeated repairs and avoids selecting a survivor based on whichever external ID happened to arrive in the latest response.

### External IDs are retained as aliases, not competing local providers

The survivor keeps its existing `ExternalProviderId` when present; otherwise the current imported ID becomes its primary external ID. All non-empty external IDs from the survivor, superseded records, and current import are merged into structured provider metadata as unique Printify aliases. Future import matching uses the normalized Store-scoped name first, so an alias does not recreate a duplicate.

The existing scalar `ExternalProviderId` remains for compatibility with current persistence and callers. Alias metadata preserves source provenance without requiring a schema migration.

### Repair occurs at catalog boundaries

`CatalogSetupService.LoadForStoreAsync` normalizes the target Store before building `CatalogSetupState`. `PrintifyCatalogImportService` normalizes the target Store before matching imported providers and applies the same consolidation rules to the import snapshot before persistence. The final snapshot is then passed through `CatalogCompatibilitySynchronizer` as today.

Manual provider creation validates the active same-name invariant and returns a recoverable application result when a duplicate is requested. The ViewModel continues using the existing provider picker and add-provider flow; it receives the service result and does not need a new merge interaction.

### Atomicity and failure behavior

Normalization operates on the immutable `WorkspaceSnapshot` working copy. If validation, relationship reassignment, metadata normalization, compatibility synchronization, or repository save fails, the repository remains unchanged. Archived duplicates remain restorable through existing catalog lifecycle behavior.

## Risks / Trade-offs

- [A provider display name may be changed by Printify] → Treat the name-based consolidation policy as the explicit product rule for this module; preserve source IDs as aliases and keep the normalization Store-scoped.
- [A stale duplicate may be referenced by archived or historical offerings] → Reassign all Store-owned offerings, not only active ones, and archive rather than delete the duplicate provider.
- [Existing metadata may not contain a JSON alias list] → Parse only the module's known Printify metadata shape, preserve unknown metadata fields, and add aliases without failing otherwise valid records.
- [A load-time repair changes persisted data without a separate confirmation] → Limit the mutation to deterministic same-name provider consolidation, make it atomic and reversible through archive state, and keep it within the existing focused Catalog setup boundary.
- [The legacy compatibility projection could reintroduce a provider] → Normalize before synchronization and add a persistence/compatibility regression test proving the duplicate does not return after reload.

## Migration Plan

1. Add the shared application-layer normalization routine and focused tests.
2. Invoke it from catalog loading, Printify import, and manual provider creation validation.
3. Store aliases in existing provider metadata; no SQLite schema migration is required.
4. Save the repaired snapshot through the existing transactional repository path.
5. If a release must be rolled back, the code can be reverted without destructive data loss because superseded providers were archived, not deleted. A later version can restore them using existing lifecycle controls, although the duplicate-normalizing version will consolidate them again on the next load.

## Implementation Plan

1. Add a focused provider-normalization component under `src/FusionCanvas.Application/Catalog` responsible for name normalization, deterministic survivor selection, alias extraction/merge, offering-reference reassignment, and archival.
2. Apply the component from `CatalogSetupService.LoadForStoreAsync` before `BuildState`; save only when the snapshot changed, then run the existing compatibility synchronization.
3. Update `PrintifyCatalogImportService.ImportSelected` to normalize the target Store before provider matching, match by normalized name, merge incoming external IDs into the canonical provider, and preserve offering identities by external offering ID.
4. Update `CatalogSetupService.CreatePrintProviderAsync` to reject an active same-name provider in the same Store without mutating the snapshot.
5. Keep `CatalogSetupViewModel.AvailablePrintProviders` bound to active normalized state, ensuring the selected offering remains selected when its provider ID is reassigned. Add property-change coverage only if the existing load path does not refresh the collection after repair.
6. Add application tests for stale duplicate repair, deterministic survivor selection, offering reassignment, alias preservation, Store isolation, manual duplicate rejection, and repeated import with different provider IDs.
7. Add App tests for one visible picker entry and selected-provider continuity after a repair; use an Avalonia headless test only if the binding/visual-tree behavior is not covered by the existing ViewModel test seam.
8. Add or extend integration persistence tests to save, reload, and verify archived duplicates, canonical provider identity, aliases, and offering references.
9. Run focused tests, `openspec validate --changes`, and `dotnet test .\\FusionCanvas.sln`.

## Acceptance-to-Verification Mapping

| Acceptance area | Planned verification |
|---|---|
| One active same-name provider and duplicate creation rejection | `CatalogSetupServiceTests` |
| Load-time stale duplicate repair and offering reassignment | `CatalogSetupServiceTests` plus persistence round-trip test |
| Import with same name and different external IDs | `PrintifyCatalogImportServiceTests` |
| Repeated import remains idempotent and aliases persist | `PrintifyCatalogImportServiceTests` plus `PrintifyCatalogImportPersistenceTests` |
| Store isolation | Existing and extended application import tests |
| Provider picker shows one entry and retains selection | `CatalogSetupViewModelTests`; headless Store Editor test only if needed |
| Existing catalog/import behavior remains valid | Focused regression suite and full solution baseline |
