# Design: Group Selection Headless Coverage

## Approach

Keep the dialog behavior unchanged and exercise its compiled XAML bindings and code-behind click handlers from `FusionCanvas.App.Tests`. Add stable names to the name and destination controls solely to make the meaningful controls directly addressable in headless tests. Construct the dialog with deterministic `GroupDestination` values, show it in the Avalonia headless environment, pump layout, and interact with the actual `TextBox`, `ComboBox`, and `Button` controls.

The tests will verify:

1. The constructor exposes the supplied destinations, selects the default destination, and propagates edits through the two-way bindings.
2. Clicking Group with an empty name sets the existing validation message and leaves the window visible.
3. Providing a valid name and clicking Group closes the window.

## Test Isolation

The dialog has no workspace dependency. Tests use in-memory `GroupDestination` records with generated identifiers and never construct an application factory or persistence adapter.

## Decisions Not to Reopen

- No production behavior change is needed; the issue is missing framework-level coverage.
- The existing validation message and close semantics are the accepted behavior under test.
- A live desktop test is not required because the deterministic headless lane covers bindings, routed input, visibility, and close behavior.
