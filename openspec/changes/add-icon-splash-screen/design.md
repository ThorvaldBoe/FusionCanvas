## Context

`FusionCanvas.App` currently loads application resources in `App.Initialize`, performs synchronous composition in `App.OnFrameworkInitializationCompleted`, and assigns a constructed `MainWindow` to the classic desktop lifetime. The application has no repository-owned branding assets or startup surface. The supplied square PNG is suitable as the visual logo, while the wide JPG is suitable for a centered, borderless splash window.

The existing startup path is local-first and may perform settings, SQLite workspace, and bundled-library initialization before the main window is ready. The splash therefore represents actual startup work; it is not a marketing screen or fixed-delay animation.

## Goals / Non-Goals

**Goals:**

- Make the built application identifiable through its executable/window icon.
- Show the supplied FusionCanvas banner during startup composition.
- Close the splash based on readiness of the main window rather than elapsed time.
- Keep all assets inside the repository and application package.
- Preserve existing workspace startup behavior and keep the change confined to the UI composition root.
- Cover resource and lifecycle behavior with deterministic tests where framework behavior is material.

**Non-Goals:**

- Progress reporting, animated transitions, or a minimum splash duration.
- Changes to workspace persistence, settings, AI, or startup task semantics.
- A first-run wizard or branded loading experience after the main window is usable.
- Replacing the supplied artwork or redesigning the application shell.

## Decisions

### Use repository-owned packaged resources

Copy the two supplied images into an application asset directory and reference them through Avalonia resource URIs. The temporary `C:\temp\FusionCanvas\` location is source material only and must never be a runtime dependency.

For the executable/application icon, add a repository-owned `.ico` derived from the square logo if the Windows packaging toolchain requires ICO metadata, while retaining the PNG for Avalonia window/resource use. This avoids relying on a platform conversion at runtime.

### Use a dedicated borderless splash window

Create a small UI-owned splash window with no taskbar ownership, system chrome, or interactive commands. It displays the banner with uniform scaling, a neutral background, and a constrained size so the wide source image does not dictate the main application's window dimensions. The splash is shown before the main window is assigned, then closed immediately after the main window is created and assigned.

Alternative considered: place the banner in the first main-window page. Rejected because the issue requests a startup splash and the existing foundation explicitly keeps the initial workspace shell sparse.

Alternative considered: use a fixed timer. Rejected because it makes startup slower on fast machines and can be too short on slow startup paths; readiness is the meaningful boundary.

### Keep composition behavior in the existing startup path

The splash coordinates the current synchronous startup path rather than moving workspace initialization into a new service or introducing a general startup orchestration abstraction. `App.OnFrameworkInitializationCompleted` will create/show the splash, execute the existing composition, assign/show the main window, and close the splash in both success and failure paths.

If composition fails, the splash must close before the exception is surfaced through the existing application failure path. The implementation must not swallow the exception or invent a second persistence/retry mechanism.

### Icon assignment has two layers

Set the project/application icon metadata for packaged desktop output and assign the same visual identity to the Avalonia main window. Platform-specific metadata belongs in the App project; no Domain, Application, or Integration project changes are expected.

## Risks / Trade-offs

- [Risk] Showing a window before `MainWindow` may expose platform-specific lifetime behavior. → Mitigation: keep the splash owned by the classic desktop lifetime, close it before failure propagation, and add a headless construction/lifecycle test where Avalonia permits.
- [Risk] The banner is a large JPG and may increase application size. → Mitigation: use the supplied asset without runtime conversion and verify published output; do not add a second copy or dynamic image processing path.
- [Risk] The PNG's dark/black background may look different against the splash background. → Mitigation: use a matching dark neutral splash background and uniform, centered image presentation; visual QA should verify the result on Windows.
- [Risk] A startup exception can leave a transient splash visible. → Mitigation: close it in a `finally` path and preserve the existing exception/reporting semantics.
- [Risk] ICO conversion may lose visual fidelity at small sizes. → Mitigation: include standard desktop icon sizes generated from the square source and verify the built executable/window icon visually.

## Migration Plan

No data or settings migration is required. Add the packaged assets and startup presentation code, then verify a debug launch and a published output directory. Rollback is a code/resource revert.

## Open Questions

None for the bounded first module. The implementation should not reopen splash timing, user interaction, progress reporting, or branding artwork decisions.

## Implementation Plan

1. Add repository-owned copies of the logo and banner under the App project's resource directory, plus the required desktop icon representation, and update `FusionCanvas.App.csproj` resource/icon metadata.
2. Add a focused splash window/view using the existing semantic theme resources and the packaged banner. Keep it borderless, non-interactive, centered, constrained, and compiled-binding compatible.
3. Update `App.OnFrameworkInitializationCompleted` so the splash is shown before existing service/main-window composition, closed after successful main-window assignment, and closed before propagating startup failures.
4. Assign the packaged icon to `MainWindow` and confirm the project-level icon is present in build/publish output.
5. Add focused App tests for resource presence/configuration and splash/main-window lifecycle behavior at the lowest reliable Avalonia headless layer. Add a supplemental Windows visual check for executable icon, taskbar/window icon, splash scaling, and failure cleanup.
6. Run strict OpenSpec validation and the full `dotnet test .\FusionCanvas.sln` baseline.

### Acceptance-to-verification mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Contributor runs the app | Headless App lifecycle test plus supplemental Windows launch check |
| Startup completes | Headless test asserts splash closes after main-window assignment and no timer dependency exists in the composition path |
| Packaged application runs without source assets | Build/publish output inspection and resource-resolution test |
| Startup fails before the main window is ready | Focused composition failure test asserting splash cleanup; existing exception path remains observable |
