# Verification: Global Color Removal

## Result

The global color-removal module is implemented and verified. The source image is preserved; Apply creates a same-kind managed PNG derivative linked to the same Item. The module uses deterministic global RGB matching and intentionally does not include contiguous selection, local AI, or direct OpenAI integration.

## Criterion evidence

| Acceptance area | Evidence | Result |
| --- | --- | --- |
| Eligible Design/supporting images only | `GlobalColorRemovalServiceTests.ApplyAsync_RejectsReadOnlyItemBeforeWriting`; `DesignSlotViewModel.CanRemoveColor`; `GlobalColorRemovalService.ResolveSourceAsync` | Pass |
| Explicit color and normalized tolerance | `GlobalColorRemovalParameters`; `GlobalColorRemovalServiceTests.Parameters_AllowExactBoundsAndRejectOutOfRangeTolerance`; `GlobalColorRemovalTests.ViewModel_UsesHexColorAndPreventsApplyUntilPreviewIsValid` | Pass |
| Deterministic global matching | `ImageSharpGlobalColorRemovalProcessorTests.PreviewAndApply_MatchEveryVisiblePixelGlobally` covers exact matching, nearby-color tolerance, disconnected matches, existing transparency, and binary alpha output | Pass |
| Preview overlay and non-destructive preview | `ImageSharpGlobalColorRemovalProcessor.PreviewAsync`; `GlobalColorRemovalTests.Window_RendersAccessibleControlsAndApplyClosesTheWindow` verifies the rendered preview and controls | Pass |
| No-match/all-visible safeguards | `GlobalColorRemovalRasterPreview` exposes match/remaining counts; `GlobalColorRemovalServiceTests.ApplyAsync_RejectsWhenAllVisibleArtworkWouldBeRemoved`; view-model status/CanApply logic | Pass |
| Derived transparent PNG and source preservation | `GlobalColorRemovalServiceTests.ApplyAsync_CreatesDerivedAssetAndPreservesSource`; ImageSharp PNG alpha assertions | Pass |
| Persistence failure cleanup | `GlobalColorRemovalServiceTests.ApplyAsync_CleansOutputWhenWorkspacePersistenceFails` | Pass |
| Cancellation and duplicate-operation protection | `GlobalColorRemovalService` cancellation guard; `GlobalColorRemovalServiceTests.ApplyAsync_CancellationBeforeOutputDoesNotMutateWorkspace`; generation/cancellation guards in `GlobalColorRemovalViewModel` | Pass |
| Keyboard/accessibility and Apply/Cancel lifecycle | `GlobalColorRemovalWindow` uses accessible names, focusable preview, Slider, Apply, and Cancel; `GlobalColorRemovalTests.Window_RendersAccessibleControlsAndApplyClosesTheWindow`; coordinator restores owner focus | Pass |
| Invalid raster handling | `ImageSharpGlobalColorRemovalProcessorTests.PreviewAsync_RejectsMalformedRasterPayload`; bounded decode/PNG output checks in the ImageSharp processor | Pass |
| Appium decision | No Appium journey is warranted: the module adds no native file picker or OS-specific seam; deterministic Application/Integration tests and Avalonia headless tests cover the meaningful boundaries | Recorded decision |

## Commands

- `openspec validate global-color-removal --type change --strict --no-interactive` — passed.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj -m:1 --no-restore --filter FullyQualifiedName~ImageSharpGlobalColorRemovalProcessorTests` — 3 passed.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj -m:1 --no-restore --filter FullyQualifiedName~GlobalColorRemovalServiceTests` — 6 passed.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj -m:1 --no-restore --filter FullyQualifiedName~GlobalColorRemovalTests` — 2 passed.
- `dotnet test .\FusionCanvas.sln -m:1` — passed: 2,189 tests total across the solution, with 0 failed and 0 skipped.

The build/test runs emitted existing NU1900 warnings because the NuGet vulnerability endpoint was unavailable, along with the repository's existing analyzer warnings. No new test failure or compilation error remained.
