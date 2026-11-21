# UI axis run — Store Editor tab-content inset

- Audit run ID: `57cda538-ec0d-4e58-9f89-2b0e0f5a3c0b`
- Parent audit ID: `eaa3c241-08c8-4f62-a651-2d3abdc3677e`
- Status: `PROVISIONAL`
- Axis: UI
- Scope: `UI-SURF-STORE-EDITOR-TAB-CONTENT` — Niches, Tags, Catalog & mockups; Basic info checked as a shared-container regression case.
- Started: `2026-11-21T17:02:16Z`
- Completed: `2026-11-21T17:08:19Z`
- Profile: `STANDARD_TARGETED`
- Standard and definition: FusionCanvas UI Standard v1.5 / Draft; UI Source Extract Definition v1.5 / Draft
- Source revision: `c5f8c79c0d1f86da526856e4f1e5a15e3a8f82de` with scoped working-tree remediation
- Extract: `source-extract.yaml`, revision 1
- Jev: `NOT_APPLICABLE`; all candidate checks retained for one explicitly requested Surface and three distinct reported tab layouts, with one related regression tab.

## Finding and cause

`F-UI-STORE-003` — Shared tab-content boundary has no inner spacing in three tabs.

- Rules: FC-UI-096 and FC-UI-100
- Initial evidence: at the source revision, `StoreEditor.TabContent` used zero padding while only Basic info's child had a 14 DIP margin. The supplied screenshot shows Niches content touching the light-blue outline. Static XAML confirms the same shared root serves the tab content.
- Expected: every tab's nearest visible content stays inset from the shared boundary using a design token; a border is not itself evidence of spacing.
- Severity: Medium
- Reassessment: pass. The shared root now uses `Token.Layout.CardPadding` (14 DIP) on all sides. The Basic info child margin was removed. The test verifies all four sides of the Border and actual first-content bounds for each tab.

## Assessments

| Rule | Result | Evidence |
|---|---|---|
| FC-UI-094 — tab grouping and selected content relationship | Pass | `StoreEditor.TabNavigation` and existing selected classes; Store Editor tests pass. |
| FC-UI-095 — separate contextual return navigation | N/A | The audited Store Management tab row has no contextual back/return action. |
| FC-UI-096 — bounded tab content region | Pass | `StoreEditor.TabContent` remains a bounded, bordered region below the navigation band. |
| FC-UI-100 — inset from border on populated edges | Pass | Shared 14 DIP token padding plus headless coordinate assertions across Basic info, Niches, Tags, and Catalog & mockups; static scan found no content Border without explicit padding. |

## Verification

- `dotnet build .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 -v quiet`: passed, 0 errors.
- `dotnet .\tests\FusionCanvas.App.Tests\bin\Debug\net10.0\FusionCanvas.App.Tests.dll -method '*StoreEditorTabs_KeepContentInsetFromTheSharedBoundary' -reporter verbose`: 1 passed.
- `dotnet .\tests\FusionCanvas.App.Tests\bin\Debug\net10.0\FusionCanvas.App.Tests.dll -class 'FusionCanvas.App.Tests.StoreEditorHeadlessTests' -reporter quiet`: passed.
- `dotnet test .\FusionCanvas.sln --no-build --no-restore -m:1 -v minimal`: 2,555 passed, 0 failed.
- `git diff --check`: passed.

## Standard and process changes

- FC-UI-100 now requires explicit token-backed inset at every populated border edge and separate assessment of each distinct tab layout.
- The UI source extract now records each tab and its distinct content roots/layouts for independent assessment.
- The comparison procedure now requires checking each tab's first content row and action area; a shared border cannot stand in for per-tab inset evidence.
- No challenges or unresolved checks remain. Audit issue creation was not requested.
