## Context

FusionCanvas currently has a buildable Avalonia shell in a single application project and architecture documents that describe modular, local-first, plugin-friendly growth. The existing direction intentionally avoided splitting projects before there was production code, but the next feature work will introduce domain concepts, application workflows, integrations, persistence, and UI behavior that need clear boundaries.

This change updates the architecture guidance before those responsibilities become tangled. It keeps the current incremental-complexity principle, while making Clean Architecture the default shape for new system behavior.

## Goals / Non-Goals

**Goals:**
- Establish Clean Architecture as the expected project structure for meaningful implementation work.
- Define domain, application, integration, and UI layer responsibilities.
- Require dependencies to point inward so domain behavior stays independent.
- Align documentation, OpenSpec guidance, and future solution structure.

**Non-Goals:**
- Refactor the existing Avalonia shell immediately.
- Implement workspace, product pipeline, storage, plugin, AI, or marketplace behavior.
- Decide final naming for every future project beyond the required layer roles.
- Introduce new runtime dependencies.

## Decisions

### Use Clean Architecture as the default structure

FusionCanvas will follow a Clean Architecture structure with separate projects for domain, application, integration, and UI layers as meaningful implementation code is added.

Rationale: the product is expected to grow across workflow orchestration, local storage, plugins, marketplace APIs, AI providers, and desktop UI. A Clean Architecture structure protects core product rules from framework and provider churn.

Alternative considered: continue with a single application project until the code becomes uncomfortable. This keeps setup cheaper in the short term, but it makes architectural drift harder to detect and correct.

### Define layer responsibilities explicitly

The domain layer owns business concepts and rules. The application layer owns use cases and orchestration. The integration layer owns external systems such as persistence, marketplace APIs, file system adapters, AI providers, and plugin host adapters. The UI layer owns Avalonia presentation and user interaction.

Rationale: explicit responsibilities make future OpenSpec proposals easier to place and review.

Alternative considered: organize only by product module, such as Workspace, Pipeline, Listing, and Mockup. Product modules are still useful inside layers, but layer boundaries are needed to keep UI and infrastructure concerns out of domain behavior.

### Keep dependencies pointing inward

The domain layer must not depend on application, integration, or UI projects. The application layer may depend on domain abstractions and can define ports. Integration and UI projects may depend on application contracts and domain types as needed.

Rationale: this dependency direction keeps core behavior testable without Avalonia, SQLite, marketplace services, AI providers, or plugin runtime setup.

Alternative considered: let projects reference whatever is convenient. This is faster for early prototypes, but it undermines local-first reliability and makes integrations harder to replace.

### Introduce project splits incrementally

Architecture documentation should describe the target structure, but implementation can split projects when a layer has real responsibilities to hold.

Rationale: this preserves the project's incremental-complexity principle while making the intended destination clear.

Alternative considered: create all layer projects immediately even if some remain empty. This would signal intent strongly, but empty projects add maintenance overhead and may invite placeholder abstractions.

## Risks / Trade-offs

- Project boundaries could slow early iteration -> Mitigation: split only when real domain, application, integration, or UI responsibilities exist.
- Layer names could become inconsistent across proposals -> Mitigation: document canonical roles and prefer stable project names such as `FusionCanvas.Domain`, `FusionCanvas.Application`, `FusionCanvas.Integration`, and `FusionCanvas.App`.
- Integrations may need shared contracts with plugins -> Mitigation: keep contracts in inward-facing application or domain abstractions unless they are explicitly plugin API contracts.
- UI convenience code could leak business rules into Avalonia views -> Mitigation: require new feature proposals to identify which layer owns each behavior.

## Migration Plan

1. Update architecture and project guidance to state the Clean Architecture target.
2. When implementation begins, update the solution structure only as needed for current responsibilities.
3. Preserve the existing Avalonia shell while moving new non-UI behavior into the appropriate layer project.
4. Validate the solution still builds after any project split.

Rollback is documentation-only unless implementation creates new projects. If a project split proves premature, the guidance can remain while unused projects are removed before feature work depends on them.

## Open Questions

- Should the integration layer be named `FusionCanvas.Integration` or `FusionCanvas.Infrastructure` in the final solution?
- Should plugin contracts live in the application layer initially, or in a separate contracts project once the plugin system begins?
