## Context

FusionCanvas currently has a buildable Avalonia shell in a single application project and architecture documents that describe modular, local-first, plugin-friendly growth. The existing direction intentionally avoided splitting projects before there was production code, but the next feature work will introduce domain concepts, application workflows, integrations, persistence, and UI behavior that need clear boundaries, disciplined design principles, and reliable tests.

This change updates the architecture guidance before those responsibilities become tangled. It keeps the current incremental-complexity principle, while making Clean Architecture, SOLID design, and appropriate unit testing the default shape for new system behavior.

## Goals / Non-Goals

**Goals:**
- Establish Clean Architecture as the expected project structure for meaningful implementation work.
- Establish SOLID principles as the expected code design guidance without encouraging bloated abstractions.
- Establish unit testing as a core architectural expectation for feature work.
- Define domain, application, integration, and UI layer responsibilities.
- Require dependencies to point inward so domain behavior stays independent.
- Align documentation, OpenSpec guidance, and future solution structure.

**Non-Goals:**
- Refactor the existing Avalonia shell immediately.
- Implement workspace, product pipeline, storage, plugin, AI, or marketplace behavior.
- Decide final naming for every future project beyond the required layer roles.
- Introduce new runtime dependencies.
- Require exhaustive tests for trivial generated or framework-owned behavior.

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

### Apply SOLID pragmatically

FusionCanvas will use SOLID principles to guide class, interface, and dependency design. Single responsibility, dependency inversion, and interface segregation are especially important across domain, application, integration, and UI boundaries.

Rationale: SOLID keeps code easier to test and change as features grow, but it should be applied to solve real design pressure rather than to produce layers of indirection by default.

Alternative considered: rely only on Clean Architecture project boundaries. Project boundaries help at a coarse level, but individual classes can still become tightly coupled, oversized, or hard to test without code-level design guidance.

### Treat unit testing as architectural support

Every feature should include appropriate unit tests for domain rules, application use cases, and integration-facing contracts. UI behavior can be tested at the appropriate level when it contains decision logic, while purely declarative Avalonia markup does not need superficial tests.

Rationale: Clean Architecture is valuable partly because it makes core behavior testable without UI, persistence, marketplace APIs, AI providers, or plugin hosts. Requiring appropriate tests ensures the architecture remains real rather than only documentary.

Alternative considered: defer tests until features stabilize. This can feel faster early, but it makes architectural drift harder to spot and raises the cost of refactoring once workflows become central.

## Risks / Trade-offs

- Project boundaries could slow early iteration -> Mitigation: split only when real domain, application, integration, or UI responsibilities exist.
- Layer names could become inconsistent across proposals -> Mitigation: document canonical roles and prefer stable project names such as `FusionCanvas.Domain`, `FusionCanvas.Application`, `FusionCanvas.Integration`, and `FusionCanvas.App`.
- Integrations may need shared contracts with plugins -> Mitigation: keep contracts in inward-facing application or domain abstractions unless they are explicitly plugin API contracts.
- UI convenience code could leak business rules into Avalonia views -> Mitigation: require new feature proposals to identify which layer owns each behavior.
- SOLID guidance could encourage unnecessary interfaces or abstractions -> Mitigation: require abstractions to protect a real boundary, variation point, or test seam.
- Test expectations could slow small changes -> Mitigation: require appropriate tests based on risk and behavior, not blanket tests for every line or framework artifact.

## Migration Plan

1. Update architecture and project guidance to state the Clean Architecture target.
2. Update guidance to include pragmatic SOLID principles and appropriate unit testing expectations.
3. When implementation begins, update the solution structure only as needed for current responsibilities.
4. Preserve the existing Avalonia shell while moving new non-UI behavior into the appropriate layer project.
5. Validate the solution still builds and relevant tests pass after any project split.

Rollback is documentation-only unless implementation creates new projects. If a project split proves premature, the guidance can remain while unused projects are removed before feature work depends on them.

## Open Questions

- Should the integration layer be named `FusionCanvas.Integration` or `FusionCanvas.Infrastructure` in the final solution?
- Should plugin contracts live in the application layer initially, or in a separate contracts project once the plugin system begins?
- Which .NET test framework should become the default for new unit test projects?
