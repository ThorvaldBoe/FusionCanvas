# Verification

## Acceptance evidence

| Scenario | Result | Evidence |
| --- | --- | --- |
| Empty editable slot explains the primary interaction | PASS | `ConfiguredState_EmptySlotsClearlyExposeDropAndBrowseArtwork` in `tests/FusionCanvas.App.Tests/DesignStageToolHeadlessTests.cs` verifies the final-artwork label, PNG drag/drop guidance, enabled Browse action, and drop-target state. |
| Populated editable slot keeps replacement available | PASS | `ConfiguredState_AssignedArtwork_ExposesEnlargeDownloadRemoveAndReplace` verifies the Replace action; existing replacement service coverage remains green. |
| Protected slot remains informative but not editable | PASS | `ConfiguredState_ReadOnlyDisablesFinalArtworkEditing` verifies read-only slots retain the slot surface while disabling drop and browse editing. |
| User drops valid artwork | PASS | Existing `OnSlotDrop` validation routes the selected slot through `AssignSlotImageAsync`; focused application tests verify managed PNG assignment and thumbnail state. |
| User browses for valid artwork | PASS | Browse uses the same slot-scoped assignment helper and managed-file service as drop; shared assignment and reload coverage is green. Native picker invocation is not exercised in headless tests. |
| Invalid artwork is rejected recoverably | PASS | `AssignSlotImageAsync_NonPng_Rejected` and `AssignSlotImageAsync_FileNotFound_Rejected` verify rejection before persistence; the UI helper reports an actionable PNG error. |
| User enlarges assigned artwork | PASS | The assigned-slot headless test verifies the discoverable Enlarge action and accessible name; existing preview service/view-model coverage is green. |
| User downloads assigned artwork | PASS | Existing `ExportSlotImageAsync` application coverage is green; the UI action is labelled Download final design artwork and remains slot-scoped. |
| User removes assigned artwork | PASS | `RemoveSlotImageAsync_ClearsAssignment` and existing remove command coverage are green; the populated/empty slot state is covered by the headless UI tests. |
| User assigns multiple slot artworks | PASS | `AssignSlotImageAsync_MultipleSlots_PersistsIndependentArtwork` assigns two slots, verifies distinct assets, and reloads both assignments. |
| User revisits Design after persistence | PASS | The multiple-slot persistence test reloads the service state and verifies both assignments; existing Design-stage reload coverage remains green. |
| User distinguishes image categories | PASS | Existing `SupportingImagesSection_AlwaysVisible` and `SupportingImages_ImportButtonExists` coverage remains green; final slot labels explicitly say Final design artwork and the service excludes exported slot artwork from Supporting Images. |

## Commands

- PASS: `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore --filter "FullyQualifiedName~DesignStageToolHeadlessTests"` — 24 passed.
- PASS: `dotnet test .\\tests\\FusionCanvas.Application.Tests\\FusionCanvas.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~DesignStageServiceTests"` — 24 passed.
- PASS: `dotnet test .\\FusionCanvas.sln -m:1 --no-restore -v minimal` with `AVALONIA_TELEMETRY_OPTOUT=1` — 1,050 passed, 11 unrelated pre-existing `StoreEditorHeadlessTests` failures.
- PASS: `openspec validate --changes --strict`.

## Baseline limitation

The exact unconstrained `dotnet test .\\FusionCanvas.sln` invocation was stopped after it spawned approximately 1,300 .NET processes without producing test progress. The constrained rerun completed all projects deterministically. Its only failures are existing StoreEditor mockup/catalog UI assertions outside this change; the changed Design-stage suites are fully green.
