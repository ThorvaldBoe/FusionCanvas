# Application Data Environments Verification

## Acceptance scenarios

| Scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Installed application keeps its existing data root | Focused App path resolver test | PASS | `AppDataPathResolverTests.Resolve_WhenProfileIsUnset_PreservesInstalledPaths` verifies the database, workspace-file directory, and settings path remain under `%LOCALAPPDATA%\FusionCanvas`. No migration or copy code was added. |
| Repository Development profile uses an isolated root | Focused App path resolver test and launch-profile inspection | PASS | `Resolve_WhenDevelopmentProfileIsSelected_IsolatesAllDefaultPaths` verifies defaults under `%LOCALAPPDATA%\FusionCanvas-Development`; `src/FusionCanvas.App/Properties/launchSettings.json` selects `Development`. Launch settings are absent from the App build output. |
| A new Development database opens empty and does not touch installed data | Focused App persistence test | PASS | `DevelopmentDatabaseInitializesEmptyWithoutTouchingInstalledData` opens a new profile database, verifies the workspace is empty, saves the empty snapshot to initialize SQLite, and confirms a sentinel in the installed root is unchanged. |
| Explicit data-root override is normalized and selects all default paths | Focused App path resolver test | PASS | `Resolve_WhenDataRootIsExplicit_NormalizesItAndOverridesTheProfileRoot` verifies the full-path override wins over the Development profile. |
| Existing per-path overrides remain available | Focused App path resolver tests | PASS | `Resolve_WhenSpecificPathsAreOverridden_UsesThemAndKeepsOtherPathsUnderSelectedRoot` verifies DB, managed files, and settings overrides; `Resolve_WhenOnlyLegacyDatabaseOverrideIsSet_DerivesWorkspaceFilesBesideDatabase` preserves the previous custom-database fallback. |
| Unknown profile does not fall through to installed storage | Focused App path resolver test | PASS | `Resolve_WhenProfileIsUnknown_FailsBeforeReturningAnyPaths` verifies a misspelled profile throws with the supported profile named. |
| Provider credentials remain shared | Changed-scope inspection | PASS | `NativeAiCredentialStore` and `NativeStorePrintifyCredentialStore` remain unchanged and continue using the OS-protected `FusionCanvas` credential service. No credential copy or migration was added. |

## Automated checks

| Check | Result | Evidence |
| --- | --- | --- |
| App test build | PASS | `dotnet build .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -m:1 --nologo -v quiet` completed with 0 errors. The repository emitted existing compiler/analyzer warnings and NU1900 because NuGet vulnerability metadata was unreachable. |
| Focused profile/path/persistence tests | PASS | `FusionCanvas.App.Tests.exe -class FusionCanvas.App.Tests.Storage.AppDataPathResolverTests -noLogo -noColor`: 7/7 passed. |
| Strict OpenSpec validation | PASS | `openspec validate separate-development-data-environment --strict` reports the change is valid. |
| Solution baseline | PASS | `dotnet test .\FusionCanvas.sln -m:1`: Domain 294/294, Application 713/713, Integration 460/460, App 1,040/1,040, and UI Description 29/29 passed (2,536 total; 0 failed, 0 skipped). Existing NU1900 warnings report that NuGet vulnerability metadata could not be fetched; the test run completed successfully. |

## Changed-scope review

- No installer, SQLite schema, workspace package, credential-store, or installed-user data migration behavior changed.
- The Development launch profile is in source project configuration and was not present in the App build output.
- No interactive desktop check was needed because the change adds no user-facing UI; path selection and persistence boundaries are covered deterministically.
