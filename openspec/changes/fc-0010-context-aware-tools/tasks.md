## 1. Context Contract

- [ ] 1.1 Add application/domain model types for tool context, context scope, workflow stage, inherited values, nearby work summaries, and item-bound availability.
- [ ] 1.2 Add a request type for resolving context from explicit workspace, navigation selection, workflow stage, and optional scope override inputs.
- [ ] 1.3 Add a scope summary model that can describe item, topic, topic path, niche, store, subtree, and unsupported or unavailable scopes for UI display.

## 2. Context Resolution

- [ ] 2.1 Define an application-layer tool context resolver contract that does not depend on Avalonia or integration-layer implementations.
- [ ] 2.2 Implement context resolution for selected topic and selected item inputs using existing workspace relationships.
- [ ] 2.3 Resolve inherited tags and metadata separately from explicitly assigned values, preserving their source context where available.
- [ ] 2.4 Resolve bounded nearby work summaries for sibling topics, sibling items, active work, rejected work, and archived work where the workspace model can provide them.
- [ ] 2.5 Return clear unavailable results for item-bound tool requests when no selected item exists.

## 3. Creation and Scope Behavior

- [ ] 3.1 Add topic-scoped creation behavior or command input defaults that place new work under the selected topic.
- [ ] 3.2 Apply applicable inherited tags and metadata to new work by default while preserving room for explicit user overrides.
- [ ] 3.3 Support explicit scope override input and re-resolve context before running subsequent tool actions.
- [ ] 3.4 Ensure manual creation paths and future generative paths can consume the same resolved context contract.

## 4. UI Integration

- [ ] 4.1 Expose resolved context state from the relevant application/UI view model without requiring tools to scrape view state.
- [ ] 4.2 Display a compact tool scope summary for context-aware tool surfaces.
- [ ] 4.3 Represent unsupported or unavailable item-bound tool states clearly when only a topic or broader context is selected.
- [ ] 4.4 Wire supported scope changes to request a new resolved context before tool actions run.

## 5. Tests and Validation

- [ ] 5.1 Add unit tests for topic context resolution including store, niche, topic path, workflow stage, inherited values, and scope summary.
- [ ] 5.2 Add unit tests for item context resolution including selected item, parent topic path, workflow stage, inherited values, and item-bound availability.
- [ ] 5.3 Add unit tests for topic-default creation placement and inherited tag/metadata application.
- [ ] 5.4 Add unit tests for scope override behavior and context re-resolution.
- [ ] 5.5 Add unit tests for nearby work summary bounds and inclusion of rejected or archived work as non-active guidance.
- [ ] 5.6 Run the relevant project tests and the solution build to verify the change.
