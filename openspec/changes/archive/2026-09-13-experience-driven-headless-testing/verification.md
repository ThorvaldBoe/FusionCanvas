# Verification — experience-driven-headless-testing

Status: complete; all implementation and verification gates passed.

## Baseline observation before repository configuration

On 2026-09-13, `dotnet test .\FusionCanvas.sln --no-restore --nologo -v normal` exited with code 1 after MSBuild reported a failed VSTest target but emitted no compiler or test diagnostic. The equivalent serialized invocation with `-m:1 -p:UsedAvaloniaProducts=` exited successfully in this worktree. Repository configuration now suppresses the telemetry side effect, and `-m:1` is the canonical deterministic invocation; parallel orchestration remains an environment-sensitive failure mode rather than a hidden diagnostic.

## Acceptance scenarios

| Capability / scenario | Planned method | Result / evidence |
| --- | --- | --- |
| testing-baseline / A user-facing module becomes implementation-ready | Inspect updated verification guidance and one pilot mapping for affected jobs, representative states, and proportionate evidence | Pass: `design.md`, `tasks.md`, and this criterion-level table map the three pilots and scoped non-pilot gaps. |
| testing-baseline / An existing surface is reviewed | Inspect the seeded user-job inventory for job-based rows, evidence, gaps, and static-markup rationales | Pass: `docs/testing-strategy.md` inventories implemented surface families with evidence/gap rationale. |
| testing-baseline / A critical workflow crosses framework seams | Three rendered pilot journeys plus inspection that variants remain at focused layers | Pass: Printify, Blueprint, and grouped navigation journeys cover their selected cross-seam outcomes. |
| testing-baseline / A user expects a mutation to persist | Printify and Blueprint disposable-SQLite journeys with fresh presentation reconstruction and clean dirty state | Pass: both Store Editor pilots reconstruct from scenario-scoped SQLite and assert the visible result plus clean Save state. |
| testing-baseline / A workflow does not justify an experience journey | Inventory and QA evidence include at least one focused-test or not-applicable rationale without a ceremonial journey | Pass: inventory records focused-only and deferred surfaces with rationale. |
| testing-baseline / A journey performs user actions | Inspect pilot action phases and drivers; verify routed/control interaction and absence of command/property/private-handler shortcuts | Pass for Printify/Blueprint; navigation feedback uses `RaiseEvent` on the rendered row and no private reflection. |
| testing-baseline / A journey arranges deterministic state | Inspect pilot arrange/action separation and deterministic offline collaborator setup | Pass: synthetic credential/provider fakes and scenario-owned SQLite are used. |
| testing-baseline / A journey verifies an outcome | Inspect visible assertions for selected shop, Blueprint name/save state, and navigation feedback/hierarchy plus durable checks | Pass: all three pilots assert visible outcomes and navigation durable parent reconstruction. |
| testing-baseline / A journey waits for asynchronous UI state | `HeadlessUiWait` focused tests and touched-pilot inspection showing no arbitrary readiness delays | Pass: 5 infrastructure tests pass; pilots use bounded observable waits. |
| testing-baseline / A journey uses a persistent workspace | Disposable workspace isolation tests plus Printify/Blueprint fresh-composition evidence | Pass: isolation tests and both SQLite pilots use unique roots and fresh compositions. |
| testing-baseline / Shared test drivers are introduced | Driver code review for thin user-language APIs, semantic locators, bounded waits, and no assertions/business shortcuts | Pass: Store Editor and Main Window drivers contain locator/action helpers only. |
| testing-baseline / A defect is fixed | Pilot red/green or controlled negative-mutation records, escape classification, and similar-risk inspection | Pass: Printify, Blueprint, and grouped navigation regressions are covered with escape classifications. |
| testing-baseline / Deterministic reproduction is unsuitable | Not applicable unless the drag preflight proves a headless limitation; otherwise record the exact limitation and separable deterministic evidence | Not applicable: the grouped fixture routes coordinate-driven drop deterministically in Avalonia headless. |
| testing-baseline / Escape analysis identifies a reusable pattern | Updated shared guidance/support and at least one representative prevention beyond the originating local regression | Pass: user-boundary rule, `HeadlessUiWait`, inventory, QA checklist, and PR template promote recurring seam prevention. |
| testing-baseline / Escape analysis identifies an isolated defect | Pilot escape records show any local-only conclusion without unjustified global duplication | Pass: Blueprint re-entry and navigation drop persistence are recorded as scoped follow-ups rather than duplicated globally. |
| testing-baseline / A pull request changes the repository | Inspect Windows PR workflow and verify restore plus canonical solution test is a failing check on errors | Pass by inspection: `.github/workflows/ci.yml` runs restore and serialized solution tests on PR/main. |
| testing-baseline / The baseline runs in supported contributor environments | Run canonical `dotnet test .\FusionCanvas.sln -m:1` locally after repository-controlled telemetry correction; inspect documentation consistency | Pass: canonical serialized solution run completed successfully with 1,566 tests passed. |
| testing-baseline / Real-desktop automation remains separate | Inspect `FusionCanvas.sln`, CI workflow, and `FusionCanvas.UITests` documentation for Appium exclusion | Pass: solution and CI omit `FusionCanvas.UITests`; its README remains separately selectable. |
| qa-review-baseline / Completion QA reviews a user-facing module | Execute scoped completion QA using the updated job inventory, journey-boundary checks, and re-entry evidence | Pass: inventory, pilots, and retrospective reviewed. |
| qa-review-baseline / Completion QA finds component-only evidence for a critical journey | Exercise the QA checklist against a documented pre-pilot gap and confirm it is reported as experience coverage rather than accepted aggregate evidence | Pass: the pre-pilot component-only gaps are now covered by the three journeys. |
| qa-review-baseline / QA reviews a defect correction | Review all three pilot escape records for regression/limitation evidence, cause, similar-risk check, and promotion decision | Pass: records and promotion decisions are in `retrospective.md`. |
| qa-review-baseline / A proposed global lesson is disproportionate | Inspect at least one local-only classification or record not-applicable if every pilot mechanism justifies broader prevention | Pass: promotion is limited to recurring seam rules; no unrelated surface conversion was added. |
| qa-review-baseline / QA-6 reviews the headless suite | Run updated QA-6 over pilot scope and report semantic gaps, shortcuts, sleeps, shared state, weak assertions, and duplication findings | Pass: pilot actions are routed, isolated, and bounded; untouched suite cleanup remains outside scope. |
| qa-review-baseline / QA evaluates strategy effectiveness | Retrospective reports escaped-defect learning, flake observations, baseline duration, maintenance cost, and failure-localization quality without numeric coverage gates | Pass: `retrospective.md` records command correction, pilot value, maintenance cost, and follow-up scope. |

## Required completion gates

| Gate | Result / evidence |
| --- | --- |
| Delivery-package approval | Pass: implementation proceeded from the approved delivery package and the completed artifacts match the implemented scope. |
| Focused helper, headless, and persistence tests | Pass: helper suite 5/5, pilot journeys, and the full App suite pass. |
| `dotnet build .\FusionCanvas.sln` | Pass: build succeeded with 0 errors (warnings pre-existing analyzer guidance). |
| `dotnet test .\FusionCanvas.sln -m:1` | Pass: 248 Domain + 430 Application + 218 Integration + 643 App + 27 UI-description tests passed. |
| `openspec validate experience-driven-headless-testing --strict` | Pass. |
| `openspec validate --strict` | Pass via `openspec validate --all --strict`: 68 items passed. |
| Scoped completion QA and drift review | Pass: all three pilot outcomes are covered; no known pilot gap remains. |
| Retrospective | Pass: `retrospective.md` added. |


Optional live-desktop evidence is not planned because the grouped drag/drop journey routes deterministically in Avalonia headless.
