## 1. Inspect and align the asset surface

- [x] 1.1 Review `AssetsWindow`, `AssetsViewModel`, asset-management service, file-store, and existing App tests; record the exact store-level controls and observable states used by the journey.
- [x] 1.2 Confirm the existing accepted asset-management scenarios and update the change artifacts if implementation evidence invalidates a scoped assumption; do not expand into item-inline or transfer workflows.
- [x] 1.3 Add stable automation IDs only to asset controls that lack a reliable semantic locator, with no visual or behavioral production change.

## 2. Add thin deterministic interaction support

- [x] 2.1 Implement `AssetsWindowDriver` with user-language actions for import, confirm/cancel, purpose selection, preview, removal request/cancel, and close plus read-only observations.
- [x] 2.2 Ensure the driver uses rendered controls or routed input, bounded `HeadlessUiWait`, and no assertions, business logic, private-handler reflection, direct view-model mutation, or direct command shortcuts.
- [x] 2.3 Add or reuse deterministic picker and scenario-owned file/workspace fixtures; prove resource ownership and cleanup in focused tests where new support is needed.

## 3. Implement the rendered asset journey

- [x] 3.1 Add a store-level headless journey that arranges two deterministic assets and a supported image picker result, opens `AssetsWindow`, confirms the suggested purpose, and asserts the visible imported row and selection.
- [x] 3.2 Extend the journey through rendered purpose relabeling and bounded settling, asserting the visible purpose, managed-file name, context label, and stable selection.
- [x] 3.3 Exercise the rendered preview thumbnail, assert the observable `AssetPreviewWindow` context, and close the preview without pixel or platform-rendering assertions.
- [x] 3.4 Exercise rendered removal request and cancellation, asserting the row, selection, purpose, and file state remain unchanged.
- [x] 3.5 Close the asset surface, reconstruct a fresh SQLite-backed service/view-model/window composition, reload the store context, and assert rehydrated assets, purposes, managed references, and empty/loading state behavior.

## 4. Preserve focused lower-layer coverage

- [x] 4.1 Retain focused invalid-extension, picker-cancel, missing-file, save-failure, and confirmed-removal tests; remove only assertions made redundant or misleading by the rendered journey.
- [x] 4.2 Add a focused regression first if the journey exposes an asset-surface defect, and record its escape class, similar surfaces inspected, and local-versus-reusable prevention decision.

## 5. Verify and review

- [x] 5.1 Run focused AssetsViewModel, AssetsWindow, preview, helper, and SQLite persistence tests; record exact results and any retained temporary resources.
- [x] 5.2 Run `dotnet build .\\FusionCanvas.sln` and canonical `dotnet test .\\FusionCanvas.sln -m:1`; record duration, failures, flakes, and warnings.
- [x] 5.3 Run `openspec validate assets-headless-experience-journey --strict` and `openspec validate --all --strict`; correct every validation error.
- [x] 5.4 Complete criterion-level verification for every scenario in the testing-baseline delta, including explicit native picker/preview limitations and no-applicable rationale where appropriate.
- [x] 5.5 Perform scoped completion QA covering user-job inventory, rendered-action truthfulness, dispatcher waits, fixture isolation, persistence re-entry, architecture, and documentation/spec drift.
- [x] 5.6 Write `verification.md` and `retrospective.md` with evidence, escaped-defect learning, strategy-health signals, and deferred scope before archive.
