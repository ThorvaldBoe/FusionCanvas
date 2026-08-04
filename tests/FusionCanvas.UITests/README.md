# FusionCanvas Windows UI Tests

This project is the selected real-desktop UI automation lane. It is intentionally not part of `FusionCanvas.sln`, so the normal deterministic baseline remains:

```powershell
dotnet test .\FusionCanvas.sln
```

## Prerequisites

- Windows 10 or later with Developer Mode enabled.
- An Appium-compatible Windows automation server running locally. The initial harness uses the endpoint `http://127.0.0.1:4723` by default; WinAppDriver is the supported server setup described in the [Avalonia Appium guide](https://docs.avaloniaui.net/docs/testing/ui-testing-with-appium).
- A built `FusionCanvas.App.exe`. The harness defaults to `src\FusionCanvas.App\bin\Debug\net10.0\FusionCanvas.App.exe`; set `FUSIONCANVAS_UI_APP_PATH` to use another build output.

Optional environment settings:

```powershell
$env:FUSIONCANVAS_UI_AUTOMATION_SERVER_URL = 'http://127.0.0.1:4723'
$env:FUSIONCANVAS_UI_APP_PATH = 'C:\path\to\FusionCanvas.App.exe'
```

## Run the smoke journey

Start the Windows automation server in a separate terminal, then run:

```powershell
dotnet test .\tests\FusionCanvas.UITests\FusionCanvas.UITests.csproj --filter "Suite=UiSmoke"
```

Each session passes a unique temporary database, workspace root, and settings path to the compiled app. Those paths are removed after the test; if cleanup fails, the failing test reports the retained temporary root.

If the server is unavailable, the smoke test fails before a journey begins and names this prerequisite. No UI automation test reads or mutates the normal FusionCanvas workspace.
