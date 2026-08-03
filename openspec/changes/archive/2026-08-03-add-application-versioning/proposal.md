## Why

FusionCanvas currently has no consistent versioning system. The application does not display its version, builds cannot be traced to a Git commit, and there is no single source of truth that would keep local builds and future GitHub Actions builds in sync. Issue #92 asks for automatic application versioning using Nerdbank.GitVersioning so the same Git commit always produces the same version, the version is visible in the application, and diagnostic information can be copied for bug reports.

## What Changes

- Add `Nerdbank.GitVersioning` as a private build dependency, configured centrally through a repository-root `Directory.Build.props` so every Clean Architecture project receives version-stamped assembly metadata from a single PackageReference.
- Add a repository-root `version.json` that owns the manually maintained `Major.Minor` version (initial `0.1`) plus release-branch and cloud-build configuration.
- Define an application-level versioning abstraction: an `ApplicationVersionInfo` record and an `IApplicationVersionProvider` port in `FusionCanvas.Application`, with pure parsing/formatting helpers for the informational-version string and the copyable diagnostic text.
- Provide a concrete `AssemblyApplicationVersionProvider` in `FusionCanvas.App` that reads Nerdbank.GitVersioning-generated assembly metadata, plus a clipboard abstraction so view models never touch assembly metadata or the clipboard directly.
- Surface the product version and a copyable diagnostic block (version, short commit id, platform) in a new `About` section of the existing Settings window.
- Document the GitHub Actions checkout requirement (`fetch-depth: 0`) and the `vMajor.Minor.Build` release tag convention in the contributor documentation.
- Remove any conflicting manually maintained version properties from the layer projects; the existing `.csproj` files already set none, and the change keeps it that way.

## Capabilities

### New Capabilities

- `application-versioning`: automatic application version generation from Git history, the application-level version information contract, and the copyable diagnostic presentation of version, commit, and platform.

### Modified Capabilities

- `application-settings`: the Settings window gains an `About` section that displays the product version and exposes a copyable diagnostic block.

## Impact

- `version.json` (new, repository root): manual `Major.Minor` source of truth and Nerdbank.GitVersioning release/cloud-build configuration.
- `Directory.Build.props` (new, repository root): a single central `PackageReference` for `Nerdbank.GitVersioning` applied to all projects.
- `src/FusionCanvas.Application/Versioning/`: `ApplicationVersionInfo`, `IApplicationVersionProvider`, and pure diagnostics parsing/formatting helpers.
- `src/FusionCanvas.App/Versioning/`: `AssemblyApplicationVersionProvider`, clipboard abstraction, and production wiring.
- `src/FusionCanvas.App/Settings/`: `SettingsSection.About`, `SettingsViewModel` version projection and copy command, and the `SettingsWindow.axaml` About pane.
- `src/FusionCanvas.App/AppServicesFactory.cs`: compose the production version provider and clipboard service.
- `tests/FusionCanvas.Application.Tests/` and `tests/FusionCanvas.App.Tests/`: framework-free parsing/formatting tests and a headless Settings About view test.
- `CONTRIBUTING.md` (or a focused CI section): document the GitHub Actions checkout configuration and the release tag convention.
- No Domain, Integration, persistence schema, file format, plugin, AI, marketplace, or workflow behavior changes.
- Risks: a CI checkout with shallow history would degrade the build number; Nerdbank.GitVersioning must run during build, so the documentation and the central configuration must require full history for canonical versions. No data or settings migration is required.

## Origin

- Primary issue: #92 — `[Feature]: Implement versioning` (https://github.com/ThorvaldBoe/FusionCanvas/issues/92).
