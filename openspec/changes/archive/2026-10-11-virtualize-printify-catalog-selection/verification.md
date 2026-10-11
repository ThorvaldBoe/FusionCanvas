# Verification — Virtualize Printify catalog selection

| Acceptance scenario | Result | Evidence |
| --- | --- | --- |
| Many products are displayed in a virtualized grid | Passed | `StoreEditorHeadlessTests.PrintifyCatalogGridVirtualizesRowsAndPreservesAccessibleSelectionAcrossScrolling` loads 120 products, verifies four fixed-width aligned columns and non-sortable/non-resizable settings, confirms the viewport realizes fewer rows than the full collection, and observes scroll extent beyond the viewport. |
| Selection survives row recycling | Passed | The same headless test selects the first product with keyboard Space, scrolls so it is recycled, pointer-selects a later product, returns to the top and verifies both selections, confirms no product IDs were submitted before confirmation, then verifies exactly `product-0` and `product-70` are submitted. |
| Long product text remains available | Passed | The same test verifies ellipsis trimming is configured, the long Blueprint and title remain available through tooltips and help text, the identity column exposes its full subtitle, and the checkbox accessible name contains the complete Blueprint and shop-product title. |

## Required validation

- Focused Printify view-model and Store Editor UI regression set: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter "FullyQualifiedName~PrintifyCatalogImportViewModelTests|FullyQualifiedName~StoreEditorHeadlessTests.Printify"` — passed (12 tests).
- Focused virtualization journey: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~StoreEditorHeadlessTests.PrintifyCatalogGridVirtualizesRowsAndPreservesAccessibleSelectionAcrossScrolling` — passed (1 test).
- Solution baseline: `dotnet test .\FusionCanvas.sln -m:1` — passed (exit code 0; 2,391 tests across the solution).
- Strict OpenSpec validation: `openspec validate --all --strict` — passed (90 items before sync; the accepted-spec sync is validated before archive).
- `git diff --check` — passed.

The test runs report existing SixLabors.ImageSharp package vulnerability advisories (NU1902/NU1903); this change does not alter package references.
