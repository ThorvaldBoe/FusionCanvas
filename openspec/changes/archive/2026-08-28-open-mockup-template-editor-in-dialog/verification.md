# Focused Mockup Template Editor Verification

## Acceptance Evidence

| Acceptance scenario | Method | Result | Evidence and limitations |
| --- | --- | --- | --- |
| User reviews the collection | Headless/UI-description tests | Pass | `MockupTemplateManagement_UsesListOnlySurfaceAndGuardedAddDialog` proves the full-width list, one Add action, and absence of inline preview/configuration regions. The deterministic description now uses collection-only default and focused dialog states. |
| User adds a template | Headless test | Pass | The same test verifies one Store Editor-owned modal, Add title, fresh baseline, initial Template name focus, scrolling, minimum size, and disabled parent lifetime. |
| User edits a template | Headless/ViewModel tests | Pass | `MockupTemplateManagement_EditDialogPopulatesAndReturnsFocusOnCancel` and `MockupTemplateDraft_EditModePreservesInvalidDraftAndOfferingSwitchEndsIt` verify title, stable identity, Design Area, name, revision mapping, and populated baseline. |
| Preview-first mapping remains available | Existing focused headless tests | Pass | `MockupPreview_WithImageSynchronizesPlacementRectangleAndMappingFields`, mapping-label, provider-data, and unavailable-image tests now run against the modal and retain two-way placement evidence. |
| Save fails | ViewModel test | Pass | Invalid identity disables Save while leaving the active draft and confirmed template collection unchanged. Existing service tests retain recoverable persistence-failure coverage. |
| Save succeeds | Existing service/headless coverage | Pass | The unchanged `CreateTemplateCommand` path persists through existing services, ends the dialog only without error, refreshes/selects the record, and owner handling restores focus. |
| Unchanged and meaningful dismissal | ViewModel/headless tests | Pass | Cancel closes an unchanged edit; Escape/window close on a changed Add shows the discard prompt, Keep preserves values, and confirmed discard closes without mutation. |
| Context becomes stale | ViewModel test | Pass | Offering selection resets the active template draft and prevents cross-context persistence. Workspace reload shares the same reset path. |
| Archived store is reviewed | ViewModel test | Pass | `MockupTemplateDraft_ArchivedStoreCannotOpenAddOrEdit` proves read-only Add/Edit commands and guarded methods cannot raise a dialog request. Dialog controls also retain `CanEdit` bindings. |
| Keyboard and supported sizes | Headless test | Pass | Focus, Escape, close interception, accessible controls, scrollability, resizability, and 640×540 minimum construction are deterministic. |

## Required Gates

- Focused new dialog/draft tests: 5 passed, 0 failed; legacy mockup editor tests also pass against the modal.
- App suite: 580 passed, 0 failed.
- Solution baseline: 1,412 passed, 0 failed, 0 skipped (Domain 232; Application 384; Integration 189; App 580; UI-description 27).
- Strict OpenSpec: 54 passed, 0 failed.
- `git diff --check`: passed with expected line-ending normalization notices only.

## Supplemental Review

No live desktop check was required because ownership, focus, placement synchronization, visibility, accessibility, and supported-size behavior are covered headlessly.
