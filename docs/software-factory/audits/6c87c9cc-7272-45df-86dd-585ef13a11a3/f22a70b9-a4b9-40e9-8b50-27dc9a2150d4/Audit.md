# UI Audit Run

- Audit ID: 6c87c9cc-7272-45df-86dd-585ef13a11a3
- Audit run ID: f22a70b9-a4b9-40e9-8b50-27dc9a2150d4
- Status: PROVISIONAL
- Baseline readiness: PROVISIONAL
- Started at: 2026-10-09T15:40:12.6981226+02:00
- Auditor/reviewer: Codex
- Request scope: SINGLE_AXIS; UI only, Settings AI section and containing scroll surface.
- Profile: STANDARD_TARGETED
- Standard: FusionCanvas UI Standard v1.4, Draft normative baseline
- Source definition: UI Source Extract Definition v1.4, Draft
- Source revision: 15b0b1d; initially clean.
- Jev: MANUAL_FALLBACK; automatic approval review rejected external routing before execution. No successful invocation claimed. Factory unavailable-adapter rules retain UNKNOWN and mandatory checks.
- Source items: 2 current, 2 manually routed, 2 in scope, 0 filtered, 2 completed.
- Checks: 12 current planned, 12 completed, 0 remaining; 12 initial assessments also retained.
- Phase: FINALIZE; 100% of selected checks, not a compliance score. Historical checkpoints are retained below.
- Current item: UI-SURF-AI-SETTINGS, position 1/2.
- Next: PR delivery; finding retested successfully.
- Certification: NOT CERTIFIED; targeted scope and draft baseline.
- GitHub issue creation: NOT REQUESTED; user requested finding remediation and PR delivery.

## Initial finding UI-001 — Advanced toggle clips at maximum scroll

- Audit IDs: as above; assessment A-AI-020-1; source UI-SURF-AI-SETTINGS@1 and UI-SURF-SETTINGS@1; revision 15b0b1d.
- Rules: FC-UI-020 (required actions must not clip or become unreachable), FC-UI-084 (adaptive scrolling must avoid clipping).
- Severity: Blocker, per UI standard: hides an existing settings control.
- Surface/pattern/component: Settings AI content viewport / scrolling settings form / Advanced ToggleSwitch.
- Evidence methods: V user-supplied screenshot; S SettingsWindow.axaml ScrollViewer Padding=24; A headless scroll-end measurement.
- Current: at 860x680, switch Y=662 and height=55 in a 680-high presenter after maximum offset 193; bottom=717, clipped by 37 DIPs. At 720x520, Y=502, height=55, bottom=557, clipped by 37 DIPs at offset 368. The taller empty-profile fixture fits without overflow, showing why an existence-only assertion misses this defect.
- Initial regression run: 6 existing AiSettingsViewTests pass; 2 new geometry cases fail for clipping; tall empty-profile case passes. Command: dotnet test tests/FusionCanvas.App.Tests/FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~AiSettingsViewTests.
- Expected: the full switch track, label and hit target can be reached by scrolling, at supported sizes, with the intended page inset preserved.
- Cause: inset on the ScrollViewer presenter is outside the reported scrollable extent; overflowing content cannot scroll far enough.
- Recommendation: move the existing page inset into the scrolling content using the shared page-inset token; test actual viewport geometry plus keyboard operation.
- Scope: shared Settings content container, local to SettingsWindow; all selected sections inherit the corrected inset. No workflow, AI policy, persistence or architecture change; direct maintenance under openspec-project-workflow.
- Likely files: SettingsWindow.axaml, AiSettingsView.axaml (stable target identifier), AiSettingsViewTests.cs.
- Verification: supported compact/default/reported sizes, Light/Dark, populated profiles, full end-of-content visibility, Space-key advanced disclosure, solution baseline, strict OpenSpec validation.
- Cross-axis routing: NONE; no new product behavior is proposed.
- Issue URL: NONE / NOT REQUESTED. User authorized remediation and merged PR.
- Challenges: NONE; existing clipping requirements unambiguously apply. Screenshot scroll offset/DPI remain UNKNOWN, but deterministic reproduction establishes the defect independently.
- Standard change decision: NOT NEEDED; step 1 generated a finding under the existing baseline. Step 2 conditional retry does not apply.

## Routing checkpoint

Manual shadow assessment retained all six candidates for both surfaces: FC-UI-019, FC-UI-020, FC-UI-074, FC-UI-078, FC-UI-083, FC-UI-084. No candidate was filtered. FC-UI-020/084 are MANDATORY due to the deterministic clipping signal; others RELEVANT. Jev confidence is unavailable; manual classification is not a Jev result. Context: XAML, compiled state owner, Fluent templates and headless interaction. Log: jev/9490894d-2710-45f3-9e3c-f07dbb30559e.jsonl, invocation 9490894d-2710-45f3-9e3c-f07dbb30559e, BYPASSED/MANUAL_FALLBACK.

Progress: AUDIT, 2 surfaces manually routed, 2 in scope, 0 filtered; clipping assessments complete, correction and remaining checks pending. Approximately 40%; next action: populated regression and bounded content-inset correction.

## Criterion-level assessment history

Every row inherits this note's audit ID, run ID, UI axis, standard v1.4 and source-definition v1.4. Source state @1 is initial clean production; @2 is the committed correction. Implementation locators are the exact source paths in the extract. UNKNOWN in the initial keyboard/automation checks is preserved and replaced by current evidence, not presented as a pass. No uncertainty about the standard's clipping interpretation required a challenge.

| Assessment ID | Exact source entry | Source revision | Standard | Criterion | Result | Rationale / evidence | Finding | Challenge | Supersedes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A-AI-019-1 | UI-SURF-AI-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-019 | PASS | Parent defines 720x520 minimum; source inspection | NONE | NONE | NONE |
| A-AI-020-1 | UI-SURF-AI-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-020 | FAIL | Pre-fix geometry shows 37-DIP clipping; UI-001 | UI-001 | NONE | NONE |
| A-AI-074-1 | UI-SURF-AI-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-074 | UNKNOWN | Existing disclosure test assigns AdvancedMode directly; routed keyboard behavior not yet demonstrated | NONE | NONE | NONE |
| A-AI-078-1 | UI-SURF-AI-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-078 | UNKNOWN | SectionSelector stable ID; switch exposes meaningful content; stable switch automation ID not verified in initial test | NONE | NONE | NONE |
| A-AI-083-1 | UI-SURF-AI-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-083 | PASS | Window minimum dimensions enforced; existing construction coverage | NONE | NONE | NONE |
| A-AI-084-1 | UI-SURF-AI-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-084 | FAIL | Max scroll still clips switch; UI-001 | UI-001 | NONE | NONE |
| A-SET-019-1 | UI-SURF-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-019 | PASS | Parent defines 720x520 minimum; source inspection | NONE | NONE | NONE |
| A-SET-020-1 | UI-SURF-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-020 | FAIL | Pre-fix geometry shows 37-DIP clipping; UI-001 | UI-001 | NONE | NONE |
| A-SET-074-1 | UI-SURF-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-074 | UNKNOWN | Existing disclosure test assigns AdvancedMode directly; routed keyboard behavior not yet demonstrated | NONE | NONE | NONE |
| A-SET-078-1 | UI-SURF-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-078 | UNKNOWN | SectionSelector stable ID; switch exposes meaningful content; stable switch automation ID not verified in initial test | NONE | NONE | NONE |
| A-SET-083-1 | UI-SURF-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-083 | PASS | Window minimum dimensions enforced; existing construction coverage | NONE | NONE | NONE |
| A-SET-084-1 | UI-SURF-SETTINGS@1 | 15b0b1d | 1.4 | FC-UI-084 | FAIL | Max scroll still clips switch; UI-001 | UI-001 | NONE | NONE |
| A-AI-019-2 | UI-SURF-AI-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-019 | PASS | 720x520, 860x680 and 856x1303 layouts; minimum preserved | NONE | NONE | A-AI-019-1 |
| A-AI-020-2 | UI-SURF-AI-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-020 | PASS | Six populated-profile resize/theme cases pass full viewport geometry checks | UI-001 / retested | NONE | A-AI-020-1 |
| A-AI-074-2 | UI-SURF-AI-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-074 | PASS | Advanced switch accepts focus and Space toggles on/off; last profile switch reachable; scoped to affected disclosure, not whole-window tab order | NONE | NONE | A-AI-074-1 |
| A-AI-078-2 | UI-SURF-AI-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-078 | PASS | Settings.AI.AdvancedMode and AdvancedModeToggle now provide stable semantic target; SectionSelector ID retained | NONE | NONE | A-AI-078-1 |
| A-AI-083-2 | UI-SURF-AI-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-083 | PASS | 720x520 minimum retained; regression exercises minimum actual window size | NONE | NONE | A-AI-083-1 |
| A-AI-084-2 | UI-SURF-AI-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-084 | PASS | ScrollToEnd, expanded-tail visibility and BringIntoView pass; fixed-dark-settings.png shows full track | UI-001 / retested | NONE | A-AI-084-1 |
| A-SET-019-2 | UI-SURF-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-019 | PASS | 720x520, 860x680 and 856x1303 layouts; minimum preserved | NONE | NONE | A-SET-019-1 |
| A-SET-020-2 | UI-SURF-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-020 | PASS | Six populated-profile resize/theme cases pass full viewport geometry checks | UI-001 / retested | NONE | A-SET-020-1 |
| A-SET-074-2 | UI-SURF-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-074 | PASS | Advanced switch accepts focus and Space toggles on/off; last profile switch reachable; scoped to affected disclosure, not whole-window tab order | NONE | NONE | A-SET-074-1 |
| A-SET-078-2 | UI-SURF-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-078 | PASS | Settings.AI.AdvancedMode and AdvancedModeToggle now provide stable semantic target; SectionSelector ID retained | NONE | NONE | A-SET-078-1 |
| A-SET-083-2 | UI-SURF-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-083 | PASS | 720x520 minimum retained; regression exercises minimum actual window size | NONE | NONE | A-SET-083-1 |
| A-SET-084-2 | UI-SURF-SETTINGS@2 | 5931e01 | 1.4 | FC-UI-084 | PASS | ScrollToEnd, expanded-tail visibility and BringIntoView pass; fixed-dark-settings.png shows full track | UI-001 / retested | NONE | A-SET-084-1 |

## Coverage and verification

- Relevant scope: adaptive presentation and reachability of existing AI Settings disclosure. Other FC-UI rules are outside this bounded check, unassessed and not certified. All other axes are out of scope.
- Routing extension @2: same six candidates per surface retained by manual impact analysis; no filters; FC-UI-020/084 mandatory. Original bypass invocation is retained; no Jev execution claimed.
- Table/list checkpoint: N/A, this scope presents forms and profile cards, not repeated data records.
- Tabbed-surface checkpoint: N/A, section rail is a ListBox rather than tab chrome; unchanged rail and content relationship inspected.
- Visual evidence: reported-clipping.png is the user report; fixed-dark-settings.png is a local Skia/headless render at 860x680 and 96 DPI using synthetic test/model metadata. Full Advanced label, track and Off state are visible. Screenshot state does not claim a live provider connection.
- Focused Settings verification: 90 tests passed after production correction. Six new geometry/keyboard cases originally failed for 37-DIP clipping in populated fixtures across Light/Dark and all tested sizes.
- Test method: AiSettingsViewTests.AiSection_AdvancedToggleIsFullyReachableAtScrollEnd. It resizes the window, scrolls to end, asserts complete control containment, uses routed Space press/release, checks expanded-tail visibility, returns to the disclosure and collapses it. Fixtures use in-memory settings/offline AI, no real credentials or workspace.
- Appium decision: zero new journeys. This maintenance defect is Avalonia scroll extent/layout behavior proven in deterministic headless component coverage; it introduces no native-window, OS-input, external-provider or persistence behavior. Optional desktop lane omitted.

| User job / gate | Method | Result / evidence |
| --- | --- | --- |
| Reach and operate Advanced settings | Six headless resize/theme cases, routed keyboard input | PASS |
| Reach final advanced profile | Full viewport geometry after expansion and scroll-end | PASS |
| Existing settings sections and bindings | Settings-focused headless/view-model suite | 90 PASS |
| Preserve visual page inset | Shared Token.Layout.PageInset and synthetic dark render | PASS |
| Full build, baseline and strict OpenSpec validation | Recorded completion checkpoint below | Pending baseline completion |

## Escape analysis and bounded remediation review

Existing tests constructed the view, found a ScrollViewer and set AdvancedMode directly. They did not measure the control against the clipped viewport at maximum scroll. Thus they passed while the switch track was unreachable. The regression asserts the actual containment and operates the rendered switch.

Similar-surface inspection: General, Workspace, Terms and About share the corrected Settings content container and are covered by Settings tests. AiProfileEditorView uses the same outer viewport without another independent page ScrollViewer. DesignSystemGalleryWindow also places a page inset on ScrollViewer; it is outside this reported bug's surface scope and is a follow-up candidate for the same scroll-end check, not certified by this run. Prevention remains the focused regression and this retained cross-surface observation; no unrequested gallery correction or standard edit.

Maintenance boundary: production changes are confined to App XAML. No use cases, policies, credentials, persistence, migrations, external calls or layer dependencies changed. This restores an existing control's presentation under the small-maintenance exception; no new OpenSpec behavior is introduced. All original assessments and extract entries are retained. Draft standard/definition status keeps the process result PROVISIONAL even after successful remediation.

## Completion checkpoint

- Ended at: 2026-10-09T15:54:42.681805+02:00
- Process status: PROVISIONAL; standard and definition remain Draft. Profile coverage is complete; this is not whole-Surface certification.
- Progress updated at: 2026-10-09T15:54:42.681805+02:00; FINALIZE; two current surfaces, routed/in scope/completed 2/2/2, filtered 0; 12 selected current pairs complete, 0 remaining; latest extract extension 2; current item NONE, next PR delivery. Progress 100% reflects coverage, not compliance.
- Source revisions: 15b0b1d -> 5931e01; initial source/assessments preserved; affected prior results reassessed manually as @2.
- Finding UI-001: FIXED AND RETESTED in 5931e01; no remaining clipping finding in assessed scope. Delivery link to be recorded on parent and private run after PR merge.
- Final build: dotnet build FusionCanvas.sln --no-restore -m:1 PASS, zero errors. Initial --no-restore build had missing package assets in the new worktree; solution restore resolved that environmental failure, and the build was rerun successfully.
- Baseline: dotnet test FusionCanvas.sln -m:1 PASS: Domain 294, Application 671, Integration 352, App 982, UiDescription 29; total 2,328, zero failures/skips. Includes six new resize/theme/keyboard cases.
- OpenSpec: openspec validate --all --strict --no-interactive PASS: 86 items, zero failures.
- Existing ImageSharp NU1902/NU1903 and pre-existing analyzer warnings remain visible. No package files changed; dependency remediation is outside this UI-only request.
- Challenges: NONE. Remaining UNKNOWN: original screenshot DPI/scroll offset, native desktop observation and full-Surface certification; these do not invalidate the locally reproduced and retested clipping finding.
- GitHub issue creation: NOT REQUESTED; no standalone issue created. User requested the correction and PR merge.
- Standard changes: NONE; existing FC-UI-020/084 generated the finding on the first run, so conditional standard revision/retry was unnecessary.
