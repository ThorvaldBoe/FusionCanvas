# Verification

## Acceptance evidence

- `CompositionRootTests` passes: the two main-window presentation files contain no Integration namespace reference, and the App factories contain the concrete adapter construction.
- The App services startup test remains independent of workspace database startup; workspace creation is lazy through `AppWorkspaceFactory`.
- The solution builds with 0 errors using the serial build command.
- Strict OpenSpec validation passes for specs and changes.
