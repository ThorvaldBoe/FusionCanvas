## Context

`AppWorkspaceFactory` currently defaults the SQLite database to `%LOCALAPPDATA%\FusionCanvas\workspace.db` and the managed workspace files beside it. `AppSettingsFactory` defaults settings to `%LOCALAPPDATA%\FusionCanvas\settings.json`; `AppServicesFactory` places AI model caches beside settings and telemetry in the workspace database. The installed executable and repository development launch therefore share data. Existing per-path environment overrides are used for tests and isolated runs but do not select a coherent environment.

This change is limited to local application data selection. Installer files remain under Program Files, user data remains per-user, and provider credentials remain in the existing OS credential store.

## Goals / Non-Goals

**Goals:**

- Give repository development runs a dedicated, initially empty local data root.
- Preserve the installed app's current `%LOCALAPPDATA%\FusionCanvas` paths for backward compatibility.
- Resolve the database, workspace files, settings, and AI caches consistently from the selected root.
- Keep existing per-path overrides useful for tests and deliberately isolated runs.
- Make development selection explicit in the repository's supported project launch profile.

**Non-Goals:**

- Copying, moving, migrating, or deleting the existing shared data.
- Changing SQLite schema, workspace package formats, database migrations, or installer behavior.
- Separating provider credentials, changing the OS credential service name, or adding a user-facing environment selector.
- Adding a general multi-profile manager or changing the installed app's default data location.

## Decisions

### Select a named data profile at process launch

Add `FUSIONCANVAS_DATA_PROFILE` with the supported value `Development`. When set to `Development`, resolve the profile root as `%LOCALAPPDATA%\FusionCanvas-Development`. When unset, use the legacy installed root `%LOCALAPPDATA%\FusionCanvas`. Reject an unknown non-empty profile with an actionable startup error so a typo cannot silently select the installed database. Configure the App project's repository launch profile to set `FUSIONCANVAS_DATA_PROFILE=Development`; the installed application does not inherit that project-only launch setting.

Also support `FUSIONCANVAS_DATA_ROOT` as an explicit root override for tests and custom local runs. Normalize it with `Path.GetFullPath`. Root selection precedence is: explicit `FUSIONCANVAS_DATA_ROOT`, named profile, then the legacy installed root.

Alternatives considered: selecting by Debug/Release build configuration is unreliable because build configuration does not identify how a binary is being used; inferring development from its executable location is brittle and fails for published builds; requiring every developer to set three existing path variables is easy to misconfigure and leaves caches/settings behind.

### Resolve all local data paths through one App-layer helper

Add an App composition helper (for example `AppDataPaths`) that resolves the selected root and default paths. Under the root, use `workspace.db`, `workspace-files`, and `settings.json`. Keep AI model caches beside the resolved settings file, preserving the existing layout. Keep telemetry in the resolved workspace database, as it is today.

Existing specific overrides retain highest precedence for the corresponding path: `FUSIONCANVAS_WORKSPACE_DB`, `FUSIONCANVAS_WORKSPACE_ROOT`, and `FUSIONCANVAS_SETTINGS_PATH`. Unspecified paths use the selected data root. When no profile/root is selected, preserve existing fallback behavior for explicit individual overrides, including deriving managed workspace files beside an explicitly configured database.

The helper belongs in `FusionCanvas.App` because it resolves process/environment and user-profile paths at the executable composition boundary. Domain, Application, and Integration persistence contracts do not need to know about development or installed profiles.

### Keep installed data and credentials in place

The installed app continues using the existing LocalAppData directory. No first-run copy or migration is performed. Development starts with an empty database and settings when its new root does not exist. Existing workspace data and files under the shared legacy root remain with the installed app. Provider credentials continue to be shared through the OS credential store, matching the user's decision.

## Risks / Trade-offs

- **[A developer launches the app outside the configured project profile]** → Document the profile and provide a repository-supported launch path; test the profile configuration and path resolver.
- **[An explicit path override points back to the installed root]** → Preserve overrides for test/custom scenarios but document that explicit paths intentionally supersede profile isolation; tests verify normal development profile paths are isolated.
- **[A profile typo selects the legacy root]** → Reject unknown non-empty profile names before composing services or opening storage.
- **[Developers expect their current workspace to appear in development]** → State that development begins empty and existing data remains available in the installed application; copying data is outside this module.

## Migration Plan

No data migration is required. Existing installed builds retain the exact current paths. The first repository development launch creates only the new development directory and initializes its database/settings on use. Rollback consists of removing the development launch profile and helper behavior; the legacy installed root remains untouched throughout.

## Open Questions

None. The user's accepted decisions are: installed data stays at its existing location, development starts empty in a separate LocalAppData directory, no data is copied automatically, and OS credential storage remains shared.

## Implementation Plan

1. Add focused App-layer profile/path resolution that reads `FUSIONCANVAS_DATA_PROFILE` and `FUSIONCANVAS_DATA_ROOT`, rejects unknown profiles, normalizes explicit roots, and resolves database, managed-file, and settings paths with documented override precedence.
2. Update `AppWorkspaceFactory` to consume the resolved database and workspace-file defaults while preserving the current no-profile behavior and explicit `FUSIONCANVAS_WORKSPACE_DB` / `FUSIONCANVAS_WORKSPACE_ROOT` overrides. Confirm `ResolveDefaultDatabasePath` remains the telemetry database path.
3. Update `AppSettingsFactory` and the cache composition in `AppServicesFactory` to use the resolved settings path so development caches stay beneath the development root. Preserve `FUSIONCANVAS_SETTINGS_PATH` override behavior.
4. Add a project launch profile under `src/FusionCanvas.App/Properties/launchSettings.json` that sets `FUSIONCANVAS_DATA_PROFILE=Development`. Confirm the normal project launch workflow honors it and do not add the profile to packaged app runtime configuration.
5. Add App tests for unset/Development/unknown profile, explicit-root normalization, all derived locations, per-path override precedence, and legacy fallback. Use temporary roots and restore process environment after each test.
6. Update concise developer documentation with the development data location, empty-start behavior, installed-data compatibility, explicit override names, and shared-credential boundary.
7. Run focused App tests, `openspec validate separate-development-data-environment --strict`, and `dotnet test .\\FusionCanvas.sln -m:1`; record each acceptance result and any environment limitation.

## Acceptance-to-Verification Mapping

| Scenario | Planned verification |
| --- | --- |
| Installed app uses legacy root and retains its data | App unit test asserts unset profile resolves to current LocalAppData paths; no migration code exists. |
| Development launch uses isolated root | Launch profile inspection plus App path-resolution test asserts all defaults resolve below `FusionCanvas-Development`. |
| New development root starts empty | App composition/persistence test starts with a disposable new development root and verifies a fresh empty workspace/database initialization. |
| Explicit path overrides remain available | App unit tests assert each explicit path wins independently and unspecified paths still use the selected root. |
| Provider credentials remain shared | Changed-scope inspection confirms the credential store service name/behavior is unchanged; no credentials are copied or migrated. |

## Decisions Not to Reopen During Implementation

- Unset profile means the current installed data directory; installed users keep their data in place.
- The repository development profile uses `%LOCALAPPDATA%\FusionCanvas-Development` and starts empty.
- No automatic data migration/copy occurs.
- OS-protected provider credentials remain shared.
- Specific existing path overrides take precedence over the selected profile root.
- No new user-facing environment selector is added.
