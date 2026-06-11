## 1. Solution and Project Setup

- [ ] 1.1 Create `FusionCanvas.sln` at the repository root.
- [ ] 1.2 Create `src/FusionCanvas.App/FusionCanvas.App.csproj` as an Avalonia desktop application project.
- [ ] 1.3 Add the desktop application project to the solution.
- [ ] 1.4 Configure the project with stable Avalonia package references and .NET SDK settings.

## 2. Avalonia Application Shell

- [ ] 2.1 Add the application entry point and Avalonia startup wiring.
- [ ] 2.2 Add `App.axaml` and application initialization code.
- [ ] 2.3 Add `MainWindow.axaml` and code-behind for the first FusionCanvas main window.
- [ ] 2.4 Implement a static initial shell with a left navigation region and right detail region.
- [ ] 2.5 Ensure placeholder content does not require persistent storage or external services.

## 3. Verification

- [ ] 3.1 Verify the solution lists the desktop application project.
- [ ] 3.2 Run a command-line build for the solution and fix any build errors.
- [ ] 3.3 Run the desktop application enough to confirm startup reaches the main window.
- [ ] 3.4 Confirm the initial shell does not implement pipeline, persistence, plugin, AI, listing, mockup, or marketplace behavior.
