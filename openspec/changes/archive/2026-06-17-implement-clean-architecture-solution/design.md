## Context

FusionCanvas currently has a Visual Studio solution with one Avalonia project: `src/FusionCanvas.App/FusionCanvas.App.csproj`. The architecture guidance now expects a Clean Architecture layout with separate domain, application, integration, and UI projects, plus unit test projects that mirror production layers.

This change turns that guidance into solution structure. It should keep the existing app shell working while creating empty-but-buildable project boundaries for future behavior.

## Goals / Non-Goals

**Goals:**
- Add buildable production projects for `FusionCanvas.Domain`, `FusionCanvas.Application`, and `FusionCanvas.Integration`.
- Keep `FusionCanvas.App` as the Avalonia UI project.
- Wire project references so dependencies point inward.
- Add unit test projects for domain, application, integration, and app layers.
- Add at least one lightweight test that proves the test infrastructure runs.
- Ensure `dotnet build` and `dotnet test` work from the solution.

**Non-Goals:**
- Implement product pipeline, persistence, plugin loading, AI, marketplace, listing, mockup, or real workspace behavior.
- Move the existing static Avalonia UI into MVVM or introduce a UI framework beyond Avalonia.
- Add broad abstractions only to make the new projects look populated.
- Choose long-term integration technologies such as SQLite libraries, plugin frameworks, or AI SDKs.

## Decisions

### Create four production layer projects

Create:

- `src/FusionCanvas.Domain/FusionCanvas.Domain.csproj`
- `src/FusionCanvas.Application/FusionCanvas.Application.csproj`
- `src/FusionCanvas.Integration/FusionCanvas.Integration.csproj`
- Keep `src/FusionCanvas.App/FusionCanvas.App.csproj`

Rationale: these names match the architecture guidance and make the solution boundary visible in Visual Studio and command-line tooling.

Alternative considered: use `Infrastructure` instead of `Integration`. The current project guidance names the layer `Integration`, so this change should use that name for consistency.

### Enforce inward references

Use these production references:

```text
FusionCanvas.Application -> FusionCanvas.Domain
FusionCanvas.Integration -> FusionCanvas.Application
FusionCanvas.Integration -> FusionCanvas.Domain, if needed for adapter contracts or domain types
FusionCanvas.App -> FusionCanvas.Application
FusionCanvas.App -> FusionCanvas.Domain, only if needed for presentation of domain types
```

`FusionCanvas.Domain` should have no project references. `FusionCanvas.Application` should not reference integration or UI projects. `FusionCanvas.Integration` should not be referenced by the domain or application projects.

Rationale: this gives contributors a concrete dependency graph that supports testability and provider replacement.

Alternative considered: make the UI reference integration directly for early convenience. That would weaken the clean boundary before real features exist.

### Add mirror test projects under `tests`

Create:

- `tests/FusionCanvas.Domain.Tests/FusionCanvas.Domain.Tests.csproj`
- `tests/FusionCanvas.Application.Tests/FusionCanvas.Application.Tests.csproj`
- `tests/FusionCanvas.Integration.Tests/FusionCanvas.Integration.Tests.csproj`
- `tests/FusionCanvas.App.Tests/FusionCanvas.App.Tests.csproj`

Each test project should reference the matching production project and use a single test framework consistently.

Rationale: mirror test projects make it clear where future feature tests belong.

Alternative considered: create one `FusionCanvas.Tests` project. That is simpler now, but it becomes less clear where layer-specific tests belong as the solution grows.

### Use xUnit for the initial unit test framework

Use xUnit with the .NET test SDK for the initial test projects.

Rationale: xUnit is common in .NET projects, works well with `dotnet test`, and is enough for lightweight unit testing support without choosing heavier test infrastructure.

Alternative considered: MSTest. It is built into many Microsoft templates, but xUnit tends to be concise and familiar for domain and application unit tests.

### Keep placeholders minimal

Add only minimal marker or assembly-level code needed to keep projects meaningful and testable. Avoid placeholder services, repositories, or interfaces that imply product decisions.

Rationale: the purpose of this change is structure, not feature behavior.

Alternative considered: add example service and tests across all layers. That would demonstrate references but risks creating artificial patterns contributors may copy.

## Risks / Trade-offs

- Empty projects may feel like premature structure -> Mitigation: this is explicitly implementing accepted architecture guidance before feature work begins.
- Test dependencies add NuGet packages -> Mitigation: keep dependencies limited to the .NET test SDK, xUnit, and the runner packages.
- Solution file churn can be noisy -> Mitigation: add projects once with stable names and folder placement.
- UI tests can become brittle -> Mitigation: the app test project should begin with smoke-level or non-visual tests only, not full UI automation.

## Migration Plan

1. Create the new production project directories and SDK-style `.csproj` files.
2. Add the project references that enforce inward dependency direction.
3. Create mirror test projects under `tests/`.
4. Add the production and test projects to `FusionCanvas.sln`, grouped under `src` and `tests` solution folders.
5. Add lightweight tests proving the test infrastructure runs.
6. Run `dotnet build FusionCanvas.sln` and `dotnet test FusionCanvas.sln`.

Rollback is straightforward: remove the new project directories, remove their solution entries, and remove any project references added to `FusionCanvas.App`.

## Open Questions

- Should future UI tests remain unit-level in `FusionCanvas.App.Tests`, or should full UI automation eventually live in a separate test project?
