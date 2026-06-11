## 1. Solution and Project Setup

- [x] 1.1 Create `FusionCanvas.sln` at the repository root.
- [x] 1.2 Create `src/FusionCanvas.App/FusionCanvas.App.csproj` as an Avalonia desktop application project.
- [x] 1.3 Add the desktop application project to the solution.
- [x] 1.4 Configure the project with stable Avalonia package references and .NET SDK settings.

## 2. Avalonia Application Shell

- [x] 2.1 Add the application entry point and Avalonia startup wiring.
- [x] 2.2 Add `App.axaml` and application initialization code.
- [x] 2.3 Add `MainWindow.axaml` and code-behind for the first FusionCanvas main window.
- [x] 2.4 Implement a static initial shell with a left navigation region and right detail region.
- [x] 2.5 Ensure placeholder content does not require persistent storage or external services.

## 3. Verification

- [x] 3.1 Verify the solution lists the desktop application project.
- [x] 3.2 Run a command-line build for the solution and fix any build errors.
- [x] 3.3 Run the desktop application enough to confirm startup reaches the main window.
- [x] 3.4 Confirm the initial shell does not implement pipeline, persistence, plugin, AI, listing, mockup, or marketplace behavior.
