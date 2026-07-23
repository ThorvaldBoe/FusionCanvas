# FusionCanvas Project Context

## Overview

FusionCanvas is an open-source desktop application for managing the complete Print-on-Demand product lifecycle.

The project focuses on helping creators move from ideas to published products through a structured workflow that combines ideation, concept refinement, design management, listing preparation, and workflow automation.

FusionCanvas is intentionally niche-independent and is designed to support creators across many different markets and product categories.

---

# Mission

Help creators spend more time creating and less time managing tools.

FusionCanvas aims to become a creative operating system for Print-on-Demand businesses by providing a unified workspace for product development and workflow management.

---

# Vision

FusionCanvas should support the complete journey from inspiration to publication.

```text
Theme
↓
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

The system should preserve relationships between these stages so that creators always understand where products originated and how they evolved.

---

# Core Principles

## Creativity First

FusionCanvas exists to support creative decision-making.

Automation should reduce repetitive work while preserving creative control.

## Local First

Users own their data.

The application should remain useful without cloud services.

Cloud integrations should be optional.

## Structured Flexibility

Creators work differently.

Some begin with themes.

Some begin with phrases.

Some begin with graphics.

The system should support multiple creative entry points while maintaining a coherent workflow.

## AI Assisted

AI is a tool, not a replacement for creator judgment.

FusionCanvas should remain fully functional without AI services.

## Plugin Friendly

The platform should encourage extension through plugins and integrations.

Core functionality should remain focused and maintainable.

## Incremental Complexity

Prefer simple solutions first.

Introduce complexity only when there is a demonstrated need.

---

# Product Differentiators

FusionCanvas differs from traditional Print-on-Demand tools in several important ways.

## Idea-First Workflow

Most tools begin with finished designs.

FusionCanvas begins with themes, ideas, and concepts.

## Design Triangle

FusionCanvas uses the Design Triangle model:

```text
Idea
Phrase
Graphic
```

This helps creators evaluate and strengthen concepts before investing production effort.

## Lifecycle Visibility

Published products remain connected to their upstream ideas, concepts, designs, and listings.

## Local Ownership

Users retain ownership and control of their data.

## Open Ecosystem

FusionCanvas is designed to support plugins, integrations, and community-driven extensions.

---

# Target Users

Primary users include:

* Print-on-Demand creators
* Etsy sellers
* Shopify merchants
* Merch designers
* Creative entrepreneurs
* Small creative businesses

The project should prioritize practical workflows used by individual creators and small teams.

---

# Architectural Direction

Current technology direction:

```text
UI:
Avalonia

Language:
C#

Runtime:
.NET

Storage:
SQLite

Extensions:
Plugin-based

AI:
Provider abstraction layer
```

Architecture structure:

```text
FusionCanvas.Domain:
Core business concepts, invariants, calculations, and workflow rules

FusionCanvas.Application:
Use cases, orchestration, ports, and application-facing contracts

FusionCanvas.Integration:
Persistence, file system access, marketplace APIs, AI providers,
plugin host adapters, and other external system adapters

FusionCanvas.App:
Avalonia UI, presentation state, navigation, and user interaction
```

Project dependencies should point inward:

```text
FusionCanvas.App -> FusionCanvas.Application -> FusionCanvas.Domain
FusionCanvas.Integration -> FusionCanvas.Application -> FusionCanvas.Domain
```

Domain code must remain independent of UI frameworks, persistence technologies, marketplace SDKs, AI provider SDKs, plugin host implementations, and file system adapters.

Implementation should follow SOLID principles pragmatically. Prefer focused responsibilities, explicit dependencies, and abstractions that protect real boundaries or variation points. Avoid speculative indirection, oversized classes, and broad interfaces that are not justified by current behavior.

Testing is part of the architecture. Every feature that adds or changes behavior should include appropriate automated tests for domain rules, application use cases, integration-facing contracts, or UI-owned behavior. Test at the lowest reliable layer: keep decision logic in framework-free tests, and use Avalonia headless view tests when construction, bindings, control state, routed input, focus, selection, or visual-tree behavior is material. Static markup does not require superficial tests.

Headless view tests run in the deterministic `dotnet test` baseline without an interactive display, so Codex, OpenCode, CI, and humans share the same routine. Live testing through the built desktop application is optional and ad hoc for risks that headless tests do not represent well, such as native windows, operating-system input, assistive-technology exposure, platform integration, or visual judgment. Optional mutating checks use isolated disposable application data and are supplemental evidence rather than completion gates.

Preferred test project naming should mirror the production project being tested:

```text
tests/FusionCanvas.Domain.Tests
tests/FusionCanvas.Application.Tests
tests/FusionCanvas.Integration.Tests
tests/FusionCanvas.App.Tests
```

Technology choices may evolve, but the architectural principles should remain stable.

---

# Current Priorities

The current focus is establishing a strong foundation.

Priority order:

1. Core workspace experience
2. Product pipeline management
3. Design Triangle workflow
4. Local storage model
5. Plugin architecture
6. Mockup workflows
7. Listing workflows
8. Marketplace integrations
9. AI-assisted capabilities

The project should avoid premature optimization and unnecessary infrastructure.

---

# What FusionCanvas Is Not

FusionCanvas is not intended to become:

* A replacement for Affinity Designer
* A replacement for Photoshop
* A replacement for Illustrator
* A social network
* A marketplace
* A cloud-only SaaS platform

FusionCanvas complements existing creative tools rather than replacing them.

---

# OpenSpec Usage

FusionCanvas follows a specification-first development process.

Significant changes begin with collaborative discovery and an OpenSpec delivery package before implementation.

Expected workflow:

```text
Discover → Define module → Propose → Review → Apply → Verify → Learn → Archive
```

Specifications are considered the source of truth for intended system behavior.

Code should follow specifications, not the other way around.

## Rolling Module Delivery

Detailed planning advances one **delivery module** at a time. A delivery module is a cohesive, independently verifiable set of features that produces one clear user or platform outcome. It is a planning unit, not necessarily a code module or project boundary.

There is no fixed feature count. Scope is justified by cohesion, uncertainty, dependencies, and verification cost:

* Group features when they share an outcome, data model, surface, fixture, and acceptance pass.
* Split scope when it contains independent outcomes, unresolved high-impact decisions, or too much cross-layer and UI risk for one reviewer to understand and diagnose.
* Keep future opportunities lightweight. Do not create detailed specifications for later modules until the current module is verified and its lessons have informed what comes next.

Module discovery is conversational. The human and planning agent resolve goals, examples, non-goals, edge cases, assumptions, dependencies, and important product or architecture decisions. Do not create a separate module-specification document by default. Once the module is understood well enough to name and bound, create or refine its OpenSpec change and capture conclusions in the appropriate authoritative artifact. Implementation begins only after the delivery package is approved or approval is explicitly delegated.

Each module normally uses one OpenSpec change with these complementary parts:

* `proposal.md`: the module-level anchor for outcome, included features, boundaries, dependencies, risks, scope rationale, and overall verification approach.
* Delta specs: durable requirements and observable acceptance scenarios.
* `design.md`: conceptual and functional design plus a dedicated, implementation-ready plan identifying affected layers, data and UI behavior, edge cases, sequencing, tests, migrations, and decisions the implementer must not reopen.
* `tasks.md`: ordered, bounded, verifiable implementation steps.
* `verification.md`: criterion-level results, commands, desktop evidence or handoff, limitations, and deferred checks.

Acceptance criteria are completion gates. Every scenario is mapped to planned verification before implementation and to evidence afterwards. A failed criterion returns to implementation or specification correction and is rerun; an aggregate test-suite pass does not waive it.

Agent assignments are capability-based. High-reasoning agents or humans handle discovery, specification, design review, ambiguous corrections, and final acceptance. Lower-cost agents may implement bounded approved tasks when the delivery package is explicit enough. A handoff names the change, artifacts, task range, validation commands, prohibited scope expansion, and escalation conditions. Missing product or architecture decisions are escalated rather than guessed.

## Historical Planning References

The original LifeOS roadmap and product requirements are preserved under `docs/LifeOS` as optional historical idea sources. They may be stale and are not required reading, current plans, feature ordering, acceptance criteria, or accepted specifications.

Start module discovery from current user intent, accepted specs, the current application, recent verification and retrospectives, and `docs/roadmap.md`. Consult LifeOS material only when historical ideas or rationale would help, then revalidate anything useful through the normal discovery and OpenSpec workflow. Do not copy its inventory or assumptions into current scope by default.

---

# Decision-Making Guidelines

When evaluating new features or changes, contributors should ask:

1. Does this help creators produce better products?
2. Does this reduce workflow friction?
3. Does this align with the Design Pipeline?
4. Does this preserve creator ownership?
5. Does this increase complexity unnecessarily?
6. Can this be implemented as a plugin instead?

If a proposal does not clearly support the project's mission, it should be reconsidered.

---

# Long-Term Goal

FusionCanvas should become the preferred open-source workspace for managing Print-on-Demand product development.

Success is measured by helping creators:

* Generate better ideas
* Create better products
* Reduce repetitive work
* Maintain visibility across their workflow
* Build sustainable creative businesses

Every major decision should move the project closer to that goal.
