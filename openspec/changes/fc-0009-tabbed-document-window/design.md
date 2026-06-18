## Context

FusionCanvas currently has the foundation projects and an initial Avalonia main window, but the workspace UI has not yet grown into a coordinated navigation, workflow, and document surface. FC-0009 introduces the document-window behavior described by the Phase 0 PRD: users need multiple open working contexts, and the selected tab must drive navigation context, workflow stage state, and the detail area.

This change depends conceptually on the navigation tree and workflow stage navigator foundations, but it should stay lightweight. It should not introduce persistence, plugin discovery, session restore, marketplace behavior, or advanced pane management.

## Goals / Non-Goals

**Goals:**

- Represent open document contexts as tabs inside the main workspace document window.
- Keep an active document context that includes a stable context identity, display title, hierarchy location, and workflow stage.
- Make tab selection the source of truth for the visible workflow stage and detail view.
- Allow navigation-open actions to create or activate tabs without discarding other open contexts.
- Allow users to close tabs and move active focus predictably.
- Keep tab state testable outside static Avalonia markup.

**Non-Goals:**

- Persist open tabs across app restarts.
- Add split panes, tab groups, drag-and-drop tab rearrangement, or restored closed tabs.
- Implement plugin loading or marketplace behavior.
- Build complete Phase 1 item editors, listing inspectors, or persistence-backed workspace data.
- Replace future navigation or stage tool host specs.

## Decisions

### Keep Document Tab State In UI-Owned Presentation Models

The initial implementation should model document tabs in `FusionCanvas.App` because the behavior is presentation coordination: opening, selecting, displaying, and closing visible working contexts. Domain rules are not needed until real product entities or workflow invariants are introduced.

Alternative considered: place tab management in the domain or application layer immediately. That would make the foundation heavier than the current behavior justifies and risks turning transient UI state into product state before persistence and workflow use cases exist.

### Use A Small Document Context Shape

Each tab should carry enough context to coordinate the UI: a document context id, title, optional hierarchy path or navigation reference, current workflow stage, and selected detail tool or view key. This keeps the tab strip, navigation highlight, workflow stage navigator, and detail area synchronized without binding those surfaces directly to each other's controls.

Alternative considered: store only a title and stage on each tab. That would satisfy the visible tab strip but leave navigation restoration under-specified.

### Opening An Already Open Context Activates The Existing Tab

When a navigation action opens a context that is already present, the document window should activate that tab instead of creating an indistinguishable duplicate. A later feature can define whether the same item at different workflow stages deserves separate tabs.

Alternative considered: always create a new tab. That keeps implementation simple but makes context coordination noisy and weakens the expectation that a tab represents a working context.

### Closing The Active Tab Selects A Neighbor

When the active tab closes and other tabs remain, the document window should select the nearest remaining tab so the workspace always has a coherent active context. If no tabs remain, the document area should present an empty workspace state.

Alternative considered: leave no active context even while tabs remain. That creates unnecessary blank states and makes navigation and workflow coordination ambiguous.

## Risks / Trade-offs

- Context identity may evolve as real workspace entities mature -> Keep the initial context model simple and avoid persistence-specific identifiers where possible.
- Navigation and workflow stage foundations may still be incomplete during implementation -> Provide testable coordination state and simple placeholders rather than wiring to unavailable production behavior.
- A single tab per context may not cover future "same item, different stage" workflows -> Document this as an open product question and keep the context key easy to extend.
- UI tests can become brittle if they assert static layout details -> Prefer tests for tab state transitions and view model coordination over superficial markup assertions.

## Migration Plan

No data migration is required. The change adds transient document-window behavior to the desktop app foundation. Rollback consists of removing the new presentation state, view markup, and tests.

## Open Questions

- Should topic-level contexts and concrete item contexts both open as tabs in the first implementation, or should the first pass focus on concrete items?
- Should selecting a different workflow stage for the same item update the current tab or create a stage-specific tab?
- Should closed tabs be restorable within the same app session?
