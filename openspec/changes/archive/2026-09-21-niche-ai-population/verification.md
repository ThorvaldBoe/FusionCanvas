# Verification

## Focused evidence

The focused application suite passed with 6/6 tests:

`dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore --filter FullyQualifiedName~NichePopulation -v q`

Evidence is provided by `NichePopulationServiceTests` for General-purpose request assembly, blank-field filtering, print-on-demand visual guidance, independent supported values, excluded fields, malformed/empty responses, invalid requests, and safe provider failures.

The focused App suite passed with 6/6 tests:

`dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~NichePopulation -v q`

Evidence is provided by `NichePopulationViewModelTests` for eligibility and no-request guarantees, draft-only application and explicit save, preservation of concurrent edits, overlap prevention, failure recovery, and settings availability refresh. `StoreEditorHeadlessTests.NichePopulationButton_IsPlacedBesideNameAndAppliesDraftSuggestions` covers rendered placement, binding, automation metadata, successful draft population, and existing discard behavior.

## Acceptance scenario mapping

| Requirement / scenarios | Evidence |
| --- | --- |
| Eligible new/existing niche; blank name; unavailable General AI; archived niche | `NichePopulationViewModelTests.Populate_IsEnabledOnlyForNamedActiveNicheWhenGeneralAiIsReady` |
| Blank eligible fields only; General purpose; print-on-demand visual guidance; independent structured values; excluded Risks/Research/unknown fields | `NichePopulationServiceTests.PopulateAsync_UsesGeneralPurposeAndRequestsOnlySelectedFields`; `PopulateAsync_IgnoresUnknownAndExcludedFields` |
| Blank fields populated; name and existing values preserved; draft-only changes; reviewed values saved through normal path | `NichePopulationViewModelTests.Populate_AppliesBlankSuggestionsToDraftOnlyAndSavePersistsReviewedValues`; `Populate_DoesNotOverwriteFieldEditedWhileRequestIsRunningOrStartOverlap`; headless editor test |
| Busy state, overlapping request prevention, provider failure, malformed/empty/no-usable response, stable recovery | `NichePopulationViewModelTests.Populate_DoesNotOverwriteFieldEditedWhileRequestIsRunningOrStartOverlap`; `Populate_FailurePreservesValuesAndReturnsToEditableState`; `NichePopulationServiceTests.PopulateAsync_RejectsMalformedOrEmptyResponses`; `PopulateAsync_MapsProviderFailureToSafeGuidance` |
| Settings change while editor remains open | `NichePopulationViewModelTests.RefreshAvailability_ReevaluatesPopulationWithoutReopeningEditor` |
| Keyboard/accessible placement and discard confirmation | `StoreEditorHeadlessTests.NichePopulationButton_IsPlacedBesideNameAndAppliesDraftSuggestions` |

## Delivery and limitations

- The full solution baseline passed: `dotnet test .\FusionCanvas.sln` (1,655 passed, 0 failed, 0 skipped across the four test assemblies). Existing analyzer/compiler warnings remain outside this feature's scope.
- Strict OpenSpec validation passed: `openspec validate niche-ai-population --strict --no-interactive`.
- Application and App production projects built successfully during the focused and full-solution runs.
- Real-desktop Appium coverage is not warranted for this small editor action; the approved design records that decision. Avalonia headless coverage exercises the framework-sensitive placement, bindings, metadata, and discard interaction.
