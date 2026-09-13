## Context

`AssetsWindow` is a focused Avalonia surface opened from the workspace shell for store- or context-scoped asset management. The current suite exercises `AssetsViewModel`, `AssetManagementService`, file storage, and `AssetPreviewWindow` separately, but no rendered journey proves the complete creator job across file picking, confirmation, persistence, relabeling, preview, and fresh re-entry. The merged experience-testing strategy provides the dispatcher wait and disposable workspace primitives needed for this pilot.

## Goals / Non-Goals

**Goals:**

- Prove one store-level asset job through rendered controls: import a supported image, confirm its purpose, observe the imported row, relabel it, open and close its preview, and verify persistence after fresh reconstruction.
- Keep arrangement, user actions, visible assertions, and re-entry visibly separated.
- Use a deterministic file-picker fake and scenario-owned SQLite/file-store resources; never use the contributor workspace or external services.
- Add only semantic automation metadata and a thin `AssetsWindowDriver` where existing labels and control types are insufficient.
- Retain focused tests for invalid extensions, picker cancellation, missing files, save failures, and removal confirmation variants.

**Non-Goals:**

- No production asset behavior, persistence schema, file format support, or UX redesign.
- No repository-wide conversion of asset-related tests or inline Item asset sections.
- No mandatory live-desktop/Appium coverage, screenshots, or pixel comparison.
- No workspace-transfer end-to-end journey or batch import behavior.

## Decisions

### Use the store-level surface as the pilot boundary

The store-level view lists linked and unlinked assets and exercises the broadest accepted surface without requiring a separate listing fixture. Item, niche, and group context selection remains covered by lower-layer tests and can be added only when a later defect justifies it.

### Exercise rendered controls for every claimed seam

The journey will locate Import, Confirm import, purpose selectors, preview thumbnail, and Close through stable automation IDs or semantic control predicates. User actions will use routed keyboard/control interaction. The deterministic file picker and initial snapshot are arrangement collaborators, not substitutes for action-phase UI interaction.

### Prove durability with fresh composition

The journey will use `DisposableHeadlessWorkspace`, a real `SqliteWorkspaceRepository`, and the existing managed file-store boundary. After import and relabel operations, it will close the window, create a new service/view-model/window composition, reload the store context, and assert the visible name, purpose, managed-file state, and list contents.

### Keep destructive removal as focused coverage in this module

The existing view-model test already proves request/cancel/confirm removal semantics. The pilot will assert that the rendered removal prompt is reachable and cancellation preserves the row, but full file-deleting confirmation/re-entry is deferred unless implementation reveals a material UI seam gap. This keeps the journey small and avoids duplicating lower-layer variants.

### Keep preview assertions observable, not visual-regression based

The journey will activate the rendered thumbnail, assert that `AssetPreviewWindow` opens with the expected asset context, and close it through the window lifecycle. It will not compare pixels or depend on platform image rendering.

## Risks / Trade-offs

- [Risk] Native file-picker behavior cannot run deterministically headless. → Mitigation: inject the existing `IAssetFilePicker` boundary with a scenario-owned fake and retain the optional desktop lane for native picker risks.
- [Risk] Preview dialog ownership may make lifecycle assertions brittle. → Mitigation: assert the observable dialog/data context and use bounded dispatcher settling; avoid private-handler invocation.
- [Risk] SQLite/file cleanup can leave locks after window disposal. → Mitigation: close all windows in cleanup, use unique roots, disable unsafe pooling where the fixture already supports it, and retain cleanup diagnostics.
- [Risk] The store-level list may contain no selectable row after removal. → Mitigation: arrange two deterministic assets and assert the documented empty/remaining-selection outcome explicitly.

## Migration Plan

No migration or production deployment step is required. The change adds tests, test support, and stable automation metadata only. Reverting the commit removes the journey and identifiers without affecting persisted workspaces.

## Open Questions

None blocking. The store-level import/relabel/preview path, isolated persistence strategy, focused-removal boundary, and non-visual preview oracle are fixed for implementation.

## Implementation Plan

1. Inspect existing `AssetsWindow`, `AssetsViewModel`, asset service, and SQLite/file-store composition; identify semantic locators and any missing automation IDs.
2. Add stable automation IDs to the Import, confirmation, purpose, preview, removal, and Close controls only where a semantic locator is not already reliable.
3. Add `AssetsWindowDriver` under `tests/FusionCanvas.App.Tests/TestSupport/Drivers/` with thin user-language actions and observations; use `HeadlessUiWait` for asynchronous state and no assertions or view-model shortcuts.
4. Add a rendered store-level journey with deterministic picker/file-store setup, import confirmation, purpose relabel, preview open/close, removal-cancel observation, and fresh SQLite-backed re-entry assertions.
5. Retain or adjust focused asset tests only when they become redundant or misleading; do not duplicate invalid-input, cancellation, or persistence-error variants in the journey.
6. Run focused App and persistence tests, the canonical serialized solution baseline, strict OpenSpec validation, and criterion-level QA; record evidence and any escape analysis in `verification.md` and `retrospective.md`.

## Acceptance-to-Verification Plan

- Rendered import and purpose confirmation: App headless journey through `AssetsWindowDriver` and visible pending/imported state assertions.
- Relabel persistence: rendered purpose selection followed by fresh SQLite-backed composition and visible purpose/file assertions.
- Preview lifecycle: rendered thumbnail activation, `AssetPreviewWindow` observable context assertion, and close lifecycle.
- Removal cancellation: rendered removal request and cancel controls with row/list unchanged assertion; destructive file deletion remains focused evidence.
- Deterministic isolation: `DisposableHeadlessWorkspace` path ownership and no external file-picker/network dependencies.
- Strategy gates: `dotnet test .\\FusionCanvas.sln -m:1` and `openspec validate --all --strict`.
