# Focused Design Area Editor Verification

## Acceptance Evidence

| Acceptance scenario | Method | Result | Evidence and limitations |
| --- | --- | --- | --- |
| User reviews the Design Area collection | Avalonia headless and UI-description tests | Pass | `DesignAreaManagement_UsesListOnlySurfaceAndGuardedAddDialog` proves the Store Editor renders a list-only management surface, while `Design_areas_collection_uses_the_full_management_surface` verifies the deterministic descriptor layout no longer reserves an editor column. |
| User adds a Design Area | Avalonia headless test | Pass | `DesignAreaManagement_UsesListOnlySurfaceAndGuardedAddDialog` invokes **Add Design Area**, verifies one owner-modal `DesignAreaEditorWindow`, the **Add Design Area** title, default draft values, initial Name focus, scrollable content, and disabled parent. |
| User edits a Design Area | Avalonia headless and ViewModel tests | Pass | `DesignAreaManagement_EditDialogPopulatesSavesAndReturnsFocus` and `DesignAreaDraft_AddAndEditModesTrackMeaningfulChangesAndDiscardChoices` verify the shared **Edit Design Area** dialog and populated stable-identity draft, including compatibility selections. |
| Save fails validation or persistence | ViewModel test | Pass | `DesignAreaDraft_InvalidSaveStaysOpenAndOfferingSwitchEndsStaleDraft` submits an invalid draft and proves the active draft and values remain available without changing confirmed records. Existing catalog tests retain persistence-failure coverage. |
| Save succeeds | Avalonia headless test | Pass | `DesignAreaManagement_EditDialogPopulatesSavesAndReturnsFocus` saves through the existing command/service path, observes one updated Design Area, closes the modal, and restores focus to the matching Edit control. |
| User dismisses an unchanged draft | Avalonia headless test | Pass | `DesignAreaManagement_UsesListOnlySurfaceAndGuardedAddDialog` exercises Cancel without changes and observes immediate closure without persistence and focus restoration. |
| User dismisses a meaningful draft | ViewModel and Avalonia headless tests | Pass | The focused tests exercise the common Cancel/Escape/window-close request path, verify the discard prompt, preserve the complete draft on **Keep editing**, and close without persistence after confirmed discard. |
| Editing context becomes stale | ViewModel test | Pass | `DesignAreaDraft_InvalidSaveStaysOpenAndOfferingSwitchEndsStaleDraft` switches Offering context and proves the stale draft is discarded rather than transferred. Workspace reset uses the same draft-reset path. |
| Dialog is used with keyboard and supported sizes | Avalonia headless test | Pass | The headless tests verify accessible names, initial and restored focus, Escape/close routing, scrollability, resizability, and construction/layout at normal and narrow supported dimensions. This is deterministic interaction/layout evidence, not pixel-perfect visual regression. |

## Required Gates

- Focused Design Area tests: 6 passed, 0 failed.
- App regression suite: 579 passed, 0 failed.
- UI-description suite: 27 passed, 0 failed after reconciling the list-only layout tests and generated golden SVGs.
- Solution baseline: `dotnet test .\FusionCanvas.sln --no-restore --logger "console;verbosity=quiet"`
  - Result: 1,411 passed, 0 failed, 0 skipped (Domain 232; Application 384; Integration 189; App 579; UI-description 27).
- `openspec validate --all --strict --no-interactive`
  - Result: 54 passed, 0 failed, including this change.
- `git diff --check`
  - Result: passed; Git emitted only expected line-ending normalization notices.

## Supplemental Review

- The deterministic UI description and both Design Area golden SVGs now describe the collection-only default state and focused dialog states.
- Live desktop review was not required because modal ownership, focus, close interception, binding, layout, and state behavior are covered headlessly.
