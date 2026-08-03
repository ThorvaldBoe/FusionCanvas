## 1. Repository Version Configuration

- [x] 1.1 Add `version.json` at the repository root with `$schema`, `"version": "0.1"`, `publicReleaseRefSpec` for `main` and `vX.Y.Z` tags, and `cloudBuild.buildNumber.enabled: true`.
- [x] 1.2 Add `Directory.Build.props` at the repository root with a single `PackageReference` to the latest stable `Nerdbank.GitVersioning` (resolved via `dotnet add package`).
- [x] 1.3 Confirm no layer `.csproj` sets a competing `Version`, `VersionPrefix`, `PackageVersion`, or `AssemblyVersion` property.

## 2. Application Versioning Contract

- [x] 2.1 Add `src/FusionCanvas.Application/Versioning/ApplicationVersionInfo.cs` (record with `ProductVersion`, `InformationalVersion`, `CommitId`, `Unknown` static, and `IsCommitKnown`).
- [x] 2.2 Add `src/FusionCanvas.Application/Versioning/IApplicationVersionProvider.cs` with `GetVersion()`.
- [x] 2.3 Add `src/FusionCanvas.Application/Versioning/ApplicationVersionDiagnostics.cs` with pure `TryParse`, pure `Format`, and runtime `BuildPlatformString`.

## 3. App-Layer Provider and Clipboard

- [x] 3.1 Add `src/FusionCanvas.App/Versioning/AssemblyApplicationVersionProvider.cs` reading `AssemblyInformationalVersionAttribute` and delegating parsing to `ApplicationVersionDiagnostics`.
- [x] 3.2 Add `IClipboardService.cs`, `AvaloniaClipboardService.cs` (wraps `Application.Current.Clipboard`, no-ops when unavailable), and `NullClipboardService.cs` under `src/FusionCanvas.App/Versioning/`.

## 4. Settings About Surface

- [x] 4.1 Add `SettingsSection.About`, `IsAboutSection`, and the `About` entry to `Sections`; add trailing optional `IApplicationVersionProvider?` and `IClipboardService?` constructor parameters to `SettingsViewModel` defaulting to the unknown provider and no-op clipboard.
- [x] 4.2 Add `Version`, `DiagnosticsText`, and `CopyDiagnosticsCommand` to `SettingsViewModel` using `ApplicationVersionDiagnostics.Format`.
- [x] 4.3 Add the About pane to `SettingsWindow.axaml` showing the product name, `Version {Binding Version.ProductVersion}`, a read-only diagnostics preview, and the `Copy diagnostics` button.

## 5. Production Wiring

- [x] 5.1 In `AppServicesFactory.Create`, construct `AssemblyApplicationVersionProvider` and `AvaloniaClipboardService` and pass them to `SettingsViewModel`. Leave the offline/test path defaults unchanged.

## 6. Framework-Free Tests

- [x] 6.1 Add `tests/FusionCanvas.Application.Tests/Versioning/ApplicationVersionDiagnosticsTests.cs` covering `+g<commit>` parsing, no-`+` parsing, empty/null parsing, `Format` output, and the unknown-commit case.
- [x] 6.2 Extend `tests/FusionCanvas.App.Tests/Settings/SettingsViewModelTests.cs` with a fake clipboard asserting `CopyDiagnosticsCommand` copies the formatted block and that the About section is selectable.

## 7. Headless View Test

- [x] 7.1 Add a `SettingsWindowTests` case that selects `About` and asserts the product version `TextBlock` shows `Version 0.1.42` and the `Copy diagnostics` button is visible and bound.

## 8. Documentation

- [x] 8.1 Add a "Versioning and releases" section to `CONTRIBUTING.md` documenting `fetch-depth: 0`, the non-canonical status of `github.run_number`, and the `vMajor.Minor.Build` tag convention with aligned release title and artifact filename.

## 9. Completion Gates

- [x] 9.1 Run `dotnet build .\FusionCanvas.sln` in Debug and Release; `dotnet test .\FusionCanvas.sln`; and `openspec validate add-application-versioning --strict`. Resolve any in-scope failures.
- [x] 9.2 Record criterion-level evidence for every acceptance scenario in `verification.md`.
