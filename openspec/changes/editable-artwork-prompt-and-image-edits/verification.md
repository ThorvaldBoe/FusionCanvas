# Verification

## Acceptance scenarios and evidence

| Scenario | Method and result | Evidence / limits |
| --- | --- | --- |
| Current default reflects Concept and Store settings deterministically | Passed. `ArtworkPromptCompilerTests.BuildDefault_UsesCurrentContextAndSeparatesContrastFromBackground` checks repeatable output, phrase, current Niche/Store direction, dimensions/composition, and omission of unrelated SLL assumptions. | `tests/FusionCanvas.Application.Tests/DesignFiles/ArtworkPromptCompilerTests.cs` |
| Creator edits and uses prompt | Passed. Item metadata preference tests verify override save/reload; artwork generation tests verify the prompt sent to the provider. | `DesignStageServiceTests.ArtworkPromptPreferences_PersistOverrideContrastAndAdjustmentThenResetFromCurrentContext`; `ArtworkGenerationServiceTests.GenerateAsync_IncludesNicheContextAndPrintableArtworkConstraintInPrompt`. The prompt editor is covered by the App headless test below. |
| Reset after source changes uses current source | Passed. The persistence test updates the Concept source, clears the override, and checks the regenerated default includes the updated idea. | `DesignStageServiceTests.ArtworkPromptPreferences_PersistOverrideContrastAndAdjustmentThenResetFromCurrentContext` |
| Stale/malformed SLL guidance is excluded | Passed. Compiler tests assert stale and invalid SLL composition is omitted. | `ArtworkPromptCompilerTests.BuildDefault_OmitsStaleSllCompositionAndUsesDarkOnLightDirection`; `BuildDefault_OmitsMalformedSllComposition` |
| Light-on-dark / dark-on-light remain independent of background | Passed. Prompt compiler tests check foreground direction and empty/transparent background guidance; preference test checks saved contrast. | `ArtworkPromptCompilerTests` and `DesignStageServiceTests.ArtworkPromptPreferences_PersistOverrideContrastAndAdjustmentThenResetFromCurrentContext` |
| Design controls expose prompt, contrast, reset, and edit action | Passed. Avalonia headless view test expands the prompt section and inspects the editors, selector, reset action, disabled edit action when no artwork is assigned, and contrast selection binding. | `tests/FusionCanvas.App.Tests/DesignStageToolHeadlessTests.cs::ArtworkGenerationSection_ExposesTargetAndTransparencyControls` |
| Adjust sends current PNG with separate instructions and preserves prior image | Passed. Application test checks the exact adjustment prompt and reference bytes/media type, new slot asset, and Item-linked before-image. | `ArtworkGenerationServiceTests.GenerateAsync_EditSendsCurrentPngAndSeparateInstructionsThenPreservesBeforeImage` |
| Edit requires an endpoint advertising reference support | Passed. Endpoint selection policy requires a positive advertised reference maximum only for edit requests; OpenRouter integration tests check capability parsing and reference serialization. | `AiConfigurationTests.ImageEndpointPolicy_RequiresAdvertisedReferenceSupportOnlyForEdits`; `OpenRouterClientTests` reference capability/request tests |
| Edit requires an assigned current image | Passed. Application service rejects a slot without an assigned final PNG before provider dispatch; headless view also confirms edit is disabled when artwork is absent. | `ArtworkGenerationServiceTests.GenerateAsync_EditRejectsSlotWithoutAssignedArtworkBeforeProviderDispatch`; `DesignStageToolHeadlessTests.ArtworkGenerationSection_ExposesTargetAndTransparencyControls` |
| Failed/stale/cancelled edits do not persist partial output | Passed for stale source assignment; existing generation cleanup and cancellation tests pass, covering the shared staged-file cleanup path. | `ArtworkGenerationServiceTests.GenerateAsync_EditDiscardsProviderResultWhenAssignedSourceChanges`; existing `GenerateAsync_CleansGeneratedFileAndPropagatesWorkspaceSaveCancellation` and `GenerateAsync_StopsAfterProviderCancellationBeforeNormalization`. No separate provider cancellation test specifically for edit mode. |
| Curated styles follow-up | Passed. Created [GitHub issue #964](https://github.com/ThorvaldBoe/FusionCanvas/issues/964); no curated-style UI or presets are included in this module. | Public issue URL above |

## Validation runs

- `openspec validate editable-artwork-prompt-and-image-edits --strict` — passed.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore -v:q` — passed (exit code 0). The solution includes Domain, Application, Integration, App headless, and UI Description test projects.
- Direct full App test run after the test-project build — passed: 1,041 tests, 0 failures.
- Application test suite after the final edit-stale-context test — passed (exit code 0).
- NuGet emitted `NU1900` warnings because vulnerability metadata could not be fetched from `api.nuget.org`; cached packages restored and all test projects ran.
