## Context

The catalog overview is rendered by `StoreEditorWindow` and driven by `StoreManagementViewModel`. The normalized catalog stores archive state on `Blueprint` and its descendants, while `ProductSupplierSetupService` exposes a compatibility `StoreProductSummary` projection used by the overview and legacy editor paths. `CatalogCompatibilitySynchronizer` deliberately retains compatibility rows for archived records because historical listing/design relationships may still reference them.

The current UI hides archived Blueprints by filtering them out of the product projection, yet still exposes a Delete Blueprint button for active records. In the normalized path, that delete command currently calls the Blueprint archive cascade, so the label and behavior do not match. This module makes the state transition explicit and gives permanent deletion a separate, guarded path.

## Goals / Non-Goals

**Goals:**

- Keep active Blueprint browsing clean and archive-first.
- Let users opt into archived Blueprint review without changing Store data.
- Make permanent deletion available only for an explicitly selected archived Blueprint.
- Delete the normalized catalog graph and compatibility projection in one repository mutation.
- Protect Item listing and design relationships and provide recoverable blocker messages.
- Preserve existing archive cascade behavior and current Store read-only rules.

**Non-Goals:**

- Adding automatic restore for archived Blueprints.
- Changing Blueprint Offering archive-cascade behavior.
- Deleting shared Assets or unrelated Items when a Blueprint is deleted.
- Adding a database migration or a separate recycle-bin subsystem.
- Changing the behavior of non-Blueprint catalog record deletion.

## Decisions

1. **Use a transient overview filter.** Add `ShowArchivedProducts` to the Store Management view model, defaulting to false. The setting is session/UI state, not workspace data, so opening the overview always starts active-only.

2. **Expose archived state in the compatibility summary.** Add `IsArchived` to `StoreProductSummary` and return active and archived summaries separately from `ProductSupplierSetupState`. `EditorProducts` composes the active list with archived records only when the filter is checked; this avoids making archived rows appear accidentally in other product-service consumers.

3. **Route permanent deletion through the normalized catalog boundary.** Add `DeleteBlueprintPermanentlyAsync` to `ICatalogSetupService`. The service must verify Store ownership, archive state, and protected references before mutating. It removes all records owned by the Blueprint, including current mockup template revisions, source-image links, template color links, and legacy compatibility rows that otherwise could recreate the Blueprint.

4. **Guard external references before mutation.** The service checks Item Listing Configurations by owned offering IDs and Design Slot Assignments by owned compatibility Design Area IDs. It returns a named, recoverable failure before saving if any exist. The deletion does not remove or retarget those relationships.

5. **Use state-based action visibility.** Active detail shows Archive Blueprint only. Archived detail shows Delete permanently only when the archived list is enabled. Archived catalog records remain read-only through existing service checks; the user can review them and either retain or permanently remove them.

6. **Keep deletion confirmation focused.** The existing delete warning panel is reused for archived-only permanent deletion, with wording that matches the irreversible operation. Archive confirmation retains its existing high-impact reversible wording. Cancellation clears only pending UI state.

Alternatives considered: keeping Delete visible but disabling it for active records (less clear than removing it); deleting only the Blueprint row (would leave owned catalog records and allow compatibility repair to recreate it); and deleting all related Items/assets automatically (would violate relationship and ownership safeguards).

## Risks / Trade-offs

- **[Risk]** Compatibility and normalized graphs may be partially populated in older workspaces. **Mitigation:** derive owned IDs from both normalized and legacy offering/area rows and remove every known projection row in the same snapshot.
- **[Risk]** A stale UI selection could attempt to delete a Blueprint that has become active or newly referenced. **Mitigation:** reload and validate the latest repository snapshot inside the service mutation immediately before saving.
- **[Risk]** A large archived graph makes permanent deletion hard to understand. **Mitigation:** use a named Blueprint confirmation and keep the detailed dependency graph in the archive workflow; permanent deletion is only available after archival and is explicitly irreversible.
- **[Risk]** Existing tests construct summary/state records positionally. **Mitigation:** add new optional/default fields at the end of records or update all compile-time construction sites together.

## Migration Plan

No schema migration. Existing archive flags and stable identities remain authoritative. Existing archived Blueprints become visible only when the new checkbox is selected. Existing active Blueprints remain active and are not changed automatically. The first post-change load may synchronize compatibility rows as it does today; permanent deletion removes the synchronized rows before saving.

## Open Questions

None. The deletion boundary is the known catalog-owned graph plus compatibility projections, with Item listing and design relationships protected as blockers.

## Implementation Plan

1. **Application contracts and persistence**
   - Extend `StoreProductSummary` with `IsArchived` and `ProductSupplierSetupState` with `ArchivedProducts` while preserving active `Products` semantics.
   - Update `ProductSupplierSetupService.BuildState` to classify summaries from the normalized Blueprint archive state.
   - Add `DeleteBlueprintPermanentlyAsync` and its request contract to `ICatalogSetupService`; implement one `MutateAsync` operation in `CatalogSetupService`.
   - Compute owned offering, option, option-value, variant, placeholder, template, revision, source-image, and compatibility IDs; reject active targets and protected external references; filter all owned rows from the snapshot.

2. **Store Management state and commands**
   - Add `ShowArchivedProducts`, `ArchivedProducts`, and change `EditorProducts` to use the filter.
   - Reset the filter when entering the Products overview or changing Store context; raise dependent property notifications.
   - Make `CanDeleteSelectedProduct` true only for archived selected Blueprints and route `ConfirmDeleteProductAsync` to the catalog permanent-delete service.
   - Keep archive command eligibility limited to active Blueprints and refresh selection/state after archive or delete.

3. **Avalonia surface**
   - Add an accessible checkbox labeled **Show archived Blueprints** to the overview.
   - Add an archived visual/text marker to rows without changing active row navigation.
   - Remove the active Delete Blueprint button. Show **Delete permanently** in the Blueprint detail only when the selected Blueprint is archived; keep the existing warning panel and bind it to the permanent-delete command.
   - Preserve keyboard order, cancellation, and empty-state behavior for active-only, archived-only, and no-record views.

4. **Tests and evidence**
   - Add application tests for active-delete rejection, archived graph deletion, compatibility cleanup, protected-reference blocking, and unrelated-record preservation.
   - Add view-model/headless tests for default filter state, archived visibility, action ownership, confirmation cancellation, and post-delete selection.
   - Run focused tests, `openspec validate --strict`, and `dotnet test .\\FusionCanvas.sln`; record every scenario in `verification.md`.

## Acceptance-to-Verification Map

| Acceptance scenario | Planned verification |
| --- | --- |
| Active-only default and archived filter | Avalonia headless overview test plus view-model state test |
| Active vs archived action visibility | Avalonia headless visual-tree/control-state test |
| Archive wording and cancellation | Existing archive tests extended with headless confirmation assertions |
| Active permanent-delete rejection | Application service test |
| Archived permanent deletion and compatibility cleanup | Application snapshot mutation test with reload/synchronization assertion |
| Protected reference blocker | Application service test with listing and design references |
| Permanent-delete wording | Headless confirmation test |
| Unrelated record preservation | Application service test |
