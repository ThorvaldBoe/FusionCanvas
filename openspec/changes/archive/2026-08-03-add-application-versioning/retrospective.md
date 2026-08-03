# Add Application Versioning Retrospective

## Outcome

FusionCanvas now derives its application version from the repository using Nerdbank.GitVersioning. The same Git commit produces the same version locally and in CI (given full-history checkout), the manually maintained `Major.Minor` lives in a single repository-root `version.json`, a central `Directory.Build.props` applies NB.GV to every Clean Architecture project, the application exposes version information through an Application-layer port consumed by an App-layer adapter, and Settings - About displays the product version plus a copyable diagnostic block (version, short commit id, platform). The `vMajor.Minor.Build` release tag convention and the GitHub Actions `fetch-depth: 0` requirement are documented in `CONTRIBUTING.md`.

## Feedback-Driven Adjustments

No feedback-driven corrections were made during implementation. The issue (#92) was unusually detailed: it specified the versioning mechanism, the version format, the initial `Major.Minor`, the diagnostic block contents, the CI checkout requirement, the tag convention, and explicit non-goals. Discovery resolved the placement of the About surface (Settings section rather than a new dialog), the central dependency mechanism (`Directory.Build.props` rather than Central Package Management), the initial `0.1` milestone, and the trailing-optional-constructor-parameter strategy for preserving existing `SettingsViewModel` call sites. None of these decisions were overturned by later feedback.

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| `Application.Current.Clipboard` exists in Avalonia 12 | Build error: `'Application' does not contain a definition for 'Clipboard'` | Switched to `(Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Clipboard` plus the `ClipboardExtensions.SetTextAsync` extension method | Implementation defect | One-off | None - change-specific code |
| Read assembly metadata directly from the App assembly via `typeof(App).Assembly` | None - decided in design | Used `typeof(AssemblyApplicationVersionProvider).Assembly` as the default so the provider is not coupled to a specific `App` type | One-off preference | Change-specific | None |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: issue #92, the approved proposal/design/specs/tasks, the final code, the build/test/OpenSpec-validation gates, and the Git history of PR #103.
- Promotions completed: none required. The accepted behavior is captured in the new `application-versioning` spec and the `application-settings` modification; they will be synced to `openspec/specs/` during archive. No durable UX/UI/architecture guidance beyond what the specs already encode was identified.
- Deferred promotions: none.

### Reusable lessons

1. **Avalonia 12 clipboard access**: the clipboard is reached through the desktop lifetime's main window, not `Application.Current.Clipboard`. This is a framework-specific implementation detail, not a durable product rule, so it stays in the change-specific retrospective rather than being promoted to UI/UX guidance.
2. **Trailing optional constructor parameters for additive App-layer dependencies**: when a view model has many existing test/offline construction sites, adding a new dependency as a trailing optional parameter with a no-op default keeps the change minimal and the existing call sites compiling. This is consistent with the existing `SettingsViewModel` pattern and is already implicit in the codebase; no promotion needed.
3. **`Directory.Build.props` for a single central build-time dependency**: centralizing one package reference without adopting full Central Package Management is a useful middle ground for build-time-only tooling like NB.GV. This is a structural engineering choice but is specific enough to this change that it does not require a new architecture-guidance rule.
