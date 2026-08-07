## 1. Concept surface composition

- [x] 1.1 Replace the upper Concept idea, Phrase, and Graphic direction editors in `MainWindow.axaml` with a read-only Base idea field bound to the original Idea value.
- [x] 1.2 Remove the `Refine with AI` heading and separate framing while preserving Initialize, the three refinement rows, Instructions fields, completeness score, history, and SLL ordering.
- [x] 1.3 Preserve accessible names, keyboard order, read-only review state, disabled-action guidance, and the distinction between Base idea and the three editable triangle values.

## 2. Manual refinement editing behavior

- [x] 2.1 Add a single controlled commit path for copying pending Concept idea, Phrase, and Graphic direction working values into `ItemInspectorViewModel` and invoking the existing automatic-save drain.
- [x] 2.2 Commit the edited corner on field exit without persisting instruction text or changing unrelated triangle values.
- [x] 2.3 Flush pending refinement working values before Item, tab, stage, lifecycle, or document-close transitions, preserving drafts and inline errors when validation or persistence fails.
- [x] 2.4 Verify that AI-unavailable users can manually edit and persist all three triangle fields while Initialize, Fine tune, and Change remain visible but disabled.
- [x] 2.5 Verify that AI application and history rollback still use the existing inspector draft and automatic-save paths without duplicate manual history entries.

## 3. Focused tests

- [x] 3.1 Update `ConceptRefinementViewTests` for the unified surface, read-only Base idea, absence of duplicate upper fields, absence of the `Refine with AI` heading, and retained refinement controls.
- [x] 3.2 Add or update session/view-model tests for manual field-exit commits, AI-unavailable manual editing, pending-edit transition flushing, and commit-failure draft preservation.
- [x] 3.3 Add or update history tests proving that selecting a history entry restores and persists the selected Concept idea, Phrase, and Graphic direction without appending a duplicate history entry.
- [x] 3.4 Run the relevant Avalonia headless and Concept refinement test projects and resolve regressions in accessibility, read-only, score, instruction, and history behavior.

## 4. Specification and completion verification

- [x] 4.1 Review the implementation against every acceptance scenario in `specs/concept-refinement/spec.md` and record criterion-level evidence in the completion workflow.
- [x] 4.2 Run strict OpenSpec validation with `openspec validate` and correct any delta-spec or artifact-format issues.
- [ ] 4.3 Run the required solution baseline `dotnet test .\FusionCanvas.sln`.
