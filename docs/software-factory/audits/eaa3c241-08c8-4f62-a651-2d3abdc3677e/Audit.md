# Store Editor tab-content inset review

- Audit ID: `eaa3c241-08c8-4f62-a651-2d3abdc3677e`
- Status: `PROVISIONAL`
- Started: `2026-11-21T17:02:16Z`
- Completed: `2026-11-21T17:08:19Z`
- Request scope: UI-axis review of Store Management → Niches, Tags, and Catalog & mockups tabs, the shared tab-content boundary, and the reported border-to-content spacing issue.
- Included axis: UI
- Excluded axes: Architecture & Code Structure, Functionality & Logic, UX, Security & Privacy, Values, Vision & Taste; not requested.
- Profile: `STANDARD_TARGETED`
- Baseline: FusionCanvas UI Standard v1.5 / Draft; UI Source Extract Definition v1.5 / Draft
- Source revision: `c5f8c79c0d1f86da526856e4f1e5a15e3a8f82de` with scoped working-tree remediation
- Source extract: [`source-extract.yaml`](57cda538-ec0d-4e58-9f89-2b0e0f5a3c0b/source-extract.yaml)
- Axis run: [`57cda538-ec0d-4e58-9f89-2b0e0f5a3c0b`](57cda538-ec0d-4e58-9f89-2b0e0f5a3c0b/Audit.md)
- Jev routing: `NOT_APPLICABLE`; one explicitly targeted Surface and a small, fully retained set of tab-composition and inset checks; deterministic source and headless evidence were used.
- GitHub issue creation: `NOT REQUESTED`; the user asked to skip issue creation.

## Root cause

The prior Store Editor audit was scoped to the supplied Catalog & mockups offering-context screenshot. Its source extract explicitly excluded the other Store Editor tabs and states. It assessed the shared wrapper as a bounded tab-content region and marked that check passed after remediation, even though the wrapper had `Padding="0"`. Only Basic info had its own 14 DIP margin. Niches, Tags, and Catalog & mockups overview therefore had content flush against the shared light-blue border.

The gap had two parts: audit coverage followed one captured state rather than each distinct tab layout, and the boundary criterion allowed a border to count as the content treatment without separately checking inset. The code change fixed the shared layout; the Factory standard and audit procedure now separate those checks.

## Result

- Finding `F-UI-STORE-003` (medium): content in Niches, Tags, and Catalog & mockups touched the tab-content boundary. **Resolved and reassessed.**
- The shared `StoreEditor.TabContent` Border now uses the existing 14 DIP `Token.Layout.CardPadding`; the Basic info-only margin was removed so all tabs share the same inset.
- The Store Editor's default width increased from 860 to 900 DIP to preserve the existing Catalog & mockups working area after adding the inset. Its 720 DIP minimum and horizontal scrolling behavior remain covered by existing tests.
- A headless regression check switches through all four tabs and verifies the shared inset on all edges plus the actual first visible content origin for each tab.
- Static review of every Border declaration in `StoreEditorWindow.axaml` found no content-bearing Border without explicit padding.
- Factory learning: UI Standard advanced from v1.4 to v1.5 with FC-UI-100; the UI source-extract definition advanced from v1.4 to v1.5; checkpoint comparison instructions now require per-tab assessment.

## Evidence and completion

- Focused headless inset test: 1 passed.
- Store Editor headless test class: passed.
- Full solution baseline: 2,555 passed, 0 failed across Domain (294), Application (724), Integration (465), App (1,043), and UI Description (29).
- `git diff --check`: passed.
- Restore emitted NU1900 warnings because the NuGet vulnerability feed was unreachable; tests completed successfully.
- No audit challenges or open findings remain in the requested scope. Certification remains `PROVISIONAL` because the Factory UI standard and source-extract definition are Draft baselines.
