# Basic Product Creation Workflow Verification

## Status

**Planned — implementation has not started.** Results must be recorded per scenario. `dotnet test` and strict OpenSpec validation are required completion gates but do not replace the rows below.

## Environment and isolation

- Build/commit: _Pending_
- Operating system/runtime: _Pending_
- Desktop capability: _Pending_
- Disposable SQLite database: _Pending_
- Disposable managed workspace root: _Pending_
- Material screenshots/automation logs: _Pending_
- Limitations: _Pending_

## Criterion-level evidence

| ID | Capability | Acceptance scenario | Planned verification | Result | Evidence / notes |
| --- | --- | --- | --- | --- | --- |
| BPW-01 | basic-product-workflow | Item opens at its current stage | App state test + desktop critical journey | Planned | |
| BPW-02 | basic-product-workflow | Earlier stage is reviewed | App visibility/editability test + desktop navigator check | Planned | |
| BPW-03 | basic-product-workflow | User attempts to edit reviewed upstream content | Application rejection + App read-only test | Planned | |
| BPW-04 | basic-product-workflow | User regresses intentionally | Domain/Application test + desktop regression path | Planned | |
| BPW-05 | basic-product-workflow | Empty Item advances through all stages | Application parameterized test + desktop journey | Planned | |
| BPW-06 | basic-product-workflow | Stage move succeeds | Application atomicity + Integration reload test | Planned | |
| BPW-07 | basic-product-workflow | User saves an Idea | Application metadata test + Integration round trip | Planned | |
| BPW-08 | basic-product-workflow | Existing Item contains Audience metadata | Application compatibility test | Planned | |
| BPW-09 | basic-product-workflow | User saves Concept values | Application normalization/independence test + round trip | Planned | |
| BPW-10 | basic-product-workflow | Concept remains empty | Application movement test | Planned | |
| BPW-11 | basic-product-workflow | Item surface is populated | App visibility/composition test + desktop inspection | Planned | |
| BPW-12 | basic-product-workflow | Window height is reduced | Targeted desktop resize/scroll/keyboard check | Planned | |
| BPW-13 | basic-product-workflow | User leaves a meaningful text draft | App Save/Discard/Cancel test + desktop focus check | Planned | |
| BPW-14 | basic-product-workflow | User changes a Tag while text is dirty | Application/App independence test | Planned | |
| BPW-15 | basic-product-workflow | Save fails | Failing-repository Application/App recovery test | Planned | |
| BPW-16 | basic-product-workflow | Completed workflow survives restart | Integration round trip + desktop restart journey | Planned | |
| BPW-17 | basic-product-workflow | Item is archived and restored | Application/Integration preservation tests + desktop archive/restore | Planned | |
| BPW-18 | basic-product-workflow | Successful mutation synchronizes open contexts | App coordination/filter tests + desktop multi-context journey | Planned | |
| BPW-19 | basic-product-workflow | User operates the workflow by keyboard | App accessibility-state inspection + desktop keyboard pass | Planned | |
| BPW-20 | basic-product-workflow | User repeats an action while it is running | App busy-state/command tests + desktop duplicate-submission check | Planned | |
| CDM-01 | core-domain-model | Contributor identifies item entities | Domain API/boundary inspection test | Planned | |
| CDM-02 | core-domain-model | Item belongs to a store topic context | Domain/Application relationship test | Planned | |
| CDM-03 | core-domain-model | Item is created without user-entered content | Application creation test + Integration round trip | Planned | |
| CDM-04 | core-domain-model | Empty Item needs a display label | Formatter test + tree/tab/Overview App tests | Planned | |
| CDM-05 | core-domain-model | Existing workspace is upgraded | Real v4-to-v5 Integration fixture | Planned | |
| IM-01 | listing-management | User creates an empty Item from a selected topic | Application creation/destination test + desktop | Planned | |
| IM-02 | listing-management | Selected Item supplies its containing topic | Application destination test | Planned | |
| IM-03 | listing-management | Store context uses the default niche | Application default-niche test | Planned | |
| IM-04 | listing-management | Store has no resolvable topic | Application validation + App blocked-state test | Planned | |
| IM-05 | listing-management | User supplies optional creation details | Application creation metadata test | Planned | |
| IM-06 | listing-management | Generated identity is invalid | Application deterministic ID failure test | Planned | |
| IM-07 | listing-management | Empty Item appears across surfaces | Formatter/App synchronization test + desktop | Planned | |
| II-01 | listing-inspector | Item is at Concept | App Stage Tool composition/editability test | Planned | |
| II-02 | listing-inspector | User reviews an earlier stage | App read-only test + desktop | Planned | |
| II-03 | listing-inspector | User saves valid Item edits | Application stage-save test + Integration round trip | Planned | |
| II-04 | listing-inspector | Save targets a non-current stage | Application stale-stage rejection test | Planned | |
| II-05 | listing-inspector | User switches context with unsaved text | App guard/focus tests + desktop | Planned | |
| II-06 | listing-inspector | Tag changes while text is unsaved | Application/App independent mutation test | Planned | |
| II-07 | listing-inspector | Status options are displayed | Domain transition matrix + App selector test | Planned | |
| II-08 | listing-inspector | Stage or status changes | App cross-context synchronization test | Planned | |
| ILS-01 | listing-lifecycle-status | Item persists stage and status independently | Domain + Integration round trip | Planned | |
| ILS-02 | listing-lifecycle-status | Status change does not move the stage | Application status test | Planned | |
| ILS-03 | listing-lifecycle-status | Item exposes allowed transitions | Exhaustive Domain matrix + App options test | Planned | |
| ILS-04 | listing-lifecycle-status | Confirmation is required | Domain decision + App confirmation/cancel tests | Planned | |
| ILS-05 | listing-lifecycle-status | Status change fails to persist | Failing-repository Application/App test | Planned | |
| ILS-06 | listing-lifecycle-status | Published Item cannot move stages | Domain/Application policy + App command test | Planned | |
| ILS-07 | listing-lifecycle-status | Rejected Item is reactivated | Application transition + desktop | Planned | |
| ILS-08 | listing-lifecycle-status | Published Item is reviewed | App policy matrix + desktop | Planned | |
| ILS-09 | listing-lifecycle-status | User modifies Published content intentionally | Application orchestration + desktop confirm/pause/regress | Planned | |
| ILS-10 | listing-lifecycle-status | Rejected Item is reviewed | Domain/App metadata-versus-content test | Planned | |
| WSN-01 | workflow-stage-navigator | Earlier stage is selected | Application/App navigator test | Planned | |
| WSN-02 | workflow-stage-navigator | User needs to edit an earlier stage | Application move + App editability test | Planned | |
| WSN-03 | workflow-stage-navigator | Published Item is active | App navigator movement/review test | Planned | |
| AM-01 | asset-management | User imports Design files | Application + Integration managed-file tests + desktop | Planned | |
| AM-02 | asset-management | User selects a non-PNG file | Application pre-copy validation + desktop | Planned | |
| AM-03 | asset-management | Same source is imported twice | Application/Integration independent-assets test | Planned | |
| AM-04 | asset-management | User previews a Design file | Integration read + App preview + desktop | Planned | |
| AM-05 | asset-management | User exports a Design file | Integration byte equality + desktop | Planned | |
| AM-06 | asset-management | Managed file is missing | Application/App missing-state test + desktop | Planned | |
| AM-07 | asset-management | User removes a Design file | Application/Integration confirmed-removal + desktop | Planned | |
| AM-08 | asset-management | Removal persistence fails | Failing-repository compensation test | Planned | |
| AM-09 | asset-management | Item has Design and reference assets | Application/App filtering test | Planned | |
| WFS-01 | workspace-file-storage | Preview resolves a valid managed reference | Integration managed-read/handle-release test | Planned | |
| WFS-02 | workspace-file-storage | Preview reference escapes the workspace | Integration traversal-security test | Planned | |
| WFS-03 | workspace-file-storage | Export copy succeeds | Integration byte-equality/source-preservation test | Planned | |
| WFS-04 | workspace-file-storage | Export is cancelled or invalid | Integration/App cancellation, missing, unwritable, same-path tests | Planned | |
| STH-01 | stage-tool-host | Basic built-in tool is registered | Application registry/host test | Planned | |
| STH-02 | stage-tool-host | Shared Item chrome is rendered | App composition/visibility test + desktop | Planned | |
| STH-03 | stage-tool-host | Later plugin tools are added | Application built-in/contributed selection regression test | Planned | |
| NT-01 | navigation-tree | Empty-title Item appears in the tree | App tree projection test + desktop | Planned | |
| NT-02 | navigation-tree | Published or Rejected Item appears | App inactive-treatment/filter test + desktop | Planned | |
| TDW-01 | tabbed-document-window | Empty-title Item opens in a tab | App tab fallback test + desktop | Planned | |
| TDW-02 | tabbed-document-window | Item title is saved | App multi-tab refresh test | Planned | |
| SF-01 | search-filtering | Status change leaves the active filter | Application/App filter/selection test + desktop | Planned | |
| SF-02 | search-filtering | Empty Item is searched | Application/App filter-versus-fallback test | Planned | |
| LSP-01 | local-sqlite-persistence | Version 4 workspace is opened | Real SQLite migration fixture + foreign-key check | Planned | |
| LSP-02 | local-sqlite-persistence | Item migration fails | Transaction rollback fault test or explicit limitation evidence | Planned | |
| LSP-03 | local-sqlite-persistence | Migrated workspace is saved and reopened | Complete snapshot comparison | Planned | |
| TM-01 | tag-management | Tag is applied while Item text is dirty | Application/App independent-draft test | Planned | |
| TM-02 | tag-management | Item terminology migration preserves Tags | Integration migration fixture | Planned | |
| CAT-01 | context-aware-tools | Built-in Stage Tool opens for an Item | Tool Context + Stage Tool Host Application test | Planned | |
| CAT-02 | context-aware-tools | No Item is selected | Application unavailable-context test | Planned | |

## Aggregate deterministic gates

| Gate | Result | Evidence / notes |
| --- | --- | --- |
| `dotnet build .\FusionCanvas.sln` | Planned | |
| `dotnet test .\FusionCanvas.sln` | Planned | Use `-m:1` only if the documented Avalonia output-lock issue occurs. |
| `openspec validate basic-product-creation-workflow --strict` | Planned | |
| Configured format/whitespace verification | Planned | |
| `git diff --check` | Planned | |

## Targeted real-desktop pass

| Desktop scenario | Result | Evidence / notes |
| --- | --- | --- |
| ID-only Item creation and fallback label across tree/tab/Overview | Planned | |
| Idea/Concept explicit Save, Tag independence, and ungated movement | Planned | |
| Earlier-stage review, regression, editability, and downstream preservation | Planned | |
| PNG import/duplicate/non-PNG rejection/preview/Export/missing/remove | Planned | |
| Publish protection, approved metadata, Paused modification path, Rejected recovery | Planned | |
| Archive/restore, active filter, and multiple-tab synchronization | Planned | |
| Restart and complete Item reconstruction | Planned | |
| Minimum-height scrolling, keyboard/focus, confirmations, cancellation, and errors | Planned | |

## Scoped completion QA

- Architecture/layering review: _Pending_
- Item terminology/spec-drift review: _Pending_
- SQLite migration/data-loss review: _Pending_
- File-boundary/path-traversal/compensation review: _Pending_
- UI state/focus/accessibility review: _Pending_
- Learning review and promoted lessons: _Pending_
