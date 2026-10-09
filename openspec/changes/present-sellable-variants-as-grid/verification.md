# Verification: Present Sellable Variants as Grid

## Acceptance Scenarios

| Scenario | Verification | Result | Evidence |
| --- | --- | --- | --- |
| Creator scans sellable Variants | Avalonia headless rendered-view test | Passed | `VariantManagement_GridAlignsSemanticValuesAndVirtualizesTheActiveRows` verifies the Name/Color/Size/Other/Action headings and cells, resolved values, active order, the active-only row source, fixed 32-pixel rows, and that 41 active rows use fewer realized rows than the provider count. |
| A cell value exceeds its visible width | Avalonia headless rendered-view test | Passed | The same test checks ellipsis and full matching values in both tooltip and automation help text for long Name/Color/Size/Other cells. |
| Creator archives a sellable Variant | Avalonia headless rendered-view and view-model tests | Passed | `VariantManagement_GridArchiveActionPreservesBlockedDependencyFeedbackByPointerAndKeyboard` verifies the Archive target is the exact row, pointer and keyboard activation, and the existing blocked Placeholder/Front message. `VariantManagement_GridRefreshesAfterSuccessfulArchive` verifies successful use of the existing archive command removes the row, refreshes the count to zero, and resets the provider. Existing `CatalogSetupViewModelTests` also cover stale row and archive behavior. |

## Verification Runs

- Focused App tests: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj -m:1 --no-build --no-restore --filter "FullyQualifiedName~VariantManagement_GridArchiveActionPreservesBlockedDependencyFeedbackByPointerAndKeyboard|FullyQualifiedName~VariantManagement_GridAlignsSemanticValuesAndVirtualizesTheActiveRows|FullyQualifiedName~VariantManagement_GridRefreshesAfterSuccessfulArchive|FullyQualifiedName~StoreEditorWindow_DetachesViewModelAndCatalogSubscriptionsOnRebindAndClose"` — **4 passed, 0 failed**.
- Strict accepted-spec validation: `openspec validate --specs --strict --no-interactive` — **65 passed, 0 failed**.
- Strict change validation: `openspec validate present-sellable-variants-as-grid --type change --strict --no-interactive` — **valid**.
- Full repository baseline: `dotnet test .\FusionCanvas.sln -m:1` — **2,343 passed, 0 failed** (Domain 294, Application 671, Integration 352, App 997, UI Description 29).
- `git diff --check` — passed.

## Limitations and Warnings

- No real-desktop or Appium journey was run; the user-facing risk is covered by deterministic headless view tests for layout, virtualization, routed pointer and keyboard input, provider refresh, and view lifetime.
- Restore reported NU1900 because `https://api.nuget.org/v3/index.json` was unreachable for vulnerability data. Existing ImageSharp 3.1.12 advisories and unrelated test analyzer/compiler warnings were reported; all tests passed.
