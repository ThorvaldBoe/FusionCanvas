## 1. Information Box Presentation

- [x] 1.1 Add a static inline SLL information box immediately below the SLL heading in `src/FusionCanvas.App/Views/MainWindow.axaml`, including an info icon, the exact approved copy, and shared theme background, border, and text resources.
- [x] 1.2 Give the information box one meaningful accessible description, keep the icon and container non-focusable and non-clickable, and bind visibility only to the existing Concept SLL section visibility state.
- [x] 1.3 Verify that SLL unavailable, incomplete, busy, stale, error, generate, regenerate, reset, and keep states retain their current behavior and ordering around the new box.

## 2. Deterministic UI Verification

- [x] 2.1 Extend `tests/FusionCanvas.App.Tests/SllSectionHeadlessTests.cs` to verify the box, icon, approved copy, shared theme surface, and existing action controls during normal editable Concept work.
- [x] 2.2 Add read-only, incomplete, unavailable, and stale-state coverage proving the box remains visible while existing action gating and guidance remain unchanged; verify keyboard focus does not stop on the static explanation.
- [x] 2.3 Correct the proposal, design, spec delta, or task breakdown if verification exposes artifact drift, while keeping the approved copy and presentation-only scope fixed.

## 3. Completion Gates

- [x] 3.1 Map every delta-spec scenario to named headless or unit evidence in `verification.md`, and record why no Application, Integration, persistence, AI, or serialization tests are needed for this presentation-only change.
- [x] 3.2 Run `openspec validate explain-sll --strict` and resolve all validation findings.
- [x] 3.3 Run `dotnet test .\\FusionCanvas.sln -m:1` and complete a changed-scope UI, accessibility, architecture, and security drift review.
