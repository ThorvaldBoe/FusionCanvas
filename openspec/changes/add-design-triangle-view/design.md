## Context

The in-flight concept-versions change adds the Concept entity with idea, phrase, graphic direction, audience reaction, risks, quality notes, and design-triangle score metadata, plus a selected-concept invariant. The accepted stage-tool-host capability defines how Concept-stage tools are hosted and receive context. The accepted context-aware-tools capability defines inherited context resolution. The Phase 2 PRD makes the Design Triangle View the central interaction model of the Basic Concept Tool: idea at the top, phrase at the lower left, graphic at the lower right, each node editable and clickable as a refinement target, with optional advisory scoring in the middle and notes for concerns or alternatives.

What is missing is the triangle presentation, the per-node interaction model, the not-used markers, and the advisory scoring display.

## Goals / Non-Goals

**Goals:**

- Present idea, phrase, and graphic direction together, sourced from the selected concept version.
- Make each node editable inline and selectable as the target for refinement.
- Treat idea as required; allow phrase or graphic to be marked as intentionally not used.
- Display advisory scoring (overall, weak element, readiness, critique hint) in the middle when AI scoring is available.
- Capture concept notes for concerns, alternatives, or improvement ideas.
- Keep the view usable with human judgment only — no AI required.
- Delegate all persistence to the concept-versions service.

**Non-Goals:**

- Do not implement mandatory AI critique, mandatory automated scoring, image analysis, or aesthetic validation.
- Do not implement the Basic Concept Tool shell, history pane, or save-as-new-item flow; those belong to FC-0210.
- Do not implement AI scoring itself; that belongs to future AI-capability changes.
- Do not implement version comparison UI.

## Decisions

### 1. The triangle is a read/write view over the selected concept

The Design Triangle View reads idea, phrase, graphic, score, and notes from the selected concept version through the concept-versions service. Every edit is committed through the service's edit operation; the view never writes directly to persistence or workspace storage. When no concept is selected, the view reports the empty state defined by concept-versions rather than fabricating values.

Alternative considered: let the view own a local copy and sync on save. Rejected because it would duplicate the selected-concept invariant and risk divergence.

### 2. Idea is required; phrase and graphic support a not-used marker

The idea node is always required and non-empty. The phrase and graphic nodes support a "not used" marker that distinguishes "the creator has not entered this yet" from "the creator intentionally omitted this." A text-only design marks graphic as not used; a graphic-only design marks phrase as not used. The not-used marker is stored on the concept so it survives reloads.

Alternative considered: treat empty as not-used. Rejected because it loses the distinction between pending and intentionally absent, which matters for readiness checks.

### 3. Each node is a refinement target

Selecting a node sets it as the current refinement target. The Basic Concept Tool (FC-0210) uses the selected target to scope AI improvement or manual focus. The view exposes the selected target through an explicit property rather than relying on focus state, so the hosting tool can read it deterministically.

Alternative considered: infer the target from keyboard focus. Rejected because focus is transient and the hosting tool needs a stable target reference.

### 4. Advisory scoring is optional and non-blocking

When the selected concept carries a design-triangle score (overall, weak element, confidence, critique hint), the middle of the triangle shows it. When no score is present, the middle shows a neutral state, not a failure. Scoring is advisory: low scores do not block stage advancement and high scores do not guarantee success. The view never computes scores; it only displays scores written by the concept-versions service or future AI capabilities.

Alternative considered: compute a heuristic score in the view. Rejected because scoring is owned by concept-versions metadata and future AI capabilities.

### 5. Notes are concept-level

Notes for concerns, alternatives, or improvement ideas are stored on the concept (the concept-versions quality-notes and risks fields) and edited through the concept-versions service. The view does not maintain a separate notes store.

Alternative considered: a view-local notes field. Rejected because notes must survive tool switches and reloads.

### 6. Reuse stage-tool-host context

The Design Triangle View is hosted inside the Basic Concept Tool through the Stage Tool Host. It receives an explicit context snapshot (active store, niche, topic path, item, stage, selected concept, inherited tags, metadata) instead of scraping UI state. It uses application-layer accessors for any additional context.

Alternative considered: read context from the tree view model. Rejected because stage-tool-host already mandates explicit context.

## Risks / Trade-offs

- [View and concept can diverge if the view caches] -> Always read through the service on context change and commit every edit through the service.
- [Not-used marker can be confused with empty] -> Show the marker visibly and distinctly from an empty input.
- [Scoring display can imply false precision] -> Label scores as advisory and never gate advancement on them.

## Migration Plan

No database migration is expected. The view reads and writes concept-versions fields. Existing concepts load with absent scores and a neutral middle state.

## Open Questions

- Should the triangle support a "compare two concepts" mode in FC-0208, or defer comparison to a later change? (Default: defer; FC-0208 is single-concept.)
- Should the not-used marker be a tri-state (pending, not-used, has-value) or a separate boolean? (Default: tri-state through the concept's phrase/graphic fields, to keep the model explicit.)
