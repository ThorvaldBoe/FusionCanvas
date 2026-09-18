## 1. OpenSpec and layout

- [x] 1.1 Add the approved delta requirement and implementation-ready design for a bounded, vertically scrollable Mockup Template Color list.
- [x] 1.2 Wrap the Color choices in a bounded Avalonia `ScrollViewer` with automatic vertical scrolling and an automation identifier, without changing applicability bindings or action semantics.

## 2. Verification

- [x] 2.1 Add a focused Avalonia headless test covering a long Color list, vertical scrolling configuration, bounded layout, and reachable Save/Cancel actions.
- [x] 2.2 Run the focused App test project and the full `dotnet test .\\FusionCanvas.sln` baseline; resolve any regressions.
- [x] 2.3 Run `openspec validate` and complete `verification.md` with evidence for every acceptance scenario.
