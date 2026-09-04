## 1. Package branding assets

- [x] 1.1 Copy the supplied logo and banner into repository-owned `FusionCanvas.App` resources without retaining a runtime dependency on `C:\temp\FusionCanvas\`.
- [x] 1.2 Add the desktop icon representation and update `FusionCanvas.App.csproj` so icon metadata and Avalonia resources are included in build and publish output.

## 2. Implement startup presentation

- [x] 2.1 Add a borderless, centered splash window that displays the packaged banner with constrained uniform scaling and no interactive commands.
- [x] 2.2 Assign the packaged FusionCanvas icon to `MainWindow` and preserve the existing window title and shell behavior.
- [x] 2.3 Update application lifetime startup to show the splash before existing composition work, close it after main-window assignment, and close it before propagating startup failures.

## 3. Add focused verification

- [x] 3.1 Add App tests covering resource resolution, icon configuration, splash construction, and successful splash-to-main-window lifecycle behavior at the lowest reliable headless Avalonia layer.
- [x] 3.2 Add a focused failure-path test proving startup failure does not leave the splash as the only visible surface and does not swallow the failure.
- [x] 3.3 Run a supplemental Windows launch smoke check for executable startup and splash-to-main-window transition; interactive visual inspection remains a limitation of this environment.

## 4. Acceptance and regression gates

- [x] 4.1 Verify each scenario in the desktop-application-foundation delta spec and record criterion-level evidence in the change verification record.
- [x] 4.2 Run strict OpenSpec validation and correct any artifact or delta-spec issues.
- [x] 4.3 Run `dotnet test .\FusionCanvas.sln` and confirm the full deterministic baseline passes.
