## 1. Shell Layout

- [ ] 1.1 Replace the current centered startup page in `FusionCanvas.App` with a main-window workspace shell.
- [ ] 1.2 Add a persistent shell header or command area that preserves FusionCanvas identity and reserves access to application-level commands.
- [ ] 1.3 Add a left navigation region that communicates where workspace navigation will appear without implementing interactive store, niche, group, or listing behavior.
- [ ] 1.4 Add a right document window region that remains visible when no workspace item is selected.

## 2. Document Window Structure

- [ ] 2.1 Add a tab area to the document window that reserves space for future document tabs without implementing full tab lifecycle behavior.
- [ ] 2.2 Add a workflow stage area that visibly reserves Idea, Concept, Design, and Listing positions without enabling workflow transitions.
- [ ] 2.3 Add a detail view host area that communicates the current empty or unavailable document state.

## 3. Shell States

- [ ] 3.1 Add shell/document empty-state presentation for no workspace or no selected content.
- [ ] 3.2 Add lightweight loading-state presentation for shell-owned content if a shell state model is introduced.
- [ ] 3.3 Add lightweight error/unavailable-state presentation that keeps the main shell frame usable if a shell state model is introduced.
- [ ] 3.4 Ensure placeholder text and disabled affordances do not imply that product pipeline, persistence, marketplace, plugin, AI, listing, mockup, or automation behavior exists.

## 4. Tests and Validation

- [ ] 4.1 Add `FusionCanvas.App.Tests` coverage for any introduced shell presentation model, state mapping, or layout decision logic.
- [ ] 4.2 Verify `dotnet build` succeeds for the solution.
- [ ] 4.3 Verify `dotnet test` succeeds for the solution.
- [ ] 4.4 Run OpenSpec validation/status for `fc-0001-application-shell` and confirm the change is apply-ready.
