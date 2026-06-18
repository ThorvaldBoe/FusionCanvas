## Why

FusionCanvas needs a consistent way to show the right creator tool for the active Idea, Concept, Design, or Listing stage without hard-coding each future workflow surface into the document window. FC-0011 is the final Phase 0 foundation slice that makes later built-in and plugin-provided tools possible while preserving the current navigation context.

## What Changes

- Add a Stage Tool Host capability for the lower detail area of the document window.
- Define a stage tool registration model that supports built-in tools first and plugin-provided tools later through the same application-facing contract where practical.
- Provide active workflow stage and navigation context to the selected tool instead of requiring tools to scrape UI state.
- List tools available for the current stage and context, including whether each tool can run from topic context or requires a selected item.
- Add a compact tool selector when multiple tools are available.
- Ensure the default built-in tool remains available when plugin tools are missing, disabled, or fail to load.
- Keep durable changes routed through application services and commands rather than direct storage writes from hosted tools.

## Capabilities

### New Capabilities

- `stage-tool-host`: Hosts stage-specific tools in the document window, resolves available tools from workflow and navigation context, and exposes stable context to hosted tool implementations.

### Modified Capabilities

- None.

## Impact

- Affected UI: document window lower detail area, tool selector, empty and unavailable tool states.
- Affected application layer: stage tool descriptors, registration/query services, active tool selection, tool context construction, and command/service boundaries used by tools.
- Affected domain model: workflow stage and entity context concepts may be referenced by contracts but should not depend on UI or plugin implementation details.
- Affected future systems: built-in Idea, Concept, Design, Listing tools and later plugin registration can share the same host model.
- Tests: application tests for tool resolution and context construction, plus UI decision tests for selector visibility, fallback behavior, and item-required states.
