## MODIFIED Requirements

### Requirement: Visual Studio solution is present
The repository SHALL include a Visual Studio solution for FusionCanvas that contains the Clean Architecture production projects and matching unit test projects.

#### Scenario: Contributor opens the solution
- **WHEN** a contributor opens the FusionCanvas solution in Visual Studio
- **THEN** the solution includes projects for the domain, application, integration, and UI layers
- **AND** the initial desktop application project is available as the UI project

#### Scenario: Contributor lists solution projects
- **WHEN** a contributor lists projects in the solution using .NET tooling
- **THEN** the domain, application, integration, UI, and unit test projects are included

### Requirement: Avalonia desktop application project builds
The repository SHALL include an Avalonia desktop application project that builds successfully with the .NET SDK as part of the Clean Architecture solution.

#### Scenario: Contributor builds the app
- **WHEN** a contributor runs a command-line build for the solution
- **THEN** the build completes successfully without requiring external services
- **AND** all production layer projects compile

#### Scenario: Project declares Avalonia support
- **WHEN** a contributor inspects the desktop application project
- **THEN** the project references Avalonia packages needed to run an Avalonia desktop application

## ADDED Requirements

### Requirement: Clean Architecture layer projects are present
The repository SHALL include separate production projects for the domain, application, integration, and UI layers.

#### Scenario: Contributor inspects production projects
- **WHEN** a contributor inspects the `src` directory
- **THEN** it contains projects for `FusionCanvas.Domain`, `FusionCanvas.Application`, `FusionCanvas.Integration`, and `FusionCanvas.App`

#### Scenario: Contributor reviews layer project purposes
- **WHEN** a contributor reviews the project layout
- **THEN** domain, application, integration, and UI responsibilities are represented by separate projects

### Requirement: Production project dependencies point inward
Production project references SHALL follow Clean Architecture dependency direction.

#### Scenario: Contributor inspects domain project references
- **WHEN** a contributor inspects `FusionCanvas.Domain`
- **THEN** it has no project references to application, integration, or UI projects

#### Scenario: Contributor inspects application project references
- **WHEN** a contributor inspects `FusionCanvas.Application`
- **THEN** it references `FusionCanvas.Domain`
- **AND** it does not reference `FusionCanvas.Integration` or `FusionCanvas.App`

#### Scenario: Contributor inspects integration project references
- **WHEN** a contributor inspects `FusionCanvas.Integration`
- **THEN** it references inward-facing domain or application projects
- **AND** no domain or application project references it

#### Scenario: Contributor inspects UI project references
- **WHEN** a contributor inspects `FusionCanvas.App`
- **THEN** it references inward-facing application or domain projects
- **AND** it does not require domain or application projects to reference Avalonia

### Requirement: Unit test projects are present
The repository SHALL include unit test projects that mirror the production layer projects.

#### Scenario: Contributor inspects test projects
- **WHEN** a contributor inspects the `tests` directory
- **THEN** it contains test projects for `FusionCanvas.Domain.Tests`, `FusionCanvas.Application.Tests`, `FusionCanvas.Integration.Tests`, and `FusionCanvas.App.Tests`

#### Scenario: Contributor reviews test project references
- **WHEN** a contributor inspects each test project
- **THEN** each test project references the production project for the layer it tests

### Requirement: Unit tests run from the solution
The repository SHALL support running unit tests for the solution through command-line .NET tooling.

#### Scenario: Contributor runs solution tests
- **WHEN** a contributor runs `dotnet test` for the solution
- **THEN** all included unit test projects are discovered and run successfully

#### Scenario: Contributor verifies test infrastructure
- **WHEN** a contributor inspects the initial test projects
- **THEN** at least one lightweight test proves the unit test infrastructure executes

### Requirement: Foundation avoids product feature behavior
The Clean Architecture solution foundation SHALL NOT implement product pipeline, persistence, plugin loading, AI, marketplace, listing, or mockup behavior.

#### Scenario: User launches the initial application after the project split
- **WHEN** the application starts
- **THEN** it only displays the initial desktop shell and does not perform product workflow automation or external integration actions

#### Scenario: Contributor inspects placeholder code
- **WHEN** a contributor inspects newly added layer projects
- **THEN** placeholder code does not introduce product workflow behavior or speculative abstractions
