## Context

Stage Tool Host sits between the tabbed document window, the workflow stage navigator, and context-aware creation tools. The PRD frames it as a Phase 0 foundation capability: the lower document-window detail area must be able to host the active tool for the current stage while future Idea, Concept, Design, Listing, AI, and plugin capabilities share a common model.

The accepted architecture requires Clean Architecture boundaries. Tool resolution and context construction belong in the application layer. Avalonia views render the host and tool selector, but must not own workflow rules or durable persistence behavior. Plugin loading is not part of this change; however, the registration shape should not prevent plugin-provided tools from being added later.

## Goals / Non-Goals

**Goals:**

- Define application-layer contracts for stage tool descriptors, availability, selection, and context.
- Host the active stage tool in the document window lower detail area.
- Resolve tools by workflow stage and entity context, including topic-based and item-required tools.
- Provide a compact selector when multiple tools are available.
- Keep built-in default tools available through the same registration/query path as future contributed tools where practical.
- Surface unavailable, missing, or failed plugin tools without blocking the default tool.
- Route durable changes through application services or commands.

**Non-Goals:**

- Implement the full external plugin discovery or marketplace system.
- Build the final Idea, Concept, Design, Listing, AI, mockup, or marketplace tool behavior.
- Add arbitrary plugin-defined layout regions around the host.
- Add a scripting engine or multi-pane tool composition.
- Persist every final tool preference rule before real workflow usage proves the correct scope.

## Decisions

### Decision: Put tool resolution and context construction in the application layer

The application layer will own contracts such as stage tool descriptors, supported stages, supported context requirements, availability results, active selection, and a stage tool context snapshot. The UI layer will ask for the current host state and render it.

Rationale: this keeps workflow decisions testable and independent from Avalonia controls while following the accepted Clean Architecture dependency direction.

Alternatives considered:

- UI-owned tool lookup: simpler initially, but would couple workflow rules to views and make plugin support harder.
- Domain-owned tool registry: too infrastructure-oriented for the domain; tools are application capabilities rather than core invariants.

### Decision: Model availability as a result, not only a boolean

Each tool descriptor should be evaluated against the active workflow stage and navigation context and return an availability state such as available, unavailable because an item is required, disabled, or failed to load. The host filters available tools for selection but can use unavailable reasons to render clear empty states.

Rationale: Concept, Design, and Listing tools often require a selected item, while Idea tools can operate from topic context. A reasoned result supports those states without special casing each tool in the UI.

Alternatives considered:

- Boolean `CanOpen`: compact, but loses the reason needed for useful UI and fallback behavior.
- Throwing exceptions for unavailable tools: appropriate only for unexpected failures, not normal context mismatches.

### Decision: Use built-in placeholder/default tools through the registry

The first implementation can register minimal built-in stage tools that represent the default behavior for supported stages. Later feature work can replace placeholder content with richer built-in tool implementations while keeping the host contract stable.

Rationale: the PRD asks built-in tools to follow plugin-facing contracts where practical. Routing default tools through the registry proves the extension point before plugin loading exists.

Alternatives considered:

- Hard-code the default view and only use the registry for plugins later: faster for the first UI, but risks two parallel models.
- Wait until plugin infrastructure exists: blocks Phase 0 and delays the host needed by Phase 1 and Phase 2 tools.

### Decision: Keep selected tool preference scoped conservatively

The initial selection behavior should choose the default available tool unless a valid prior selection exists for the active stage and context kind. The persistence mechanism can be in-memory at first unless the implementation already has a suitable user settings surface.

Rationale: the PRD leaves global versus per-workspace persistence open. Per-stage/context-kind memory avoids surprising cross-stage replacement without committing to durable settings too early.

Alternatives considered:

- Persist globally by tool ID: can make a choice in Idea unexpectedly affect Concept, Design, or Listing.
- Persist per entity: likely too granular and noisy before real usage data exists.

## Risks / Trade-offs

- Plugin-facing contracts may be shaped before real plugins exist -> keep contracts small, application-facing, and focused on host needs.
- Placeholder default tools could imply unfinished product workflows -> render honest empty/default tool states and avoid fake workflow content.
- Context snapshots may grow too large -> start with explicit current store, topic path, selected topic/item, workflow stage, inherited metadata, and nearby work hooks, then add fields through later specs when needed.
- Tool selection persistence may need revision -> treat initial selection as conservative and easy to replace when user settings or workspace settings are specified.
- Tool failures could degrade the document window -> isolate failed tools in availability/load results and keep built-in defaults selectable.

## Migration Plan

1. Add application-layer contracts and tests for tool descriptors, availability evaluation, context construction, and active selection.
2. Add built-in default registrations for the initial workflow stages without implementing deep stage-specific product behavior.
3. Add UI host view model/state that consumes the application-layer host service.
4. Render the lower detail host area, compact selector, unavailable item-required state, and fallback default behavior.
5. Add UI-level tests for selector visibility and state decisions where the current test infrastructure supports it.

Rollback is straightforward while the change is foundational: remove the host UI wiring and built-in registrations, leaving existing shell behavior intact.

## Open Questions

- Should final tool selection preferences persist globally, per workspace, per stage, per context kind, or per entity?
- Which user settings surface should eventually own durable tool selection preferences?
- How much surrounding context should be included before AI-assisted tools are implemented?
