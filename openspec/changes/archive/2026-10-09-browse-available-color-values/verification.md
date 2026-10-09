# Verification

## Acceptance scenario evidence

| Scenario | Method and evidence | Result | Limitations |
| --- | --- | --- | --- |
| User scans configured Colors | `AvailableColorValuesGridViewModelTests.Defaults_to_configured_order_and_excludes_archived_values` verifies saved `SortOrder` and archived-value exclusion. `StoreEditorHeadlessTests.Available_colors_grid_renders_accessible_controls_templates_stripes_and_empty_states` renders two rows in Light and Dark, verifies Color-only row templates, actual `Token.Color.SurfaceSubtle` fills, and the Manage values command/Option binding. | Pass | Variant persistence is unchanged by the read-only view model; no database migration is involved. |
| User searches Color values across all pages | `AvailableColorValuesGridViewModelTests.Search_filters_the_complete_set_case_insensitively_and_returns_to_page_one` finds a match beyond the first page, verifies trimmed case-insensitive matching, then clears the search and verifies restoration. | Pass | Search is local to the already loaded Offering values. |
| User changes the Color page size | `AvailableColorValuesGridViewModelTests.Page_sizes_bound_rows_and_report_ceiling_page_count` checks page sizes 10, 25, and 50, page counts, row limits, range totals, and next-page availability. | Pass | Grid rows may scroll within the fixed viewport on larger pages. |
| User sorts Color values | `AvailableColorValuesGridViewModelTests.Sort_modes_are_global_stable_and_configured_order_can_be_restored` verifies ordinal case-insensitive names, saved-order tie breaks, and name sorting across page boundaries. | Pass | Sort state is transient presentation state. |
| User returns to saved Color order | The same sort test returns to Configured order and verifies the original sequence. `AvailableColorValuesGridViewModelTests.Refresh_preserves_same_offering_preferences_clamps_page_and_context_switch_resets_them` verifies refresh and Offering state behavior. | Pass | No sort preference is persisted. |
| Color grid has no rows to show | Light/Dark headless test checks the no-match and no-configured-Colors messages, zero result summary, and disabled Previous/Next commands and buttons. | Pass | The empty Color Option case is injected into presentation state; persistence is not modified. |
| User reaches controls and truncated names accessibly | Light/Dark headless test verifies descriptive automation names, stable IDs, focusable/tab-stop search and selectors, `SelectionMode.None`, full-name tooltip values, and the non-editable row template. | Pass | Native screen-reader and operating-system input checks are supplemental; the view uses standard Avalonia focusable controls. |
| User edits Colors through the existing action | The headless test verifies the Color card's Manage values button still binds to the existing command and the same Color Option. `CatalogSetupViewModelTests.ManageOptionCommandRequestsDialogAndCloseDiscardsDraft` verifies dialog request and existing lifecycle behavior. | Pass | Existing edit/archive persistence flows remain covered by their current tests. |
| User scans available choices as cards | `StoreEditorHeadlessTests.AvailableOptionChoiceCards_UseBorderedCardTreatmentAndStackOnNarrowWidth` and the Color-grid headless test verify the bordered card and its retained actions. | Pass | — |
| Empty Option uses the same card treatment | The Color-grid headless test verifies the no-values message, zero summary, and disabled paging within the bordered Color card. | Pass | — |
| Custom Option kind uses the same card treatment | Existing and focused headless tests verify Size/custom summaries remain compact and only Color receives the grid. | Pass | The default fixture has Size as the non-Color Option. |
| Cards align cleanly in the available width | `StoreEditorHeadlessTests.AvailableOptionChoiceCards_UseBorderedCardTreatmentAndStackOnNarrowWidth` sets enough width and verifies side-by-side alignment. | Pass | At the default window width the wider Color card can wrap the Size card. |
| Cards stack at narrower supported widths | The same responsive card test narrows the window to its minimum and verifies the Color and Size cards wrap onto separate rows. | Pass | — |
| Long content remains readable | Color row template binds ellipsis and full-name tooltip; the headless test verifies full tooltip content. Existing card layout test covers wrapping summaries and headings. | Pass | The row template uses a fixed width and height. |

## Validation

- Focused view-model and Light/Dark headless tests: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~AvailableColorValuesGridViewModelTests|FullyQualifiedName~Available_colors_grid_renders_accessible_controls_templates_stripes_and_empty_states|FullyQualifiedName~AvailableOptionChoiceCards_UseBorderedCardTreatmentAndStackOnNarrowWidth" --logger "console;verbosity=minimal"` — pass, 11 passed.
- Strict change validation: `openspec validate browse-available-color-values --strict` — pass.
- Spec sync and repository validation: synced `variant-management` requirements; `openspec validate --specs --strict --no-interactive` — pass, 64 specs validated.
- Required solution baseline: `dotnet test .\FusionCanvas.sln -m:1` — pass, 2,332 passed across five test projects (Domain 294, Application 671, Integration 352, App 986, UI-description 29).
- Diff whitespace check: `git diff --check` — pass; Git reports only its normal LF-to-CRLF notice for `CatalogSetupViewModel.cs`.
- Build warnings: existing NU1902/NU1903 advisories for ImageSharp 3.1.12 and existing nullable/xUnit analyzer warnings. No build or test failures were reported.
