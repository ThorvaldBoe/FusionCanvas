## Why

Launching FusionCanvas from the repository currently uses the same per-user data folder as the installed application. That makes development runs able to modify the installed app's workspace and settings. Give development its own data root while keeping the installed app on its existing path so current user data remains available.

## What Changes

- Add one application data-root setting that consistently locates the SQLite workspace database, managed workspace files, application settings, and related local caches.
- Configure the repository's development launch profile to use a separate, empty data folder under LocalAppData.
- Keep the installed application's default data folder unchanged for compatibility; do not copy or migrate data automatically.
- Preserve explicit path overrides for tests and isolated runs, and keep OS-protected provider credentials shared as they are today.

## Capabilities

### New Capabilities

- `application-data-environments`: Define how FusionCanvas selects and isolates its local data root for installed and repository development runs.

### Modified Capabilities

None.

## Impact

- Affects startup/path composition in `FusionCanvas.App`, repository development launch configuration, and developer documentation.
- No SQLite schema change, data migration, installer change, or new UI is required.
- Validation covers path selection and isolation in App tests plus the full deterministic solution test baseline.
- UX preflight is not applicable: this adds no user-facing interaction or visual surface.
