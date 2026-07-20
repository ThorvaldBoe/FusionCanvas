# FusionCanvas Architecture

## Overview

FusionCanvas is a local-first desktop application built around a modular, extensible architecture.

The application is designed to support long-term evolution through clear separation of responsibilities, a plugin-based extension model, and a structured domain model.

The core application should remain relatively small while allowing new functionality to be added through plugins.

## Architectural Goals

The architecture should prioritize:

- maintainability
- extensibility
- testability
- performance
- offline capability
- AI integration
- plugin support
- long-term evolution

Features should be easy to add without requiring major changes to existing code.

The application should follow Clean Architecture principles where practical. Domain and application logic should remain independent of UI frameworks, persistence details, plugin implementations, and external services.

Code should aim to follow SOLID design principles, with small focused types, clear dependencies, explicit abstractions, and behavior that can be extended without repeatedly modifying stable core code.

## Technology Stack

Current technology choices:

| Layer | Technology |
| --- | --- |
| UI | Avalonia |
| Language | C# |
| Framework | .NET |
| Database | SQLite |
| Flexible Metadata | JSON |
| Architecture | MVVM |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| Specifications | OpenSpec |
| Source Control | Git |
| Testing | xUnit |

These choices may evolve, but the architectural principles should remain stable.

## Clean Architecture Direction

FusionCanvas should move toward the following layer structure as soon as each layer has real responsibility:

```text
FusionCanvas.Domain
    Core business concepts, invariants, calculations, and workflow rules

FusionCanvas.Application
    Use cases, orchestration, ports, and application-facing contracts

FusionCanvas.Integration
    Persistence, file system access, marketplace APIs, AI providers,
    plugin host adapters, and other external systems

FusionCanvas.App
    Avalonia UI, presentation state, navigation, and user interaction
```

The preferred dependency direction is inward:

```text
UI and Integration -> Application -> Domain
```

The domain project should not reference Avalonia, persistence frameworks, marketplace SDKs, AI providers, plugin host implementations, file system adapters, or other external infrastructure. The application layer may define ports and use domain types. Integration and UI projects should satisfy those contracts from the outside.

This structure should remain practical. Do not create empty projects or speculative abstractions before they have useful responsibilities, but do not place new domain or application behavior in the UI project merely because it is convenient.

## Layer Responsibilities

### Presentation Layer

Responsible for windows, dialogs, controls, tree views, inspectors, editors, and user interaction.

The presentation layer should contain no business logic.

### View Models

View models expose data to the user interface.

Responsibilities include:

- state management
- commands
- validation
- UI interaction
- selection management

Business rules should remain in the application layer.

### Application Layer

The application layer coordinates use cases such as:

- Create Listing
- Move Listing
- Generate Mockups
- Publish Products
- Run AI Prompt
- Import Assets

Application services orchestrate workflow without containing business rules that belong in the domain.

### Domain Layer

The domain is the heart of FusionCanvas.

It contains concepts such as:

- Workspace
- Store
- Niche
- Group
- Listing
- Design
- Idea
- Phrase
- Graphic
- Mockup
- Asset

The domain should contain the business rules that define how these objects behave.

The domain should have no knowledge of Avalonia, SQLite, HTTP, AI providers, Printify, Shopify, or plugin implementations.

### Integration Layer

Integration communicates with the outside world.

Examples include:

- SQLite
- filesystem
- AI APIs
- marketplace APIs
- image processing
- exporters
- importers

Integration implements interfaces defined by the application or domain layers.

## Core Modules

### Workspace Module

The Workspace Module is responsible for organizing user projects and content. In user-facing terms, a workspace is the top-level scope above stores; internally, the module also contains repository and snapshot types that represent the local structured data set.

Responsibilities:

- workspace, store, niche, topic, group, and item management
- navigation tree
- workspace organization
- user preferences
- context derivation for tools

The workspace serves as the entry point for most user interactions.

### Product Workflow Module

The Product Workflow Module manages product progression through the core workflow:

```text
Idea
Concept
Design
Listing
Archive
```

Archive is a retained state for work that should leave the active workflow without being forgotten.

Responsibilities:

- workflow stage tracking
- operational status management
- workflow transitions
- progress visibility
- stage filtering

Workflow stage and operational status are separate concepts. `WorkflowStage` drives the visible navigator and should stay close to Idea, Concept, Design, Listing, and Archive. `Status` describes operational state, such as Draft, Published, Paused, or Rejected.

### Design Triangle Module

The Design Triangle Module helps creators evaluate and refine concepts.

Triangle components:

```text
Idea
Phrase
Graphic
```

Responsibilities:

- concept evaluation
- scoring systems
- refinement workflows
- improvement suggestions
- selected concept version tracking

This module focuses on helping creators improve product quality before investing significant design effort.

### Mockup Module

The Mockup Module manages product presentation assets.

Responsibilities:

- mockup products
- mockup templates
- color variants
- placement settings
- mockup generation
- batch processing
- export workflows

The mockup system should be extensible and support multiple generation strategies.

### Listing Module

The Listing Module manages marketplace-ready product information.

Responsibilities:

- product titles
- descriptions
- tags
- keywords
- prices
- marketplace notes
- selected final artwork
- readiness checks

The goal is to separate listing preparation from marketplace-specific publishing implementations.

### Integration Module

The Integration Module connects FusionCanvas to external systems.

Potential integrations include:

- Shopify
- Printify
- Etsy
- CSV import/export
- cloud storage providers

Integrations should remain isolated from the core application whenever possible.

### Plugin System

The Plugin System allows third parties to extend FusionCanvas.

Potential plugin categories:

- marketplace integrations
- mockup generators
- export formats
- AI providers
- workflow automation
- stage tools
- reporting tools

Plugins should be discoverable and manageable through a consistent extension model.

Default workflow modules should be shaped as built-in stage tools where practical. For example, the first ideation experience may ship with the application, but it should use the same host and context model that future plugin-provided ideation tools will use.

Stage tools can contribute interactive work surfaces for specific workflow stages such as Idea, Concept, Design, and Listing. The document window hosts the selected stage tool in its lower detail area, while the workflow stage navigator and navigation pane provide context.

### AI Services

AI should be provider-independent.

The core application should define generic interfaces. Providers such as OpenAI, Anthropic, local LLMs, or future services should be implemented as plugins or infrastructure adapters.

Changing AI providers should not require changes to the rest of the application.

## Data Storage

FusionCanvas uses SQLite as its primary data store.

Structured information should be stored in relational tables. Flexible or evolving information should be stored as JSON.

```text
Listing
--------
Id
Name
StoreId
GroupId
Created
Modified
MetadataJson
```

This provides both efficient querying and schema flexibility.

## File Storage

Large assets should remain as files.

Examples:

- PNG
- SVG
- PSD
- Affinity Designer files
- mockups

The database stores paths to files rather than embedding them.

Imported assets are copied into a FusionCanvas-managed workspace. The managed copy becomes the authoritative file used by the application, which keeps local projects durable when original source files are renamed, moved, or deleted outside FusionCanvas.

## Dependency Injection

Services should be resolved through dependency injection.

Benefits include:

- loose coupling
- easier testing
- plugin discovery
- easier replacement of implementations

## Commands

User actions should be implemented as commands where practical.

Examples:

- Create Listing
- Rename Group
- Delete Asset
- Publish Product

Commands simplify undo/redo, automation, scripting, and testing.

## Events

The application should gradually move toward an event-driven internal architecture.

Examples:

- ListingCreated
- IdeaRejected
- AssetImported
- MockupGenerated
- ProductPublished

Events allow plugins to react without tightly coupling components.

## Testing Strategy

FusionCanvas uses complementary code-level and real-desktop test lanes. Business logic must remain testable without the UI, while user-facing behavior should also be verified through the built Avalonia application when the contributing agent can run an interactive desktop session (e.g., Codex). OpenCode cannot perform interactive desktop verification, records that pass as not-applicable, and relies on the code-level baseline plus UI-owned decision-logic tests.

FusionCanvas should maintain a high level of unit test coverage, especially for domain logic, application services, persistence boundaries, and plugin contracts. New behavior should generally include focused unit tests unless there is a clear reason another test type provides better confidence.

Priority should be given to testing:

- domain logic
- application services
- plugin interfaces
- data persistence
- import/export functionality

Every new or changed user-facing feature also expects a proportional real desktop UI pass derived from its accepted scenarios when an interactive desktop is available. Cover the feature's primary workflow and all applicable interaction risks: keyboard and pointer input, focus and selection, validation and filtering, destructive confirmation, persistence and restart, recovery, accessibility exposure, and tabs or windows.

Desktop UI verification runs separately from `dotnet test .\FusionCanvas.sln`, uses a disposable workspace or database rather than normal user data, and records the tested build and environment, scenarios, results, isolation method, limitations, and material screenshots or automation logs. The full QA review expands this into an all-features regression matrix for the current build.

## OpenSpec Development

FusionCanvas is developed using OpenSpec.

The expected workflow is:

```text
Idea
Specification
Review
Implementation
Testing
Archive
```

Specifications become part of the project's permanent knowledge base and should accurately describe the intended behavior of the system.

## Architectural Principles

When making architectural decisions, prefer:

- composition over inheritance
- interfaces over concrete implementations
- explicit models over hidden behavior
- dependency injection over global state
- immutable data where practical
- modularity over convenience
- stable dependencies
- well-supported open-source libraries
- minimal external infrastructure requirements

Dependencies should not leak inward. Domain code should remain independent of UI frameworks, persistence technologies, external service SDKs, plugin host implementations, and provider-specific AI libraries.

Every major feature should fit naturally into the existing architecture rather than introducing special cases.
