## 1. Triangle View Model and Hosting

- [ ] 1.1 Add a `DesignTriangleViewModel` that reads idea, phrase, graphic, not-used markers, score, and notes from the selected concept through the concept-versions service.
- [ ] 1.2 Expose the selected refinement-target node as an explicit property the hosting tool can read.
- [ ] 1.3 Receive an explicit context snapshot from the Stage Tool Host and refresh on context change.
- [ ] 1.4 Report the "no concept selected" empty state defined by concept-versions without fabricating values.
- [ ] 1.5 Add view-model tests for load, context refresh, refinement-target selection, and empty state.

## 2. Node Editing and Not-Used Markers

- [ ] 2.1 Implement inline node editing that commits through the concept-versions service and refreshes from the selected concept.
- [ ] 2.2 Enforce idea-required validation and preserve recoverable input on rejection.
- [ ] 2.3 Implement phrase and graphic not-used markers distinct from empty pending values.
- [ ] 2.4 Add view-model tests for node edits, idea-required rejection, not-used markers, and persistence-failure recovery.

## 3. Advisory Scoring and Notes

- [ ] 3.1 Display overall score, weak element, readiness state, and critique hint in the middle when present, with an advisory label.
- [ ] 3.2 Show a neutral middle state when no score is present.
- [ ] 3.3 Ensure low or absent scores never block stage advancement or selection.
- [ ] 3.4 Implement a notes area that persists concerns, alternatives, or improvement ideas on the concept with dirty tracking and unsaved-change prompts.
- [ ] 3.5 Add view-model tests for score display, neutral state, non-blocking advancement, notes save, and unsaved-change prompts.

## 4. Avalonia Triangle Control

- [ ] 4.1 Add an Avalonia triangle control hosted inside the Basic Concept Tool through the Stage Tool Host.
- [ ] 4.2 Apply compact action sizing, icon tooltips, keyboard-reachable editing and cancellation, busy states, duplicate-submission prevention, and shared desktop control guidance.
- [ ] 4.3 Add UI automation tests for node editing, refinement-target selection, not-used markers, score display, notes, keyboard flow, busy states, and error recovery.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve concept-versions, stage-tool-host, context-aware-tools, and workflow-stage-navigator regressions.
- [ ] 5.2 Manually verify triangle display, node editing, refinement-target selection, not-used markers, advisory scoring, notes, context refresh, keyboard flow, and application reload.
- [ ] 5.3 Run strict OpenSpec validation and confirm every design-triangle-view scenario is covered by implementation or automated tests.
