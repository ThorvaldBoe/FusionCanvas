# QA-225 dependency update plan

Verified 2026-09-03 against `https://api.nuget.org/v3/index.json` with `dotnet list .\FusionCanvas.sln package --outdated` and `--vulnerable --include-transitive`.

No vulnerable packages were reported. The outdated report identifies these planned batches:

1. Runtime persistence/tooling patch updates: Nerdbank.GitVersioning 3.10.94 across projects; Microsoft.Data.Sqlite 10.0.11; SQLitePCLRaw.bundle_e_sqlite3 3.0.5; ktsu.CredentialCache 1.3.34.
2. Avalonia batch: Avalonia, Desktop, Fonts.Inter, Themes.Fluent, and Avalonia.Headless.XUnit 12.1.2. This needs focused headless UI regression verification.
3. Test tooling batch: xUnit v3 and runner 4.0.0, Microsoft.NET.Test.Sdk 18.9.0, and coverlet.collector 10.0.1. This needs the full deterministic suite and coverage/tooling verification.

The batches are intentionally planned separately because the Avalonia and xUnit upgrades are major-version changes with larger framework/test risk. No package versions are changed by this planning finding; each batch should be implemented and reviewed as its own maintenance PR.
