# Software Factory Audit

- Audit ID: `e40e7d86-c50b-4948-9e91-66b5261b4a1e`
- Scope: Printify listing import commits `710c450` and `013feb0`, compared with base `93b0840`.
- Profile: `STANDARD_TARGETED` across all six axes.
- Started: 2026-10-10T14:47:50.8530763+00:00
- Source revision: `013feb0`; branch `codex/import-printify-listings`; working tree was clean at audit start.
- Status: **BLOCKED**. Security & Privacy Pass C's codebase-wide automated sweep could not be completed: the package audit command exited before producing a report. Solution restore did produce ImageSharp vulnerability advisories, which remain actionable evidence. UI and Values baselines are draft, and Security source extraction is a working definition; those axis judgments are provisional even if remaining work is completed.
- Process coverage: 5/6 axes have a scoped assessment; Security & Privacy has partial Pass C coverage. Estimate: 85%, based on review of all changed paths, relevant active specs, feature verification evidence, and test gates; no automated security inventory output or native desktop journey was available.

## Standards and definitions

| Axis | Standard | Definition | Baseline |
|---|---|---|---|
| Architecture & Code Structure | FC-ARCH 2.0.0 | 2.0 Active | Active |
| Functionality & Logic | 1.2 | 1.2 Active | Active |
| UX | 1.1 | 1.2 Active | Active |
| UI | 1.4 | 1.4 Draft | Draft |
| Security & Privacy | 1.0 | 1.2 Working definition | Working |
| Values, Vision & Taste | 1.1 | 1.2 Draft | Draft |

## Results

- **Architecture & Code Structure — no finding.** New orchestration and provider transport follow the Application/Integration boundary; UI launches the workflow and does not own persistence or provider logic. See [run](Runs/6b7061bc-b054-4942-9463-93518c5c230b.md).
- **Functionality & Logic — one verification challenge.** The change's own verification record marks HTTP retrieval retry and repository-save failure assertions as partial; no functional defect was established from reviewed evidence. See [run](Runs/44fd3708-15e3-4a70-b7c0-5659baec2dbd.md).
- **UX — one low-severity finding.** The linked-products toggle label does not expose its current state or the action it will take. See [run](Runs/4c9194a4-f236-49ff-bac3-59536360f5a1.md).
- **UI — no finding identified; provisional.** Context menu, dialog, status/error region, disabled linked rows, and headless coverage align with the selected workflow. Native Appium journey was not run.
- **Security & Privacy — no confirmed feature vulnerability; blocked/partial.** Source review found HTTPS host allowlisting, disabled redirects, response-size limits, image signature checks, bearer auth to the Printify API, and sanitized provenance. Dependency and wider automated sweeps remain incomplete. See [run](Runs/4f0a024b-673d-4242-ab4e-c1a7bbb71736.md).
- **Values, Vision & Taste — aligns with user-approved direction, provisional.** Import is explicit, user-selected, Printify-only, local-first after download, and leaves variant setup optional. See [run](Runs/0ed35b93-4497-4854-a4df-785346bea966.md).

## Findings and challenges

### Finding UX-001 — Linked-products toggle is state-ambiguous

- Severity: Low.
- Evidence: `src/FusionCanvas.App/Stores/PrintifyListingImportWindow.axaml` renders the fixed label “Show/hide linked products”; `PrintifyListingImportViewModel.ShowLinked` toggles state, but the label does not change and does not communicate whether linked products are currently visible.
- Impact: Users must infer the dialog state after clicking, making the control harder to understand and use with assistive technology or when returning attention to the dialog.
- Suggested direction: Bind the label/accessibility name to state (“Show linked products” / “Hide linked products”) and expose the current state.
- State: Open; [issue #914](https://github.com/ThorvaldBoe/FusionCanvas/issues/914).

### Challenge FL-001 — Retry and persistence-failure verification is partial

- Evidence: `openspec/changes/import-printify-listings/verification.md` marks retrieval-error retry and local repository-save failure as partial test coverage. The implementation isolates products and cleans staged files, but these failure paths lack focused assertions.
- Disposition: Retain as a verification challenge, not a demonstrated defect. No change made.

### Challenge SEC-001 — Automated security sweep unavailable

- Evidence: `dotnet test .\FusionCanvas.sln` restore emitted multiple NU1902/NU1903 advisories for `SixLabors.ImageSharp` 3.1.12. `dotnet list ... package --vulnerable --include-transitive` exited during restore without a report, so package scope and direct/transitive status are unknown from that command.
- Disposition: Security Pass C remains incomplete; audit is blocked pending a successful automated inventory. The advisories are codebase evidence, not attributed to these commits.

### Challenge UI-001 — Native desktop journey not run

- Evidence: change verification says the Appium journey was not run because the Appium server was unavailable; compile/headless UI evidence is recorded there.
- Disposition: Keep as a limitation; no visual or native interaction claim made.

## Verification and routing evidence

- `dotnet test .\FusionCanvas.sln` completed successfully in this environment; observed project summaries included 29 UI-description, 294 Domain, 356 Integration, and 1,014 App tests. Restore emitted ImageSharp NU1902/NU1903 advisories.
- `openspec validate import-printify-listings --type change --strict` — passed.
- `git diff --check 93b0840..013feb0` — passed.
- `python -m unittest discover -s tools/software-factory/tests -v` — passed (11 tests) after setting `TEMP` and `TMP` to a workspace-local directory.
- Jev was not executed. The installed executable could not be invoked in this environment. Manual routing was recorded as `BYPASSED` for one source item per axis; these are not Jev execution attestations. Invocation IDs and logs are linked from each run.
- Change verification reports the Appium journey was not run. No live user data or external Printify account was used.

## Issue creation and checkpoint

- GitHub issue creation: completed for UX-001; [issue #914](https://github.com/ThorvaldBoe/FusionCanvas/issues/914) is open with `audit-finding`. The challenges remain unconfirmed defects.
- Axes included: all six, as requested. None excluded.
- Next checkpoint: complete the Security & Privacy automated inventory and rerun the package audit when restore/audit tooling yields a report; then update this parent and the Security run. UI/VVT results remain provisional until their normative baselines are finalized.
- End timestamp: not set; audit remains blocked and resumable.
