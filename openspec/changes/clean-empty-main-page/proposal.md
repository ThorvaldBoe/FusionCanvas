## Why

The current startup window presents many placeholder controls and future workflow regions before the application has real workspace behavior. A cleaner empty launch state will make FusionCanvas feel calm, intentional, and ready without implying that unfinished features already exist.

## What Changes

- Simplify the initial main window to a clean, mostly empty starting surface.
- Remove placeholder navigation trees, tabs, workflow stage controls, loading cards, retry cards, and fake workspace content from startup.
- Keep a minimal FusionCanvas identity signal so the empty application does not feel broken.
- Preserve the existing Avalonia app startup and main window behavior.
- Avoid adding workspace creation, loading, persistence, product pipeline, integration, or automation behavior.

## Capabilities

### New Capabilities

### Modified Capabilities
- `desktop-application-foundation`: Update the initial workspace shell requirement so the application starts with a clean empty main page instead of dense placeholder workspace content.

## Impact

- Affects the Avalonia main window markup under `src/FusionCanvas.App`.
- May affect UI-focused tests or smoke tests if they inspect the main window.
- Does not affect domain, application, integration, persistence, plugin, AI, marketplace, listing, or mockup behavior.
