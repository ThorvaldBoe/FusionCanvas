## Context

The in-flight concept-versions change adds the Concept entity with idea, phrase, graphic, score, and notes; the selected-concept invariant; and supersede/reject lifecycle. The in-flight design-triangle-view change adds the triangle presentation with per-node editing, refinement-target selection, not-used markers, advisory scoring, and notes. The in-flight creative-history-timeline change adds the cross-stage event log. The accepted stage-tool-host, context-aware-tools, and workflow-stage-navigator capabilities define hosting, context, and stage advancement. The Phase 2 PRD makes the Basic Concept Tool the default Concept-stage tool: it hosts the triangle, supports manual and AI-assisted refinement, records history, saves promising alternates as new items, and advances the item toward Design. AI is optional and depends on a configured provider.

What is missing is the tool itself: the hosting registration, the action area, the AI-assisted refinement flow, the save-as-new-item flow, the history pane with restore, and the stage-advancement action.

## Goals / Non-Goals

**Goals:**

- Provide the default Concept-stage tool for a single selected item, hosted through the Stage Tool Host.
- Support manual concept refinement without requiring AI.
- Optionally invoke AI to improve the selected triangle node when a provider is configured.
- Constrain AI to the selected node unless the user accepts a broader rewrite; never overwrite saved concept data without approval.
- Let the user accept, edit, reject, regenerate, or save an AI suggestion as a new item.
- Record history-pane entries and feed important entries into the Creative History Timeline.
- Allow restoring a previous history entry.
- Advance the item toward Design through the workflow-stage-navigator.

**Non-Goals:**

- Do not implement final design image generation, detailed design production UI, marketplace listing metadata generation, sales prediction, guaranteed originality, full legal validation, full undo/redo, or multi-item batch refinement.
- Do not implement the AI provider or AI settings; those belong to future AI-capability changes.
- Do not implement the triangle presentation itself; that belongs to FC-0208.
- Do not implement concept storage; that belongs to FC-0202.

## Decisions

### 1. The tool is a Stage Tool Host built-in default

The Basic Concept Tool is registered as the default Concept-stage tool through the same host-facing tool registry used to query available stage tools. It declares support for the Concept stage and requires a selected-item context. When no item is selected, the host shows an empty state or creation path instead of opening the tool, per the accepted stage-tool-host behavior. Future plugin-provided Concept-stage tools can coexist and remain selectable through the host.

Alternative considered: open the tool without an item and let it prompt for one. Rejected because the PRD and stage-tool-host require item context for Concept work.

### 2. Manual refinement is always available; AI is conditional

Manual edits to idea, phrase, and graphic nodes are valid and saveable without any AI provider configured. AI-assisted improvement of the selected node is available only when an `IConceptRefinementProvider` is configured (a future AI-capability change provides the implementation). The tool exposes AI actions as disabled when no provider is configured, with a clear explanation.

Alternative considered: hide AI actions entirely when no provider is configured. Rejected because disabled actions with explanation communicate the capability better.

### 3. AI affects only the selected node unless a broader rewrite is accepted

An AI improvement request targets the currently selected triangle node and returns a suggestion for that node only. The tool presents the suggestion alongside the current value; the user accepts, edits, rejects, regenerates, or saves it as a new item. Accepting updates only the selected node on the current concept version unless the user explicitly chooses a "broader rewrite" action, which creates a new concept version through the concept-versions service. AI never overwrites saved concept data without approval.

Alternative considered: let AI update multiple nodes freely. Rejected because the PRD requires explicit approval and per-node focus.

### 4. Save-as-new-item creates a sibling listing under the current topic

A promising alternate suggestion can be saved as a new item under the current topic in one action. The new item receives the suggested idea, phrase, and graphic values, source metadata showing it came from the current concept session, applicable inherited tags and topic metadata, and the Idea workflow stage. The current item remains open in the Concept tool. The new item is created through the existing listing-management service, so FC-0210 does not bypass listing creation invariants.

Alternative considered: create a new concept version on the current item. Rejected because the PRD wants creative branching into a separate item.

### 5. History pane is local; important entries feed the timeline

The history pane is local to the current item and concept workflow. It receives entries for manual edits, AI suggestion requested/accepted/rejected, score updates, concept version changes, restored states, and suggestions saved as new items. Each entry preserves enough data to restore a useful previous state. The tool records important entries into the Creative History Timeline through the timeline writer. The user can restore a previous history entry; restoration is a Concept-tool action (not a timeline action) and creates or selects a concept version through the concept-versions service.

Alternative considered: make the history pane read-only and defer restore to undo/redo. Rejected because the PRD explicitly allows restore from the Concept tool history pane.

### 6. Advisory scoring comes from the provider, not the tool

The tool displays the advisory score from the selected concept's score metadata. When a provider is configured, the tool may request a score as part of an AI improvement request and write it through the concept-versions service. The tool never computes scores itself. Low scores do not block advancement.

Alternative considered: compute a heuristic score in the tool. Rejected because scoring is owned by concept-versions metadata and future AI capabilities.

### 7. Stage advancement goes through the workflow-stage-navigator

The tool's "proceed to Design" action requests the workflow-stage-navigator to advance the listing to the Design stage. The navigator may accept or reject the transition according to accepted stage rules. The tool does not set the stage field directly.

Alternative considered: set the stage directly. Rejected to avoid duplicating stage-transition rules.

## Risks / Trade-offs

- [AI provider port may drift before FC-0401/FC-0405 land] -> Keep the port minimal (improve-node and score) and document it as provisional.
- [History-pane restore can conflict with concept-versions supersede] -> Implement restore as an explicit supersede or version-selection through the concept-versions service so the invariant holds.
- [Save-as-new-item can create clutter] -> Default the new item to the Idea stage and let the Idea Inbox triage it.

## Migration Plan

No database migration is expected. The tool reads and writes through existing and in-flight services. Existing listings load with the tool available when their stage is Concept.

## Open Questions

- Should the history pane restore create a new concept version or replace the current one in place? (Default: create a new version through supersede, to preserve history.)
- Should AI scoring be requested automatically on every node change, or only on explicit user action? (Default: only on explicit action, to avoid surprise provider calls.)
