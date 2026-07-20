## 1. Tool Registration and Hosting

- [ ] 1.1 Register the Basic Listing Tool as the default Listing-stage tool through the Stage Tool Host registry, declaring Listing-stage support and selected-item context requirement.
- [ ] 1.2 Show an empty state or creation path when no item is selected.
- [ ] 1.3 Receive explicit context (store, niche, topic path, item, stage, selected concept, selected final design variants, inherited tags, metadata, available mockup product settings) from the Stage Tool Host and refresh on context change.
- [ ] 1.4 Add view-model tests for registration, context reception, empty state, and coexistence with plugin tools.

## 2. Mockup Generation Flow

- [ ] 2.1 Implement a placeholder mockup generation flow that records the final design image reference, template image reference, color variant (with optional color-specific template image), and placement parameters as the mockup's regeneration metadata. The actual flat compositing will be wired in at a later stage using an existing ImageSharp-based component.
- [ ] 2.2 For each color/template combination, store a placeholder output asset through asset-management and create a generated mockup record through the mockup-records service with the regeneration-metadata block.
- [ ] 2.3 Handle source design dimension mismatch by recording the mismatch in the mockup's metadata or warning the user before proceeding.
- [ ] 2.4 Batch generation with per-combination progress and failure reporting; persist only successful combinations on partial failure and retain recoverable input.
- [ ] 2.5 Add application and integration tests for placeholder generation against deterministic fake product settings and designs, dimension mismatch handling, partial failure, and regeneration-metadata correctness.

## 3. Manual Attachment, Metadata, and Readiness

- [ ] 3.1 Implement manual mockup attachment through the mockup-records service with the manual source flag and optional metadata.
- [ ] 3.2 Host the listing-metadata-editor surface (or section) for title, description, price, status, notes, marketplace notes, draft tags, and basic preparation fields.
- [ ] 3.3 Add a readiness checklist view model that reads final design, mockup product settings, mockups, metadata, price, and provider product/color names, advisory except for generation requirements.
- [ ] 3.4 Add view-model tests for manual attachment, metadata editing delegation, readiness checks, and advisory-versus-blocking behavior.

## 4. Publishing Refusal, Timeline, and UI

- [ ] 4.1 Refuse direct marketplace publishing; offer no publish action and explain that publishing is delegated to future plugins; store marketplace-specific values locally as draft preparation data.
- [ ] 4.2 Feed important Listing-stage events (generated mockups, attached mockups, metadata changes, status changes) into the Creative History Timeline in the same atomic snapshot.
- [ ] 4.3 Add an Avalonia tool surface with context summary, stage tool selector, selected final design panel, metadata editor section, mockup generation panel, mockup gallery, readiness checklist, and activity area.
- [ ] 4.4 Apply compact action sizing, icon tooltips, keyboard-reachable actions, busy states, duplicate-submission prevention, per-combination progress, and shared desktop control guidance.
- [ ] 4.5 Add UI automation tests for generation, manual attachment, metadata editing, readiness, publishing refusal, timeline feeding, and keyboard flow.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve mockup-records, mockup-product-settings, listing-metadata-editor, design-records, asset-management, creative-history-timeline, stage-tool-host, context-aware-tools, and workflow-stage-navigator regressions.
- [ ] 5.2 Manually verify mockup generation across color/template combinations, manual attachment, metadata editing, readiness checks, publishing refusal, timeline feeding, partial-failure recovery, and application reload.
- [ ] 5.3 Run strict OpenSpec validation and confirm every basic-listing-tool scenario is covered by implementation or automated tests.
