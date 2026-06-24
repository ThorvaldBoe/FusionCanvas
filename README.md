# FusionCanvas

**FusionCanvas** is an open-source desktop toolkit for Print-on-Demand creators.

It is intended to help creators manage the full product workflow from early ideas to published listings, including concept development, design organization, mockup generation, listing preparation, and future automation with platforms such as Shopify and Printify.

FusionCanvas is currently in early planning and scaffolding.

## Vision

Print-on-Demand workflows often involve many disconnected tools, repeated manual steps, scattered ideas, and fragile spreadsheets.

FusionCanvas aims to become a structured creative workspace where creators can:

* Capture broad themes, design ideas, phrases, and graphic concepts
* Refine ideas into stronger product concepts
* Organize designs through a clear production pipeline
* Generate mockups and listing assets
* Prepare product listings for publishing
* Automate repetitive workflow steps
* Extend the system through plugins and integrations

The long-term goal is to provide an open-source foundation for serious PoD creators who want more control over their workflow.

## Core Workflow

FusionCanvas is built around a product pipeline:

```text
Idea → Concept → Design → Listing → Published / Archived
```

The workflow should support both structured planning and creative exploration.

An idea may start as a broad theme, a specific phrase, a visual direction, or a nearly finished product concept. FusionCanvas should help move that idea forward without forcing the creator into a rigid process too early.

## The Design Triangle

A central concept in FusionCanvas is the **Design Triangle**:

```text
Idea + Phrase + Graphic
```

A strong Print-on-Demand design often depends on the relationship between:

* The underlying idea or emotion
* The phrase, joke, quote, or message
* The visual graphic or composition

FusionCanvas will use this model to help evaluate and refine product concepts.

## Planned Features

Early planned areas include:

* Local desktop workspace
* Project and collection organization
* Idea and concept management
* Design pipeline tracking
* Design Triangle scoring and refinement
* Mockup generation
* Listing metadata preparation
* Export workflows
* Plugin-based integrations
* AI-assisted concept refinement
* Shopify and Printify workflow support

## Technology Direction

FusionCanvas is planned as a cross-platform desktop application.

Current intended technology stack:

* **.NET**
* **C#**
* **Avalonia UI**
* Local-first storage
* Plugin-friendly architecture

FusionCanvas will follow Clean Architecture as meaningful implementation code is added. The intended project layers are:

* `FusionCanvas.Domain` for business concepts, invariants, calculations, and workflow rules
* `FusionCanvas.Application` for use cases, orchestration, ports, and application-facing contracts
* `FusionCanvas.Integration` for persistence, file system access, marketplace APIs, AI providers, plugin host adapters, and other external systems
* `FusionCanvas.App` for Avalonia UI, presentation state, navigation, and user interaction

Dependencies should point inward toward the domain and application layers. Implementation should apply SOLID principles pragmatically, favoring focused responsibilities and justified abstractions without unnecessary bloat.

Every feature that adds or changes behavior should include appropriate automated tests. Test projects should mirror the production project where practical, such as `tests/FusionCanvas.Domain.Tests` or `tests/FusionCanvas.Application.Tests`.

The project is intended to remain useful as a local desktop tool even without cloud services.

## Testing Baseline

Run the automated baseline from the repository root:

```powershell
dotnet test .\FusionCanvas.sln
```

The baseline test suite uses mirrored test projects under `tests/`:

* `FusionCanvas.Domain.Tests` protects domain rules, entity relationships, invariants, workflow decisions, and persistence-neutral boundaries.
* `FusionCanvas.Application.Tests` protects use-case orchestration and application contracts with deterministic collaborators.
* `FusionCanvas.Integration.Tests` protects local persistence and workspace file boundaries with isolated temporary resources.
* `FusionCanvas.App.Tests` protects UI-owned state and navigation decisions that can be tested without full visual UI automation.

New foundational behavior should include focused automated tests in the relevant layer. Static Avalonia markup, framework-owned rendering, full UI automation, visual regression infrastructure, performance benchmarking, marketplace integration access, AI provider access, and manual QA process setup are outside the Phase 0 baseline unless a later OpenSpec change accepts that scope.

## Project Status

FusionCanvas is currently in the initial public repository setup phase.

The first milestones are:

1. Define project vision and architecture documents
2. Set up OpenSpec-based development workflow
3. Create the initial Avalonia application shell
4. Implement local workspace navigation
5. Build the first version of idea and concept management

## Development Approach

FusionCanvas will use a specification-first development process.

For meaningful changes, contributors should first describe the intended behavior in OpenSpec before implementing code. This helps keep the project understandable for both humans and AI-assisted development tools.

The expected workflow is:

```text
Propose → Implement → Validate → Archive
```

The original LifeOS product requirements are kept in `docs/LifeOS/PRD` as planning and product-intent source material. Before proposing a related OpenSpec change, review the relevant PRD files and translate only the applicable behavior into the new OpenSpec proposal and spec deltas. The PRDs should remain outside `openspec/specs` unless their behavior has gone through the OpenSpec workflow and been accepted.

## Repository Structure

The planned repository structure is:

```text
FusionCanvas/
├─ README.md
├─ LICENSE
├─ CONTRIBUTING.md
├─ ROADMAP.md
├─ docs/
│  ├─ product-vision.md
│  ├─ architecture.md
│  ├─ design-pipeline.md
│  ├─ plugin-model.md
│  ├─ ai-workflow.md
│  ├─ LifeOS/
│  │  └─ PRD/
│  └─ decisions/
├─ openspec/
│  ├─ project.md
│  ├─ specs/
│  └─ changes/
└─ src/
```

As implementation grows, `src/` should contain the Clean Architecture projects `FusionCanvas.Domain`, `FusionCanvas.Application`, `FusionCanvas.Integration`, and `FusionCanvas.App`. Feature tests should live under `tests/` in projects that mirror the production layer, such as `FusionCanvas.Domain.Tests`, `FusionCanvas.Application.Tests`, `FusionCanvas.Integration.Tests`, and `FusionCanvas.App.Tests`.

## License

FusionCanvas is released under the MIT License.

## Current Focus

The current focus is to create a clean foundation before adding application code.

The first working version should provide a simple desktop shell with:

* A main window
* Left-side navigation tree
* Right-side detail panel
* Placeholder workspace model
* Basic project structure

No mockup generation, listing automation, or platform integrations should be implemented before the core workspace model is defined.

## Contributing

FusionCanvas is in an early stage, so contributions should start with discussion, specifications, or small focused improvements.

Large changes should be proposed through OpenSpec before implementation.

## Name

FusionCanvas represents the fusion of ideas, graphics, product workflows, automation, and creative decision-making into one workspace.
