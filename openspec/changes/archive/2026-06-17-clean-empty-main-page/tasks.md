## 1. Main Window Cleanup

- [x] 1.1 Replace dense `MainWindow.axaml` placeholder layout with a clean, mostly empty startup page.
- [x] 1.2 Keep a minimal FusionCanvas identity signal on the empty page.
- [x] 1.3 Remove placeholder navigation tree, tabs, workflow stage controls, state cards, loading indicators, retry actions, and fake workspace content.
- [x] 1.4 Keep the existing `MainWindow` startup path and avoid adding view models or non-UI behavior.

## 2. Behavior Guardrails

- [x] 2.1 Confirm the startup page does not imply workspace navigation, product pipeline, listing, mockup, plugin, AI, marketplace, persistence, or automation behavior is available.
- [x] 2.2 Confirm the change stays inside the UI layer unless tests require small updates.

## 3. Validation

- [x] 3.1 Run `dotnet build FusionCanvas.sln`.
- [x] 3.2 Run `dotnet test FusionCanvas.sln`.
- [x] 3.3 Run OpenSpec status/apply checks for `clean-empty-main-page`.
