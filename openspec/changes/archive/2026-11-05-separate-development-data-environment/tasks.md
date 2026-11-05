## 1. Resolve application data profiles

- [x] 1.1 Add App-layer data-path resolution for the Development profile, explicit data-root override, legacy installed fallback, invalid-profile handling, and existing per-path override precedence.
- [x] 1.2 Update workspace database, managed workspace-file, settings, telemetry, and AI-cache composition to use resolved paths without changing the installed fallback.
- [x] 1.3 Configure the repository App project launch profile to select Development without adding environment settings to packaged runtime output.

## 2. Verify isolation and document local behavior

- [x] 2.1 Add focused App tests for installed fallback, Development profile paths, unknown profiles, explicit-root normalization, explicit path overrides, and empty development database initialization.
- [x] 2.2 Update developer documentation with the Development data location, empty-start behavior, installed-data compatibility, supported overrides, and shared OS credential behavior.
- [x] 2.3 Verify the installed fallback remains `%LOCALAPPDATA%\FusionCanvas`, while repository Development launches resolve all local workspace/settings/cache paths under `%LOCALAPPDATA%\FusionCanvas-Development` and do not copy or modify installed data.

## 3. Acceptance verification

- [x] 3.1 Map every application-data-environments scenario to focused test or inspection evidence in `verification.md`.
- [x] 3.2 Run `openspec validate separate-development-data-environment --strict` and correct all reported issues.
- [x] 3.3 Run `dotnet test .\FusionCanvas.sln -m:1` and record the deterministic solution-baseline result and any environment limitation in `verification.md`.
- [x] 3.4 Confirm the changed-scope review finds no installer, schema, credential-store, or existing user-data migration changes.
