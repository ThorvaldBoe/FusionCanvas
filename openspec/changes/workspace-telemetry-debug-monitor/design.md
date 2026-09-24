## Context

FusionCanvas is a local-first Avalonia desktop app with a SQLite database per workspace. Workspace settings already have a focused Settings section, and current network boundaries include OpenRouter and Printify clients. SQLite schema version 18 is migrated through `SqliteWorkspaceRepository`; workspace packages export a deliberate subset of workspace records.

The feature is opt-in and workspace-scoped. Debug Mode controls capture and database persistence. Show Debug Window is a presentation preference: enabling it enables Debug Mode and opens a modeless monitor; turning Debug Mode off also turns Show Debug Window off and closes the monitor. Hiding the monitor leaves capture enabled. Telemetry is local, expires after the selected age (default one day), and is excluded from workspace packages. Users can make an explicit standalone JSON export.

Primary workflow: during ordinary work or a bug reproduction, the user enables Debug Mode from Settings → Workspace, optionally opens the monitor for immediate feedback, then searches, exports, or deletes records from Settings. These controls are occasional diagnostics and do not occupy the normal creative workspace. The monitor is a separate resizable floating window positioned beside the main app where screen bounds allow. It displays new events in a readable text stream, follows new output, and exposes Copy and Clear. Copy copies all currently displayed text. Clear empties only the monitor's in-memory display buffer; persistence and future capture continue. Closing the monitor is equivalent to turning Show Debug Window off.

## Goals / Non-Goals

**Goals:**

- Capture useful, timestamped, workspace-owned diagnostic events with stable application-area identifiers and event names.
- Persist records in SQLite and support time/area filtering, standalone JSON export, and confirmed deletion.
- Capture request/response bodies at current HTTP integration boundaries while redacting credentials and recognized secret fields before persistence or display.
- Automatically expire records using workspace retention settings and keep expiration working when Debug Mode is off.
- Provide a low-friction live monitor without requiring event call sites to check Debug Mode.
- Preserve old workspace databases through the ordered migration chain and keep telemetry out of package import/export.

**Non-Goals:**

- Remote analytics, crash uploads, or any network transmission of telemetry.
- Capturing telemetry while Debug Mode is off.
- Exhaustive tracing of every method. The first pass instruments major workflow turning points and failure-prone boundaries; later work may add event coverage based on real debugging needs.
- Including telemetry in workspace packages or changing the package format for diagnostics.
- Replacing user-facing workflow errors with telemetry output.

## Decisions

1. **Workspace-owned records and settings.** Add telemetry records and workspace diagnostics settings to the workspace SQLite schema (next schema version). This follows the requested workspace scope and keeps one workspace's setting and diagnostic history separate from another's. Keep the current application-wide appearance/AI preference document unchanged.

2. **Application-facing telemetry contract; Integration persistence.** Define event/query/export/settings contracts in `FusionCanvas.Application.Telemetry`; implement them in Integration with parameterized SQLite operations. Compose one workspace-aware telemetry runtime with each active workspace. The runtime checks that workspace's Debug Mode before accepting an event, so callers make a concise call without duplicating conditionals. Avoid process-global mutable workspace state so workspace switches and tests remain explicit.

3. **Structured events with stable area and event identities.** Store UTC occurrence time, event ID, area ID, event name, severity/outcome, optional correlation ID, concise message, structured metadata, and optional HTTP request/response details. Areas are stable identifiers (for example `Ideation`, `Concept`, `Design`, `Listing`, `Workspace`, `Integration.OpenRouter`, and `Integration.Printify`); display labels may change independently. Index time and area for filtered queries. Queries accept inclusive start/exclusive end timestamps, one or more areas, and bounded paging, ordered newest first by default.

4. **Capture service exchanges at client boundaries.** Instrument the existing OpenRouter and Printify HTTP client send/parse boundaries rather than adding a generic handler that buffers every stream. This retains the actual request/response details available to each client and avoids consuming response streams twice. Record method, endpoint without secret query parameters, status, elapsed time, outcome, headers after redaction, and textual bodies. Preserve the current external client behavior if telemetry recording fails. For binary content, record content type and byte length without storing binary bytes; textual JSON bodies, including JSON-encoded image data, are eligible for capture subject to the clients' existing response limits.

5. **Redact before fan-out.** One shared sanitizer prepares an event before it reaches either SQLite or the live window. It removes authorization/cookie headers and redacts recognized credential properties and values (including API keys, tokens, passwords, and secrets) in URLs and structured JSON. It does not claim to identify arbitrary sensitive prose in prompts, generated text, or provider payloads. Debug Mode is explicitly opt-in, data stays local, and retention defaults to one day; the Settings copy and export action make the local capture scope clear. Do not emit credential values from the sanitizer itself.

6. **Retention is age-based and independent from capture.** Store a per-workspace retention enum with values one hour, one day, and one week; default new/older workspaces to one day. Delete entries whose occurrence timestamp is older than the selected duration on startup, immediately after retention changes, after writes, and on a five-minute maintenance interval while the workspace runtime is active. Turning capture off does not disable expiration. If the workspace database is unavailable, normal application work continues and cleanup retries on the next eligible pass.

7. **Settings owns record management; the monitor is a focused modeless window.** Add a compact Diagnostics group to the Workspace section for Debug Mode, retention, Show Debug Window, time/area filters, Export telemetry, and Delete telemetry. Disable workspace-specific actions when there is no active workspace and explain that state. Export all retained records for the active workspace as UTF-8 JSON through the platform save dialog. Delete removes all retained records for that workspace after explicit confirmation; deletion does not disable capture. Avoid adding persistent controls to the main shell.

8. **Monitor actions are view-only except visibility.** The monitor receives sanitized events only after accepted capture and displays a live text representation of each event. Copy places the entire current display buffer on the clipboard. Clear empties that buffer only; it does not call the persistence delete operation, stop capture, or affect later events. Window close updates Show Debug Window off. The window is modeless, resizable, and associated with the active workspace; when the active workspace changes, it follows the newly selected workspace's capture and visibility settings and never displays events from the previous workspace after the switch completes.

9. **Workspace transfer deliberately omits diagnostics.** The portable package writer continues exporting the existing workspace content but does not serialize telemetry records. Import does not restore telemetry records or Debug Mode state from a package. Imported workspaces start with Debug Mode and Show Debug Window off and one-day retention. This keeps raw diagnostic payloads out of routine sharing and avoids unexpectedly capturing data immediately after an import.

10. **Headless UI verification is required; one desktop journey is supplemental.** Settings bindings and commands, monitor rendering state, and copy/clear effects receive deterministic Avalonia headless coverage. One optional Windows Appium journey is warranted for native modeless-window placement/resizing and clipboard interaction: one fresh disposable workspace for the `debug-window` scenario pack; enable Debug Mode, show the window, observe a known emitted event, resize it, copy and verify its text, clear it and verify the stored record remains. It supplements the deterministic baseline and is not a module completion gate.

## Risks / Trade-offs

- **Raw payloads can contain private creative or business content** → Debug Mode is off by default, bodies remain local, recognized secrets are redacted before all outputs, retention defaults to one day, and export is explicit. Arbitrary sensitive prose cannot be reliably classified; make this limitation visible in Settings.
- **Telemetry may grow quickly during long or repeated operations** → use retention cleanup, bounded query pages, existing client response limits, and no binary-body storage. Track text-body volume during verification; if client limits still permit problematic database growth, add a capture-size cap as a separately reviewed follow-up rather than silently truncating accepted captures.
- **Logging must not make an otherwise successful operation fail** → make telemetry persistence best-effort, avoid recursive telemetry-on-telemetry errors, and keep HTTP results independent from diagnostic writes.
- **Concurrent events and active-workspace changes could mix or reorder records** → bind the telemetry runtime to a workspace identity, use a serialized/batched write path, and tag display events with workspace identity internally; close/clear the old view on workspace transition.
- **Migration and package compatibility** → add an ordered migration from schema 18, test both a v18 local database and an older package through the shared chain, and keep package serialization's explicit table selection excluding telemetry.
- **Clipboard or native-window APIs differ by platform** → keep commands behind existing App services/contracts and test them headlessly with fakes; use the optional Windows scenario only for native behavior.

## Migration Plan

1. Add the telemetry table, indexes, and workspace diagnostics settings to the next SQLite schema migration. Existing schema-18 databases migrate with Debug Mode off, Show Debug Window off, and one-day retention.
2. Make package import use the existing migration chain and package export/import selection explicitly omit telemetry rows. No workspace package format change is needed.
3. Compose telemetry after an active workspace database is opened and dispose its timer/stream when that workspace runtime is replaced.
4. Rollback uses the normal application rollback path; migrated telemetry tables/settings may remain unused by an older executable. Never drop captured diagnostics during downgrade.

## Open Questions

- No product decisions remain open. During implementation, confirm current HTTP client response limits and workspace package serialization paths; keep the agreed behavior if implementation details differ.

## Implementation Plan

### Affected layers and likely locations

- **Application:** add cohesive `Telemetry/` contracts and records for `ITelemetryService`, settings, `TelemetryEntry`, query filters/results, and export/delete operations. Keep contracts independent of Avalonia and SQLite.
- **Integration:** extend `SqliteDatabaseSchema` from version 18 and `SqliteWorkspaceRepository` migration infrastructure; add a dedicated SQLite telemetry store/query adapter in `Persistence/Telemetry/`; add JSON export formatting and shared secret redaction. Keep parameterized SQL and use transactions for bulk deletion/expiry.
- **App composition/runtime:** wire a workspace-bound telemetry service in `AppWorkspaceFactory`/`AppServicesFactory` (confirm exact composition route); attach/dispose it as `MainWindowViewModel` switches workspace. Add concise calls in major workflow entry points and current OpenRouter/Printify clients.
- **App settings:** extend `SettingsViewModel`/Workspace pane and add the Diagnostics group, time and area filters, export/delete commands, empty/no-workspace/error states, and retention feedback.
- **App debug window:** add a dedicated Avalonia window/view model for the live display buffer, resize behavior, clipboard copy, local clear, close synchronization, and active-workspace switching. Open next to the main window within available screen bounds.
- **Workspace transfer:** update package snapshot/serialization mappings to omit diagnostics on export/import; test that ordinary workspace records still round-trip.

### Data and processing

- A log call creates a typed event with stable area/event identifiers and UTC time. The workspace-bound service checks capture state once, sanitizes secrets, then fans the same safe event to SQLite and the live stream. If disabled, it returns without persistence or UI work.
- HTTP instrumentation captures request/response text at existing client boundaries with method, sanitized endpoint/headers, status, elapsed time, and outcome. Preserve request/response bodies already available to the client. Capture content type/length for binary responses without storing binary content.
- Query by `[from, to)` and optional area set; apply stable newest-first ordering and bounded paging. Export retained records to standalone UTF-8 JSON. Delete all workspace telemetry only after confirmation.
- Retention cleanup uses UTC occurrence time and selected duration. Run on database open, after setting changes, after successful writes, and every five minutes. Cleanup errors are surfaced in Settings when user-triggered and are otherwise retried without breaking user workflows.
- A monitor clear operation only resets the in-memory presentation buffer. Clipboard copy serializes exactly the currently displayed buffer in order.

### UX and interaction states

- Diagnostics appears in Settings → Workspace so the active workspace scope is always visible. With no workspace, show `No workspace` and disable Debug Mode, retention, search, export, and delete; Show Debug Window is unavailable.
- Debug Mode defaults off. Turning it on starts capture immediately. Show Debug Window defaults off; turning it on first enables Debug Mode, persists both settings, and opens a modeless window. Turning Debug Mode off closes the window and clears Show Debug Window. Hiding/closing the window leaves Debug Mode on.
- Search offers start/end time and application-area selection, a Search action, loading state, results, and an empty state. Export operates on all retained records in the active workspace, independent of current filters. A failed export preserves records and reports an actionable error. Delete all uses an explicit confirmation naming the active workspace; cancel leaves records unchanged.
- No unsaved drafts are involved. Settings changes persist immediately. If preference persistence fails, report the failure and reconcile window visibility with the actual persisted/runtime state. Copy reports success/failure without changing telemetry. Clear asks no confirmation because it changes only the disposable view buffer.
- Focus returns to Show Debug Window in Settings when the monitor closes; Copy/Clear remain keyboard reachable. Window geometry persistence is not part of this module.

### Sequence

1. Define event, setting, query, export, redaction, and workspace-bound Application contracts with focused unit tests.
2. Add schema migration, SQLite persistence/query/retention/export adapters, package exclusion, and isolated Integration tests including v18 migration and older package import.
3. Compose workspace runtime and event fan-out; instrument core workflow turning points and OpenRouter/Printify HTTP boundaries; verify disabled capture, failure isolation, sanitization, and active-workspace separation.
4. Add Settings controls and telemetry management workflows with view-model and Avalonia headless view coverage.
5. Add modeless monitor and copy/clear/resize behavior with headless coverage plus the optional single Windows Appium scenario pack.
6. Map all acceptance scenarios to verification, run `openspec validate --strict`, then run `dotnet test .\FusionCanvas.sln -m:1`.

### Acceptance-to-verification plan

| Spec scenario | Planned verification |
|---|---|
| Telemetry disabled by default | Application service unit test; settings view-model/headless binding test |
| Workspace event capture and isolation | Application tests with deterministic runtime; SQLite integration query |
| Stable workflow areas and service details | Client tests using fake HTTP handlers; assert event metadata/body without secrets |
| Secret redaction before storage and display | Sanitizer unit tests and integration persistence/readback tests |
| Time and area search | Application query tests and SQLite integration tests for boundaries/order/paging |
| Retention options/default/expiry | Domain or Application policy tests; SQLite integration cleanup tests for each age and off-state cleanup |
| Export and delete management | JSON codec and repository tests; Settings VM/headless confirm/cancel tests |
| Package excludes telemetry | Workspace transfer integration test for export and import |
| Show window enables Debug Mode and closes when disabled | Settings VM and Avalonia headless view tests |
| Window renders live events and resizes | Avalonia headless view test for stream and size state; optional Appium native resize journey |
| Copy and clear window content | Clipboard fake plus headless command tests; assert database unchanged after clear; optional Appium clipboard journey |
| Preference/write/storage failure states | Application tests with failing store, App settings VM tests, persistence failure integration test |

The Appium pack contains one shared-state journey with a fresh disposable workspace/database per pack. It is optional Windows evidence; all required criteria remain covered by deterministic tests.
