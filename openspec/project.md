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

Significant changes should begin with an OpenSpec proposal before implementation.

Expected workflow:

```text
Propose
↓
Review
↓
Implement
↓
Validate
↓
Archive
```

Specifications are considered the source of truth for intended system behavior.

Code should follow specifications, not the other way around.

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
