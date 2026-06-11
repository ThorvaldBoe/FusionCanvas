## Why

FusionCanvas now has architecture guidance that calls for Clean Architecture, SOLID design, and unit testing, but the Visual Studio solution still contains only the Avalonia UI project. The solution should provide the project boundaries and test foundation needed before meaningful domain, application, and integration behavior is added.

## What Changes

- Add separate production projects for the Clean Architecture layers: domain, application, integration, and UI.
- Keep the existing Avalonia app as the UI project while wiring it to depend inward through the application layer.
- Add unit test projects that mirror the production layer projects.
- Add solution organization for `src` and `tests` projects.
- Add minimal placeholder code only where needed to keep projects buildable and testable without introducing product workflow behavior.
- Validate the full solution builds and unit tests run from command-line .NET tooling.

## Capabilities

### New Capabilities

### Modified Capabilities
- `desktop-application-foundation`: Expand the foundation from a single Avalonia project into a buildable Clean Architecture solution with layer projects and unit testing support.

## Impact

- Affects `FusionCanvas.sln`, project files under `src/`, and new test projects under `tests/`.
- Adds test package dependencies for the chosen .NET unit test framework.
- May add project references to enforce inward dependency direction.
- Does not add persistence, product pipeline behavior, plugin loading, AI behavior, marketplace integration, listing workflows, or mockup generation.
