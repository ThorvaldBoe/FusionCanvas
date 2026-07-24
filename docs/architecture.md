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

- Create Item
- Move Item
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
- Item
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
Item
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

- Create Item
- Rename Group
- Delete Asset
- Publish Product

Commands simplify undo/redo, automation, scripting, and testing.

## Events

The application should gradually move toward an event-driven internal architecture.

Examples:

- ItemCreated
- IdeaRejected
- AssetImported
- MockupGenerated
- ProductPublished

Events allow plugins to react without tightly coupling components.

## Testing Strategy

FusionCanvas uses a lowest-reliable-layer testing strategy. Business logic remains testable without the UI framework, UI-owned decision logic is covered through focused view-model and coordinator tests, and meaningful Avalonia framework behavior is covered through deterministic headless view tests that require no interactive display.

FusionCanvas should maintain a high level of unit test coverage, especially for domain logic, application services, persistence boundaries, and plugin contracts. New behavior should generally include focused unit tests unless there is a clear reason another test type provides better confidence.

Priority should be given to testing:

- domain logic
- application services
- plugin interfaces
- data persistence
- import/export functionality

Use Avalonia headless tests when view construction, bindings, control state, routed input, focus, selection, or visual-tree behavior carries meaningful risk. Avoid superficial tests of static markup or framework implementation details. These tests belong in `dotnet test .\FusionCanvas.sln` and must run consistently under Codex, OpenCode, CI, and normal contributor environments.

The `FusionCanvas.App.Tests` project includes an Avalonia headless harness (`HeadlessTestApp`) and representative view tests (`MainWindowLayoutTests`) covering `MainWindow` construction, compiled bindings, control state, and visual-tree behavior. View-model, command, and navigation tests remain framework-free. Additional headless view coverage should be added as meaningful framework behavior is introduced.

Live desktop testing is optional and ad hoc. It can supplement deterministic evidence for native-window behavior, operating-system input, assistive-technology exposure, platform integration, visual judgment, or difficult interaction defects. Any mutating live check uses a disposable workspace or database and never becomes a standing module-completion or QA requirement.

## OpenSpec Development

FusionCanvas is developed using OpenSpec.

The expected workflow is:

```text
Discover → Define module → Propose → Review → Apply → Verify → Learn → Archive
```

Detailed planning advances one cohesive delivery module at a time. Specifications become part of the project's permanent behavioral knowledge base; change-specific conceptual design and implementation guidance live in the active/archived delivery package so they can be explicit without turning current source-file choices into permanent requirements.

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
