# Verification — Virtualize Manual Listing Variant Pricing

## Acceptance Scenarios

| Scenario | Method | Result | Evidence / limitations |
| --- | --- | --- | --- |
| User edits Variant pricing | Headless Avalonia test edits both bound fields on two distinct row models and asserts equal column bounds across rows. | Passed | `VirtualDataGridIntegrationTests.Template_editors_support_keyboard_navigation_and_two_way_text_input`; focused App run passed 2 tests in 3s. Existing Listing Details tests continue covering validation, gross profit, dirty state, and save mapping. |
| User moves between realized price editors with the keyboard | Headless Avalonia test clicks the first price editor, presses Tab through cost and the next Variant's price/cost, and checks focus at each step. | Passed | `VirtualDataGridIntegrationTests.Template_editors_support_keyboard_navigation_and_two_way_text_input`; focused App run passed 2 tests in 3s. |
| Listing details cannot edit fulfillment terms | MainWindow headless fixture verifies the grid is disabled with no active Offering and provider rows refresh when the Variants collection changes. | Passed | `MainWindowConstructionTests.ListingDetailsToolShowsAccessibleFieldsForManualStoreAndEmptyOfferingGuidance`; focused App run passed 2 tests in 3s. |

## Required Validation

- Focused App headless tests: passed, 2/2 (3s).
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore`: passed, 2,344 tests across Domain (294), Application (671), Integration (352), App (998), and UiDescription (29); no failures or skips.
- Strict OpenSpec validation: `openspec validate --strict --type change virtualize-manual-listing-variant-pricing` passed.
- Changed-scope review: changes are limited to the MainWindow presentation and provider lifecycle, focused App headless coverage, and OpenSpec artifacts. No Domain/Application/persistence/database/API/dependency changes. `git diff --check` passed.

## Limitations

- Test and build output includes existing NuGet vulnerability audit warnings, including current ImageSharp advisories; this issue does not change package versions or dependencies.
