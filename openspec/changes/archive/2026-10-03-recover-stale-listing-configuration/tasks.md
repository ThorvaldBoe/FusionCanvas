## 1. Application Recovery Contract

- [x] 1.1 Add deterministic `DesignStageServiceTests` that reproduce archived and missing persisted Offering configurations, distinguish stale recovery from other read-only causes, and cover the no-candidate state.
- [x] 1.2 Extend `DesignStageState` and `DesignStageService.BuildState` with persisted stale identity, explicit recovery permission, active same-Store candidate projection, and actionable no-candidate guidance without changing ordinary configuration behavior.
- [x] 1.3 Add `RecoverStaleConfigurationAsync` to `IDesignStageService` and update all test fakes/implementers to compile while preserving their existing behavior.
- [x] 1.4 Add application tests for valid recovery, the exact reset/preservation boundary, replacement catalogs with different Placeholders/Variants, protected/cross-Store/inactive/concurrently changed candidates, save failure atomicity, and authoritative reload.
- [x] 1.5 Implement the confirmed recovery operation with fresh policy/catalog validation, one snapshot transformation and repository save, no managed-file deletion, and no inferred relationship mapping; share a private transformation with normal configuration selection only when behavior remains unchanged.

## 2. Recovery Presentation

- [x] 2.1 Add `DesignStageToolViewModelTests` for candidate selection without persistence, named pending confirmation, cancellation, duplicate suppression, validation/persistence error retention, and successful authoritative refresh.
- [x] 2.2 Extend `DesignStageToolViewModel` with separate recovery selection and confirmation state, busy/error handling, context-reset behavior, and confirm/cancel operations without routing through the auto-persisting normal selector.
- [x] 2.3 Update the Listing Configuration section in `MainWindow.axaml` with stale identity, recovery-only candidate selection, no-candidate Store Editor guidance, and a progressively disclosed inline reset/preservation confirmation with accessible action names.
- [x] 2.4 Add minimal `MainWindow.axaml.cs` routed action and focus coordination so confirmation focuses Replace, Cancel/Escape returns to recovery selection, success returns to the normal configuration selector, and failures leave a reachable retry path.
- [x] 2.5 Add `DesignStageToolHeadlessTests` proving stale review state, the sole enabled recovery mutation, no-candidate guidance, routed confirm/cancel and Escape behavior, accessible names, focus return, and restored normal editability after success.

## 3. Persistence and Criterion Verification

- [x] 3.1 Add or extend a focused SQLite persistence test in `ProductCatalogPersistenceTests` when needed to prove replacement reload, absence of cleared Offering-specific relationships, and preservation of representative Asset, Supporting Image, workflow/lifecycle, and downstream Listing/mockup records. (No new SQLite case was needed: recovery uses the existing transactional whole-snapshot `SaveAsync`; application tests prove the transformation and authoritative reload, while the existing persistence suite proves round-trip coverage for the affected snapshot collections.)
- [x] 3.2 Verify every scenario in both delta specs against named application, view-model, headless, and persistence tests; correct implementation or artifacts for any failed criterion rather than accepting an aggregate pass.
- [x] 3.3 Inspect adjacent read-only Design paths and normal configuration selection for regression risk, confirming that recovery authority is not exposed to archived Stores, protected Items, or non-current Design views.

## 4. Validation and Completion

- [x] 4.1 Run the focused Domain/Application/App/Integration tests touched by the change and correct all failures and warnings.
- [x] 4.2 Run strict OpenSpec validation for `recover-stale-listing-configuration` and correct any proposal, design, task, or delta-spec defect.
- [x] 4.3 Run `dotnet test .\FusionCanvas.sln -m:1` and retain criterion-level evidence for `verification.md` and completion QA.
- [x] 4.4 Perform changed-scope architecture, security, persistence, and UI drift review; document results, limitations, and any optional Windows live-check observation without making live desktop testing a completion gate.
