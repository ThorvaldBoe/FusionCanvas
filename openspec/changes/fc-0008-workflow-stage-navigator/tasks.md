## 1. Domain and Application Model

- [ ] 1.1 Add or reuse a workflow stage type for `Idea`, `Concept`, `Design`, and `Listing` outside Avalonia UI code.
- [ ] 1.2 Add a navigator stage state model that includes stage key, label, current flag, availability flag, and navigation command eligibility.
- [ ] 1.3 Add an active item workflow context model that can describe current stage, available stages, and archive or inactive state.
- [ ] 1.4 Implement stage-state derivation so the navigator produces ordered `Idea`, `Concept`, `Design`, and `Listing` entries for an active item.

## 2. UI Integration

- [ ] 2.1 Add the workflow stage navigator surface to the document window area without showing fake workflow progress when no item is active.
- [ ] 2.2 Bind visual states for current, available, and unavailable stages.
- [ ] 2.3 Wire available stage selection to open the selected stage view for the same active item.
- [ ] 2.4 Ensure unavailable stage selection does not navigate or change the active stage view.
- [ ] 2.5 Expose archive or inactive state separately from the primary four-stage row when applicable.

## 3. Active Context Synchronization

- [ ] 3.1 Update navigator state when the active item changes.
- [ ] 3.2 Update navigator state when the active document tab changes.
- [ ] 3.3 Update navigator state when the active item's workflow stage changes.

## 4. Tests and Validation

- [ ] 4.1 Add tests for ordered stage generation and current-stage marking.
- [ ] 4.2 Add tests for available and unavailable stage navigation decisions.
- [ ] 4.3 Add tests for no-active-item and archived-item navigator state.
- [ ] 4.4 Add UI-owned decision logic tests or view model tests for active tab and active item synchronization.
- [ ] 4.5 Run the relevant unit test project or solution test command and confirm the change remains buildable.
