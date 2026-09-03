# Design

The pure `NavigationDocumentContext` projection is extracted to `NavigationContextFactory`. The view model remains the coordinator and calls the factory when its workspace snapshot or selected store changes. The factory retains the existing recursive traversal and workflow-state calculations byte-for-byte in intent.

## Implementation plan

1. Add the focused factory with the existing projection logic.
2. Replace the view-model call site and remove the extracted methods.
3. Run focused App tests, build, and strict validation.
