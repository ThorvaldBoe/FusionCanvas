# Verification record

## Current status

The implementation is in progress. The deterministic domain, application, integration, and headless UI checks below are passing. The external-provider benchmark remains pending because no configured live analyzer credentials/provider were available in this workspace; no live content was sent externally.

## Acceptance scenario evidence

| Acceptance area | Evidence | Result |
| --- | --- | --- |
| Customer-facing text starts `Unreviewed` and keeps an advisory warning | `ContentRiskReviewTests`; `ItemInspectorServiceTests.Save_CreatesAdvisoryReviewAndRemovesClearedContentReview`; `ItemInspectorViewModelTests.Load_ShowsAdvisoryWarningAndProgressivelyDisclosedFindingDetails` | Pass |
| Generated artwork receives a review target using final bytes | `ArtworkGenerationService` integration path; `ContentRiskReviewServiceTests`; `DesignStageToolHeadlessTests.ConfiguredState_AssignedArtwork_ExposesEnlargeDownloadRemoveAndReplace` | Pass |
| Uploaded PNG artwork receives a persistent review record | `DesignFileServiceTests.ImportAsync_PersistsAssetAndLink` with recording review service | Pass |
| Private supporting/reference material remains outside the boundary | `DesignStageService.ImportSupportingImageAsync` remains without review hook; review target creation is restricted to final customer-facing assignment/import paths | Pass by implementation inspection |
| Findings distinguish IP, safety, and marketplace suitability | `ContentRiskAnalyzerTests.CodecParsesSeparateRiskCategories`; domain category tests; warning projection tests | Pass |
| No findings do not become clearance | `ContentRiskReviewTests`; `ContentRiskWarningProjectionTests` | Pass |
| Provider unavailable/failure/cancellation remains visible and non-blocking | `ContentRiskReviewServiceTests.ReviewFailurePersistsUnavailableAndDoesNotChangeConfirmedContent`; retry and cancellation tests; Integration boundary failure test | Pass |
| Fingerprint changes invalidate prior results | `ContentRiskReviewTests.NewFingerprintDoesNotReusePriorReview`; domain fingerprint tests; cleared-content regression test | Pass |
| Reviews survive reload/transfer and malformed persistence is safe | `ContentRiskPersistenceTests`; workspace transfer tests | Pass |
| Inputs and responses are bounded and privacy-aware | `ContentRiskAnalyzerTests`; `ContentRiskAnalyzerBoundaryTests`; strict response codec tests | Pass |
| Warning appears before Concept in Ideation | `IdeationWindowTests.WindowConstructsWithScopeInputModeCountAndAccessibleCandidateList` | Pass |
| Warning and details are keyboard-reachable/inline | `ItemInspectorViewModelTests`; `IdeationWindowTests`; `DesignStageToolHeadlessTests` | Pass; inline disclosure keeps focus on the invoking control |
| Light/Dark state is not color-only | Existing shared `WarningTextBrush`, `WarningBackgroundBrush`, and text explanations in `DesignTokens.axaml`; UI tests assert warning copy/visibility | Pass by implementation inspection |

## Commands run

- `dotnet test tests\FusionCanvas.Domain.Tests\FusionCanvas.Domain.Tests.csproj --no-restore` — 279 passed.
- `dotnet test tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ContentRisk"` — 12 passed.
- `dotnet test tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ItemInspectorServiceTests"` — 23 passed.
- `dotnet test tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore --filter "FullyQualifiedName~ContentRiskAnalyzerBoundaryTests"` — 2 passed.
- `dotnet test tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter "FullyQualifiedName~IdeationWindowTests|FullyQualifiedName~ItemInspectorViewModelTests"` — 46 passed.
- `dotnet test tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter "FullyQualifiedName~DesignStageToolHeadlessTests.ConfiguredState_AssignedArtwork_ExposesEnlargeDownloadRemoveAndReplace"` — 1 passed.
- `openspec validate content-risk-awareness-advisory-review --type change --strict --json` — valid.
- `dotnet test .\FusionCanvas.sln --no-restore --logger "console;verbosity=minimal"` — 2,139 passed across the solution after splitting the Terms Consent interface into `IExternalLinkLauncher.cs` and the implementation into `ExternalLinkLauncher.cs`.

## Benchmark and supplemental desktop plan

No live benchmark was executed: the configured analyzer path had no available provider credentials in the test environment. Deterministic cases cover a possible IP finding, a safety finding, an empty result, malformed/oversized responses, image bounds, provider failure, and cancellation. A live benchmark is still required before tuning provider choice or thresholds; the feature remains advisory regardless of its result.

The supplemental Appium scenario pack should use one disposable workspace and cover: open Ideation and observe the default warning; accept a Phrase or title and observe the persistent warning; assign/import artwork and inspect the slot warning; expand details by keyboard; and simulate unavailable review while confirming the content remains usable. It is intentionally supplemental to the deterministic baseline.
