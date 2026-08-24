# Verification — present-options-as-bordered-cards

Baseline evidence: all commands ran in `C:\Code\FusionCanvas-193-option-bordered-cards` (branch `codex/193-option-bordered-cards`).

| # | Delta-spec scenario | Evidence | Result |
| --- | --- | --- | --- |
| V-1 | User scans available choices as cards | `StoreEditorHeadlessTests.AvailableOptionChoiceCards_UseBorderedCardTreatmentAndStackOnNarrowWidth` asserts exactly two `Catalog.OptionCard` borders for a Color + Size fixture and per-card `BorderThickness == 1`, non-null `BorderBrush`/`Background`, and `CornerRadius == 6`. Full App.Tests run passes all 509 tests, including this one. | PASS |
| V-2 | Empty Option uses the same card treatment | Shared card template renders all groups; `OfferingChoiceGroupViewModel.ValuesSummary` returns truthful `"No values configured"` for zero-value groups (existing unit-covered projection in `CatalogSetupViewModelTests`), and the card template applies to every group without branching. Confirmed structurally in the changed template; existing App.Tests suite (509) green. | PASS (structural evidence) |
| V-3 | Custom Option kind uses the same card treatment | Kind label binds `Option.OptionKind.ToString()` (`OfferingChoiceGroupViewModel`), so custom kinds render in the identical card; no special casing in the template. Existing `OfferingChoiceGroupViewModel` projection tests and the full App.Tests suite (509) green. | PASS (structural evidence) |
| V-4 | Cards align cleanly in the available width | Same headless test asserts, via window-relative `TranslatePoint`, that Color and Size cards share the same Y and that `colorLeft + width <= sizeLeft` at the default 860×620 window (card width 235; measured ~498px card area). | PASS |
| V-5 | Cards stack at narrower supported widths | Same headless test resizes the window to `MinWidth` (720), runs layout, and asserts the second card's Y is below the first card's Y (wraps to a new row). | PASS |
| V-6 | Long content does not clip | Option name and value summary `TextBlock`s use `TextWrapping="Wrap"` (changed template); kind label top-aligned in an Auto column. Verified structurally; App.Tests suite (509) green. | PASS (structural evidence) |

## Command evidence

- `dotnet build .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj` → 0 errors.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --filter "FullyQualifiedName~StoreEditorHeadlessTests"` → 12/12 passed.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj` → 509/509 passed.
- `dotnet test .\FusionCanvas.sln` → all projects passed (Domain 232, UiDescription 27, Application 384, Integration 184, App 509).
- `openspec validate present-options-as-bordered-cards --strict` → valid.

## Scope-drift check

Changed scope is limited to `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` (new `Border.choiceCard` style + Available choices card template) and `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` (one new headless test plus `using Avalonia;`). No Domain, Application, Integration, schema, or data changes. Issue #191-style overflow-menu archive (Issue #192) is explicitly out of scope.

## Supplemental (optional) evidence

Theme appearance in Light and Dark was verified via the shared theme resources (`ControlBorderBrush`/`ElevatedSurfaceBrush` over `PanelSurfaceBrush`); a live desktop visual check may supplement but is not a completion gate for this change.