# QA-225 dependency update plan

Verified 2026-09-03 against `https://api.nuget.org/v3/index.json` with `dotnet list .\FusionCanvas.sln package --outdated` and `--vulnerable --include-transitive`.

No vulnerable packages were reported. The outdated report identifies these planned batches:

1. Runtime persistence/tooling patch updates: Nerdbank.GitVersioning 3.10.94 across projects; Microsoft.Data.Sqlite 10.0.11; SQLitePCLRaw.bundle_e_sqlite3 3.0.5; ktsu.CredentialCache 1.3.34.
2. Avalonia batch: Avalonia, Desktop, Fonts.Inter, Themes.Fluent, and Avalonia.Headless.XUnit 12.1.2. This needs focused headless UI regression verification.
3. Test tooling batch: xUnit v3 and runner 4.0.0, Microsoft.NET.Test.Sdk 18.9.0, and coverlet.collector 10.0.1. This needs the full deterministic suite and coverage/tooling verification.

The batches are intentionally planned separately because the Avalonia and xUnit upgrades are major-version changes with larger framework/test risk. No package versions are changed by this planning finding; each batch should be implemented and reviewed as its own maintenance PR.

## 2026-10-02 triage for #694

The fresh NuGet report was triaged against the current `main` revision. The bounded patch-level batch was applied:

- `Nerdbank.GitVersioning` `3.10.91` → `3.10.94` in `Directory.Build.props`.
- `Microsoft.Data.Sqlite` `10.0.10` → `10.0.12`.
- `SQLitePCLRaw.bundle_e_sqlite3` `3.0.3` → `3.0.5`.
- `ktsu.CredentialCache` `1.3.18` → `1.3.59`.

The following updates remain deliberately deferred to separate compatibility batches:

- Avalonia and `Avalonia.Headless.XUnit` `12.0.4` → `12.1.3`; this is a framework/minor upgrade requiring dedicated headless UI regression review.
- `SixLabors.ImageSharp` `3.1.12` → `4.1.2`; this is a major upgrade requiring API and image-processing compatibility review.
- `xunit.v3`/runner, `Microsoft.NET.Test.Sdk`, and `coverlet.collector`; these cross major test-tooling boundaries and require an isolated test-infrastructure batch.

Verification on 2026-10-02 used `dotnet` SDK `10.0.401` and the NuGet v3 feed:

- Vulnerable package scan: no vulnerable packages reported.
- Deprecated package scan: no deprecated packages reported.
- Post-update outdated scan: only the deferred Avalonia, ImageSharp, and test-tooling families remained.
- Solution build: passed with 0 errors.
- Domain tests: 265 passed; Application tests: 555 passed; Integration tests: 301 passed; UiDescription tests: 29 passed.
- Focused App Settings/UI credential tests: 68 passed.
- The complete solution test command reached `FusionCanvas.App.Tests`, where the test process crashed with exit code `-1073741571` after 257 tests; the remaining UiDescription tests passed. This is retained as a verification limitation for the existing App test lane, not used as evidence to justify additional package upgrades.
