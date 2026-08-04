# FusionCanvas Windows UI Tests

This project is the selected real-desktop UI automation lane. It is intentionally not part of `FusionCanvas.sln`, so the normal deterministic baseline remains:

```powershell
dotnet test .\FusionCanvas.sln
```

## Prerequisites

- Windows 10 or later with Developer Mode enabled.
- Appium and its Windows driver running locally as Administrator. Install once with:

  ```powershell
  npm install -g appium
  appium driver install windows
  appium driver run windows install-wad
  ```

  Start the server before running tests:

  ```powershell
  appium --address 127.0.0.1 --port 4723
  ```

  The Windows driver installs the required WinAppDriver binary with `install-wad`. The harness uses `http://127.0.0.1:4723` by default. See the [Appium Windows driver documentation](https://appium.io/docs/en/3.2/ecosystem/drivers/) for driver setup.
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

If the Appium Windows server is unavailable, the smoke test fails before a journey begins and names this prerequisite. No UI automation test reads or mutates the normal FusionCanvas workspace.
