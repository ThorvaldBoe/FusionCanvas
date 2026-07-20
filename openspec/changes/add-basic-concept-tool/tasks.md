## 1. Tool Registration and Hosting

- [ ] 1.1 Register the Basic Concept Tool as the default Concept-stage tool through the Stage Tool Host registry, declaring Concept-stage support and selected-item context requirement.
- [ ] 1.2 Show an empty state or creation path when no item is selected instead of opening the tool.
- [ ] 1.3 Receive explicit context (store, niche, topic path, item, stage, selected concept, inherited tags, metadata) from the Stage Tool Host and refresh on context change.
- [ ] 1.4 Add view-model tests for registration, context reception, empty state, and coexistence with plugin tools.

## 2. Manual Refinement

- [ ] 2.1 Host the Design Triangle View and route node edits, not-used markers, and notes through the concept-versions service.
- [ ] 2.2 Ensure manual editing, not-used markers, notes, and save are fully available without an AI provider.
- [ ] 2.3 Add view-model tests for manual edits, not-used markers, notes save, and persistence-failure recovery.

## 3. AI-Assisted Refinement Provider Port

- [ ] 3.1 Define an `IConceptRefinementProvider` application port for improve-node and score requests, documented as provisional until FC-0401/FC-0405 land.
- [ ] 3.2 Implement AI improvement that targets the selected node, uses the full context, presents suggestions without overwriting, and constrains updates to the selected node unless a broader rewrite is explicitly accepted.
- [ ] 3.3 Implement accept, edit, reject, regenerate, and save-as-new-item actions with explicit approval and history entries.
- [ ] 3.4 Disable AI actions when no provider is configured, with a clear explanation.
- [ ] 3.5 Add application tests with a deterministic fake provider for improve-node, broader-rewrite, accept/edit/reject/regenerate, no-provider disabled state, and provider failure recovery.

## 4. Save-as-New-Item, History, and Scoring

- [ ] 4.1 Implement save-as-new-item through the listing-management service, assigning Idea stage, copying idea/phrase/graphic and source metadata, and applying inherited tags and topic metadata.
- [ ] 4.2 Implement a local history pane that records manual edits, AI suggestion requested/accepted/rejected, score updates, concept version changes, restored states, and suggestions saved as new items.
- [ ] 4.3 Implement restore of a previous history entry through the concept-versions service (supersede or version selection) without deleting the current concept.
- [ ] 4.4 Feed important Concept-stage events into the Creative History Timeline through the timeline writer in the same atomic snapshot.
- [ ] 4.5 Display advisory scoring from the selected concept and request scores only on explicit user action when a provider is configured; never gate advancement on scores.
- [ ] 4.6 Add view-model and UI tests for save-as-new-item, history recording, restore, timeline feeding, score display, and explicit score request.

## 5. Stage Advancement and UI

- [ ] 5.1 Implement the proceed-to-Design action through the workflow-stage-navigator and surface accepted or rejected transitions.
- [ ] 5.2 Add an Avalonia tool surface with context summary, stage tool selector, triangle canvas, action area, and history pane.
- [ ] 5.3 Apply compact action sizing, icon tooltips, keyboard-reachable actions, busy states, duplicate-submission prevention, and shared desktop control guidance.
- [ ] 5.4 Add UI automation tests for manual refinement, AI-assisted refinement against a fake provider, accept/reject/regenerate/save-as-new-item, history restore, scoring display, stage advancement, and keyboard flow.

## 6. Verification

- [ ] 6.1 Run domain, application, integration, app, and UI automation test suites and resolve concept-versions, design-triangle-view, creative-history-timeline, stage-tool-host, context-aware-tools, workflow-stage-navigator, and listing-management regressions.
- [ ] 6.2 Manually verify manual refinement, AI-assisted refinement with a configured provider, accept/reject/regenerate/save-as-new-item, history restore, scoring, stage advancement, and application reload.
- [ ] 6.3 Run strict OpenSpec validation and confirm every basic-concept-tool scenario is covered by implementation or automated tests.
