## 1. Telemetry contracts and policy

- [ ] 1.1 Add application-facing telemetry event, area, query, settings, export, and deletion contracts under `FusionCanvas.Application.Telemetry`.
- [ ] 1.2 Implement the workspace-bound capture gate, stable event identities, time/area filter policy, secret redaction, and retention policy with focused Application tests.
- [ ] 1.3 Verify disabled capture, workspace identity isolation, UTC interval boundaries, secret redaction, and failure isolation against the acceptance criteria.

## 2. SQLite persistence and compatibility

- [ ] 2.1 Add the next ordered SQLite schema migration for telemetry records, query indexes, Debug Mode and retention settings, defaulting migrated workspaces to capture off and one-day retention.
- [ ] 2.2 Implement parameterized telemetry insert, paged query, export read, confirmed-delete use case support, and expiry cleanup in the Integration persistence layer.
- [ ] 2.3 Add isolated Integration tests for event persistence, time/area filtering, ordering/paging, deletion, retention cleanup while capture is off, and telemetry storage failures.
- [ ] 2.4 Add migration tests from schema 18 and verify older supported workspace packages pass through the same migration chain.
- [ ] 2.5 Update package export/import mapping to exclude telemetry records and request/response bodies; verify ordinary workspace data still round-trips and imported diagnostics start safely.
- [ ] 2.6 Implement sanitized standalone UTF-8 JSON export and test content, redaction, cancellation behavior at the application boundary, and unchanged source records.

## 3. Workspace composition and capture points

- [ ] 3.1 Compose a workspace-bound telemetry runtime and its cleanup timer with the active workspace; dispose and replace it safely when workspace runtime changes.
- [ ] 3.2 Add concise telemetry calls to major first-pass workflow decision points and failure-prone operations across Ideation, Concept, Design, Listing, Workspace, and current external-service flows.
- [ ] 3.3 Instrument OpenRouter and Printify request/response boundaries for method, sanitized endpoint/headers, status/outcome, elapsed time, textual body, and binary body metadata without changing client behavior.
- [ ] 3.4 Add Application/Integration tests proving raw textual bodies remain useful, recognized secrets are absent from database output, failed logging does not alter service results, and active workspace changes do not mix events.

## 4. Settings diagnostics controls

- [ ] 4.1 Add a progressive Diagnostics group to Settings → Workspace with Debug Mode, retention choices, Show Debug Window, time/area filters, Export, and Delete actions.
- [ ] 4.2 Implement active/no-workspace, empty/loading/result/error, save-failure, export-cancel, and delete-confirm/cancel presentation states in the Settings view model.
- [ ] 4.3 Add focused Settings view-model and Avalonia headless view tests for bindings, toggle dependencies, workspace changes, keyboard reachability, query actions, and destructive confirmation.

## 5. Live debug window

- [ ] 5.1 Add a modeless resizable floating telemetry window positioned beside the main app when screen bounds allow, with active-workspace stream binding and close/toggle synchronization.
- [ ] 5.2 Implement Copy for all currently displayed sanitized text and Clear for the in-memory display buffer only; preserve capture and SQLite records after Clear.
- [ ] 5.3 Add Avalonia headless coverage for window state, stream updates, resize state, Copy/Clear commands, and database independence of Clear.
- [ ] 5.4 Add one optional Windows Appium `debug-window` scenario pack using a fresh disposable workspace/database: enable capture, open and resize the window, observe a known event, copy and inspect clipboard text, clear the window, and confirm the stored record remains.

## 6. Criterion verification and completion gates

- [ ] 6.1 Complete `verification.md` with pass/fail evidence for every scenario in the three delta specs, including explicit rationale for any scenario not covered by an automated test.
- [ ] 6.2 Run `openspec validate --strict` and resolve every validation finding.
- [ ] 6.3 Run `dotnet test .\FusionCanvas.sln -m:1` and resolve failures.
- [ ] 6.4 Review changed scope for secret leakage, workspace/package isolation, schema compatibility, Settings focus/state behavior, and telemetry failure side effects.
