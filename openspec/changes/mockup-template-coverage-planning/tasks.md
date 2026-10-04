## 1. Domain coverage model

- [x] 1.1 Add framework-independent coverage value types for grouping strategy, requirement status, affected Variant identity, applicability values, and matched source-image IDs.
- [x] 1.2 Implement the coverage planner against `MockupTemplateSourcePolicy.Resolve`, preserving one result per compatible Variant and distinguishing resolved, missing, ambiguous, and incomplete states.
- [x] 1.3 Implement Color-first grouping with unrestricted secondary options, explicit finer-grained fallback, and creator-facing grouping explanations.
- [x] 1.4 Add stale-plan/context checks for target Design Area, active Variants, and Option Values without mutating confirmed source-image configuration.

## 2. Application coverage and assignment services

- [x] 2.1 Extend the existing source-image application boundary with a read/plan operation that returns derived requirements without creating empty source-image or Asset records.
- [ ] 2.2 Add requirement assignment commands for upload and existing managed-image selection, reusing current ownership, applicability, managed-file, mapping, and revision services.
- [ ] 2.3 Implement exemplar-derived applicability defaults and dimension-safe mapping reuse; reject or surface incompatible mappings as explicit incomplete state.
- [ ] 2.4 Add application tests for plan generation, grouping, assignment prefill, exact-one ambiguity, archived/incomplete rows, stale context, revision preservation, and the no-placeholder invariant.

## 3. Store editor coverage workflow

- [ ] 3.1 Add focused-editor coverage state and view-model projection for loading, no target Design Area, complete, missing, ambiguous, incomplete, stale, archived/read-only, and recoverable-error states.
- [ ] 3.2 Add grouping selection, Generate coverage plan, explicit refresh, and requirement selection commands while preserving existing draft save/discard/cancel behavior.
- [ ] 3.3 Add Add mockup image and Assign existing image actions with Template context and requirement applicability prefilled; retain the selected-row mapping editor.
- [ ] 3.4 Add exemplar selection and confirmation feedback for applicability and mapping defaults, including clear correction guidance for incompatible dimensions.
- [ ] 3.5 Add coverage-panel bindings and presentation states with progressive disclosure, accessible names/help text, keyboard traversal, focus placement/return, and supported narrow sizing.
- [ ] 3.6 Add view-model tests and deterministic Avalonia headless tests for coverage states, commands, draft preservation, stale refresh, bindings, routed actions, accessibility, focus behavior, and narrow layout.

## 4. Listing diagnostics and bulk-upload seam

- [x] 4.1 Extend the Listing diagnostic result and translator/view model with authoritative resolved/missing counts and grouped affected Variant guidance for source-resolution blockers.
- [x] 4.2 Render the Listing coverage summary as ordinary accessible text and controls while keeping repair actions in Store settings and ready-only eligibility unchanged.
- [ ] 4.3 Define the application-facing coverage requirement/result contract that the separate bulk-upload workflow can consume without introducing a second compatibility calculation.
- [ ] 4.4 Add Listing application and headless binding tests for configured Draft templates, affected Variant guidance, ready-template transition, unavailable diagnostics, and presentation-only behavior.

## 5. Verification and artifact reconciliation

- [ ] 5.1 Add or update any focused tests needed to cover every acceptance scenario in both delta specs, including partial saves and persisted reload behavior.
- [ ] 5.2 If implementation decisions alter scope or behavior, reconcile proposal, delta specs, design, and this task list before final validation; do not leave contradictory artifacts.
- [x] 5.3 Run `openspec validate --strict` and resolve all validation findings for this change.
- [ ] 5.4 Run `dotnet test .\\FusionCanvas.sln -m:1` and record criterion-level evidence for each acceptance scenario, including any supplemental real-desktop scenario-pack result.


