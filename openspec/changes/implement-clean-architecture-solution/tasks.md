## 1. Production Project Structure

- [ ] 1.1 Create `src/FusionCanvas.Domain/FusionCanvas.Domain.csproj` as a buildable class library.
- [ ] 1.2 Create `src/FusionCanvas.Application/FusionCanvas.Application.csproj` as a buildable class library.
- [ ] 1.3 Create `src/FusionCanvas.Integration/FusionCanvas.Integration.csproj` as a buildable class library.
- [ ] 1.4 Keep `src/FusionCanvas.App/FusionCanvas.App.csproj` as the Avalonia UI project.
- [ ] 1.5 Add only minimal marker code needed for buildability and avoid product workflow placeholders.

## 2. Project References

- [ ] 2.1 Reference `FusionCanvas.Domain` from `FusionCanvas.Application`.
- [ ] 2.2 Reference inward-facing application or domain projects from `FusionCanvas.Integration`.
- [ ] 2.3 Reference inward-facing application or domain projects from `FusionCanvas.App`.
- [ ] 2.4 Verify `FusionCanvas.Domain` has no project references.
- [ ] 2.5 Verify `FusionCanvas.Application` does not reference `FusionCanvas.Integration` or `FusionCanvas.App`.

## 3. Solution Organization

- [ ] 3.1 Add all production projects to `FusionCanvas.sln`.
- [ ] 3.2 Add or keep a `src` solution folder containing the production projects.
- [ ] 3.3 Add a `tests` solution folder for unit test projects.

## 4. Unit Test Support

- [ ] 4.1 Create `tests/FusionCanvas.Domain.Tests/FusionCanvas.Domain.Tests.csproj`.
- [ ] 4.2 Create `tests/FusionCanvas.Application.Tests/FusionCanvas.Application.Tests.csproj`.
- [ ] 4.3 Create `tests/FusionCanvas.Integration.Tests/FusionCanvas.Integration.Tests.csproj`.
- [ ] 4.4 Create `tests/FusionCanvas.App.Tests/FusionCanvas.App.Tests.csproj`.
- [ ] 4.5 Configure all test projects with the same .NET unit test framework and .NET test SDK packages.
- [ ] 4.6 Reference the matching production project from each test project.
- [ ] 4.7 Add at least one lightweight test proving the test infrastructure executes.
- [ ] 4.8 Add all test projects to `FusionCanvas.sln` under the `tests` solution folder.

## 5. Validation

- [ ] 5.1 Run `dotnet build FusionCanvas.sln`.
- [ ] 5.2 Run `dotnet test FusionCanvas.sln`.
- [ ] 5.3 Verify solution project listing includes domain, application, integration, app, and all test projects.
- [ ] 5.4 Confirm no product pipeline, persistence, plugin loading, AI, marketplace, listing, or mockup behavior was added.
- [ ] 5.5 Run OpenSpec status/apply checks for `implement-clean-architecture-solution`.
