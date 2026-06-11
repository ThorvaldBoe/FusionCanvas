## Context

FusionCanvas currently contains planning documentation and OpenSpec configuration, but no buildable application code. The project direction already identifies C#, .NET, and Avalonia UI as the intended desktop stack. This change creates the first executable foundation while preserving the repository's local-first, plugin-friendly, and incrementally complex architecture goals.

The first application milestone in the README is a simple desktop shell with a main window, left-side navigation tree, right-side detail panel, placeholder workspace model, and basic project structure. This design supports that milestone without introducing storage, plugin loading, AI, marketplace integrations, or production workflow behavior.

## Goals / Non-Goals

**Goals:**

- Create a Visual Studio solution that can be opened in Visual Studio and built from the command line.
- Add an Avalonia desktop application project under `src/`.
- Establish naming and folder conventions that leave room for future domain, infrastructure, storage, and plugin projects.
- Provide a minimal startup shell with a main window suitable for later workspace navigation.
- Keep the initial app simple enough to build reliably before deeper architecture is added.

**Non-Goals:**

- Implement the product pipeline, Design Triangle, listings, mockups, integrations, AI services, or plugin runtime.
- Add persistent storage or SQLite schema.
- Define final application styling or navigation behavior beyond an initial shell.
- Package installers or release artifacts.
- Introduce complex MVVM infrastructure before the first workflow needs it.

## Decisions

### Use a single Avalonia application project first

Create `src/FusionCanvas.App/FusionCanvas.App.csproj` as the first executable project and include it in `FusionCanvas.sln`.

Rationale: the repository needs a buildable application before internal boundaries are worth splitting into multiple projects. A single app project reduces setup friction and keeps early iteration cheap.

Alternative considered: create separate `Core`, `Infrastructure`, and `Plugin` projects immediately. This better signals the long-term architecture, but it adds project structure before there is enough production code to justify the boundaries.

### Use Avalonia's standard desktop application shape

Use the conventional Avalonia files and startup flow: application entry point, `App`, `MainWindow`, and XAML views. The project should use current stable Avalonia packages available through NuGet at implementation time.

Rationale: following Avalonia conventions makes the app easier for contributors to understand and keeps Visual Studio designer/tooling support viable.

Alternative considered: hand-build a custom host abstraction immediately. That would make later dependency injection and plugin loading explicit, but it is premature for the first window.

### Keep the initial UI as a workspace shell

The main window should present a minimal shell that reflects the planned workspace experience: navigational space on the left and detail/work area on the right. Placeholder data is acceptable if it is clearly local and static.

Rationale: the shell demonstrates the product direction while avoiding feature work that belongs in later specs.

Alternative considered: create a blank Avalonia window only. That validates the build, but it misses the first milestone described in the README.

### Prefer SDK and solution defaults over custom build orchestration

Use standard .NET SDK project files and solution structure. Avoid custom scripts unless needed for repeatable build or run behavior.

Rationale: Visual Studio and `dotnet` compatibility is the primary need for this change.

Alternative considered: add task runners or repository-level build wrappers. These can be useful later, but they are unnecessary while there is only one application project.

## Risks / Trade-offs

- Initial single-project structure could accumulate mixed responsibilities → Mitigation: keep non-UI domain logic minimal and split projects when real domain code appears.
- Avalonia template/package versions may differ by installed SDK or template state → Mitigation: use stable NuGet package references and verify with `dotnet build`.
- A placeholder shell could be mistaken for implemented workspace behavior → Mitigation: keep placeholder content clearly non-persistent and avoid workflow actions.
- Visual Studio solution files can create noisy churn → Mitigation: generate once and keep project naming stable.
