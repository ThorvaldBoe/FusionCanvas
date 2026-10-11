# Verification

## Acceptance scenarios

| Scenario | Result | Evidence |
| --- | --- | --- |
| User reviews a large asset collection | Pass | `AssetsWindow_RealizesOnlyVisibleRowsAndScrollsToLaterAsset` loaded 120 assets, confirmed fewer row controls than assets were realized in the initial viewport, and confirmed the collection extent exceeded the viewport. |
| User interacts with an asset row | Pass | The same headless test scrolled to the final asset, then verified its purpose selector and Remove action remained bound. Existing `StoreAssetJourney_ImportsRelabelsPreviewsCancelsRemovalAndRehydrates` passed in the focused and full App test runs. |
| Asset details exceed available row width | Pass | The headless test verified the final long name remains exposed through its tooltip, automation name, and automation help text. The row template exposes the managed filename and context label similarly. |
| User scrolls after removing an asset | Pass | Existing asset journey verifies removal confirmation/cancellation, collection updates, and durable reload behavior; no removal semantics changed in this module. |

## Verification commands

- Focused tests: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj -m:1 --filter "FullyQualifiedName~AssetsWindowHeadlessTests"` — passed, 2 tests.
- Solution baseline: `dotnet test .\FusionCanvas.sln -m:1` — passed, 2,392 tests (Domain 294, Application 686, Integration 364, App 1,019, UI Description 29).
- Strict OpenSpec validation: `openspec validate --all --strict` — passed, 90 items.
- Whitespace validation: `git diff --check` — passed.

Restore reported existing NU1902/NU1903 advisories for the repository's current SixLabors.ImageSharp dependency; they are unrelated to this UI change.
