# Verification evidence

This is the current implementation checkpoint for issue #731. Remaining unchecked tasks in `tasks.md` are intentionally not presented as complete.

| Acceptance area | Result | Evidence |
| --- | --- | --- |
| Coverage value types and statuses | PASS | Framework-independent domain types compile; `MockupTemplateCoveragePlannerTests` covers grouping, incomplete rows, no target area, and stale context. |
| Authoritative exact-one resolution | PASS | Planner delegates to `MockupTemplateSourcePolicy.Resolve`; application load regression confirms derived plans do not create source rows. |
| Color-first and finer grouping | PASS | Domain tests cover Color-first unrestricted secondary options and Color+Size refinement. |
| Stale planning context | PASS | Domain fingerprint test and editor command guards prevent assignment while stale. |
| Requirement assignment and managed-asset reuse | PASS | `AssignExistingReusesManagedAssetAndCreatesTargetRevision` and `AssignExistingCanSkipMappingWithoutCreatingAnotherAsset` prove cross-template reuse preserves the existing Asset, applicability, mapping choice, and target revision history. |
| Exemplar applicability and mapping safety | PASS | `CoverageExemplarPrefillsSafeApplicabilityAndMatchingMapping` and `CoverageExemplarSurfacesMappingReviewWhenDimensionsDiffer` prove unrestricted secondary-option defaults, dimension-matched mapping reuse, and explicit correction state. |
| Focused editor projection | PARTIAL | Coverage summary, requirement list, refresh/grouping controls, add-image prefill, managed-image assignment, exemplar feedback, and accessible panel bindings are wired. Full stale/error/focus/narrow-layout interaction coverage remains. |
| Listing diagnostics | PARTIAL | Listing exposes the authoritative plan summary beside readiness blockers. Full configured-Draft transition coverage remains. |
| Build | PASS | `dotnet build .\FusionCanvas.sln -m:1 --no-restore` — 0 errors. |
| Focused deterministic tests | PASS | Domain planner: 5 passed; application revision/coverage regression: 7 passed; exemplar view-model tests: 2 passed; focused Avalonia coverage-panel test: 1 passed; prior App stage/layout/catalog selection set: 29 passed. |
| OpenSpec validation | PASS | `openspec validate mockup-template-coverage-planning --strict`. |
| Full solution test baseline | BLOCKED BY EXISTING FAILURE | `dotnet test .\FusionCanvas.sln -m:1 --no-restore`: Domain 278 passed, Application 587 passed, Integration 309 passed, App 922 passed/1 failed, UI description 29 passed. The remaining failure is the unrelated pre-existing `ProductionSourceLayoutTests.ProductionFiles_ContainAtMostOneTopLevelType` in `src\FusionCanvas.App\TermsConsent\ExternalLinkLauncher.cs`. |
