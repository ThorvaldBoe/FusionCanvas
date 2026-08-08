# Verification: remember-window-layout

## Criterion Results

| Criterion / scenarios | Result | Evidence |
| --- | --- | --- |
| Versioned optional layout round-trip | Pass | `JsonApplicationSettingsStoreTests.Version3_RoundTripsWindowLayout`; version 3 JSON write/read verified. |
| Legacy versions and missing layout preserve defaults | Pass | `JsonApplicationSettingsStoreTests.LoadAsync_LegacyVersionDefaultsWindowLayout` plus existing missing-file/version tests. |
| Malformed/partial layout does not discard readable appearance | Pass | `JsonApplicationSettingsStoreTests.LoadAsync_InvalidWindowLayoutPreservesReadableSettings`; invalid layout returns a warning and null layout while Dark mode remains true. |
| Finite positive dimensions and splitter bounds | Pass | `MainWindowLayoutNormalizerTests.TryNormalize_RejectsInvalidOrUnsupportedValues`; valid normalization tests. |
| Off-screen and changed-screen placement remains usable | Pass | `MainWindowLayoutNormalizerTests.TryNormalize_ClampsLargeWindowAndOffScreenPosition`; scaling-aware screen test also passes. |
| Valid layout restores after window creation | Pass by implementation and helper coverage | `MainWindow.OnWindowOpened` applies normalized width, height, position, and splitter width after `Opened`; deterministic normalizer tests cover the algorithm. |
| Maximized/fullscreen bounds are not captured | Pass | `MainWindowLayoutNormalizerTests.TryCapture_IgnoresPlatformManagedWindowStates`. |
| Latest layout uses existing settings save/flush path | Pass | `SettingsViewModelTests.UpdateWindowLayout_QueuesLatestLayoutForSave`; `MainWindow.OnWindowClosing` merges the final normal snapshot before app shutdown flush. |
| Save failure leaves session usable | Pass by existing save contract and unchanged error handling | Existing settings save-failure tests pass; layout update uses the same non-throwing queued save path. |
| Deterministic relevant tests | Pass | Integration: 176/176. Focused App settings/layout: 36/36. |
| Full solution baseline | Partial / blocked by unrelated existing failures | Domain 219/219, Application 357/357, Integration 176/176. App suite 474 passed / 6 failed in pre-existing multi-selection/tree and store-editor tests outside this change. |

## Commands

- `dotnet restore .\\FusionCanvas.sln` — passed.
- `dotnet test .\\tests\\FusionCanvas.Integration.Tests\\FusionCanvas.Integration.Tests.csproj --no-restore --verbosity:minimal` — passed, 176/176.
- `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore --filter "FullyQualifiedName~MainWindowLayoutNormalizerTests|FullyQualifiedName~SettingsViewModelTests" --verbosity:minimal` — passed, 36/36.
- `dotnet test .\\FusionCanvas.sln --no-restore --verbosity:minimal` — failed only on six unrelated existing App tests; changed-scope tests passed.
- `openspec validate remember-window-layout --type change --strict --no-interactive --json` — passed before implementation; rerun after final artifact updates is required before completion.

## Limitations

- No live multi-monitor verification was performed in this non-interactive environment. Native validation of monitor removal and differing display scales remains supplemental follow-up evidence.
- The full App baseline remains red because of unrelated failures in `WorkspaceTreeMultiSelectionTests`, `WorkspaceTreeViewModelTests`, `MainWindowConstructionTests`, and `StoreEditorHeadlessTests`; none involve the changed files or new tests.
