## 1. Application Contracts

- [x] 1.1 Add workflow stage, entity context kind, and stage tool availability types in `FusionCanvas.Application`.
- [x] 1.2 Add stage tool descriptor and registry contracts that support built-in and future contributed tools.
- [x] 1.3 Add a stage tool context snapshot model that carries active stage, store, topic path, selected topic, selected item, inherited metadata hooks, nearby work hooks, user settings hooks, and provider capability hooks as available.
- [x] 1.4 Add an application-layer host service that evaluates registered tools for the active context and returns available, unavailable, disabled, and failed states.
- [x] 1.5 Add active tool selection logic scoped by workflow stage and context kind, including fallback to the default available tool.

## 2. Built-In Tool Registration

- [x] 2.1 Register built-in default stage tool descriptors for the initial Idea, Concept, Design, and Listing stages.
- [x] 2.2 Mark item-bound built-in tools as requiring selected item context where appropriate.
- [x] 2.3 Ensure built-in descriptors are discoverable when no plugin or contributed tools exist.
- [x] 2.4 Keep built-in tools routed through the same registry/query service used for contributed tools.

## 3. UI Host Integration

- [x] 3.1 Add a Stage Tool Host view model or UI state model in `FusionCanvas.App` that consumes the application-layer host service.
- [x] 3.2 Add the lower document-window detail host area to the main document surface without implying unavailable workflow features.
- [x] 3.3 Render the selected tool's display surface or honest default placeholder for the current stage.
- [x] 3.4 Render a compact tool selector in a consistent top-right location when multiple tools are available.
- [x] 3.5 Hide or minimize the selector when exactly one tool is available.
- [x] 3.6 Render an item-required or unavailable state when the active context does not satisfy a selected tool's requirements.
- [x] 3.7 Update host state when the active tab, navigation selection, selected item, or workflow stage changes.

## 4. Tool Failure and Durable Change Boundaries

- [x] 4.1 Represent contributed tool load failures as availability states that do not break the document window.
- [x] 4.2 Verify the default built-in tool remains selectable when a contributed tool is missing, disabled, unavailable, or failed.
- [x] 4.3 Expose application-layer command or service boundaries that hosted tools must use for durable workspace changes.
- [x] 4.4 Ensure hosted tool contracts do not require direct persistence, workspace storage, or UI control access.

## 5. Tests and Verification

- [x] 5.1 Add application tests for tool availability filtering by workflow stage.
- [x] 5.2 Add application tests for topic-context tools versus item-required tools.
- [x] 5.3 Add application tests for selected-tool fallback when the prior selection is invalid.
- [x] 5.4 Add application tests proving built-in default tools remain discoverable without contributed tools.
- [x] 5.5 Add app-layer tests for selector visibility and unavailable-state decisions where practical.
- [x] 5.6 Run `dotnet test` for `FusionCanvas.sln` and resolve failures.
- [x] 5.7 Run `openspec status --change "stage-tool-host"` and confirm the change is apply-ready.
