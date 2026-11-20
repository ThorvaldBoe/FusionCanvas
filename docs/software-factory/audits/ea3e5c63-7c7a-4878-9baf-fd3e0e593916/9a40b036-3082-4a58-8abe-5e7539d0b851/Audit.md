# UI Audit Run — 9a40b036-3082-4a58-8abe-5e7539d0b851

- Audit ID: `ea3e5c63-7c7a-4878-9baf-fd3e0e593916`
- Audit run ID: `9a40b036-3082-4a58-8abe-5e7539d0b851`
- Status: IN_PROGRESS
- Baseline readiness: PROVISIONAL
- Started at: 2026-11-20T17:02:00+01:00
- Ended at: UNKNOWN
- Auditor/reviewer: Codex
- Scope: All FusionCanvas.App UI surfaces; first target Store Editor / Basic info tab.
- Audit profile: LIGHT_BASELINE
- Standard ID/version/status: FC-UI / 1.4 / Draft normative baseline
- Source extract definition version/status: 1.4 / Draft
- Jev classifier/configuration: LIGHT_BASELINE, threshold 0.75; first invocation returned `FAILED/CLI_ERROR`; manual fallback is recorded by `jev_audit.py` with invocation `f9b42fd3-79f1-4c33-8166-2936730f427d`, relative log `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/f9b42fd3-79f1-4c33-8166-2936730f427d.jsonl`. No successful Jev routing is claimed.
- Repository revision and working-tree state: `783ad061aacbd0e438bf72846be4c56fce57eae5`; clean at audit start.
- Append-only extract path and identity: `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/source-extract.yaml`; `UI-EXTRACT-001`.
- Latest extract extension revision: 4
- Progress phase: AUDIT
- Progress updated at: 2026-11-20T17:24:00+01:00
- Source items: 36 / 36 routed by recorded manual fallback / 36 in scope for Light-baseline review / 0 filtered / 0 completed; all surfaces still await visual/runtime review.
- Checks: Global plan / remaining UNKNOWN; 65 selected static assessments completed.
- Current item and position: UI-SURF-STORE-EDITOR, 1/36; target tabbed checkpoint awaiting live render.
- Current item relevant checks: FC-UI-022, FC-UI-096
- Progress estimate and basis: Approximately 30%; 25% routing weight is complete and static triage covers 65 selected assessments; per-surface visual/runtime checks remain, so the estimate is provisional.

## Extract history

| Extension revision | Source revision | Working-tree state | Standard/definition versions | Generated at | Change summary | Affected extract-entry IDs |
|---|---|---|---|---|---|---|
| 1 | `783ad061aacbd0e438bf72846be4c56fce57eae5` | clean at audit start | FC-UI 1.4 / extract 1.4 | 2026-11-20T17:02:00+01:00 | Initial user-reported Store Editor inventory; repository-wide discovery pending | UI-SURF-STORE-EDITOR@1 |
| 2 | `783ad061aacbd0e438bf72846be4c56fce57eae5` | audit-artifacts-only-changed | FC-UI 1.4 / extract 1.4 | 2026-11-20T17:10:00+01:00 | Reconciled remaining XAML surfaces and static registrations; includes a provisional DocumentContextHeader classification | UI source entries appended in revision 2 |
| 3 | `783ad061aacbd0e438bf72846be4c56fce57eae5` | audit-artifacts-only-changed | FC-UI 1.4 / extract 1.4 | 2026-11-20T17:15:00+01:00 | Removed reusable DocumentContextHeader component from the Surface set and retained it as a MainWindow dependency; corrected current count to 36 | UI-SURF-DOCUMENTCONTEXTHEADER@1 tombstone |

## Item/criterion assessments

| Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Standard version | Criterion ID/heading | Result | Evidence references | Implementation references/dependencies | Finding IDs | Challenge IDs | Supersedes assessment ID |
|---|---|---|---|---|---|---|---|---|---|---|
| A-STORE-022-1 | UI-SURF-STORE-EDITOR | UI-SURF-STORE-EDITOR@1 | `783ad06` + supplied screenshot | 1.4 | FC-UI-022 Whitespace separates hierarchy and relationships | FAIL | User screenshot: Basic info title and form fields are flush to light blue tab-content border; static XAML sets Padding=0 | StoreEditorWindow.axaml:131-141; active content boundary Border | UI-001 | NONE | NONE |
| A-STORE-096-1 | UI-SURF-STORE-EDITOR | UI-SURF-STORE-EDITOR@1 | `783ad06` + supplied screenshot | 1.4 | FC-UI-096 Bounded active tab content region | FAIL | User screenshot shows tab content is bordered but content begins directly at boundary; no visible inner inset | StoreEditorWindow.axaml:131-141; tab content Border and StackPanel | UI-001 | NONE | NONE |

## Findings

### UI-001 — Store Editor Basic info content touches the tab boundary

- Assessment IDs: A-STORE-022-1, A-STORE-096-1
- Source item: UI-SURF-STORE-EDITOR; extract entry `UI-SURF-STORE-EDITOR@1`; source revision `783ad06` plus supplied screenshot.
- Rules: FC-UI-022, FC-UI-096.
- Type/severity: UI presentation / Medium.
- Surface/pattern: Store Editor / active-tab content boundary and form.
- Evidence method: V (user-provided screenshot) and S (XAML inspection).
- Current: The light blue bordered tab-content container has zero padding. The Basic info heading and form fields sit against its edge.
- Expected: A visible, consistent content inset separates the active-tab content from its boundary while preserving the current tab and form workflow.
- Impact: The border and controls visually collide, making the form feel cramped and weakening the relationship between the active tab and its bounded content region.
- Remediation: Apply the approved shared section/content inset to the active-tab container; if the repository has no applicable token, establish/use the shared semantic spacing resource rather than a view-specific competing value.
- Likely files: `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`, shared design resources if a spacing token is required.
- Verification: Render Store Editor at its default and minimum supported sizes, inspect all tabs and long/scrolling content, and confirm an inset is present on each side of the bounded active-tab region.
- Scope: Local surface unless other bordered tab-content surfaces show the same cause; cross-surface review pending.
- Cross-axis routing: NONE; presentation issue only.
- GitHub issue: [#966](https://github.com/ThorvaldBoe/FusionCanvas/issues/966), open with `audit-finding`.
- Challenges: NONE; the active-tab checkpoint explicitly includes FC-UI-022 and FC-UI-096.

## Challenges

None recorded.

## Coverage, limitations, and certification status

Source inventory currently contains 36 independently reviewable XAML surfaces: 33 windows and three user controls; test-only surfaces and the reusable DocumentContextHeader component are excluded. All-page rendered review remains pending because the desktop is locked.

### Automated test evidence

- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj -m:1 --no-restore`: PASS, 1,041 tests, 0 failed, 0 skipped. This includes Avalonia.Headless view coverage but does not provide screenshots for every Surface.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore`: Domain (294), Application (723), and Integration (461) suites passed before the Microsoft Testing Platform process terminated while creating `tests/FusionCanvas.App.Tests/bin/Debug/net10.0/TestResults/FusionCanvas.App.Tests_net10.0_x64.log` (file was in use). The solution run therefore did not reach the remaining projects; this is not recorded as a product assertion failure.
- Restore emitted NU1900 warnings because the vulnerability-feed service index was unreachable from this environment.

The supplied Store Editor screenshot confirms UI-001; it does not substitute for inspecting the running desktop. The Draft standard/definition statuses make final certification unavailable.

## Relevance-routing register

The initial Store Editor invocation `92727a98-a01c-496e-a8ca-e5b8c39a4107` ended `FAILED/CLI_ERROR`; its empty response is not a Jev result (log: `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/92727a98-a01c-496e-a8ca-e5b8c39a4107.jsonl`). Manual fallback was recorded with the wrapper for every current surface. Decisions are auditor-authored and carry no Jev confidence. No candidate is certified by a filtered route. The Light-baseline review retains deterministic signals and any uncertain checks.

| Source item | Routing | Wrapper invocation | Relative JSONL log |
|---|---|---|---|
| `UI-SURF-SETTINGSWINDOW` | MANUAL_FALLBACK | `0a28ed28-699d-4f54-a54b-feb1bef244c6` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/0a28ed28-699d-4f54-a54b-feb1bef244c6.jsonl` |
| `UI-SURF-BULKADDVARIANTSWINDOW` | MANUAL_FALLBACK | `2124951a-4808-4b95-8366-1edbd629a69b` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/2124951a-4808-4b95-8366-1edbd629a69b.jsonl` |
| `UI-SURF-MOCKUPTEMPLATEEDITORWINDOW` | MANUAL_FALLBACK | `21b22dd9-17fb-4e2c-b8ef-9925508e623f` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/21b22dd9-17fb-4e2c-b8ef-9925508e623f.jsonl` |
| `UI-SURF-SPLASHWINDOW` | MANUAL_FALLBACK | `2217b05a-ebbd-44df-823c-3d89657f1394` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/2217b05a-ebbd-44df-823c-3d89657f1394.jsonl` |
| `UI-SURF-ADDVARIANTWINDOW` | MANUAL_FALLBACK | `26ac7e77-92fd-4b19-a304-d0542258ad46` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/26ac7e77-92fd-4b19-a304-d0542258ad46.jsonl` |
| `UI-SURF-GROUPSELECTIONWINDOW` | MANUAL_FALLBACK | `2ddfd404-5a0a-4cc4-a858-e322447f2b97` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/2ddfd404-5a0a-4cc4-a858-e322447f2b97.jsonl` |
| `UI-SURF-AIPROFILEEDITORVIEW` | MANUAL_FALLBACK | `2febd789-0d57-4a0f-a86d-cb6961316119` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/2febd789-0d57-4a0f-a86d-cb6961316119.jsonl` |
| `UI-SURF-SNOWCLONELIBRARYWINDOW` | MANUAL_FALLBACK | `31be3d73-ecaf-48a7-af35-96769175c34e` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/31be3d73-ecaf-48a7-af35-96769175c34e.jsonl` |
| `UI-SURF-ASSETPREVIEWWINDOW` | MANUAL_FALLBACK | `39321a2e-805a-4533-9dc1-fa2896b38389` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/39321a2e-805a-4533-9dc1-fa2896b38389.jsonl` |
| `UI-SURF-DESIGNAREAEDITORWINDOW` | MANUAL_FALLBACK | `3c52d8bd-6568-47a1-88cf-635168ad11b8` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/3c52d8bd-6568-47a1-88cf-635168ad11b8.jsonl` |
| `UI-SURF-ENLARGEDMOCKUPPLACEMENTEDITORWINDOW` | MANUAL_FALLBACK | `3cce1b14-e12e-4f70-a441-786eda0ebf2b` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/3cce1b14-e12e-4f70-a441-786eda0ebf2b.jsonl` |
| `UI-SURF-REJECTEDPHRASESWINDOW` | MANUAL_FALLBACK | `3ebcf377-123a-4dab-a7d6-7858441a7228` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/3ebcf377-123a-4dab-a7d6-7858441a7228.jsonl` |
| `UI-SURF-DESIGNPREVIEWWINDOW` | MANUAL_FALLBACK | `40129881-c506-4c08-8029-2fbb86f5d3b8` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/40129881-c506-4c08-8029-2fbb86f5d3b8.jsonl` |
| `UI-SURF-AISETTINGSVIEW` | MANUAL_FALLBACK | `4383f54e-dfb0-4d94-8fd5-754202806152` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/4383f54e-dfb0-4d94-8fd5-754202806152.jsonl` |
| `UI-SURF-GROUPACTIONCONFIRMATIONWINDOW` | MANUAL_FALLBACK | `4d7f7c64-475d-4040-abac-192a8401f837` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/4d7f7c64-475d-4040-abac-192a8401f837.jsonl` |
| `UI-SURF-OPTIONVALUEMANAGEMENTWINDOW` | MANUAL_FALLBACK | `5b7e5270-8ffa-4b1e-80ea-605abef8285a` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/5b7e5270-8ffa-4b1e-80ea-605abef8285a.jsonl` |
| `UI-SURF-ITEMIMPORTWINDOW` | MANUAL_FALLBACK | `60da9caf-8789-4c33-bfba-a40fd5d36c43` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/60da9caf-8789-4c33-bfba-a40fd5d36c43.jsonl` |
| `UI-SURF-WORKSPACEMANAGEMENTWINDOW` | MANUAL_FALLBACK | `753d5ffa-f3a8-4d76-b768-db3647b0c059` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/753d5ffa-f3a8-4d76-b768-db3647b0c059.jsonl` |
| `UI-SURF-REJECTIDEAWINDOW` | MANUAL_FALLBACK | `8486598f-9089-468d-ba56-de1eb801d19a` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/8486598f-9089-468d-ba56-de1eb801d19a.jsonl` |
| `UI-SURF-WORKSPACETELEMETRYSETTINGSVIEW` | MANUAL_FALLBACK | `9064c7c5-bad8-47ec-b90c-82cff6115139` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/9064c7c5-bad8-47ec-b90c-82cff6115139.jsonl` |
| `UI-SURF-ARTWORKADJUSTMENTDISCARDWINDOW` | MANUAL_FALLBACK | `99587db7-1053-4507-9c67-c6ca7d26780c` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/99587db7-1053-4507-9c67-c6ca7d26780c.jsonl` |
| `UI-SURF-TELEMETRYDEBUGWINDOW` | MANUAL_FALLBACK | `abd47b9a-0d8d-4e0e-8fb0-7a10cee8d0e8` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/abd47b9a-0d8d-4e0e-8fb0-7a10cee8d0e8.jsonl` |
| `UI-SURF-GROUPDELETECONFIRMATIONWINDOW` | MANUAL_FALLBACK | `baab45f7-1881-454b-867a-5a402b74143a` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/baab45f7-1881-454b-867a-5a402b74143a.jsonl` |
| `UI-SURF-IDEATIONWINDOW` | MANUAL_FALLBACK | `bb97ccfc-c589-487e-8f92-68a80c0665d0` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/bb97ccfc-c589-487e-8f92-68a80c0665d0.jsonl` |
| `UI-SURF-MAINWINDOW` | MANUAL_FALLBACK | `beb5a95f-8466-4651-ba29-61e28329e6e4` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/beb5a95f-8466-4651-ba29-61e28329e6e4.jsonl` |
| `UI-SURF-ASSETSWINDOW` | MANUAL_FALLBACK | `bede4767-b17e-4c5c-8b10-e64d9c70d05d` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/bede4767-b17e-4c5c-8b10-e64d9c70d05d.jsonl` |
| `UI-SURF-DESIGNSYSTEMGALLERYWINDOW` | MANUAL_FALLBACK | `c1a02a26-ee8b-4e8f-977f-1afd026144d9` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/c1a02a26-ee8b-4e8f-977f-1afd026144d9.jsonl` |
| `UI-SURF-PRINTIFYLISTINGIMPORTWINDOW` | MANUAL_FALLBACK | `c95cdfca-eed8-4c29-8a1d-d7fd65bf6191` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/c95cdfca-eed8-4c29-8a1d-d7fd65bf6191.jsonl` |
| `UI-SURF-TERMSCONSENTWINDOW` | MANUAL_FALLBACK | `dcf49e68-db2f-4de1-ad62-d0c4371da1e3` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/dcf49e68-db2f-4de1-ad62-d0c4371da1e3.jsonl` |
| `UI-SURF-IDEATIONDISCARDCONFIRMATIONWINDOW` | MANUAL_FALLBACK | `e1edc2ce-4ece-414c-85ad-eaa2480e2789` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/e1edc2ce-4ece-414c-85ad-eaa2480e2789.jsonl` |
| `UI-SURF-MOCKUPPREVIEWWINDOW` | MANUAL_FALLBACK | `e234ce6c-099d-4d93-8892-32497bc50179` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/e234ce6c-099d-4d93-8892-32497bc50179.jsonl` |
| `UI-SURF-MOCKUPTEMPLATEARCHIVECONFIRMATIONWINDOW` | MANUAL_FALLBACK | `e7a2f6a8-0356-4790-b31e-5605d284630d` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/e7a2f6a8-0356-4790-b31e-5605d284630d.jsonl` |
| `UI-SURF-DESIGNAREAARCHIVECONFIRMATIONWINDOW` | MANUAL_FALLBACK | `ed579459-b0e8-452a-9d6d-edd698f6f431` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/ed579459-b0e8-452a-9d6d-edd698f6f431.jsonl` |
| `UI-SURF-GLOBALCOLORREMOVALWINDOW` | MANUAL_FALLBACK | `f351abd9-a9e9-4a6b-8d27-75269f1f909e` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/f351abd9-a9e9-4a6b-8d27-75269f1f909e.jsonl` |
| `UI-SURF-PRINTIFYAPIKEYWINDOW` | MANUAL_FALLBACK | `f8857ee6-aad5-4da1-a762-f48bb0f132b7` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/f8857ee6-aad5-4da1-a762-f48bb0f132b7.jsonl` |
| `UI-SURF-STORE-EDITOR` | FAILED then MANUAL_FALLBACK | `92727a98-a01c-496e-a8ca-e5b8c39a4107 (failed); f9b42fd3-79f1-4c33-8166-2936730f427d` | `docs/software-factory/audits/ea3e5c63-7c7a-4878-9baf-fd3e0e593916/9a40b036-3082-4a58-8abe-5e7539d0b851/jev/f9b42fd3-79f1-4c33-8166-2936730f427d.jsonl` |

## Additional static candidate findings (runtime review pending)

### UI-002 — Semantic text uses sizes below the 11-DIP floor

- Criteria: FC-UI-097. The two workspace-tree badges (`HiddenTagLabel`, `CountLabel`) and the Store Editor “Primary” status label use `FontSize="10"` (3 text elements across MainWindow and Store Editor). These communicate metadata/status, so the decorative-text exception does not apply.
- Current/expected: meaningful status and metadata text renders at 10 DIPs; it must render at least 11 DIPs and use the semantic typography token.
- Evidence: static XAML scan; `MainWindow.axaml:440,452`, `StoreEditorWindow.axaml:1448-1451`.
- Severity/scope: Medium; local instances share the same typography-floor correction.
- Issue: PENDING until the all-pages surface review is closed.

### UI-003 — The tag-removal target is undersized and unnamed

- Criteria: FC-UI-031 and FC-UI-098. The “×” tag-removal button is explicitly `Width="18" Height="18"`; it has a tooltip but no accessible name explaining its action.
- Expected: a supported target of at least 28 DIPs with an accessible name such as “Remove tag” and a discoverable tooltip.
- Evidence: static XAML at `MainWindow.axaml:1465-1473`.
- Severity/scope: Medium; tag chip action in MainWindow.
- Issue: PENDING until the all-pages surface review is closed.

### UI-004 — Surface spacing bypasses the shared spacing tokens

- Criterion: FC-UI-015. Static scan found 745 positive numeric `Margin`, `Padding`, `Spacing`, `RowSpacing`, or `ColumnSpacing` values across 33 current surfaces. The shared spacing registry exists in `DesignTokens.axaml`, but these layout values are local literals instead of semantic shared resources. Intentional all-zero spacing and resource-bound values are excluded from the count.
- Expected: use an applicable approved spacing resource; establish a semantic token centrally before introducing recurring values the registry does not cover.
- Representative evidence: `MainWindow.axaml`, `StoreEditorWindow.axaml`, and `MockupTemplateEditorWindow.axaml`; complete affected-surface counts are in assessment rows below.
- Severity/scope: Medium; shared design-system adoption gap across 33 surfaces.
- Issue: PENDING until the all-pages surface review is closed.

### UI-005 — Semantic text styles bypass the typography tokens

- Criterion: FC-UI-024. Static scan found 334 literal `FontSize` values on TextBlocks across 26 current surfaces, while the shared semantic typography family is defined in `DesignTokens.axaml`. Three of those elements also fail FC-UI-097 as recorded in UI-002.
- Expected: assign the approved semantic typography resource for each text role; propose missing semantic styles centrally rather than creating view-specific font sizes.
- Severity/scope: Medium; shared design-system adoption gap across 26 surfaces.
- Issue: PENDING until the all-pages surface review is closed.

| Assessment ID | Source item | Exact entry | Source revision | Criterion | Result | Evidence / implementation reference | Finding |
|---|---|---|---|---|---|---|---|
| `A-SPACING-ASSETPREVIEWWINDOW-1` | `UI-SURF-ASSETPREVIEWWINDOW` | `UI-SURF-ASSETPREVIEWWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 3 positive literal spacing properties in `src/FusionCanvas.App/Assets/AssetPreviewWindow.axaml`; shared token resources exist. | UI-004 |
| `A-SPACING-ASSETSWINDOW-1` | `UI-SURF-ASSETSWINDOW` | `UI-SURF-ASSETSWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 21 positive literal spacing properties in `src/FusionCanvas.App/Assets/AssetsWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-ASSETSWINDOW-1` | `UI-SURF-ASSETSWINDOW` | `UI-SURF-ASSETSWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 14 literal TextBlock FontSize values in `src/FusionCanvas.App/Assets/AssetsWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-GROUPACTIONCONFIRMATIONWINDOW-1` | `UI-SURF-GROUPACTIONCONFIRMATIONWINDOW` | `UI-SURF-GROUPACTIONCONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 4 positive literal spacing properties in `src/FusionCanvas.App/Groups/GroupActionConfirmationWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-GROUPACTIONCONFIRMATIONWINDOW-1` | `UI-SURF-GROUPACTIONCONFIRMATIONWINDOW` | `UI-SURF-GROUPACTIONCONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 2 literal TextBlock FontSize values in `src/FusionCanvas.App/Groups/GroupActionConfirmationWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-GROUPDELETECONFIRMATIONWINDOW-1` | `UI-SURF-GROUPDELETECONFIRMATIONWINDOW` | `UI-SURF-GROUPDELETECONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 5 positive literal spacing properties in `src/FusionCanvas.App/Groups/GroupDeleteConfirmationWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-GROUPDELETECONFIRMATIONWINDOW-1` | `UI-SURF-GROUPDELETECONFIRMATIONWINDOW` | `UI-SURF-GROUPDELETECONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 2 literal TextBlock FontSize values in `src/FusionCanvas.App/Groups/GroupDeleteConfirmationWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-GROUPSELECTIONWINDOW-1` | `UI-SURF-GROUPSELECTIONWINDOW` | `UI-SURF-GROUPSELECTIONWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 5 positive literal spacing properties in `src/FusionCanvas.App/Groups/GroupSelectionWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-GROUPSELECTIONWINDOW-1` | `UI-SURF-GROUPSELECTIONWINDOW` | `UI-SURF-GROUPSELECTIONWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 1 literal TextBlock FontSize values in `src/FusionCanvas.App/Groups/GroupSelectionWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-IDEATIONDISCARDCONFIRMATIONWINDOW-1` | `UI-SURF-IDEATIONDISCARDCONFIRMATIONWINDOW` | `UI-SURF-IDEATIONDISCARDCONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 3 positive literal spacing properties in `src/FusionCanvas.App/Ideation/IdeationDiscardConfirmationWindow.axaml`; shared token resources exist. | UI-004 |
| `A-SPACING-IDEATIONWINDOW-1` | `UI-SURF-IDEATIONWINDOW` | `UI-SURF-IDEATIONWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 16 positive literal spacing properties in `src/FusionCanvas.App/Ideation/IdeationWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-IDEATIONWINDOW-1` | `UI-SURF-IDEATIONWINDOW` | `UI-SURF-IDEATIONWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 1 literal TextBlock FontSize values in `src/FusionCanvas.App/Ideation/IdeationWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-REJECTIDEAWINDOW-1` | `UI-SURF-REJECTIDEAWINDOW` | `UI-SURF-REJECTIDEAWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 4 positive literal spacing properties in `src/FusionCanvas.App/Ideation/RejectIdeaWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-REJECTIDEAWINDOW-1` | `UI-SURF-REJECTIDEAWINDOW` | `UI-SURF-REJECTIDEAWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 1 literal TextBlock FontSize values in `src/FusionCanvas.App/Ideation/RejectIdeaWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-ITEMIMPORTWINDOW-1` | `UI-SURF-ITEMIMPORTWINDOW` | `UI-SURF-ITEMIMPORTWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 16 positive literal spacing properties in `src/FusionCanvas.App/Items/Import/ItemImportWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-ITEMIMPORTWINDOW-1` | `UI-SURF-ITEMIMPORTWINDOW` | `UI-SURF-ITEMIMPORTWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 4 literal TextBlock FontSize values in `src/FusionCanvas.App/Items/Import/ItemImportWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-REJECTEDPHRASESWINDOW-1` | `UI-SURF-REJECTEDPHRASESWINDOW` | `UI-SURF-REJECTEDPHRASESWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 26 positive literal spacing properties in `src/FusionCanvas.App/RejectedPhrases/RejectedPhrasesWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-REJECTEDPHRASESWINDOW-1` | `UI-SURF-REJECTEDPHRASESWINDOW` | `UI-SURF-REJECTEDPHRASESWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 8 literal TextBlock FontSize values in `src/FusionCanvas.App/RejectedPhrases/RejectedPhrasesWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-AIPROFILEEDITORVIEW-1` | `UI-SURF-AIPROFILEEDITORVIEW` | `UI-SURF-AIPROFILEEDITORVIEW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 21 positive literal spacing properties in `src/FusionCanvas.App/Settings/AiProfileEditorView.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-AIPROFILEEDITORVIEW-1` | `UI-SURF-AIPROFILEEDITORVIEW` | `UI-SURF-AIPROFILEEDITORVIEW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 26 literal TextBlock FontSize values in `src/FusionCanvas.App/Settings/AiProfileEditorView.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-AISETTINGSVIEW-1` | `UI-SURF-AISETTINGSVIEW` | `UI-SURF-AISETTINGSVIEW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 28 positive literal spacing properties in `src/FusionCanvas.App/Settings/AiSettingsView.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-AISETTINGSVIEW-1` | `UI-SURF-AISETTINGSVIEW` | `UI-SURF-AISETTINGSVIEW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 14 literal TextBlock FontSize values in `src/FusionCanvas.App/Settings/AiSettingsView.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-SETTINGSWINDOW-1` | `UI-SURF-SETTINGSWINDOW` | `UI-SURF-SETTINGSWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 28 positive literal spacing properties in `src/FusionCanvas.App/Settings/SettingsWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-SETTINGSWINDOW-1` | `UI-SURF-SETTINGSWINDOW` | `UI-SURF-SETTINGSWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 17 literal TextBlock FontSize values in `src/FusionCanvas.App/Settings/SettingsWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-TELEMETRYDEBUGWINDOW-1` | `UI-SURF-TELEMETRYDEBUGWINDOW` | `UI-SURF-TELEMETRYDEBUGWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 3 positive literal spacing properties in `src/FusionCanvas.App/Settings/TelemetryDebugWindow.axaml`; shared token resources exist. | UI-004 |
| `A-SPACING-WORKSPACETELEMETRYSETTINGSVIEW-1` | `UI-SURF-WORKSPACETELEMETRYSETTINGSVIEW` | `UI-SURF-WORKSPACETELEMETRYSETTINGSVIEW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 12 positive literal spacing properties in `src/FusionCanvas.App/Settings/WorkspaceTelemetrySettingsView.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-WORKSPACETELEMETRYSETTINGSVIEW-1` | `UI-SURF-WORKSPACETELEMETRYSETTINGSVIEW` | `UI-SURF-WORKSPACETELEMETRYSETTINGSVIEW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 4 literal TextBlock FontSize values in `src/FusionCanvas.App/Settings/WorkspaceTelemetrySettingsView.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-SNOWCLONELIBRARYWINDOW-1` | `UI-SURF-SNOWCLONELIBRARYWINDOW` | `UI-SURF-SNOWCLONELIBRARYWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 24 positive literal spacing properties in `src/FusionCanvas.App/Snowclones/SnowcloneLibraryWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-SNOWCLONELIBRARYWINDOW-1` | `UI-SURF-SNOWCLONELIBRARYWINDOW` | `UI-SURF-SNOWCLONELIBRARYWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 7 literal TextBlock FontSize values in `src/FusionCanvas.App/Snowclones/SnowcloneLibraryWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-SPLASHWINDOW-1` | `UI-SURF-SPLASHWINDOW` | `UI-SURF-SPLASHWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 3 positive literal spacing properties in `src/FusionCanvas.App/SplashWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-SPLASHWINDOW-1` | `UI-SURF-SPLASHWINDOW` | `UI-SURF-SPLASHWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 2 literal TextBlock FontSize values in `src/FusionCanvas.App/SplashWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-ARTWORKADJUSTMENTDISCARDWINDOW-1` | `UI-SURF-ARTWORKADJUSTMENTDISCARDWINDOW` | `UI-SURF-ARTWORKADJUSTMENTDISCARDWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 3 positive literal spacing properties in `src/FusionCanvas.App/StageTools/ArtworkAdjustmentDiscardWindow.axaml`; shared token resources exist. | UI-004 |
| `A-SPACING-DESIGNPREVIEWWINDOW-1` | `UI-SURF-DESIGNPREVIEWWINDOW` | `UI-SURF-DESIGNPREVIEWWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 5 positive literal spacing properties in `src/FusionCanvas.App/StageTools/DesignPreviewWindow.axaml`; shared token resources exist. | UI-004 |
| `A-SPACING-MOCKUPPREVIEWWINDOW-1` | `UI-SURF-MOCKUPPREVIEWWINDOW` | `UI-SURF-MOCKUPPREVIEWWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 4 positive literal spacing properties in `src/FusionCanvas.App/StageTools/MockupPreviewWindow.axaml`; shared token resources exist. | UI-004 |
| `A-SPACING-ADDVARIANTWINDOW-1` | `UI-SURF-ADDVARIANTWINDOW` | `UI-SURF-ADDVARIANTWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 3 positive literal spacing properties in `src/FusionCanvas.App/Stores/AddVariantWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-ADDVARIANTWINDOW-1` | `UI-SURF-ADDVARIANTWINDOW` | `UI-SURF-ADDVARIANTWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 2 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/AddVariantWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-BULKADDVARIANTSWINDOW-1` | `UI-SURF-BULKADDVARIANTSWINDOW` | `UI-SURF-BULKADDVARIANTSWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 5 positive literal spacing properties in `src/FusionCanvas.App/Stores/BulkAddVariantsWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-BULKADDVARIANTSWINDOW-1` | `UI-SURF-BULKADDVARIANTSWINDOW` | `UI-SURF-BULKADDVARIANTSWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 2 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/BulkAddVariantsWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-DESIGNAREAARCHIVECONFIRMATIONWINDOW-1` | `UI-SURF-DESIGNAREAARCHIVECONFIRMATIONWINDOW` | `UI-SURF-DESIGNAREAARCHIVECONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 4 positive literal spacing properties in `src/FusionCanvas.App/Stores/DesignAreaArchiveConfirmationWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-DESIGNAREAARCHIVECONFIRMATIONWINDOW-1` | `UI-SURF-DESIGNAREAARCHIVECONFIRMATIONWINDOW` | `UI-SURF-DESIGNAREAARCHIVECONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 2 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/DesignAreaArchiveConfirmationWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-DESIGNAREAEDITORWINDOW-1` | `UI-SURF-DESIGNAREAEDITORWINDOW` | `UI-SURF-DESIGNAREAEDITORWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 19 positive literal spacing properties in `src/FusionCanvas.App/Stores/DesignAreaEditorWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-DESIGNAREAEDITORWINDOW-1` | `UI-SURF-DESIGNAREAEDITORWINDOW` | `UI-SURF-DESIGNAREAEDITORWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 10 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/DesignAreaEditorWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-ENLARGEDMOCKUPPLACEMENTEDITORWINDOW-1` | `UI-SURF-ENLARGEDMOCKUPPLACEMENTEDITORWINDOW` | `UI-SURF-ENLARGEDMOCKUPPLACEMENTEDITORWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 4 positive literal spacing properties in `src/FusionCanvas.App/Stores/EnlargedMockupPlacementEditorWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-ENLARGEDMOCKUPPLACEMENTEDITORWINDOW-1` | `UI-SURF-ENLARGEDMOCKUPPLACEMENTEDITORWINDOW` | `UI-SURF-ENLARGEDMOCKUPPLACEMENTEDITORWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 1 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/EnlargedMockupPlacementEditorWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-MOCKUPTEMPLATEARCHIVECONFIRMATIONWINDOW-1` | `UI-SURF-MOCKUPTEMPLATEARCHIVECONFIRMATIONWINDOW` | `UI-SURF-MOCKUPTEMPLATEARCHIVECONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 4 positive literal spacing properties in `src/FusionCanvas.App/Stores/MockupTemplateArchiveConfirmationWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-MOCKUPTEMPLATEARCHIVECONFIRMATIONWINDOW-1` | `UI-SURF-MOCKUPTEMPLATEARCHIVECONFIRMATIONWINDOW` | `UI-SURF-MOCKUPTEMPLATEARCHIVECONFIRMATIONWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 2 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/MockupTemplateArchiveConfirmationWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-MOCKUPTEMPLATEEDITORWINDOW-1` | `UI-SURF-MOCKUPTEMPLATEEDITORWINDOW` | `UI-SURF-MOCKUPTEMPLATEEDITORWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 41 positive literal spacing properties in `src/FusionCanvas.App/Stores/MockupTemplateEditorWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-MOCKUPTEMPLATEEDITORWINDOW-1` | `UI-SURF-MOCKUPTEMPLATEEDITORWINDOW` | `UI-SURF-MOCKUPTEMPLATEEDITORWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 20 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/MockupTemplateEditorWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-OPTIONVALUEMANAGEMENTWINDOW-1` | `UI-SURF-OPTIONVALUEMANAGEMENTWINDOW` | `UI-SURF-OPTIONVALUEMANAGEMENTWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 12 positive literal spacing properties in `src/FusionCanvas.App/Stores/OptionValueManagementWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-OPTIONVALUEMANAGEMENTWINDOW-1` | `UI-SURF-OPTIONVALUEMANAGEMENTWINDOW` | `UI-SURF-OPTIONVALUEMANAGEMENTWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 1 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/OptionValueManagementWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-PRINTIFYAPIKEYWINDOW-1` | `UI-SURF-PRINTIFYAPIKEYWINDOW` | `UI-SURF-PRINTIFYAPIKEYWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 5 positive literal spacing properties in `src/FusionCanvas.App/Stores/PrintifyApiKeyWindow.axaml`; shared token resources exist. | UI-004 |
| `A-SPACING-PRINTIFYLISTINGIMPORTWINDOW-1` | `UI-SURF-PRINTIFYLISTINGIMPORTWINDOW` | `UI-SURF-PRINTIFYLISTINGIMPORTWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 9 positive literal spacing properties in `src/FusionCanvas.App/Stores/PrintifyListingImportWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-PRINTIFYLISTINGIMPORTWINDOW-1` | `UI-SURF-PRINTIFYLISTINGIMPORTWINDOW` | `UI-SURF-PRINTIFYLISTINGIMPORTWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 4 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/PrintifyListingImportWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-STORE-EDITOR-1` | `UI-SURF-STORE-EDITOR` | `UI-SURF-STORE-EDITOR@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 181 positive literal spacing properties in `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-STORE-EDITOR-1` | `UI-SURF-STORE-EDITOR` | `UI-SURF-STORE-EDITOR@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 67 literal TextBlock FontSize values in `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-MAINWINDOW-1` | `UI-SURF-MAINWINDOW` | `UI-SURF-MAINWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 207 positive literal spacing properties in `src/FusionCanvas.App/Views/MainWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-MAINWINDOW-1` | `UI-SURF-MAINWINDOW` | `UI-SURF-MAINWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 111 literal TextBlock FontSize values in `src/FusionCanvas.App/Views/MainWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-SPACING-WORKSPACEMANAGEMENTWINDOW-1` | `UI-SURF-WORKSPACEMANAGEMENTWINDOW` | `UI-SURF-WORKSPACEMANAGEMENTWINDOW@1` | `783ad06` + audit source | FC-UI-015 Layout uses spacing tokens | FAIL | 17 positive literal spacing properties in `src/FusionCanvas.App/Workspace/WorkspaceManagementWindow.axaml`; shared token resources exist. | UI-004 |
| `A-TYPE-WORKSPACEMANAGEMENTWINDOW-1` | `UI-SURF-WORKSPACEMANAGEMENTWINDOW` | `UI-SURF-WORKSPACEMANAGEMENTWINDOW@1` | `783ad06` + audit source | FC-UI-024 Semantic typography tokens | FAIL | 9 literal TextBlock FontSize values in `src/FusionCanvas.App/Workspace/WorkspaceManagementWindow.axaml`; semantic typography resources exist. | UI-005 |
| `A-MAIN-097-1` | `UI-SURF-MAINWINDOW` | `UI-SURF-MAINWINDOW@1` | `783ad06` + audit source | FC-UI-097 Minimum semantic text size | FAIL | MainWindow.axaml:440,452; HiddenTagLabel and CountLabel at FontSize=10. | UI-002 |
| `A-STORE-097-1` | `UI-SURF-STORE-EDITOR` | `UI-SURF-STORE-EDITOR@1` | `783ad06` + audit source | FC-UI-097 Minimum semantic text size | FAIL | StoreEditorWindow.axaml:1448-1451; Primary status label at FontSize=10. | UI-002 |
| `A-MAIN-031-1` | `UI-SURF-MAINWINDOW` | `UI-SURF-MAINWINDOW@1` | `783ad06` + audit source | FC-UI-031 Accessible name and tooltip | FAIL | `MainWindow.axaml:1465-1473`; tooltip exists but the glyph button has no action-specific accessible name. | UI-003 |
| `A-MAIN-098-1` | `UI-SURF-MAINWINDOW` | `UI-SURF-MAINWINDOW@1` | `783ad06` + audit source | FC-UI-098 Minimum interactive target | FAIL | `MainWindow.axaml:1465-1473`; remove-tag button is 18×18 DIPs, below the 28-DIP compact minimum. | UI-003 |

## Issue linkage update — 2026-11-20

Static-review findings were submitted as GitHub audit issues while live visual review remains pending:

- UI-001 — Store Editor Basic info content touches tab boundary: [#966](https://github.com/ThorvaldBoe/FusionCanvas/issues/966).
- UI-002/UI-005 — semantic typography token adoption and minimum readable size: [#969](https://github.com/ThorvaldBoe/FusionCanvas/issues/969).
- UI-003 — tag-removal target accessible name and size: [#970](https://github.com/ThorvaldBoe/FusionCanvas/issues/970).
- UI-004 — shared spacing token adoption: [#968](https://github.com/ThorvaldBoe/FusionCanvas/issues/968).

These issues capture confirmed static findings; they do not close the audit. All 36 surfaces still require live rendered review and remaining criterion-level assessment. No fixes have started, preserving the requested review-before-remediation sequence.

## Review completion update — 2026-11-20

The user's desktop is available again. The live UI pass covered the Main workspace and item Idea/Concept states, group details, filters, all four Store Editor tabs, five Settings sections, Design System Gallery, Assets list and preview, Ideation, and Rejected Phrases including its unsaved new-entry form. UI-001 was confirmed in the running Store Editor Basic info tab as well as the supplied screenshot and source. No additional criterion-backed visual defect was found in the reachable states.

Source-level selected checks were completed across all 36 inventoried XAML surfaces. Rendered evidence is limited to the reachable states above; state-gated Design/Listing states, provider-dependent/empty workflows, and dialogs not safely opened without changing user data remain UNKNOWN. The existing sample workspace was not modified. Findings and issue links: UI-001 [#966](https://github.com/ThorvaldBoe/FusionCanvas/issues/966), UI-002/UI-005 [#969](https://github.com/ThorvaldBoe/FusionCanvas/issues/969), UI-003 [#970](https://github.com/ThorvaldBoe/FusionCanvas/issues/970), UI-004 [#968](https://github.com/ThorvaldBoe/FusionCanvas/issues/968).

Review status: PROVISIONAL; selected review scope complete, not a clean certification. All four findings fail their cited criteria; remaining runtime unknowns are retained. FC-UI v1.4 and UI Source Extract Definition v1.4 are Draft, so final certification is unavailable. The audit owner can revalidate affected results after each fix.

## Revalidation after fixes — 2026-11-20

| Finding | Source revision | Revalidated criterion | Result | Evidence |
|---|---|---|---|---|
| UI-001 / #966 | `384eaa19290a9719b6cf926b7f640237ee952de1` | FC-UI-022, FC-UI-096 | PASS | Store Editor Basic info content now has the shared 14-DIP card inset while the tab boundary remains bounded. The full solution baseline passed after the change; visual evidence was originally observed in the reachable live tab. |
| UI-004 / #968 | `b00ef43` | FC-UI-015 | PASS | Replaced the 745 audited literals across 33 surfaces plus three literals in the shared context header with centrally defined spacing/layout resources. A source rescan found zero remaining positive numeric Margin/Padding/Spacing/RowSpacing/ColumnSpacing literals outside DesignTokens.axaml. Full solution: 2,551 passed, 0 failed. |

UI-002/UI-005 (#969) and UI-003 (#970) remain open pending their fixes. Runtime-only and state-gated observations remain UNKNOWN. Overall audit status remains PROVISIONAL because findings remain open and the standard/definition are Draft.

| UI-002/UI-005 / #969 | `6a3cced` | FC-UI-024, FC-UI-097 | PASS | Replaced 334 audited literal TextBlock font sizes across 26 surfaces and four more in the shared context header with semantic typography resources. The three 10-DIP labels now use the 11-DIP Label resource. Static scan finds zero numeric TextBlock FontSize literals outside DesignTokens.axaml. Full solution: 2,551 passed, 0 failed. |
