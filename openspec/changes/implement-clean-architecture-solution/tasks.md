## 1. Production Project Structure

- [x] 1.1 Create `src/FusionCanvas.Domain/FusionCanvas.Domain.csproj` as a buildable class library.
- [x] 1.2 Create `src/FusionCanvas.Application/FusionCanvas.Application.csproj` as a buildable class library.
- [x] 1.3 Create `src/FusionCanvas.Integration/FusionCanvas.Integration.csproj` as a buildable class library.
- [x] 1.4 Keep `src/FusionCanvas.App/FusionCanvas.App.csproj` as the Avalonia UI project.
- [x] 1.5 Add only minimal marker code needed for buildability and avoid product workflow placeholders.

## 2. Project References

- [x] 2.1 Reference `FusionCanvas.Domain` from `FusionCanvas.Application`.
- [x] 2.2 Reference inward-facing application or domain projects from `FusionCanvas.Integration`.
- [x] 2.3 Reference inward-facing application or domain projects from `FusionCanvas.App`.
- [x] 2.4 Verify `FusionCanvas.Domain` has no project references.
- [x] 2.5 Verify `FusionCanvas.Application` does not reference `FusionCanvas.Integration` or `FusionCanvas.App`.

## 3. Solution Organization

- [x] 3.1 Add all production projects to `FusionCanvas.sln`.
- [x] 3.2 Add or keep a `src` solution folder containing the production projects.
- [x] 3.3 Add a `tests` solution folder for unit test projects.

## 4. Unit Test Support

- [x] 4.1 Create `tests/FusionCanvas.Domain.Tests/FusionCanvas.Domain.Tests.csproj`.
- [x] 4.2 Create `tests/FusionCanvas.Application.Tests/FusionCanvas.Application.Tests.csproj`.
- [x] 4.3 Create `tests/FusionCanvas.Integration.Tests/FusionCanvas.Integration.Tests.csproj`.
- [x] 4.4 Create `tests/FusionCanvas.App.Tests/FusionCanvas.App.Tests.csproj`.
- [x] 4.5 Configure all test projects with the same .NET unit test framework and .NET test SDK packages.
- [x] 4.6 Reference the matching production project from each test project.
- [x] 4.7 Add at least one lightweight test proving the test infrastructure executes.
- [x] 4.8 Add all test projects to `FusionCanvas.sln` under the `tests` solution folder.

## 5. Validation

- [x] 5.1 Run `dotnet build FusionCanvas.sln`.
- [x] 5.2 Run `dotnet test FusionCanvas.sln`.
- [x] 5.3 Verify solution project listing includes domain, application, integration, app, and all test projects.
- [x] 5.4 Confirm no product pipeline, persistence, plugin loading, AI, marketplace, listing, or mockup behavior was added.
- [x] 5.5 Run OpenSpec status/apply checks for `implement-clean-architecture-solution`.
