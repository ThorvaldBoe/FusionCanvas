## 1. Tool Registration and Hosting

- [ ] 1.1 Register the Basic Design Tool as the default Design-stage tool through the Stage Tool Host registry, declaring Design-stage support and selected-item context requirement.
- [ ] 1.2 Show an empty state or creation path when no item is selected.
- [ ] 1.3 Receive explicit context (store, niche, topic path, item, stage, selected concept, inherited tags, metadata, AI capabilities) from the Stage Tool Host and refresh on context change.
- [ ] 1.4 Add view-model tests for registration, context reception, empty state, and coexistence with plugin tools.

## 2. Design Brief and Variant Workspace

- [ ] 2.1 Derive a design brief from the selected concept or sufficient listing-level idea/phrase/graphic, with a readiness note when neither is available.
- [ ] 2.2 Allow editing Design-stage notes and constraints without modifying the underlying concept unless explicitly chosen.
- [ ] 2.3 Add a variant workspace showing imported and generated variants with name, status, notes, source method, intended use, cleanup state, related assets, and tags.
- [ ] 2.4 Add view-model tests for brief derivation, insufficient-brief readiness, variant display, and metadata edits.

## 3. Import and Generation Flows

- [ ] 3.1 Implement manual import through asset-management that creates or updates a design variant with the manual source method.
- [ ] 3.2 Implement external-AI import that creates a variant with the external-AI source method and records prompt/source metadata through prompt-history when supplied.
- [ ] 3.3 Define an `IImageGenerationProvider` application port for in-app generation, documented as provisional until FC-0401/FC-0407 land.
- [ ] 3.4 Implement in-app generation that builds the full creative context, sends it to the provider, imports generated images as assets, creates draft variants with the in-app-AI source method, and records a prompt-history entry with provider/model and generation settings.
- [ ] 3.5 Disable in-app generation actions when no provider is configured, with a clear explanation; never overwrite final artwork without explicit approval.
- [ ] 3.6 Add application tests with a deterministic fake provider for manual import, external-AI import, in-app generation, no-provider disabled state, and provider failure recovery.

## 4. Final Selection, Cleanup, and Timeline

- [ ] 4.1 Implement promote-final and demote-final through the design-records service without deleting other variants; require at least one final variant before Listing advancement.
- [ ] 4.2 Define an `IImageProcessor` port for cleanup actions (crop, transparency inspection, transparent-border removal, upscale flag, replacement attachment, mark needs revision) with built-in defaults for the simplest actions and plugin extensibility.
- [ ] 4.3 Record cleanup outcomes as asset or design metadata atomically.
- [ ] 4.4 Feed important Design-stage events (imports, generations, prompt records, variant creation/rejection, cleanup actions, final promotion, final selection changes) into the Creative History Timeline in the same atomic snapshot.
- [ ] 4.5 Add view-model and UI tests for promote/demote, cleanup actions, timeline feeding, and metadata outcomes.

## 5. Stage Advancement and UI

- [ ] 5.1 Implement the proceed-to-Listing action through the workflow-stage-navigator, enforcing the at-least-one-final-variant invariant.
- [ ] 5.2 Add an Avalonia tool surface with context summary, stage tool selector, design brief panel, variant workspace, variant detail panel, cleanup actions, and activity area.
- [ ] 5.3 Apply compact action sizing, icon tooltips, keyboard-reachable actions, busy states, duplicate-submission prevention, and shared desktop control guidance.
- [ ] 5.4 Add UI automation tests for import, generation against a fake provider, variant management, final selection, cleanup, timeline feeding, stage advancement, and keyboard flow.

## 6. Verification

- [ ] 6.1 Run domain, application, integration, app, and UI automation test suites and resolve design-records, asset-management, asset-relationships, prompt-history, creative-history-timeline, stage-tool-host, context-aware-tools, concept-versions, and workflow-stage-navigator regressions.
- [ ] 6.2 Manually verify manual import, external-AI import, in-app generation with a configured provider, variant management, final selection, cleanup actions, timeline feeding, stage advancement, and application reload.
- [ ] 6.3 Run strict OpenSpec validation and confirm every basic-design-tool scenario is covered by implementation or automated tests.
