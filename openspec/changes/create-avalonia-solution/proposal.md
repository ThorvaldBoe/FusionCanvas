## Why

FusionCanvas is ready to move from planning documents into an executable desktop foundation. Creating the Visual Studio solution and initial Avalonia project establishes the buildable application shell needed for future workspace, pipeline, and plugin work.

## What Changes

- Add a Visual Studio solution for the FusionCanvas codebase.
- Add an Avalonia-based desktop application project using C# and .NET.
- Establish an initial source layout under `src/` that can grow into separate application, domain, infrastructure, and plugin-related projects over time.
- Provide a minimal main window suitable for the first workspace shell without implementing product workflow features yet.
- Add baseline build configuration so contributors can restore, build, and run the desktop app from the command line or Visual Studio.

## Capabilities

### New Capabilities
- `desktop-application-foundation`: Defines the requirements for a buildable Avalonia desktop foundation, including solution structure, startup behavior, and initial application shell expectations.

### Modified Capabilities

None.

## Impact

- Affects repository structure by introducing `src/` and the first Visual Studio solution/project files.
- Adds Avalonia and .NET SDK project dependencies.
- Creates the first executable desktop entry point for FusionCanvas.
- Does not add persistence, marketplace integrations, AI services, plugin loading, or product pipeline behavior in this change.
