# Development Data Environment

The repository's `Development` launch profile gives a source-built FusionCanvas its own local data directory. In Visual Studio, start the `Development` profile for `FusionCanvas.App`. From PowerShell, run:

```powershell
dotnet run --project .\src\FusionCanvas.App\FusionCanvas.App.csproj --launch-profile Development
```

The development profile stores its SQLite database, managed workspace files, settings, and AI model caches under:

```text
%LOCALAPPDATA%\FusionCanvas-Development
```

The first run starts with an empty workspace. FusionCanvas does not copy, migrate, or modify data in the installed app's existing `%LOCALAPPDATA%\FusionCanvas` directory. Starting the installed application without a development profile continues to use that existing directory.

For tests or custom isolated runs, set `FUSIONCANVAS_DATA_ROOT` to select a complete data root. The more specific `FUSIONCANVAS_WORKSPACE_DB`, `FUSIONCANVAS_WORKSPACE_ROOT`, and `FUSIONCANVAS_SETTINGS_PATH` variables override their corresponding paths individually. AI model caches follow the selected settings file, and telemetry remains in the selected workspace database.

AI and Printify credentials remain shared through the OS-protected FusionCanvas credential store. The data profile separates local workspace and application files; it does not create separate credentials.
