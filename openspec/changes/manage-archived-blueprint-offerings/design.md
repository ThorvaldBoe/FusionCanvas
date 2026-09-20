## Context

The normalized catalog already persists `IsArchived` on Blueprint Offerings and their owned Options, Option Values, Variants, Placeholders, Mockup Templates, and template-owned records. `CatalogSetupService` can archive an Offering cascade and restore one catalog record, while `OfferingManagementService.LoadForBlueprintAsync` currently returns all Offerings without an archived filter. `CatalogSetupViewModel` and `StoreEditorWindow` expose the Blueprint-scoped Offering list and archive confirmation, but no archived filter, cascade restore, or Offering-specific permanent deletion.

The existing Blueprint permanent-delete path establishes the safety boundary: permanent removal is allowed only after archival, is confirmed, is atomic, and names protected Item or design relationships. This module extends that boundary from Blueprints to individual Blueprint Offerings without changing Store isolation, archived-Store read-only policy, or the existing reversible Offering archive cascade.

The primary workflow is occasional catalog administration rather than daily creative work. The archived filter and lifecycle controls belong in the existing Blueprint detail/Offering management surface, with destructive confirmation progressively disclosed only for an archived selection. No schema migration is expected because archive flags, stable identities, ownership relationships, and compatibility projections already exist.

## Goals / Non-Goals

**Goals:**

- Show active Offerings by default and let the user opt into archived Offerings for the selected Blueprint and Store.
- Make archived Offering state and available lifecycle actions explicit and read-only until restore succeeds.
- Restore an archived Offering and its catalog-owned archive cascade atomically.
- Permanently delete an archived Offering and its catalog-owned descendants atomically after named external-reference checks and explicit confirmation.
- Preserve stable identities, Store/Blueprint ownership, compatibility projections, and unrelated records.
- Cover list state, selection transitions, confirmation/cancellation, blockers, persistence, and headless UI behavior with deterministic tests.

**Non-Goals:**

- No remap workflow for Items or listing configurations; blocked references only provide guidance for a future remap/clear action.
- No change to Blueprint-level archive, restore, or permanent-delete behavior.
- No change to the existing Offering archive cascade's scope or confirmation semantics.
- No editing of archived Offering fields, descendants, or template content while archived.
- No remote provider synchronization, marketplace deletion, credential changes, or schema migration.

## Decisions

### 1. Keep the archived filter Blueprint-scoped and opt-in

The checkbox lives beside the Blueprint-scoped Offering list, is unchecked on initial load, and queries only the selected Blueprint and Store. This matches the existing Blueprint archived filter and avoids a Store-wide administration surface competing with the current context. An archived selection is cleared or replaced when the filter is turned off; active selection is preserved when possible.

**Alternative considered:** a Store-wide archived Offering view. Rejected because it weakens ownership context and would duplicate the existing Blueprint navigation model.

### 2. Make archived Offerings read-only with two explicit actions

An archived Offering exposes **Restore Blueprint Offering** and **Delete permanently**. Active Offerings expose the existing archive action and never expose permanent deletion. Archived Stores disable all three mutations. Restore and permanent delete are explicit confirmed actions, with focus returned to the invoking control or the resulting active/empty list state.

**Alternative considered:** allow inline editing or a generic lifecycle menu. Rejected because archived records must remain coherent and destructive actions need clear ownership and state-specific wording.

### 3. Restore the complete Offering-owned cascade

Restore sets the Offering and every catalog-owned descendant in its cascade scope active in one repository mutation: Options, Option Values, Variants, Placeholders, Mockup Templates, template revisions, source-image/color records, and compatibility-owned records. Stable IDs and relationships are not regenerated. This prevents a restored Offering from appearing active while its required catalog graph remains unavailable.

**Trade-off:** a child record that was independently archived before the Offering cascade is not tracked with a separate archive provenance marker in the current model and will be restored with the owned cascade. Preserving independent child archival history would require a new provenance model and is outside this bounded module.

### 4. Permanently delete only after archival and external-reference validation

The application adds a dedicated Offering deletion request rather than overloading the existing single-record `DeleteAsync` path. The service validates Store writability, explicit confirmation, archived Offering state, and protected references before constructing one replacement snapshot. The deletion set is derived from Offering ownership and includes normalized and compatibility-owned records plus template revisions, source images, and mapping rows. Any blocker returns a named recoverable error and leaves the snapshot unchanged.

**Alternative considered:** reuse Blueprint permanent deletion and delete the parent Blueprint. Rejected because users need to remove one archived fulfillment Offering while retaining the Blueprint and sibling Offerings.

### 5. Use existing presentation summaries and authoritative refresh

`BlueprintOfferingSetupSummary.IsArchived` remains the row-state source. The view model requests active-only or all Offering summaries based on the checkbox, refreshes from the repository after every successful mutation, and never edits projection instances in place. Confirmation surfaces consume an application plan/result so names and cascade counts are authoritative.

## Risks / Trade-offs

- **[Risk]** Restoring every owned descendant can reactivate a child that was independently archived before the Offering cascade. → Document this deliberate bounded behavior in tests and defer archive-provenance tracking to a separate lifecycle module.
- **[Risk]** A missed external reference could orphan an Item or design assignment. → Reuse the Blueprint deletion blocker patterns, enumerate Item listing configurations and design-slot assignments by Offering/Placeholder IDs, and add regression tests for each protected relationship.
- **[Risk]** The existing compatibility projection may recreate or retain records after deletion. → Remove Offering-owned normalized and legacy projection rows in the same snapshot and run reload/synchronization persistence tests.
- **[Risk]** Filter transitions can leave an archived selection pointing at hidden data. → Centralize selection reconciliation after filter changes and mutation refresh; cover active/archived/empty permutations in headless tests.
- **[Risk]** Large confirmation surfaces could overwhelm the focused editor. → Show a concise Offering identity and grouped descendant counts/names, with detailed blockers only when deletion is blocked.

## Migration Plan

No database migration is required. Existing snapshots load with the current archive flags and stable identities. Deploy the application/service changes, then verify that existing active and archived Offerings round-trip unchanged. If the implementation must be rolled back, revert the application/UI changes; no data transformation has occurred. Any already permanently deleted Offering is intentionally unrecoverable, so the confirmation and test gates are the rollback boundary.

## Open Questions

None for this bounded module. Product decisions are resolved: Blueprint-scoped visibility, both restore and permanent delete, deletion blocked by external references pending a future remap feature, and cascade deletion of catalog-owned descendants.

## Implementation Plan

### Application and domain boundaries

1. Extend `ICatalogSetupService` and `CatalogSetupService` with explicit Offering cascade restore and permanent-delete operations plus any confirmation/plan contracts needed by the UI. Keep validation and snapshot mutation in the Application layer; do not move business rules into Avalonia code.
2. Add a focused Offering permanent-delete request/result/plan type alongside `DeleteBlueprintPermanentlyRequest` and `CatalogArchivePlan`. Reuse the existing dependency naming pattern so blockers identify record type and display name.
3. Implement restore by validating an archived Offering in a writable Store and atomically clearing `IsArchived` for the Offering and all owned descendants in the defined cascade scope.
4. Implement permanent deletion by collecting owned IDs, checking Item listing configurations and design-slot assignments (plus any other current protected external relationship), removing normalized and compatibility-owned rows and dependent mapping/revision/source-image rows, and saving only after all checks pass.
5. Keep `CatalogCompatibilitySynchronizer` and repository snapshot validation aligned with the resulting graph; do not introduce new persistence tables or migrations.

### Offering list and Store Editor

1. Update `OfferingManagementService.LoadForBlueprintAsync`/its contract and the `CatalogSetupViewModel` projection refresh so the default query excludes archived Offerings while an explicit boolean includes them.
2. Add the archived filter near the Blueprint-scoped Offering list in `StoreEditorWindow.axaml`, expose archived status in the existing card/detail presentation, and reconcile selection when the filter, Blueprint, Store, or mutation result changes.
3. Add state-aware restore and permanent-delete commands to `CatalogSetupViewModel`, keeping archived Store and active Offering guards visible through command `CanExecute` state.
4. Add focused confirmation surfaces or existing dialog extensions for restore and permanent deletion. Confirmations must name the Offering, summarize the catalog-owned cascade, warn about irreversibility for deletion, and preserve cancellation/keyboard focus.
5. Refresh the authoritative catalog/Offering state after success and select the restored Offering, a valid remaining active Offering, or the empty state after deletion.

### Verification plan

| Acceptance area | Planned evidence |
| --- | --- |
| Active-only default, opt-in archived filter, row state, selection reconciliation | `OfferingManagementServiceTests`, `CatalogSetupViewModelTests`, and populated/empty/archive-filter cases in `StoreEditorHeadlessTests` |
| State-specific actions and archived Store read-only behavior | View-model command tests plus headless assertions for Archive/Restore/Delete visibility and enabled state |
| Restore cascade and cancellation | `CatalogSetupServiceTests` covering all descendant collections, stable IDs, unrelated records, archived Store rejection, and cancel/no-mutation path |
| Permanent deletion and named blockers | `CatalogSetupServiceTests` covering active-offering rejection, confirmation requirement, Item/listing and design-slot blockers, atomic success, and sibling/Store isolation |
| Persistence and compatibility cleanup | `CatalogArchivePersistenceRegressionTests` or a focused integration test that saves, reloads, and synchronizes before/after restore and deletion |
| Full acceptance baseline | `openspec validate` followed by `dotnet test .\\FusionCanvas.sln -m:1` |

