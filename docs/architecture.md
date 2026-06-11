# FusionCanvas Architecture

## Purpose

This document describes the high-level architecture of FusionCanvas.

Its purpose is to define the major system components, architectural principles, and long-term technical direction of the project without prescribing detailed implementation decisions.

Detailed implementation should evolve over time, but the architectural principles defined here should remain relatively stable.

---

# Architectural Vision

FusionCanvas is designed as a local-first desktop application that helps Print-on-Demand creators manage the complete product lifecycle.

The architecture should support:

* Idea management
* Concept refinement
* Design organization
* Product pipeline tracking
* Mockup generation
* Listing preparation
* Workflow automation
* Marketplace integrations
* AI-assisted workflows

while remaining modular, extensible, and easy to maintain.

---

# Clean Architecture Direction

FusionCanvas should follow Clean Architecture for meaningful implementation work.

The codebase should keep domain rules, application use cases, external integrations, and user interface concerns separated. Early milestones may begin with a small number of projects, but new behavior should move toward the following layer structure as soon as each layer has real responsibility:

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

---

# High-Level System Overview

```text
FusionCanvas
│
├── Workspace Module
├── Product Pipeline Module
├── Design Triangle Module
├── Mockup Module
├── Listing Module
├── Integration Module
├── Plugin System
├── AI Services
└── Storage Layer
```

Each module should have clear responsibilities and minimal coupling to other modules.

---

# Architectural Principles

## Local First

Users should own their data.

FusionCanvas should remain fully functional without cloud services.

Cloud integrations should enhance the experience but never become mandatory.

## Modular Design

Features should be separated into independent modules whenever practical.

The goal is to make the system easier to maintain, test, and extend.

Modules should live inside the appropriate clean architecture layer. For example, workspace domain rules belong in the domain layer, workspace use cases belong in the application layer, workspace persistence adapters belong in the integration layer, and workspace presentation belongs in the UI layer.

## SOLID Design

Implementation should follow SOLID principles pragmatically.

Classes should have focused responsibilities, dependencies should be explicit, and abstractions should protect real boundaries, variation points, or testable contracts. Interfaces and services should not be introduced only to make the code look architectural.

The goal is efficient, maintainable code without speculative layers, oversized classes, broad interfaces, or indirection that is not justified by current behavior.

## Testable Architecture

Unit testing is a vital part of the architecture.

Every feature that adds or changes behavior should include appropriate automated tests. Domain rules and application use cases should be testable without Avalonia, storage engines, marketplace APIs, AI providers, plugin hosts, or network services.

Integration-facing behavior should be tested at the appropriate boundary, such as contract tests or adapter tests with controlled dependencies. UI-owned decision logic should be tested when it contains behavior, but static markup and framework-owned wiring do not need superficial tests.

## Plugin Friendly

The architecture should encourage extension through plugins.

New functionality should be addable without modifying the core application.

## Offline Capable

Most functionality should continue working without an internet connection.

External services should be treated as optional dependencies.

## AI Assisted

AI should enhance workflows rather than control them.

FusionCanvas should remain useful even when no AI services are configured.

## Incremental Complexity

The architecture should start simple and evolve only when necessary.

Avoid introducing infrastructure before a clear need exists.

The initial Avalonia shell may remain a single UI project while it contains only presentation scaffolding. As soon as meaningful domain rules, application use cases, or integration adapters are introduced, they should move into the appropriate layer projects with tests that match the behavior being added.

---

# Core Modules

## Workspace Module

The Workspace Module is responsible for organizing user projects and content.

Responsibilities:

* Project management
* Collections
* Navigation tree
* Workspace organization
* User preferences

The workspace serves as the entry point for most user interactions.

---

## Product Pipeline Module

The Product Pipeline Module manages product progression through the workflow.

Pipeline stages:

```text
Idea
↓
Concept
↓
Design
↓
Listing
↓
Published
↓
Archived
```

Responsibilities:

* Pipeline tracking
* Status management
* Workflow transitions
* Progress visibility

---

## Design Triangle Module

The Design Triangle Module helps creators evaluate and refine concepts.

Triangle components:

```text
Idea
Phrase
Graphic
```

Responsibilities:

* Concept evaluation
* Scoring systems
* Refinement workflows
* Improvement suggestions

This module focuses on helping creators improve product quality before investing significant design effort.

---

## Mockup Module

The Mockup Module manages product presentation assets.

Responsibilities:

* Mockup templates
* Mockup generation
* Batch processing
* Export workflows

The mockup system should be extensible and support multiple generation strategies.

---

## Listing Module

The Listing Module manages marketplace listing data.

Responsibilities:

* Product titles
* Descriptions
* Tags
* Keywords
* Marketplace metadata

The goal is to separate listing management from marketplace-specific implementations.

---

## Integration Module

The Integration Module connects FusionCanvas to external systems.

Potential integrations include:

* Shopify
* Printify
* Etsy
* CSV import/export
* Cloud storage providers

Integrations should remain isolated from the core application whenever possible.

---

## Plugin System

The Plugin System allows third parties to extend FusionCanvas.

Potential plugin categories:

* Marketplace integrations
* Mockup generators
* Export formats
* AI providers
* Workflow automation
* Reporting tools

Plugins should be discoverable and manageable through a consistent extension model.

---

## AI Services

The AI Services layer provides optional AI-assisted capabilities.

Potential use cases:

* Idea generation
* Concept refinement
* Listing suggestions
* Workflow automation
* Product analysis

AI services should be abstracted behind provider interfaces.

No specific AI provider should be hardcoded into the architecture.

---

# Storage Architecture

FusionCanvas should evolve through multiple storage phases.

## Phase 1 – Local Files

Initial versions may use simple local file storage.

Goals:

* Fast development
* Easy debugging
* Human-readable data

## Phase 2 – SQLite

As the application matures, SQLite will become the primary storage mechanism.

Goals:

* Improved performance
* Query capabilities
* Data consistency

## Phase 3 – Optional Synchronization

Future versions may support optional synchronization services.

Goals:

* Multi-device workflows
* Backup
* Collaboration

Synchronization should never be required for basic operation.

---

# Technology Direction

Current intended technology stack:

## User Interface

* Avalonia UI

## Language

* C#

## Runtime

* .NET

## Data Storage

* SQLite

## Plugin Model

* .NET assemblies
* Contract-based extension points

## AI Integration

* Provider abstraction layer
* Local and cloud providers supported

---

# Dependency Strategy

FusionCanvas should favor:

* Stable dependencies
* Well-supported open-source libraries
* Minimal external infrastructure requirements

Avoid introducing dependencies unless they provide clear value.

Dependencies should not leak inward. Domain code should remain independent of UI frameworks, persistence technologies, external service SDKs, plugin host implementations, and provider-specific AI libraries.

---

# Future Architectural Goals

The following capabilities are considered future enhancements and are not required for the initial release:

* Cloud synchronization
* Team collaboration
* Shared workspaces
* Marketplace publishing
* AI agents
* Template marketplaces
* Workflow marketplaces
* Community extensions

These capabilities should be considered during architectural decisions but should not drive early complexity.

---

# Architectural Success Criteria

The architecture is successful if it allows FusionCanvas to:

* Remain maintainable as features grow
* Support plugins without major refactoring
* Operate effectively offline
* Support AI as an optional enhancement
* Keep core behavior testable through unit tests
* Scale from hobby creators to professional creators
* Evolve incrementally without requiring large rewrites

The architecture should prioritize simplicity, extensibility, and creator ownership over unnecessary complexity.
