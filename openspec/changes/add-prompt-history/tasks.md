## 1. Prompt Domain Entity

- [ ] 1.1 Extend the `Prompt` domain entity with input text, output text or output reference, prompt type enum (idea, phrase, graphic, image, listing-text, critique, other), optional provider and model strings, optional generation-settings metadata, lifecycle marker (active, superseded, rejected), and timestamps.
- [ ] 1.2 Add prompt context links to store, niche, listing, concept, design, or asset, validated against the active workspace and the prompt's store.
- [ ] 1.3 Add a `reusedFrom` reference for reuse-derived prompts.
- [ ] 1.4 Add domain tests for save, multi-association, type changes, lifecycle markers, reuse references, and zero-prompt contexts.

## 2. Prompt History Application Layer

- [ ] 2.1 Define `IPromptHistoryService` requests and results for save, associate, change type, mark lifecycle, reuse, copy, and remove, with recoverable error reporting.
- [ ] 2.2 Implement save with input, output or output reference, type, provider/model, and generation-settings metadata, rejecting any attempt to store API keys or credentials.
- [ ] 2.3 Implement association add/remove with cross-store rejection and multi-association preservation.
- [ ] 2.4 Implement lifecycle markers (active, superseded, rejected) with preservation and default active-only filtering plus an explicit superseded/rejected filter.
- [ ] 2.5 Implement reuse with `reusedFrom` reference and copy-to-clipboard without mutating the source.
- [ ] 2.6 Add application tests for save, association, type changes, lifecycle, reuse, copy, and repository-failure atomicity.

## 3. Persistence and Migration

- [ ] 3.1 Add a forward-only schema migration that creates the prompt table, prompt context links, and `reusedFrom` reference, and increments the snapshot version.
- [ ] 3.2 Migrate existing snapshots by assigning zero prompts without data backfill.
- [ ] 3.3 Add SQLite integration tests for save, associate, type change, lifecycle, reuse, complete reload, and migration of pre-existing snapshots.
- [ ] 3.4 Verify failed or cancelled prompt operations persist no partial prompts, associations, lifecycle changes, or metadata changes.

## 4. Prompt Surface and Cross-Capability Integration

- [ ] 4.1 Add a focused prompt surface for store, niche, listing, concept, design, and asset contexts with type, provider, model, lifecycle marker, empty states, and read-only detail with reuse/copy actions.
- [ ] 4.2 Add a store-level prompt view listing every store-owned prompt with associated contexts or an unlinked indicator.
- [ ] 4.3 Treat prompts as dependent creative records in the listing, concept, and design permanent-deletion guards and block connected record deletion with actionable guidance.
- [ ] 4.4 Treat prompt content as untrusted input in any future AI-facing read path and document the prompt-injection guard.
- [ ] 4.5 Add view-model and UI tests for context prompt views, lifecycle filtering, reuse/copy, deletion blocking, rollback, and shared desktop control guidance.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve core-domain-model, asset-management, concept-versions, design-records, and listing-management regressions.
- [ ] 5.2 Manually verify save, associate, type change, reject/supersede, filter, reuse, copy, per-context and store-level views, deletion-guard blocking, and application reload.
- [ ] 5.3 Run strict OpenSpec validation and confirm every prompt-history and modified core-domain-model scenario is covered by implementation or automated tests.
