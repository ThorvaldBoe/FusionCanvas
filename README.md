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
* `FusionCanvas.App.Tests` protects UI-owned state and navigation decisions and is the intended home for focused Avalonia headless view tests.

New behavior should include focused automated tests in the relevant layer. Use framework-free tests for UI decision logic and Avalonia headless tests for meaningful view construction, binding, control-state, input, focus, selection, or visual-tree behavior. Static markup, full live-desktop automation, visual regression infrastructure, performance benchmarking, marketplace integration access, and AI provider access remain outside the routine baseline. The headless harness has not been added yet; current app tests cover view models and coordination rather than instantiated views.

## Project Status

FusionCanvas is in early active development. Current and candidate work is recorded in `docs/roadmap.md`; accepted behavior lives in `openspec/specs`, and completed change history lives under `openspec/changes/archive`.

## Development Approach

FusionCanvas uses a specification-first, rolling module development process.

For meaningful changes, contributors and agents first define one cohesive delivery module, build shared understanding through discussion, and create an implementation-ready OpenSpec delivery package. Detailed planning is limited to the next module so later specifications do not become stale before use.

One delivery module normally maps to one OpenSpec change. Its proposal is the module-level anchor, and the delta specs, design, tasks, and verification artifacts carry their respective details; a separate module-specification document is not created by default.

The expected workflow is:

```text
Discover → Define module → Propose → Review → Apply → Verify → Learn → Archive
```

One module normally maps to one OpenSpec change. Requirements and conceptual/functional design define the expected behavior; a dedicated implementation plan gives the assigned implementation agent explicit layer, data, UI, edge-case, sequencing, and test guidance. Observable acceptance criteria are pass/fail completion gates and are mapped to evidence in `verification.md`.

Higher-reasoning agents or humans handle discovery, specification, design review, and ambiguous corrections. Lower-cost agents can implement bounded tasks after the delivery package is approved and sufficiently explicit. Deterministic tests, including applicable Avalonia headless view tests, are the routine verification path on every agent. Live desktop testing is optional and may be used ad hoc for platform-specific or visual risks.

The original LifeOS planning files remain under `docs/LifeOS` as optional, potentially stale historical reference. They are not required reading, a current roadmap, or acceptance authority.

## Repository Structure

The planned repository structure is:

```text
FusionCanvas/
├─ README.md
├─ AGENTS.md
├─ LICENSE
├─ FusionCanvas.sln
├─ docs/
│  ├─ architecture.md
│  ├─ principles.md
│  ├─ ui-guidelines.md
│  ├─ ux-guidelines.md
│  ├─ qa-review.md
│  ├─ data-model.md
│  ├─ design-pipeline.md
│  ├─ plugin-model.md
│  ├─ product-vision.md
│  ├─ strategic-decisions.md
│  ├─ roadmap.md
│  └─ LifeOS/
│     └─ PRD/
├─ openspec/
│  ├─ project.md
│  ├─ specs/
│  └─ changes/
├─ src/
│  ├─ FusionCanvas.Domain/
│  ├─ FusionCanvas.Application/
│  ├─ FusionCanvas.Integration/
│  └─ FusionCanvas.App/
└─ tests/
   ├─ FusionCanvas.Domain.Tests/
   ├─ FusionCanvas.Application.Tests/
   ├─ FusionCanvas.Integration.Tests/
   └─ FusionCanvas.App.Tests/
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

All new and substantially modified C# code should follow the repository's [`docs/coding-standard.md`](docs/coding-standard.md).

## Name

FusionCanvas represents the fusion of ideas, graphics, product workflows, automation, and creative decision-making into one workspace.
