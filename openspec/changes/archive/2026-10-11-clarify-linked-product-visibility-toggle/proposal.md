## Why

The Printify import dialog currently changes whether linked products appear but leaves its control label unchanged. A creator must infer both the current view and the next action, which is confusing and does not clearly expose the state to assistive technology.

## What Changes

- Give the linked-product visibility control an explicit state-bound action label and accessible name: “Show linked products” while hidden and “Hide linked products” while visible.
- Preserve the existing default-hidden behavior and linked-product import restrictions.
- Verify state, list contents, and accessible name before and after toggling in a headless Avalonia view test.

## Capabilities

### New Capabilities

- `printify-listing-import`: Clarify the linked-product visibility action in the Printify listing import dialog.

### Modified Capabilities

None.

## Impact

This is a focused clarification of the existing Printify listing import workflow. It affects the import dialog view model and XAML plus its headless UI tests. The action remains in the existing focused import dialog and is used occasionally during product review. No API, persistence, dependency, or workspace-layout changes are needed. No high-impact decisions remain unresolved.
