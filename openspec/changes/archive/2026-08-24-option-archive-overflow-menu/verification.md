# Verification — option-archive-overflow-menu

Change: `option-archive-overflow-menu` (issue #192). Baseline: `dotnet test .\FusionCanvas.sln` passes (Domain 232, UiDescription 27, Application 384, App 516, Integration 184). Strict OpenSpec validation passes.

## Scenario-to-evidence mapping

### Capability: variant-management (MODIFIED requirement)

| Scenario | Verification method | Result | Evidence |
| --- | --- | --- | --- |
| User opens Variant management | Existing headless navigation assertions | Pass | `StoreEditorHeadlessTests.ProductsPanel_DisclosesProductAndOfferingActionsByLevel`, `OfferingAndFocusedEditorsPreserveApprovedBroadComposition` |
| User scans available choices | Headless: choices before sellable, manage action disclosure | Pass | Existing `OfferingAndFocusedEditorsPreserveApprovedBroadComposition`; `OptionCardsMoveArchiveIntoOverflowMenu` asserts Manage values present, no direct Archive button |
| User manages values for one Option | Existing headless editor reveal/focus tests | Pass | `CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions` (unchanged) |
| **User opens the overflow menu for one Option** | Headless pointer + keyboard open assertions | Pass | `OptionOverflowMenu_OpensByPointerAndKeyboard` |
| **User dismisses the overflow menu** | Headless: open then hide, focus returns to overflow button, no data change | Pass | `OptionOverflowMenu_DismissalReturnsFocusAndMakesNoChange` |
| **User archives an Option from the overflow menu** | Headless: menu entry bound to archive command; invokes it with a referenced Option; inline recoverable error surfaces | Pass | `OptionOverflowMenu_ContainsDestructiveArchiveEntryForTheOption`, `OptionOverflowMenu_InvokesArchiveAndSurfacesBlockedReason` |
| **User uses the overflow menu by keyboard and assistive technology** | Headless: keyboard open via Enter; accessible name asserted against `AutomationProperties.Name`; MenuItem marked destructive (`danger` class) | Pass | `OptionOverflowMenu_OpensByPointerAndKeyboard`, `OptionOverflowMenu_ContainsDestructiveArchiveEntryForTheOption`, `OptionCardsMoveArchiveIntoOverflowMenu` (names `More actions for Color/Size`), framework-free `ChoiceGroupOverflowNameIdentifiesTheOptionKind` |
| **Option card supports every Option kind** | Framework-free theory over Color, Size, Other; shared single data template for all kinds | Pass | `ChoiceGroupOverflowNameIdentifiesTheOptionKind`; `OptionCardsMoveArchiveIntoOverflowMenu` covers Color and Size cards from one template |
| User enables provider-catalog choices | Existing Application/variant tests (unchanged) | Pass | `CatalogSetupServiceTests`, `OfferingManagementServiceTests` |
| User scans sellable Variants | Existing headless + view-model projections (unchanged) | Pass | `CatalogPresentationModelsTests.SellableVariant*` |
| User starts one Variant draft | Existing headless exclusivity tests (unchanged) | Pass | `CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions` |
| User starts a bulk Variant draft | Existing headless exclusivity/focus tests (unchanged) | Pass | same test |
| User creates one sellable Variant | Existing Application tests (unchanged) | Pass | `CatalogSetupServiceTests` |

## Scope-drift review

- Changed files: `OfferingChoiceGroupViewModel.cs` (relayed `ArchiveOptionCommand` + accessor strings), `CatalogSetupViewModel.cs` (group construction passes the command), `StoreEditorWindow.axaml` (card template + styles), two test files. Domain/Application/Integration untouched.
- One implementation correction during execution: the `MenuItem` cannot use `$parent[ItemsControl]` because flyout popup content is not in the ItemsControl's visual tree, so the binding would be null in the real application. Fixed by relaying `ArchiveOptionCommand` through the card's `OfferingChoiceGroupViewModel`, which the flyout inherits as DataContext. Command semantics unchanged.
- Archive eligibility, dependency checks, error text, and blocked behavior are untouched; `ArchiveOptionCommand`/`RunArchive`/`CatalogSetupService.ArchiveAsync` reused verbatim.

## Limitations

- Focus-return on dismissal is verified headlessly by opening/hiding the flyout in the harness; OS-level focus behavior of the native popup was not exercised on a live desktop (optional, ad hoc, not a gate).
- Visual prominence (size/color contrast of the menu entry vs. the former button) is not pixel-verified; asserted via control classes (`danger` menu item, compact `iconButton`) and the absence of the old large button.