# Verification: Adjust Design Artwork Placement

## Result

The implementation builds and the full deterministic solution test suite passes. The alpha-bound, raster-transform, persistence, live reversible preview, keyboard, Apply, read-only, and transparent-image paths have focused coverage. Dirty-close confirmation, focus restoration, window resizing, pointer dragging, and missing/unreadable preview states were reviewed in implementation but do not yet have dedicated acceptance tests.

## Criterion evidence

| Acceptance scenario | Evidence | Result |
| --- | --- | --- |
| Creator opens a final artwork preview | `DesignStageToolViewModelTests.ArtworkAdjustmentDraft_CanMoveResizeAndCancelWithoutApplying`; existing `DesignPreviewWindowTests` | Pass |
| Creator resizes the preview window | Resizable window and Uniform image binding in `DesignPreviewWindow.axaml` | Implemented; no focused resize assertion |
| PNG contains transparent margins and semi-transparent edge pixels | `ImageSharpArtworkImageProcessorTests.InspectAsync_FindsAlphaBoundsIncludingSemiTransparentPixels` | Pass |
| PNG has no transparent pixels | `ImageSharpArtworkImageProcessorTests.InspectAsync_OpaqueCanvasUsesFullBoundsAndTransparentCanvasHasNoBounds` | Pass |
| PNG is fully transparent | Same Integration test; `ArtworkAdjustmentPreview_DisablesAdjustmentForReadOnlyOrTransparentArtwork` | Pass |
| Creator moves artwork outside and back inside the canvas | `ArtworkAdjustmentSurface` redraws source bounds at draft coordinates and clips only to the canvas; view-model draft test covers negative coordinates and Cancel restoration | Pass for reversible live draft; pointer round trip not directly tested |
| Creator resizes artwork proportionally | `DesignPreviewWindowTests.AdjustmentSurface_KeyboardMovesAndThenResizesProportionally`; proportional ImageSharp transform test | Pass |
| Creator cancels an adjustment draft | `DesignStageToolViewModelTests.ArtworkAdjustmentDraft_CanMoveResizeAndCancelWithoutApplying` | Pass |
| Creator closes a preview with unapplied adjustments | Dirty-close prompt and Keep editing/Discard paths in `DesignPreviewWindow.axaml.cs` | Implemented; no dedicated dialog journey test |
| Creator applies an artwork adjustment | `DesignStageServiceTests.ApplySlotArtworkAdjustmentAsync_SavesBeforeImageAndReplacesSlotWithClippedPng`; ImageSharp transform test | Pass |
| Creator applies a later adjustment to the same slot | Same Application test verifies each before-image contains the immediately prior PNG | Pass |
| Persistence fails during Apply | Same Application test verifies staged-file cleanup and unchanged slot snapshot | Pass |
| Creator reviews artwork in a read-only Design context | `ArtworkAdjustmentPreview_DisablesAdjustmentForReadOnlyOrTransparentArtwork`; Application service rejection assertion | Pass |
| Managed slot artwork is missing or unreadable | Resolution and error handling paths in `DesignStageService` and preview view model | Implemented; no focused adjustment test |
| Creator adjusts artwork without a pointer | Headless keyboard surface and view-model tests cover movement, proportional resizing, Apply, and Cancel | Pass |
| Creator closes the preview with the keyboard | Escape and focus return paths in preview/window coordinator | Implemented; no dedicated focus journey test |
| User previews a Design file | Existing `DesignPreviewWindowTests` covers final/supporting preview binding | Pass |
| User adjusts a Design file | Integration raster tests and Application Apply test | Pass |
| Managed file is missing | Existing Design file state tests | Pass |
| User exports a Design file | Existing Application export regression tests | Pass |
| User applies an adjustment to a Design file | Application before-image/replacement test and Integration clipping test | Pass |
| User removes a Design file | Existing Application removal tests | Pass |
| Removal persistence fails | Existing Application failure regression tests | Pass |

## Appium decision

No Appium journey is planned. The relevant interaction is covered at the ImageSharp, Application, and Avalonia headless layers, with no operating-system-specific seam. Pointer movement and window resizing remain candidates for additional headless interaction assertions if acceptance review finds the lower-layer evidence insufficient.

## Commands

- `dotnet restore .\FusionCanvas.sln -m:1 --ignore-failed-sources -p:NuGetAudit=false -v:minimal` — passed; NuGet audit feed was unavailable, so restore used the local package cache.
- `dotnet build .\FusionCanvas.sln --no-restore -m:1 -v:minimal` — passed (0 errors; existing analyzer warnings).
- `dotnet test .\FusionCanvas.sln --no-restore -m:1 -v:q` — passed: 2,504 tests, 0 failures. `TEMP` and `TMP` pointed to a disposable directory inside the worktree because the default sandbox temp path is denied to workspace tests.
- `openspec validate adjust-design-artwork-placement --strict --no-interactive` — passed.
- `git diff --check` — passed (only line-ending normalization notices for the preview XAML files).
