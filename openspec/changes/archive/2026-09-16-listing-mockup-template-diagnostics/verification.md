# Verification

| Acceptance criterion | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| No mockup templates are configured | Listing view-model test with empty candidate diagnostics | Pass | `StageToolViewModelsTests.ListingTool_DistinguishesOfferingWithNoTemplates` confirms the distinct Store settings guidance. |
| Configured templates are incomplete | Application eligibility test and Listing view-model test with two blockers | Pass | `MockupTemplateSetupServiceTests.NameOnlyTemplateSavesOnceWithoutProviderAndReturnsStableId` confirms candidate blocker data; `StageToolViewModelsTests.ListingTool_ShowsTemplateBlockersWhenNoReadyTemplateExists` confirms creator-facing wording. |
| A template becomes ready | Existing readiness and eligibility regression tests plus full solution test baseline | Pass | Ready templates continue to populate the selector through the unchanged eligibility gate. |
| Readiness diagnostics are unavailable | Existing invalid-context mockup-generation coverage plus state separation in `MockupGenerationService.LoadAsync` | Pass | Eligibility errors remain in `Error` and do not produce the no-template message. |
| Diagnostic state does not weaken eligibility | Existing ready-only eligibility tests plus full solution test baseline | Pass | Candidate diagnostics are presentation data; `Templates` still contains only ready candidates. |

## Verification commands

- `openspec validate listing-mockup-template-diagnostics` — passed.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore -m:1 --filter FullyQualifiedName~MockupTemplateSetupServiceTests` — passed, 9 tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 -p:UsedAvaloniaProducts= --filter FullyQualifiedName~StageToolViewModelsTests` — passed, 7 tests.
- Full solution baseline is run separately with `-p:UsedAvaloniaProducts=` because the environment denies Avalonia telemetry writes to the user profile log path.
