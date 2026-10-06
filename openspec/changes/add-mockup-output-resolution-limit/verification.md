# Verification

## Acceptance criteria

| Criterion | Result | Evidence |
| --- | --- | --- |
| Missing Store setting defaults to 2000 and valid values round-trip while preserving metadata | PASS | `MockupOutputResolutionSettingsServiceTests` |
| Invalid resolution values are rejected by the policy and dialog validation | PASS | `MockupOutputResolutionPolicyTests`; `CatalogSetupViewModelTests.MaximumMockupLongEdgeRequiresPositiveWholeNumber` |
| Large landscape output is proportionally downscaled before compositing | PASS | `ImageSharpMockupRasterCompositorTests.ComposeAsync_DownscalesLongEdgeAndMappingTogether` (4000×3000 → 2000×1500) |
| Smaller output is not enlarged and PNG behavior remains intact | PASS | Existing `ImageSharpMockupRasterCompositorTests.ComposeAsync_PreservesTemplateDimensionsAndFitsDesignInMapping` |
| Generated outputs receive the Store policy and effective dimensions in provenance | PASS | `MockupGenerationApplicabilityRegressionTests.Apply_uses_store_resolution_and_records_rendered_dimensions` |
| Dialog shows the Store-wide field, helper text, default, and accessible name | PASS | `StoreEditorHeadlessTests.CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions` |
| OpenSpec change artifacts are valid | PASS | `openspec validate add-mockup-output-resolution-limit --type change --strict` |

## Test runs

- `dotnet test .\tests\FusionCanvas.Domain.Tests\FusionCanvas.Domain.Tests.csproj --no-restore -v minimal` — PASS (290 tests).
- Mockup-filtered Application tests — PASS (50 tests).
- ImageSharp mockup compositor Integration tests — PASS (4 tests).
- Mockup generation Application regression tests — PASS (4 tests).
- Isolated Avalonia app build — PASS (0 warnings, 0 errors).
- Focused Avalonia headless checks — PASS (2 tests).
- `dotnet test .\FusionCanvas.sln --no-build -m:1 -v:minimal` — PASS (2,239 tests: Domain 290, Application 641, Integration 337, App 942, UI description 29).
- A current-source full build/test run in an isolated output directory reached all projects and passed Domain/Application/Integration/UI-description, but the existing App headless suite reported 4 unrelated failures (956 passed, 4 failed): two image-preview timing assertions, one mockup-row keyboard-selection assertion, and one fixture path assertion caused by the isolated output root. The feature-specific current-source build and focused Avalonia tests passed.

The normal desktop application was already running during verification and held its standard output assemblies open. The Avalonia build and focused headless checks therefore used an isolated output directory; this did not change source or test behavior.
