# Proposal: Cover Group Selection Dialog Behavior

## Why

The full QA review identified `GroupSelectionWindow` as a user-facing dialog whose bindings and validation behavior were not protected by a focused Avalonia headless test. A regression in the dialog could therefore pass view-model tests while breaking the actual controls or routed click behavior.

## What Changes

- Add focused headless tests for the group-selection dialog's initial bindings and destination selection.
- Add headless coverage for invalid confirmation validation and successful confirmation through the rendered buttons.
- Record the coverage expectation in the testing baseline.

## Scope

In scope: `GroupSelectionWindow`, its existing `GroupSelectionViewModel` bindings, and the dialog's existing confirm validation path.

Out of scope: changing dialog behavior, changing grouping application logic, live desktop automation, or redesigning the dialog.

## Verification

- Run the focused `FusionCanvas.App.Tests` filter for the new headless test class.
- Run `dotnet test .\FusionCanvas.sln` as the deterministic solution baseline.
- Run strict OpenSpec validation and retain criterion-level evidence in `verification.md`.
