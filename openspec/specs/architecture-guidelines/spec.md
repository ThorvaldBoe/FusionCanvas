# Architecture Guidelines

## Purpose

Defines the architectural expectations for FusionCanvas implementation work, including Clean Architecture boundaries, SOLID design, dependency direction, and unit testing expectations.
## Requirements
### Requirement: Clean Architecture is the target structure
FusionCanvas SHALL use Clean Architecture as the default architectural structure for meaningful implementation work.

#### Scenario: Contributor evaluates a new feature proposal
- **WHEN** a contributor proposes a feature that adds domain rules, use cases, external integrations, persistence, or UI behavior
- **THEN** the proposal identifies how the feature fits within the Clean Architecture layer structure

#### Scenario: Contributor reviews architecture documentation
- **WHEN** a contributor reads the architecture guidance
- **THEN** the guidance states that FusionCanvas separates domain, application, integration, and UI responsibilities

### Requirement: SOLID principles guide implementation
FusionCanvas SHALL use SOLID principles to guide maintainable implementation while avoiding unnecessary abstraction and code bloat.

#### Scenario: Contributor refactors an oversized presentation type
- **WHEN** a presentation type contains a cohesive pure projection responsibility that can be independently named and tested
- **THEN** the responsibility may be extracted into a focused collaborator without changing the observable behavior of the presentation type

### Requirement: Layer responsibilities are separated
FusionCanvas SHALL separate domain, application, integration, and UI responsibilities into distinct layer boundaries.

#### Scenario: Domain behavior is added
- **WHEN** a feature adds business concepts, invariants, calculations, or workflow rules
- **THEN** those concerns are assigned to the domain layer instead of the UI or integration layer

#### Scenario: Application behavior is added
- **WHEN** a feature adds use case orchestration or workflow coordination
- **THEN** those concerns are assigned to the application layer instead of Avalonia views or external service adapters

#### Scenario: Integration behavior is added
- **WHEN** a feature adds persistence, file system access, marketplace APIs, AI providers, plugin host adapters, or other external system access
- **THEN** those concerns are assigned to the integration layer behind application-facing contracts

#### Scenario: UI behavior is added
- **WHEN** a feature adds presentation, navigation, input handling, or visual state
- **THEN** those concerns are assigned to the UI layer without owning domain rules

### Requirement: Layer projects are separate
FusionCanvas SHALL use separate projects for domain, application, integration, and UI layers once those layers contain implementation responsibilities.

#### Scenario: Contributor lists solution projects after layer split
- **WHEN** a contributor lists projects in the solution after non-UI behavior has been introduced
- **THEN** the solution includes separate projects representing the domain, application, integration, and UI layers

#### Scenario: Contributor adds a new layer responsibility
- **WHEN** a contributor adds behavior that belongs to a layer that does not yet have a project
- **THEN** the contributor creates or updates the appropriate layer project instead of placing the behavior in an unrelated project

### Requirement: Dependencies point inward
FusionCanvas SHALL keep project dependencies pointing inward toward domain and application abstractions.

#### Scenario: Contributor reviews domain project references
- **WHEN** a contributor inspects the domain project
- **THEN** it does not reference UI, integration, persistence, marketplace, AI provider, or plugin host projects

#### Scenario: Contributor reviews application project references
- **WHEN** a contributor inspects the application project
- **THEN** it depends on domain contracts or types and does not depend on UI frameworks or concrete integration implementations

#### Scenario: Contributor reviews integration and UI project references
- **WHEN** a contributor inspects integration or UI projects
- **THEN** they depend on inward-facing application or domain contracts rather than requiring domain behavior to depend on them

### Requirement: Unit testing is part of feature architecture
FusionCanvas SHALL include appropriate unit tests for every feature that adds or changes behavior.

#### Scenario: Contributor implements domain behavior
- **WHEN** a feature adds or changes domain rules, invariants, calculations, or workflow decisions
- **THEN** the feature includes unit tests that verify the domain behavior without requiring UI frameworks or external services

#### Scenario: Contributor implements application behavior
- **WHEN** a feature adds or changes use case orchestration or workflow coordination
- **THEN** the feature includes unit tests that verify the application behavior through domain and application contracts

#### Scenario: Contributor implements integration-facing behavior
- **WHEN** a feature adds or changes persistence, file system, marketplace, AI provider, plugin host, or other external integration behavior
- **THEN** the feature includes tests for the integration-facing contract or adapter behavior at the appropriate boundary

#### Scenario: Contributor changes UI decision logic
- **WHEN** a feature adds or changes UI-owned decision logic
- **THEN** the feature includes appropriate tests for that logic without requiring superficial tests for static markup

#### Scenario: Contributor reviews feature completeness
- **WHEN** a contributor marks feature implementation complete
- **THEN** the implemented behavior has appropriate automated test coverage or an explicit documented reason why automated unit testing is not applicable

### Requirement: Domain layer groups types by cohesive capability
The FusionCanvas production layers SHALL group production types into cohesive capability folders and namespaces (for example `Items`, `Stores`, `Niches`, `Groups`, `Tags`, `Assets`, `Prompts`, `Workflow`, `Navigation` in Domain; the matching capability folders in Application; and technical subfolders such as `Persistence` and `Files` at the Integration layer where the layer itself makes the capability clear), with a narrow shared root for cross-cutting primitives where justified, rather than a single catch-all folder.

#### Scenario: Contributor locates an item domain type
- **WHEN** a contributor looks for the `Item` domain entity or item-related rules and policies
- **THEN** they are found under the `Items` capability folder and `FusionCanvas.Domain.Items` namespace
- **AND** a single `Workspace` folder is not the home for every workspace-related domain type

#### Scenario: Contributor reviews the shared domain root
- **WHEN** a contributor inspects the shared `FusionCanvas.Domain.Workspace` root
- **THEN** it contains only cross-cutting primitives such as `Workspace`, `WorkspaceDefaults`, `WorkspaceEntity`, `WorkspaceEntityKind`, and `WorkspaceSnapshot`
- **AND** capability-specific types live in their own capability folders

#### Scenario: Contributor locates an application use case
- **WHEN** a contributor looks for an application-layer use case such as item management or group management
- **THEN** the service, its interface, and its request/result/state records are found under the matching capability folder (for example `Items/` or `Groups/`)
- **AND** the Application project does not keep every workspace-related type in a single `Workspace/` folder

#### Scenario: Contributor locates a persistence or file-storage adapter
- **WHEN** a contributor looks for the SQLite workspace repository or the local workspace file store
- **THEN** the repository is found under a `Persistence` folder and the file store under a `Files` folder at the Integration layer
- **AND** the Integration project does not keep every adapter in a single `Workspace` folder

### Requirement: Domain layer keeps one primary type per file
The FusionCanvas production layers SHALL keep one primary top-level type per file, with the file name matching the type name, per the C# coding standard. This requirement applies to Domain, Application, Integration, and App production code, except for private nested implementation types, generated code, Avalonia code-behind partial classes paired with their `.axaml` file, and framework-required partial declarations.

#### Scenario: Contributor opens a domain file
- **WHEN** a contributor opens any Domain layer file
- **THEN** it contains at most one top-level public or internal type
- **AND** the file name matches that type name
- **AND** no file is named with a vague grouping term such as `Entities.cs`, `Models.cs`, or `Relationships.cs`

#### Scenario: Contributor opens an application file
- **WHEN** a contributor opens any Application layer file
- **THEN** it contains at most one top-level public or internal type
- **AND** a service's interface, its request/result/state records, and its implementation each live in separate files
- **AND** no file is named with a vague grouping term such as `Contracts.cs`, `Models.cs`, or `Management.cs` when it bundles multiple top-level types

#### Scenario: Contributor opens an integration file
- **WHEN** a contributor opens any Integration layer file
- **THEN** it contains at most one top-level public or internal type
- **AND** the file name matches that type name

#### Scenario: Contributor opens an App file
- **WHEN** a contributor opens any App production file
- **THEN** it contains at most one top-level public or internal type unless the file is an allowed framework or generated exception
- **AND** the file name matches the primary type name

### Requirement: Composition root owns concrete integration construction
The App composition root SHALL construct concrete Integration adapters and inject them through Application-facing contracts into presentation types.

#### Scenario: Main-window presentation is composed for production
- **WHEN** the desktop App creates the main window and its view model
- **THEN** concrete workspace file stores, workspace transfer adapters, CSV codecs, and other Integration adapters are created by an App composition factory
- **AND** `MainWindow` and `MainWindowViewModel` receive application-facing contracts or already-composed runtime services
- **AND** those presentation types do not instantiate concrete types from `FusionCanvas.Integration`

#### Scenario: A view model is constructed directly by a test
- **WHEN** a test constructs a presentation type without the production composition root
- **THEN** its fallback behavior uses an explicit non-Integration test-safe default or a supplied application contract
- **AND** the production Integration assembly is not required by that fallback path

