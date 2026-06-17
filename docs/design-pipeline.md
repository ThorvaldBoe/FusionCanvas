# FusionCanvas Design Pipeline

## Purpose

This document defines the lifecycle of ideas and products within FusionCanvas.

The Design Pipeline is the core workflow of the application. It provides structure without restricting creativity and helps creators move from vague ideas to prepared listings.

Unlike traditional Print on Demand tools that begin with finished designs, FusionCanvas begins with the earliest stages of ideation and concept development.

## Core Philosophy

Many creators do not start with a completed design.

They start with:

- a niche
- a trend
- a customer observation
- a joke
- a phrase
- a graphic idea
- a market opportunity

FusionCanvas should support the journey from initial inspiration to prepared product listing while preserving creative context.

The goal is not simply to track work, but to improve the quality of ideas before production effort is invested.

## Pipeline Overview

The standard FusionCanvas pipeline consists of four core creative stages:

```text
Idea
Concept
Design
Listing
```

Archive is a related retained state for work that is rejected, retracted, paused indefinitely, or useful only for learning. Archive may appear near the workflow, but it should not obscure the core Idea -> Concept -> Design -> Listing progression.

Not every item must pass through every stage. A creator may begin with a rough idea, a clear concept, a phrase, an existing image direction, or even a nearly complete design. FusionCanvas should allow users to skip ahead when they already have enough information, while still preserving the context of earlier stages where it exists.

## Stage Definitions

### Idea

An Idea represents a specific observation, situation, emotion, problem, opportunity, or product seed.

Ideas are intentionally lightweight and should be easy to capture.

Examples:

- dog owners think their dogs own the house
- campers enjoy escaping modern life
- coffee drinkers treat coffee as fuel
- gardeners never stop buying plants

Ideas are raw material for future concepts.

Idea-stage tools may work from a selected topic and create new items. They should understand the current store, niche, topic path, tags, and nearby work so generated or captured ideas land in the right context.

### Concept

A Concept represents a potential product direction.

At this stage the creator begins evaluating how the idea might become a product. Concept work belongs to a single existing item in the navigation structure.

Concepts are where the Design Triangle becomes important:

```text
Idea
Phrase
Graphic
```

A concept may contain:

- notes
- sketches
- references
- alternative phrases
- graphic ideas
- design triangle evaluations
- selected concept versions

Promising alternate directions can branch into new items without leaving the current concept workflow.

### Design

A Design represents an actual implementation of a concept.

Examples:

- artwork
- SVG files
- PNG exports
- design source files
- AI-generated drafts
- cleanup passes
- variants for dark shirts, light shirts, or specific products

Multiple designs may be derived from the same concept.

The Design stage should allow experimentation without confusion. Many variants can exist, but one or more must be deliberately promoted as final selected artwork before later listing, mockup, export, or product-variant work treats them as ready.

### Listing

A Listing represents a marketplace-ready product preparation record.

A listing may contain:

- product title
- description
- keywords
- tags
- mockups
- price
- status
- selected final design
- product/template/color choices
- marketplace notes

The listing stage separates product presentation from design creation.

The basic Listing tool should support local mockup generation and listing preparation. Direct Printify, Shopify, Etsy, or other marketplace publishing belongs to later integrations or optional plugins.

### Archive

Archive is a retained state for work that should leave the active workflow without being forgotten.

Examples:

- rejected concepts
- paused ideas
- retired products
- obsolete designs
- experiments preserved for learning

Archive should preserve history while reducing active workspace noise.

## Workflow Stage vs Status

Pipeline stage and operational status are separate concepts.

`WorkflowStage` drives the visible stage navigator and stage filtering:

```text
Idea
Concept
Design
Listing
Archive
```

`Status` describes operational state:

```text
Draft
Published
Paused
Rejected
```

Do not use stage-like statuses such as Idea, Concept, Design, and Listing as status values. Those belong to `WorkflowStage`.

## Workflow Stage Navigator

The document window should include a workflow stage navigator for the current item.

The navigator should show Idea, Concept, Design, and Listing as distinct stage boxes. The current stage should be visually emphasized. Completed or available prior stages should be enabled. Future stages that the item has not reached should be disabled.

Clicking an enabled stage should open the relevant information for that stage. Clicking a disabled future stage should not navigate.

This gives the creator a constant answer to three questions:

- Where is this item in the workflow?
- Which previous stages can I review?
- What has not been created yet?

## Tree Structure

FusionCanvas uses a hierarchical structure to organize work.

```text
Store
  Niche
    Topic
      Item
    Topic
  Niche
```

The hierarchy should remain stable throughout the product lifecycle.

Unlike many systems, items should not lose their organizational context when promoted through the pipeline.

Example:

```text
Camping
  Weekend Forecast: Camping
    Idea
    Concept
    Design
    Listing
```

This allows creators to understand the full history and relationship between items.

## Promotion and Evolution

Items may evolve through the pipeline.

Example:

```text
Theme:
Coffee

Idea:
People depend on coffee to function

Concept:
Powered by Coffee and Determination

Design:
Typography + coffee mug illustration

Listing:
Prepared for Etsy
```

FusionCanvas should preserve these relationships throughout the lifecycle.

Creators should always be able to trace a prepared or published product back to its original idea.

## Multiple Outputs

One idea may produce many concepts.

One concept may produce many designs.

One design may produce many listings.

Example:

```text
Idea
  Concept A
    Design A1
    Design A2
  Concept B
    Design B1
    Design B2
```

The system should support branching without duplicating unnecessary information.

## Pipeline Success Criteria

The Design Pipeline is successful if creators can:

- capture ideas quickly
- refine concepts effectively
- maintain visibility across their workflow
- preserve relationships between ideas and products
- track product development from inspiration to listing preparation
- reduce lost opportunities and duplicated effort

The pipeline should provide structure while remaining flexible enough to support different creative processes and working styles.
