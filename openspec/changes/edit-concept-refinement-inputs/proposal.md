## Why

The Refine with AI panel currently renders each design-triangle value as a small, ellipsized label, which prevents creators from reviewing the full text and makes it impossible to adjust the exact text used by a refinement action. This change makes the panel's working context legible and directly editable while preserving the existing persistence, history, and AI-result behavior.

## What Changes

- Replace the truncated Concept idea, Phrase, and Graphic display labels in the Refine with AI panel with full-width editable text fields that wrap and provide enough height to read their complete values.
- Maintain a presentation-local working value for each refinement field, initialized from and synchronized with the Item inspector's current Concept drafts.
- Make Fine tune and Change capture the three local working values at activation time, so the targeted field's edited text and the other visible working values form the request's current triangle.
- Keep local typing non-persistent: it does not auto-save, change completeness, or add history until an AI result is successfully applied through the existing Item inspector draft/commit path.
- Preserve local edits after AI failure or cancellation; synchronize the local target value when AI application, rollback, manual inspector editing, or Item-session reset changes an inspector draft.
- Preserve AI availability, busy, read-only, initialization, cancellation, history, rollback, normalization, and error behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `concept-refinement`: The per-corner refinement surface exposes readable editable working values, and Fine tune/Change use those visible values as the current triangle supplied to AI.

## Delivery Module Boundaries

This module has one outcome: creators can read and adjust the exact triangle context used by per-corner AI refinement. It includes the three local editors, synchronization rules, request capture, accessibility, and focused verification because those pieces share one view, one session view model, and one action boundary.

Dependencies are limited to the existing Concept refinement session view model, Item inspector drafts, refinement service contract, and Avalonia Concept surface. Non-goals are adding a second persistence path, saving local typing, changing prompt/response formats, changing score semantics, redesigning history, or altering other stage tools.

The primary workflow is frequent, in-context creative iteration inside the main Concept workspace, so the editors remain inline beside their Fine tune and Change actions. The panel may grow vertically enough to show wrapped content; it remains within the existing scrollable stage surface. Empty, unavailable, busy, read-only, failure, cancellation, and success states continue to use existing behavior.

## Impact

- `FusionCanvas.App`: `ConceptRefinementSessionViewModel` local presentation state and action capture; `MainWindow.axaml` control/layout changes.
- `FusionCanvas.App.Tests`: framework-free state/payload tests and Avalonia headless view tests for readable editable controls and bindings.
- No Domain, Application service contract, Integration, database, file format, dependency, or migration changes.
- Main risks are stale synchronization, accidental persistence/history from local typing, losing local edits on failure, and command enablement not following local input; each receives focused regression coverage.
