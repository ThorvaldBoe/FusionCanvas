# Desktop Application Foundation

## Purpose

Defines the baseline expectations for the FusionCanvas desktop application foundation, including solution structure, Avalonia startup, and the first local workspace shell.

## Requirements

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

### Requirement: Application starts to a main window
The desktop application SHALL start to a main FusionCanvas window.

#### Scenario: Contributor runs the app
- **WHEN** a contributor launches the desktop application
- **THEN** a main window opens for FusionCanvas

### Requirement: Main window presents an initial workspace shell
The main window SHALL present a clean, mostly empty initial page suitable for an application with no workspace loaded.

#### Scenario: User views the first shell
- **WHEN** the main window is displayed
- **THEN** the user sees a clean empty page with minimal FusionCanvas identity
- **AND** the page does not display dense placeholder navigation, tabs, workflow stages, loading cards, unavailable cards, or fake workspace content

#### Scenario: User sees no workspace content before workspace behavior exists
- **WHEN** no persistent workspace data exists
- **THEN** the application displays an intentionally empty starting surface without requiring storage setup
- **AND** it does not imply that workspace navigation, product pipeline, listing, mockup, plugin, AI, marketplace, or automation behavior is available

### Requirement: Main window presents compact workspace management
The main FusionCanvas window SHALL present a compact active workspace indicator and a workspace management entry point above store selection when workspace behavior is available.

#### Scenario: User views workspace shell with active workspace
- **WHEN** the main window is displayed and an active workspace exists
- **THEN** the sidebar shows the active workspace as a small top-level selected scope
- **AND** store selection and navigation are visually subordinate to the selected workspace

#### Scenario: User switches workspace from workspace management
- **WHEN** the user opens workspace management and selects a different active workspace
- **THEN** the main window refreshes store selection and navigation for the selected workspace
- **AND** stores from the previously selected workspace are not shown in the normal store selector

#### Scenario: User opens workspace management
- **WHEN** the user chooses to manage workspaces from the regular workspace UI
- **THEN** FusionCanvas opens a workspace management surface or window
- **AND** workspace lifecycle and switching actions are available from that management surface rather than inline in the navigation tree

### Requirement: Main window preserves empty workspace clarity
The main FusionCanvas window SHALL distinguish an empty workspace from an empty store.

#### Scenario: Active workspace has no stores
- **WHEN** the active workspace contains no stores
- **THEN** FusionCanvas shows the first-store prompt for that workspace
- **AND** the prompt does not imply that other workspaces have no stores

#### Scenario: Workspace management is open for an empty workspace
- **WHEN** the workspace management window is open
- **AND** the active workspace changes to a workspace with no stores
- **THEN** FusionCanvas defers the first-store prompt until workspace management closes
- **AND** the main window does not stack a store prompt over workspace management

#### Scenario: No active workspace is available
- **WHEN** no active workspace is available
- **THEN** FusionCanvas shows an empty workspace-level state with a way to create or restore a workspace
- **AND** store and niche actions that require an active workspace are unavailable

### Requirement: Initial foundation avoids future feature behavior
The initial desktop foundation SHALL NOT implement product pipeline, persistence, plugin loading, AI, marketplace, listing, or mockup behavior.

#### Scenario: User launches the initial application
- **WHEN** the application starts
- **THEN** it only displays the initial desktop shell and does not perform product workflow automation or external integration actions
