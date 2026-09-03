# Verification: add-enlarged-mockup-placement-editor

## Criterion Evidence

| Criterion | Method | Result | Evidence / limitations |
| --- | --- | --- | --- |
| Lower-right magnifying-glass-plus control is recognizable | Avalonia headless view construction and source inspection | Pass | `MockupTemplateEditorWindow.axaml` contains a `🔍+` Button with lower-right alignment, tooltip, automation name, and help text. Full existing StoreEditor suite has unrelated stale automation failures on this checkout. |
| Compact drag/resize remains available | Focused Avalonia tests | Pass | `MockupPlacementEditorTests`: 6 pre-existing interaction cases pass; custom control hit area is unchanged and launch button is a sibling overlay. |
| Enlarged editor opens as a larger surface | Build plus view construction | Pass | `EnlargedMockupPlacementEditorWindow` is a transient owner-modal window sized 1100×760 with a stretch placement control. End-to-end StoreEditor launch is limited by existing checkout test fixture drift. |
| Enlarged editor supports reposition and independent resize | Focused placement-control tests | Pass | Drag, resize, clamping, keyboard movement, and Shift+Arrow resize pass in `MockupPlacementEditorTests`. The enlarged surface hosts the same control. |
| Selected image, mapping, and state are preserved | Binding/source inspection and shared-control construction | Pass | Enlarged XAML binds directly to `SelectedLocalSource.DisplayName`, `SelectedImagePreviewPath`, `MappingImageWidth/Height`, and `MappingX/Y/Width/Height`; no copy or alternate draft is introduced. |
| Edits synchronize and follow save semantics | Binding/source inspection and focused tests | Pass | Existing two-way mapping tests pass; enlarged editor only calls `Close`, leaving template Save/Cancel as persistence owners. |
| Keyboard accessibility and focus | Headless window test | Pass | New test validates accessible Close name and click path; code focuses the placement editor on open and handles Escape. Launch control has automation name/help text and standard Button activation. |
| Responsive narrow layout and close/cancel | Headless window test at minimum size | Pass | `EnlargedEditorProvidesAccessibleClosePathAtResponsiveMinimum` passes with MinWidth 440/MinHeight 360 and a visible Close action. |
| Strict OpenSpec validation | `openspec validate --changes --strict` | Pass | Run after artifact completion. |
| Solution baseline | `dotnet test .\\FusionCanvas.sln --no-restore -v minimal` | Warning | 1,427 tests passed; 10 existing `StoreEditorHeadlessTests` failed on provider/catalog automation IDs and an older width expectation. No failure references issue-263 code. |

## Test Commands

- `dotnet restore .\\FusionCanvas.sln` — passed.
- `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~MockupPlacementEditorTests -v minimal` — passed, 7/7.
- `dotnet test .\\FusionCanvas.sln --no-restore -v minimal` — 1,427 passed, 10 pre-existing App headless failures.

## Limitations

The existing full App headless suite contains 10 failures unrelated to this change, including stale expected automation IDs/provider-preview controls and a pre-existing minimum-width expectation. They are retained as an explicit baseline warning rather than hidden behind an aggregate pass.
